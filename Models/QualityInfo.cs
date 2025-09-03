namespace Learn.Stream.Models;

public class QualityInfo
{
    public string Label { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Bitrate { get; set; }
    public string Codec { get; set; } = string.Empty;
    public double FrameRate { get; set; }
    public int Duration { get; set; }
    public string CodecString { get; set; } = string.Empty;
}