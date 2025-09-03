namespace Learn.Stream.Models;

public class QualitySwitch
{
    public DateTime Timestamp { get; set; }
    public string Quality { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}