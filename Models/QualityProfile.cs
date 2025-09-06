namespace Learn.Stream.Controllers;

public class QualityProfile
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bitrate { get; set; }
    public long AudioBitrate { get; set; }
}