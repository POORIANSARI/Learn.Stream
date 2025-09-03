namespace Learn.Stream.Models;

public class QualityRecommendationRequest
{
    public string VideoId { get; set; } = string.Empty;
    public long BandwidthEstimate { get; set; }
    public TimeSpan CurrentBufferLevel { get; set; }
    public string CurrentQuality { get; set; } = string.Empty;
    public List<string> AvailableQualities { get; set; } = new();
    public string DeviceType { get; set; } = string.Empty;
    public bool IsFullscreen { get; set; }
}