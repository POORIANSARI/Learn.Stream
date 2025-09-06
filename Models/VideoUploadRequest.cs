namespace Learn.Stream.Controllers;

public class VideoUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsPrivate { get; set; } = false;
    public string? ThumbnailUrl { get; set; }
}