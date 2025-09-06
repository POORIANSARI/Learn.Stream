namespace Learn.Stream.Controllers;

public class UploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string? OriginalFilePath { get; set; }
    public TimeSpan EstimatedProcessingTime { get; set; }
}