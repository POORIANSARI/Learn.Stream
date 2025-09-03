namespace Learn.Stream.Models;

public class PreloadResponse
{
    public List<SegmentInfo> Segments { get; set; } = new();
    public PreloadStrategy Strategy { get; set; } = new();
    public List<CacheInstruction> CacheInstructions { get; set; } = new();
}