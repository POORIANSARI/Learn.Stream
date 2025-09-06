namespace Learn.Stream.Controllers;

public class VideoFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public TimeSpan Duration { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public double FrameRate { get; set; }
    public long Bitrate { get; set; }
    public string Codec { get; set; } = string.Empty;
    public bool HasAudio { get; set; }
}