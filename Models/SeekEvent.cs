namespace Learn.Stream.Models;

public class SeekEvent
{
    public DateTime Timestamp { get; set; }
    public TimeSpan FromPosition { get; set; }
    public TimeSpan ToPosition { get; set; }
}