namespace Learn.Stream.Models;

public class QualitySwitchEvent
{
    public DateTime Timestamp { get; set; }
    public string FromQuality { get; set; } = string.Empty;
    public string ToQuality { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}