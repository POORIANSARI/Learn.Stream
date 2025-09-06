namespace Learn.Stream.Controllers;

public class VideoProcessingDetails
{
    public string VideoId { get; set; } = string.Empty;
    public long OriginalFileSize { get; set; }
    public string[] ProcessedQualities { get; set; } = Array.Empty<string>();
    public int TotalSegments { get; set; }
    public long StorageUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}