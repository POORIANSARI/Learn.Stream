namespace Learn.Stream.Models;

public class SegmentInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public int SegmentIndex { get; set; }
    public string Url { get; set; } = string.Empty;
    public PreloadPriority Priority { get; set; }
    public long EstimatedSize { get; set; }
}