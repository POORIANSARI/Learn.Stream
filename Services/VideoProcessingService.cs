using System.Text;

namespace Learn.Stream.Controllers;

public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _outputPath;
    private readonly string _segmentPath;

    // Define quality profiles
    private readonly List<QualityProfile> _qualityProfiles = new()
    {
        new QualityProfile { Name = "360p", Width = 640, Height = 360, Bitrate = 800000, AudioBitrate = 128000 },
        new QualityProfile { Name = "480p", Width = 854, Height = 480, Bitrate = 1400000, AudioBitrate = 128000 },
        new QualityProfile { Name = "720p", Width = 1280, Height = 720, Bitrate = 2500000, AudioBitrate = 192000 },
        new QualityProfile { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000000, AudioBitrate = 256000 },
        new QualityProfile { Name = "1440p", Width = 2560, Height = 1440, Bitrate = 10000000, AudioBitrate = 256000 },
        new QualityProfile { Name = "2160p", Width = 3840, Height = 2160, Bitrate = 20000000, AudioBitrate = 320000 }
    };

    public VideoProcessingService(ILogger<VideoProcessingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _outputPath = configuration["VideoStorage:OutputPath"] ?? "processed";
        _segmentPath = configuration["VideoStorage:SegmentPath"] ?? "segments";
        
        Directory.CreateDirectory(_outputPath);
        Directory.CreateDirectory(_segmentPath);
    }

    public async Task ProcessVideoAsync(string videoId)
    {
        try
        {
            _logger.LogInformation("Starting processing for video {VideoId}", videoId);
            
            // Update status to processing
            await UpdateProcessingStatus(videoId, ProcessingStatus.Processing, 0);

            // Get original file info
            var originalFilePath = Path.Combine("uploads", videoId, "original.mp4");
            if (!File.Exists(originalFilePath))
            {
                // Try other extensions
                var directory = Path.Combine("uploads", videoId);
                var originalFile = Directory.GetFiles(directory, "original.*").FirstOrDefault();
                if (originalFile == null)
                {
                    await UpdateProcessingStatus(videoId, ProcessingStatus.Failed, 0, "Original file not found");
                    return;
                }
                originalFilePath = originalFile;
            }

            var videoInfo = await AnalyzeVideo(originalFilePath);
            
            // Strategy 1: Process whole video first, then create segments (RECOMMENDED)
            await ProcessWithWholeFileStrategy(videoId, originalFilePath, videoInfo);
            
            // Alternative Strategy 2: Direct to segments (for live streaming)
            // await ProcessDirectToSegmentsStrategy(videoId, originalFilePath, videoInfo);

            await UpdateProcessingStatus(videoId, ProcessingStatus.Completed, 100);
            _logger.LogInformation("Completed processing for video {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video {VideoId}", videoId);
            await UpdateProcessingStatus(videoId, ProcessingStatus.Failed, 0, ex.Message);
        }
    }

    /// <summary>
    /// STRATEGY 1: Whole File Strategy (RECOMMENDED for most cases)
    /// 1. Create complete video files for each quality
    /// 2. Then segment each quality into chunks
    /// 
    /// PROS: More reliable, easier to manage, supports seeking anywhere
    /// CONS: Takes more storage space temporarily, longer initial processing
    /// </summary>
    private async Task ProcessWithWholeFileStrategy(string videoId, string originalFilePath, VideoFileInfo videoInfo)
    {
        var videoOutputDir = Path.Combine(_outputPath, videoId);
        var segmentOutputDir = Path.Combine(_segmentPath, videoId);
        
        Directory.CreateDirectory(videoOutputDir);
        Directory.CreateDirectory(segmentOutputDir);

        // Determine which qualities to create based on source resolution
        var targetQualities = DetermineTargetQualities(videoInfo);
        
        // Step 1: Create complete video files for each quality
        _logger.LogInformation("Creating quality variants for video {VideoId}", videoId);
        var qualityTasks = targetQualities.Select(async (quality, index) =>
        {
            await CreateQualityVariant(originalFilePath, videoOutputDir, quality);
            var progress = (int)((index + 1) * 30.0 / targetQualities.Count);
            await UpdateProcessingStatus(videoId, ProcessingStatus.Processing, progress);
        });

        await Task.WhenAll(qualityTasks);

        // Step 2: Create segments from each quality file
        _logger.LogInformation("Creating segments for video {VideoId}", videoId);
        var segmentTasks = targetQualities.Select(async (quality, index) =>
        {
            var qualityFile = Path.Combine(videoOutputDir, $"{quality.Name}.mp4");
            await CreateSegments(qualityFile, segmentOutputDir, quality);
            var progress = 30 + (int)((index + 1) * 50.0 / targetQualities.Count);
            await UpdateProcessingStatus(videoId, ProcessingStatus.Processing, progress);
        });

        await Task.WhenAll(segmentTasks);

        // Step 3: Generate manifests and thumbnails
        await GenerateManifests(videoId, targetQualities, videoInfo);
        await GenerateThumbnails(originalFilePath, videoId, videoInfo);
        
        await UpdateProcessingStatus(videoId, ProcessingStatus.Processing, 90);
    }

    /// <summary>
    /// STRATEGY 2: Direct to Segments (for live streaming or space-constrained scenarios)
    /// Creates segments directly without storing complete quality files
    /// 
    /// PROS: Uses less storage space, faster for live streaming
    /// CONS: More complex, harder to handle seeking, less reliable
    /// </summary>
    private async Task ProcessDirectToSegmentsStrategy(string videoId, string originalFilePath, VideoFileInfo videoInfo)
    {
        var segmentOutputDir = Path.Combine(_segmentPath, videoId);
        Directory.CreateDirectory(segmentOutputDir);

        var targetQualities = DetermineTargetQualities(videoInfo);
        
        // Create segments directly for each quality
        var segmentTasks = targetQualities.Select(async quality =>
        {
            await CreateSegmentsDirectly(originalFilePath, segmentOutputDir, quality);
        });

        await Task.WhenAll(segmentTasks);
    }

    private async Task CreateQualityVariant(string inputPath, string outputDir, QualityProfile quality)
    {
        var outputPath = Path.Combine(outputDir, $"{quality.Name}.mp4");
        
        // In production, you'd use FFmpeg here
        // For demonstration, we'll simulate the process
        _logger.LogInformation("Creating {Quality} variant: {OutputPath}", quality.Name, outputPath);
        
        // Simulate processing time
        await Task.Delay(1000);
        
        // Create empty file for demonstration
        File.WriteAllText(outputPath, $"Quality variant: {quality.Name}");
        
        /* Real FFmpeg command would be:
        var ffmpegArgs = $"-i \"{inputPath}\" " +
                        $"-c:v libx264 -crf 23 " +
                        $"-b:v {quality.Bitrate} " +
                        $"-maxrate {quality.Bitrate * 1.2} " +
                        $"-bufsize {quality.Bitrate * 2} " +
                        $"-vf scale={quality.Width}:{quality.Height} " +
                        $"-c:a aac -b:a {quality.AudioBitrate} " +
                        $"-movflags faststart " +
                        $"\"{outputPath}\"";
        */
    }

    private async Task CreateSegments(string qualityFilePath, string segmentDir, QualityProfile quality)
    {
        var qualitySegmentDir = Path.Combine(segmentDir, quality.Name);
        Directory.CreateDirectory(qualitySegmentDir);

        _logger.LogInformation("Creating segments for {Quality}", quality.Name);
        
        // In production, use FFmpeg to create HLS segments
        // Simulate creating 60 segments (10 minutes of 10-second segments)
        for (int i = 0; i < 60; i++)
        {
            var segmentPath = Path.Combine(qualitySegmentDir, $"segment_{i:D6}.ts");
            File.WriteAllText(segmentPath, $"Segment {i} for {quality.Name}");
        }

        // Create playlist file for this quality
        await CreatePlaylistFile(qualitySegmentDir, quality, 60);
        
        /* Real FFmpeg command would be:
        var ffmpegArgs = $"-i \"{qualityFilePath}\" " +
                        $"-c copy " +
                        $"-f hls " +
                        $"-hls_time 10 " +
                        $"-hls_playlist_type vod " +
                        $"-hls_segment_filename \"{qualitySegmentDir}/segment_%06d.ts\" " +
                        $"\"{Path.Combine(qualitySegmentDir, "playlist.m3u8")}\"";
        */
    }

    private async Task CreateSegmentsDirectly(string inputPath, string segmentDir, QualityProfile quality)
    {
        var qualitySegmentDir = Path.Combine(segmentDir, quality.Name);
        Directory.CreateDirectory(qualitySegmentDir);

        _logger.LogInformation("Creating segments directly for {Quality}", quality.Name);
        
        /* Real FFmpeg command would be:
        var ffmpegArgs = $"-i \"{inputPath}\" " +
                        $"-c:v libx264 -crf 23 " +
                        $"-b:v {quality.Bitrate} " +
                        $"-vf scale={quality.Width}:{quality.Height} " +
                        $"-c:a aac -b:a {quality.AudioBitrate} " +
                        $"-f hls " +
                        $"-hls_time 10 " +
                        $"-hls_playlist_type vod " +
                        $"-hls_segment_filename \"{qualitySegmentDir}/segment_%06d.ts\" " +
                        $"\"{Path.Combine(qualitySegmentDir, "playlist.m3u8")}\"";
        */
    }

    private async Task CreatePlaylistFile(string segmentDir, QualityProfile quality, int segmentCount)
    {
        var playlistPath = Path.Combine(segmentDir, "playlist.m3u8");
        var playlist = new StringBuilder();
        
        playlist.AppendLine("#EXTM3U");
        playlist.AppendLine("#EXT-X-VERSION:3");
        playlist.AppendLine("#EXT-X-TARGETDURATION:10");
        playlist.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

        for (int i = 0; i < segmentCount; i++)
        {
            playlist.AppendLine("#EXTINF:10.0,");
            playlist.AppendLine($"segment_{i:D6}.ts");
        }

        playlist.AppendLine("#EXT-X-ENDLIST");
        
        await File.WriteAllTextAsync(playlistPath, playlist.ToString());
    }

    private List<QualityProfile> DetermineTargetQualities(VideoFileInfo videoInfo)
    {
        // Only create qualities that make sense for the source resolution
        var sourceHeight = int.Parse(videoInfo.Resolution.Split('x')[1]);
        
        return _qualityProfiles
            .Where(q => q.Height <= sourceHeight)
            .OrderBy(q => q.Height)
            .ToList();
    }

    private async Task GenerateManifests(string videoId, List<QualityProfile> qualities, VideoFileInfo videoInfo)
    {
        // Create master manifest
        var manifestDir = Path.Combine(_outputPath, videoId);
        var masterManifestPath = Path.Combine(manifestDir, "master.m3u8");
        
        var masterPlaylist = new StringBuilder();
        masterPlaylist.AppendLine("#EXTM3U");
        masterPlaylist.AppendLine("#EXT-X-VERSION:6");
        masterPlaylist.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        foreach (var quality in qualities)
        {
            masterPlaylist.AppendLine(
                $"#EXT-X-STREAM-INF:BANDWIDTH={quality.Bitrate + quality.AudioBitrate}," +
                $"RESOLUTION={quality.Width}x{quality.Height}," +
                $"CODECS=\"avc1.640028,mp4a.40.2\"");
            masterPlaylist.AppendLine($"../segments/{videoId}/{quality.Name}/playlist.m3u8");
        }

        await File.WriteAllTextAsync(masterManifestPath, masterPlaylist.ToString());
    }

    private async Task GenerateThumbnails(string originalFilePath, string videoId, VideoFileInfo videoInfo)
    {
        var thumbnailDir = Path.Combine(_outputPath, videoId, "thumbnails");
        Directory.CreateDirectory(thumbnailDir);

        // Generate storyboard thumbnails (every 10 seconds)
        _logger.LogInformation("Generating thumbnails for video {VideoId}", videoId);
        
        // Simulate thumbnail generation
        await Task.Delay(500);
        
        /* Real FFmpeg command for thumbnails:
        var ffmpegArgs = $"-i \"{originalFilePath}\" " +
                        $"-vf \"fps=1/10,scale=160:90\" " +
                        $"-f image2 " +
                        $"\"{thumbnailDir}/thumb_%03d.jpg\"";
        */
    }

    public async Task<ProcessingStatusResult> GetProcessingStatusAsync(string videoId)
    {
        // In production, query from database
        return new ProcessingStatusResult
        {
            VideoId = videoId,
            Status = ProcessingStatus.Processing,
            Progress = 45,
            Message = "Creating quality variants..."
        };
    }

    public async Task<VideoProcessingDetails?> GetVideoDetailsAsync(string videoId)
    {
        // In production, query from database
        return new VideoProcessingDetails
        {
            VideoId = videoId,
            OriginalFileSize = 500_000_000, // 500MB
            ProcessedQualities = new[] { "360p", "720p", "1080p" },
            TotalSegments = 180, // 30 minutes * 6 segments per minute
            StorageUsed = 1_200_000_000, // 1.2GB total
            ProcessingTime = TimeSpan.FromMinutes(45)
        };
    }

    private async Task<VideoFileInfo> AnalyzeVideo(string filePath)
    {
        // Use FFprobe to get actual video information
        var fileInfo = new FileInfo(filePath);
        
        return new VideoFileInfo
        {
            FilePath = filePath,
            FileSize = fileInfo.Length,
            Duration = TimeSpan.FromMinutes(10),
            Resolution = "1920x1080",
            FrameRate = 30,
            Bitrate = 5000000,
            Codec = "h264",
            HasAudio = true
        };
    }

    private async Task UpdateProcessingStatus(string videoId, ProcessingStatus status, int progress, string? message = null)
    {
        // In production, update database
        _logger.LogInformation("Video {VideoId}: {Status} - {Progress}% - {Message}", 
            videoId, status, progress, message ?? "");
    }
}