namespace Learn.Stream.Models;

public class VideoMetadata
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsLive { get; set; }
    public List<VideoFormat?> Formats { get; set; } = new();
    public List<QualityInfo> Qualities { get; set; } = new();
}