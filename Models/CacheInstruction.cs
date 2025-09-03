namespace Learn.Stream.Models;

public class CacheInstruction
{
    public string Url { get; set; } = string.Empty;
    public PreloadPriority Priority { get; set; }
    public TimeSpan CacheDuration { get; set; }
}