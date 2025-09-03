namespace Learn.Stream.Models;

public class ThrottleInfo
{
    public long BandwidthLimit { get; set; } // bytes per second
    public int ChunkSize { get; set; }
    public TimeSpan DelayBetweenChunks { get; set; }
}