namespace Learn.Stream.Models;

public class LiveStreamInfo
{
    public string StreamId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
}