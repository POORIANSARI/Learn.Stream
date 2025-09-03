namespace Learn.Stream.Models;

public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime LastActivity { get; set; }
    public int LastSegmentIndex { get; set; }
    public List<QualitySwitch> QualitySwitches { get; set; } = new();
}