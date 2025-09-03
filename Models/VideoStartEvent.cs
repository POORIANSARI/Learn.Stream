namespace Learn.Stream.Models;

public class VideoStartEvent
{
    public string VideoId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public VideoFormat Format { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string Referrer { get; set; } = string.Empty;
}