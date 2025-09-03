namespace Learn.Stream.Models;

public class BufferHealthSample
{
    public DateTime Timestamp { get; set; }
    public TimeSpan BufferLevel { get; set; }
    public long Bandwidth { get; set; }
}