namespace Learn.Stream.Controllers;

public class ProcessingStatusResult
{
    public string VideoId { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public int Progress { get; set; } // 0-100
    public string? Message { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}