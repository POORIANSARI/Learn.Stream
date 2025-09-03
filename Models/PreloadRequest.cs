namespace Learn.Stream.Models;

public class PreloadRequest
{
    public string VideoId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int CurrentSegment { get; set; }
    public string CurrentQuality { get; set; } = string.Empty;
    public long BandwidthEstimate { get; set; }
}