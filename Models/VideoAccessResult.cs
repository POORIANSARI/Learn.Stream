namespace Learn.Stream.Models;

public class VideoAccessResult
{
    public bool Allowed { get; set; }
    public string? UserId { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
}