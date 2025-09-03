namespace Learn.Stream.Models;

public class PreloadStrategy
{
    public int SegmentCount { get; set; }
    public string[] QualityLevels { get; set; } = Array.Empty<string>();
    public PreloadPriority Priority { get; set; }
    public TimeSpan MaxBufferSize { get; set; }
}