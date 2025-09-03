namespace Learn.Stream.Models;

public class PlaybackStatsRequest
{
    public string VideoId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public TimeSpan PlaybackPosition { get; set; }
    public List<BufferHealthSample>? BufferHealthSamples { get; set; }
    public List<QualitySwitchEvent>? QualitySwitchEvents { get; set; }
    public List<SeekEvent>? SeekEvents { get; set; }
    public List<ErrorEvent>? Errors { get; set; }
}