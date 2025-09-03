namespace Learn.Stream.Models;

public class ErrorEvent
{
    public DateTime Timestamp { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}