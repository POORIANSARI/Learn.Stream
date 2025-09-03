using Learn.Stream.Models;

namespace Learn.Stream.Services;

public class VideoMetadataService
{
    private readonly ILogger<VideoMetadataService> _logger;

    public VideoMetadataService(ILogger<VideoMetadataService> logger)
    {
        _logger = logger;
    }

    public async Task<VideoMetadata?> GetVideoMetadataAsync(string videoId)
    {
        // In production, this would query your database
        return new VideoMetadata
        {
            VideoId = videoId,
            Title = "Sample Video",
            Duration = TimeSpan.FromMinutes(10),
            IsLive = false,
            Formats = GenerateSampleFormats(),
            Qualities = GenerateSampleQualities()
        };
    }

    public async Task<LiveStreamInfo?> GetLiveStreamAsync(string streamId)
    {
        return new LiveStreamInfo
        {
            StreamId = streamId,
            IsActive = true,
            StartTime = DateTime.UtcNow.AddHours(-1)
        };
    }

    private List<VideoFormat> GenerateSampleFormats()
    {
        return
        [
            new VideoFormat
                { Itag = 18, MimeType = "video/mp4", Quality = 360, Codec = "h264", Container = "mp4", Label = "360p" },

            new VideoFormat
                { Itag = 22, MimeType = "video/mp4", Quality = 720, Codec = "h264", Container = "mp4", Label = "720p" },

            new VideoFormat
            {
                Itag = 37, MimeType = "video/mp4", Quality = 1080, Codec = "h264", Container = "mp4", Label = "1080p"
            }
        ];
    }

    private static List<QualityInfo> GenerateSampleQualities()
    {
        return
        [
            new QualityInfo
            {
                Label = "360p", Width = 640, Height = 360, Bitrate = 800000, Codec = "h264", FrameRate = 30,
                Duration = 600, CodecString = "avc1.42E01E"
            },

            new QualityInfo
            {
                Label = "720p", Width = 1280, Height = 720, Bitrate = 2500000, Codec = "h264", FrameRate = 30,
                Duration = 600, CodecString = "avc1.640028"
            },

            new QualityInfo
            {
                Label = "1080p", Width = 1920, Height = 1080, Bitrate = 5000000, Codec = "h264", FrameRate = 30,
                Duration = 600, CodecString = "avc1.640028"
            }
        ];
    }
}