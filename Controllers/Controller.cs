using System.Globalization;
using System.Text;
using Learn.Stream.Models;
using Learn.Stream.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;

namespace Learn.Stream.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoStreamController(
    IContentTypeProvider contentTypeProvider,
    ILogger<VideoStreamController> logger,
    IMemoryCache cache,
    IConfiguration configuration,
    VideoAnalyticsService analytics,
    AdaptiveBitrateManager abrManager,
    CdnManager cdnManager,
    SecurityManager security,
    VideoMetadataService metadata)
    : ControllerBase
{
    private readonly IContentTypeProvider _contentTypeProvider = contentTypeProvider;
    private readonly IConfiguration _configuration = configuration;
    private readonly string _videoPath = configuration["VideoPath"] ?? "wwwroot/videos";
    private readonly string _manifestPath = configuration["ManifestPath"] ?? "wwwroot/manifests";

    #region Adaptive Streaming (HLS/DASH-like)

    /// <summary>
    /// Master playlist for adaptive streaming (similar to YouTube's adaptive formats)
    /// </summary>
    [HttpGet("manifest/{videoId}.m3u8")]
    public async Task<IActionResult> GetMasterPlaylist(
        string videoId,
        [FromQuery] string? preferredCodec = "h264",
        [FromQuery] bool autoQuality = true,
        [FromQuery] string? userAgent = null)
    {
        try
        {
            // Security validation
            var clientInfo = await security.ValidateClientAsync(Request, videoId);
            if (!clientInfo.IsValid)
            {
                return Unauthorized("Invalid client or token");
            }

            // Get video metadata
            var videoMeta = await metadata.GetVideoMetadataAsync(videoId);
            if (videoMeta == null)
            {
                return NotFound($"Video {videoId} not found");
            }

            // Analyze client capabilities
            var deviceCapabilities = AdaptiveBitrateManager.AnalyzeDeviceCapabilities(userAgent, Request.Headers);

            // Generate adaptive playlist
            var playlist =
                await GenerateMasterPlaylistAsync(videoId, videoMeta, deviceCapabilities, preferredCodec, autoQuality);

            // Set appropriate headers
            Response.ContentType = "application/vnd.apple.mpegurl";
            Response.Headers.Append("Cache-Control", "max-age=300"); // 5 minutes cache
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            // Log analytics
            await analytics.LogPlaylistRequestAsync(videoId, clientInfo, deviceCapabilities);

            return Content(playlist);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating master playlist for video: {VideoId}", videoId);
            return StatusCode(500, "Failed to generate playlist");
        }
    }

    /// <summary>
    /// Segment playlist for specific quality (similar to YouTube's itag system)
    /// </summary>
    [HttpGet("playlist/{videoId}/{quality}.m3u8")]
    public async Task<IActionResult> GetSegmentPlaylist(
        string videoId,
        string quality,
        [FromQuery] long? startTime = null,
        [FromQuery] int segmentDuration = 10)
    {
        try
        {
            var cacheKey = $"playlist_{videoId}_{quality}_{startTime}_{segmentDuration}";

            if (cache.TryGetValue(cacheKey, out string cachedPlaylist))
            {
                Response.ContentType = "application/vnd.apple.mpegurl";
                return Content(cachedPlaylist);
            }

            var videoMeta = await metadata.GetVideoMetadataAsync(videoId);
            if (videoMeta == null) return NotFound();

            var qualityInfo = videoMeta.Qualities.FirstOrDefault(q => q.Label == quality);
            if (qualityInfo == null) return NotFound($"Quality {quality} not available");

            var playlist = await GenerateSegmentPlaylistAsync(videoId, qualityInfo, startTime, segmentDuration);

            // Cache playlist for 5 minutes
            cache.Set(cacheKey, playlist, TimeSpan.FromMinutes(5));

            Response.ContentType = "application/vnd.apple.mpegurl";
            Response.Headers.Append("Cache-Control", "max-age=300");

            return Content(playlist);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating segment playlist: {VideoId}/{Quality}", videoId, quality);
            return StatusCode(500);
        }
    }

    #endregion

    #region Advanced Segment Streaming

    /// <summary>
    /// Stream video segments with advanced buffering and preloading
    /// </summary>
    [HttpGet("segment/{videoId}/{quality}/{segmentIndex}.ts")]
    public async Task<IActionResult> StreamSegment(
        string videoId,
        string quality,
        int segmentIndex,
        [FromQuery] string? token = null,
        [FromQuery] bool preload = false)
    {
        try
        {
            // Validate access token
            if (!await security.ValidateSegmentAccessAsync(videoId, token, segmentIndex))
            {
                return Unauthorized("Invalid or expired token");
            }

            // Get optimal CDN endpoint
            var cdnEndpoint = await cdnManager.GetOptimalEndpointAsync(Request, videoId, quality);

            // Check if segment exists in cache or CDN
            var segmentPath = Path.Combine(_videoPath, "segments", videoId, quality, $"segment_{segmentIndex:D6}.ts");

            if (!System.IO.File.Exists(segmentPath))
            {
                // Try to fetch from CDN or generate on-the-fly
                segmentPath = await cdnManager.FetchOrGenerateSegmentAsync(videoId, quality, segmentIndex);
                if (segmentPath == null)
                {
                    return NotFound($"Segment {segmentIndex} not available");
                }
            }

            var fileInfo = new FileInfo(segmentPath);

            // Set optimized headers for video segments
            Response.ContentType = "video/mp2t"; // MPEG-TS format
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", fileInfo.Length.ToString());
            Response.Headers.Append("Cache-Control", "public, max-age=86400, immutable"); // 24h cache for segments
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Handle range requests for segment seeking
            var rangeHeader = Request.Headers.Range.FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                return await HandleSegmentRangeRequest(segmentPath, fileInfo, rangeHeader);
            }

            // Log segment access for analytics
            if (!preload)
            {
                await analytics.LogSegmentAccessAsync(videoId, quality, segmentIndex, Request);
            }

            // Stream the segment
            await using var fileStream = new FileStream(segmentPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await fileStream.CopyToAsync(Response.Body, HttpContext.RequestAborted);

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming segment: {VideoId}/{Quality}/{Index}", videoId, quality,
                segmentIndex);
            return StatusCode(500);
        }
    }

    #endregion

    #region Quality-Based Streaming (YouTube's itag system)

    /// <summary>
    /// Stream video by format tag (similar to YouTube's itag parameter)
    /// </summary>
    [HttpGet("watch")]
    public async Task<IActionResult> WatchVideo(
        [FromQuery] string v, // Video ID
        [FromQuery] int? itag = null, // Format tag
        [FromQuery] string? mime = null,
        [FromQuery] long? range = null,
        [FromQuery] string? sq = null, // Sequence number for live streams
        [FromQuery] bool? requiressl = true)
    {
        try
        {
            if (string.IsNullOrEmpty(v))
            {
                return BadRequest("Video ID (v) parameter is required");
            }

            // Get video metadata and available formats
            var videoMeta = await metadata.GetVideoMetadataAsync(v);
            if (videoMeta == null)
            {
                return NotFound($"Video {v} not found or unavailable");
            }

            // Determine format based on itag or mime type
            var format = DetermineOptimalFormat(videoMeta, itag, mime, Request.Headers);
            if (format == null)
            {
                return BadRequest("No suitable format found");
            }

            // Handle live streaming
            if (videoMeta.IsLive && !string.IsNullOrEmpty(sq))
            {
                return await HandleLiveStreamSegment(v, format, sq);
            }

            // Security and throttling checks
            var accessResult = await security.CheckVideoAccessAsync(v, Request, format);
            if (!accessResult.Allowed)
            {
                return StatusCode(accessResult.StatusCode, accessResult.Message);
            }

            // Apply bandwidth throttling if needed
            var throttleInfo = await analytics.GetThrottleInfoAsync(Request, v);

            // Get video file path
            var filePath = GetFormatFilePath(v, format);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Video format not available");
            }

            var fileInfo = new FileInfo(filePath);

            // Set video-specific headers
            Response.ContentType = format.MimeType;
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", fileInfo.Length.ToString());
            Response.Headers.Append("X-Content-Duration", videoMeta.Duration.ToString());
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            // Handle range requests with advanced seeking
            if (range.HasValue || !string.IsNullOrEmpty(Request.Headers.Range.FirstOrDefault()))
            {
                return await HandleAdvancedRangeRequest(filePath, fileInfo, format, range, throttleInfo);
            }

            // Log video start for analytics
            await analytics.LogVideoStartAsync(v, format, Request, accessResult.UserId);

            // Stream full video with throttling
            return await StreamWithThrottling(filePath, fileInfo, throttleInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in watch endpoint: {VideoId}", v);
            return StatusCode(500, "Playback error");
        }
    }

    #endregion

    #region Advanced Analytics and Quality Switching

    /// <summary>
    /// Get real-time quality recommendations based on network conditions
    /// </summary>
    [HttpPost("quality/recommend")]
    public async Task<IActionResult> RecommendQuality([FromBody] QualityRecommendationRequest request)
    {
        try
        {
            var recommendations = await AdaptiveBitrateManager.GetQualityRecommendationsAsync(request);

            // Log the recommendation request for ML training
            await analytics.LogQualityRecommendationAsync(request, recommendations);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating quality recommendations");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Report playback statistics for adaptive algorithm improvement
    /// </summary>
    [HttpPost("analytics/playback")]
    public async Task<IActionResult> ReportPlaybackStats([FromBody] PlaybackStatsRequest? stats)
    {
        try
        {
            // Validate the stats data
            if (stats == null || string.IsNullOrEmpty(stats.VideoId))
            {
                return BadRequest("Invalid stats data");
            }

            // Process and store analytics
            await analytics.ProcessPlaybackStatsAsync(stats, Request);

            // Update ABR algorithm with new data
            await abrManager.UpdateAlgorithmAsync(stats);

            return Ok(new { status = "success", message = "Stats recorded" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing playback stats");
            return StatusCode(500);
        }
    }

    #endregion

    #region Live Streaming Support

    /// <summary>
    /// Handle live stream chunks (similar to YouTube Live)
    /// </summary>
    [HttpGet("live/{streamId}/chunk")]
    public async Task<IActionResult> GetLiveChunk(
        string streamId,
        [FromQuery] long seq, // Sequence number
        [FromQuery] string? quality = "720p")
    {
        try
        {
            var liveStream = await metadata.GetLiveStreamAsync(streamId);
            if (liveStream == null || !liveStream.IsActive)
            {
                return NotFound("Live stream not found or inactive");
            }

            // Check if client is caught up or needs to buffer
            var latency = await CalculateStreamLatency(streamId, seq);
            if (latency > TimeSpan.FromSeconds(30))
            {
                // Client is too far behind, redirect to catch-up segment
                var catchupSeq = await GetCatchupSequence(streamId);
                return RedirectToAction(nameof(GetLiveChunk), new { streamId, seq = catchupSeq, quality });
            }

            var chunkPath = Path.Combine(_videoPath, "live", streamId, quality, $"chunk_{seq:D10}.ts");

            // Wait for chunk if it's not ready yet (live edge)
            if (!System.IO.File.Exists(chunkPath))
            {
                var timeout = DateTime.UtcNow.AddSeconds(10);
                while (!System.IO.File.Exists(chunkPath) && DateTime.UtcNow < timeout)
                {
                    await Task.Delay(100);
                    if (HttpContext.RequestAborted.IsCancellationRequested)
                        return new EmptyResult();
                }

                if (!System.IO.File.Exists(chunkPath))
                {
                    return NotFound("Chunk not available");
                }
            }

            var fileInfo = new FileInfo(chunkPath);

            Response.ContentType = "video/mp2t";
            Response.Headers.CacheControl = "no-cache"; // Live content shouldn't be cached
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("X-Sequence-Number", seq.ToString());
            Response.Headers.Append("X-Stream-Latency", latency.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));

            await using var fileStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await fileStream.CopyToAsync(Response.Body, HttpContext.RequestAborted);

            // Log live viewing analytics
            await analytics.LogLiveChunkAccessAsync(streamId, seq, quality!, Request);

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error serving live chunk: {StreamId}/{Seq}", streamId, seq);
            return StatusCode(500);
        }
    }

    #endregion

    #region Advanced Preloading and Buffering

    /// <summary>
    /// Intelligent preloading based on viewing patterns
    /// </summary>
    [HttpPost("preload/segments")]
    public async Task<IActionResult> PreloadSegments([FromBody] PreloadRequest request)
    {
        try
        {
            if (request?.VideoId == null || request.CurrentSegment < 0)
            {
                return BadRequest("Invalid preload request");
            }

            // Analyze user behavior to determine preload strategy
            var preloadStrategy = await analytics.GetPreloadStrategyAsync(request.VideoId, request.UserId, Request);

            // Get segments to preload
            var segmentsToPreload = await abrManager.DeterminePreloadSegmentsAsync(request, preloadStrategy);

            // Return preload instructions to client
            var preloadResponse = new PreloadResponse
            {
                Segments = segmentsToPreload,
                Strategy = preloadStrategy,
                CacheInstructions = GenerateCacheInstructions(segmentsToPreload)
            };

            return Ok(preloadResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating preload response");
            return StatusCode(500);
        }
    }

    #endregion

    #region Thumbnail and Preview Generation

    /// <summary>
    /// Generate and serve thumbnail sprites (storyboard) like YouTube
    /// </summary>
    [HttpGet("thumbnails/{videoId}/storyboard")]
    public async Task<IActionResult> GetStoryboard(
        string videoId,
        [FromQuery] int level = 2,
        [FromQuery] string format = "webp")
    {
        try
        {
            var cacheKey = $"storyboard_{videoId}_L{level}_{format}";

            if (cache.TryGetValue(cacheKey, out byte[] cachedStoryboard))
            {
                return File(cachedStoryboard, $"image/{format}");
            }

            var videoMeta = await metadata.GetVideoMetadataAsync(videoId);
            if (videoMeta == null) return NotFound();

            // Generate or retrieve storyboard
            var storyboardPath = Path.Combine(_videoPath, "storyboards", videoId, $"storyboard_L{level}.{format}");

            if (!System.IO.File.Exists(storyboardPath))
            {
                // Generate storyboard on-the-fly (in production, this would be pre-generated)
                storyboardPath = await GenerateStoryboardAsync(videoId, level, format);
            }

            if (storyboardPath == null || !System.IO.File.Exists(storyboardPath))
            {
                return NotFound("Storyboard not available");
            }

            var storyboardData = await System.IO.File.ReadAllBytesAsync(storyboardPath);

            // Cache for 1 hour
            cache.Set(cacheKey, storyboardData, TimeSpan.FromHours(1));

            Response.Headers.Append("Cache-Control", "public, max-age=3600");
            return File(storyboardData, $"image/{format}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating storyboard: {VideoId}", videoId);
            return StatusCode(500);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<string> GenerateMasterPlaylistAsync(
        string videoId,
        VideoMetadata videoMeta,
        DeviceCapabilities capabilities,
        string preferredCodec,
        bool autoQuality)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        sb.AppendLine("#EXT-X-VERSION:6");
        sb.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        // Filter qualities based on device capabilities
        var availableQualities = videoMeta.Qualities
            .Where(q => IsQualitySupported(q, capabilities))
            .OrderByDescending(q => q.Bitrate);

        foreach (var quality in availableQualities)
        {
            if (quality.Codec != preferredCodec && availableQualities.Any(q => q.Codec == preferredCodec))
                continue;

            sb.AppendLine(
                $"#EXT-X-STREAM-INF:BANDWIDTH={quality.Bitrate},RESOLUTION={quality.Width}x{quality.Height},CODECS=\"{quality.CodecString}\",FRAME-RATE={quality.FrameRate}");
            sb.AppendLine($"playlist/{videoId}/{quality.Label}.m3u8");
        }

        return sb.ToString();
    }

    private async Task<string> GenerateSegmentPlaylistAsync(
        string videoId,
        QualityInfo quality,
        long? startTime,
        int segmentDuration)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        sb.AppendLine("#EXT-X-VERSION:3");
        sb.AppendLine($"#EXT-X-TARGETDURATION:{segmentDuration}");
        sb.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

        var totalSegments = (int)Math.Ceiling(quality.Duration / (double)segmentDuration);
        var startSegment = startTime.HasValue ? (int)(startTime.Value / segmentDuration) : 0;

        for (int i = startSegment; i < totalSegments; i++)
        {
            sb.AppendLine($"#EXTINF:{segmentDuration}.000,");
            sb.AppendLine($"../segment/{videoId}/{quality.Label}/{i}.ts");
        }

        sb.AppendLine("#EXT-X-ENDLIST");
        return sb.ToString();
    }

    private async Task<IActionResult> HandleSegmentRangeRequest(string segmentPath, FileInfo fileInfo,
        string rangeHeader)
    {
        var ranges = ParseRangeHeader(rangeHeader, fileInfo.Length);
        if (ranges == null || ranges.Count == 0)
        {
            return new StatusCodeResult(416);
        }

        var range = ranges[0];

        Response.StatusCode = 206;
        Response.Headers.Append("Content-Range", $"bytes {range.Start}-{range.End}/{fileInfo.Length}");
        Response.Headers.Append("Content-Length", (range.End - range.Start + 1).ToString());

        await using var fileStream = new FileStream(segmentPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fileStream.Seek(range.Start, SeekOrigin.Begin);

        var remainingBytes = range.End - range.Start + 1;
        var buffer = new byte[Math.Min(65536, remainingBytes)];

        while (remainingBytes > 0)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);

            if (bytesRead == 0) break;

            await Response.Body.WriteAsync(buffer, 0, bytesRead);
            remainingBytes -= bytesRead;

            if (HttpContext.RequestAborted.IsCancellationRequested)
                break;
        }

        return new EmptyResult();
    }

    private async Task<IActionResult> HandleLiveStreamSegment(string videoId, FormatInfo format, string sequenceNumber)
    {
        // Live-streaming logic would go here
        // This is a placeholder for the complex live streaming implementation
        throw new NotImplementedException("Live streaming not implemented in this example");
    }

    private VideoFormat? DetermineOptimalFormat(VideoMetadata videoMeta, int? itag, string mime,
        IHeaderDictionary headers)
    {
        if (itag.HasValue)
        {
            return videoMeta.Formats.FirstOrDefault(f => f.Itag == itag.Value);
        }

        if (!string.IsNullOrEmpty(mime))
        {
            return videoMeta.Formats.FirstOrDefault(f => f.MimeType.StartsWith(mime))!;
        }

        // Analyze user agent and connection for best format
        var userAgent = headers["User-Agent"].ToString();
        var acceptEncoding = headers["Accept-Encoding"].ToString();

        // Default to best quality H.264 format for compatibility
        return videoMeta.Formats
            .Where(f => f.Codec == "h264")
            .OrderByDescending<VideoFormat, object>(f => f.Quality)
            .FirstOrDefault();
    }

    private string GetFormatFilePath(string videoId, VideoFormat format)
    {
        return Path.Combine(_videoPath, videoId, $"{videoId}_{format.Label}.{format.Container}");
    }

    private async Task<IActionResult> HandleAdvancedRangeRequest(
        string filePath,
        FileInfo fileInfo,
        VideoFormat format,
        long? customRange,
        ThrottleInfo throttleInfo)
    {
        // Advanced range request handling with smart seeking and throttling
        var rangeHeader = Request.Headers.Range.FirstOrDefault();

        List<ByteRange> ranges;
        if (customRange.HasValue)
        {
            // Custom range format (YouTube-style)
            ranges = new List<ByteRange> { ParseCustomRange(customRange.Value, fileInfo.Length) };
        }
        else
        {
            ranges = ParseRangeHeader(rangeHeader, fileInfo.Length);
        }

        if (ranges == null || ranges.Count == 0)
        {
            return new StatusCodeResult(416);
        }

        var range = ranges[0];

        Response.StatusCode = 206;
        Response.Headers.Append("Content-Range", $"bytes {range.Start}-{range.End}/{fileInfo.Length}");
        Response.Headers.Append("Content-Length", (range.End - range.Start + 1).ToString());

        return await StreamRangeWithThrottling(filePath, range, throttleInfo);
    }

    private async Task<IActionResult> StreamWithThrottling(string filePath, FileInfo fileInfo,
        ThrottleInfo throttleInfo)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var buffer = new byte[throttleInfo.ChunkSize];
        long totalBytesStreamed = 0;
        var startTime = DateTime.UtcNow;

        while (totalBytesStreamed < fileInfo.Length)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, fileInfo.Length - totalBytesStreamed);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);

            if (bytesRead == 0) break;

            await Response.Body.WriteAsync(buffer, 0, bytesRead);
            totalBytesStreamed += bytesRead;

            // Apply bandwidth throttling
            if (throttleInfo.BandwidthLimit > 0)
            {
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var expectedMs = (totalBytesStreamed * 8.0 / throttleInfo.BandwidthLimit) * 1000;

                if (elapsedMs < expectedMs)
                {
                    await Task.Delay((int)(expectedMs - elapsedMs));
                }
            }

            if (HttpContext.RequestAborted.IsCancellationRequested)
                break;
        }

        return new EmptyResult();
    }

    private async Task<IActionResult> StreamRangeWithThrottling(string filePath, ByteRange range,
        ThrottleInfo throttleInfo)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fileStream.Seek(range.Start, SeekOrigin.Begin);

        var remainingBytes = range.End - range.Start + 1;
        var buffer = new byte[Math.Min(throttleInfo.ChunkSize, remainingBytes)];
        var startTime = DateTime.UtcNow;
        long bytesStreamed = 0;

        while (remainingBytes > 0)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);

            if (bytesRead == 0) break;

            await Response.Body.WriteAsync(buffer, 0, bytesRead);
            remainingBytes -= bytesRead;
            bytesStreamed += bytesRead;

            // Apply throttling
            if (throttleInfo.BandwidthLimit > 0)
            {
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var expectedMs = (bytesStreamed * 8.0 / throttleInfo.BandwidthLimit) * 1000;

                if (elapsedMs < expectedMs)
                {
                    await Task.Delay((int)(expectedMs - elapsedMs));
                }
            }

            if (HttpContext.RequestAborted.IsCancellationRequested)
                break;
        }

        return new EmptyResult();
    }

    private List<ByteRange> ParseRangeHeader(string rangeHeader, long fileSize)
    {
        if (!rangeHeader.StartsWith("bytes="))
            return null;

        var ranges = new List<ByteRange>();
        var rangeSpecs = rangeHeader.Substring(6).Split(',');

        foreach (var rangeSpec in rangeSpecs)
        {
            var parts = rangeSpec.Trim().Split('-');
            if (parts.Length != 2) continue;

            if (long.TryParse(parts[0], out var start) && long.TryParse(parts[1], out var end))
            {
                if (start < fileSize && end < fileSize && start <= end)
                {
                    ranges.Add(new ByteRange { Start = start, End = end });
                }
            }
            else if (long.TryParse(parts[0], out start) && string.IsNullOrEmpty(parts[1]))
            {
                if (start < fileSize)
                {
                    ranges.Add(new ByteRange { Start = start, End = fileSize - 1 });
                }
            }
            else if (string.IsNullOrEmpty(parts[0]) && long.TryParse(parts[1], out var suffix))
            {
                if (suffix <= fileSize)
                {
                    ranges.Add(new ByteRange { Start = fileSize - suffix, End = fileSize - 1 });
                }
            }
        }

        return ranges.Count > 0 ? ranges : null;
    }

    private ByteRange ParseCustomRange(long customRange, long fileSize)
    {
        // YouTube-style range parsing (e.g., range=0-524287)
        var parts = customRange.ToString().Split('-');
        if (parts.Length == 2)
        {
            if (long.TryParse(parts[0], out var start) && long.TryParse(parts[1], out var end))
            {
                return new ByteRange { Start = start, End = Math.Min(end, fileSize - 1) };
            }
        }

        return new ByteRange { Start = 0, End = Math.Min(customRange, fileSize - 1) };
    }

    private bool IsQualitySupported(QualityInfo quality, DeviceCapabilities capabilities)
    {
        if (quality.Width > capabilities.MaxResolutionWidth || quality.Height > capabilities.MaxResolutionHeight)
            return false;

        if (quality.Bitrate > capabilities.MaxBitrate)
            return false;

        return capabilities.SupportedCodecs.Contains(quality.Codec);
    }

    private async Task<TimeSpan> CalculateStreamLatency(string streamId, long sequence)
    {
        var currentSeq = await GetCurrentLiveSequence(streamId);
        var seqDiff = currentSeq - sequence;
        return TimeSpan.FromSeconds(seqDiff * 2); // Assuming 2-second segments
    }

    private async Task<long> GetCurrentLiveSequence(string streamId)
    {
        // This would query your live streaming infrastructure
        return await Task.FromResult(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond / 2);
    }

    private async Task<long> GetCatchupSequence(string streamId)
    {
        var current = await GetCurrentLiveSequence(streamId);
        return current - 15; // Start 30 seconds behind live edge
    }

    private async Task<string?> GenerateStoryboardAsync(string videoId, int level, string format)
    {
        // In production, this would interface with video processing services
        // For now, return null to indicate storyboard generation failed
        logger.LogWarning("Storyboard generation not implemented for video: {VideoId}", videoId);
        return null;
    }

    private List<CacheInstruction> GenerateCacheInstructions(List<SegmentInfo> segments)
    {
        return segments.Select(s => new CacheInstruction
        {
            Url = s.Url,
            Priority = s.Priority,
            CacheDuration = TimeSpan.FromHours(1)
        }).ToList();
    }

    #endregion
}