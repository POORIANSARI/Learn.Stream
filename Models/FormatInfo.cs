namespace Learn.Stream.Models;

public class FormatInfo
{
    public int Itag { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string QualityLabel { get; set; } = string.Empty;
    public string Codec { get; set; } = string.Empty;
    public long Bitrate { get; set; }
    public string Url { get; set; } = string.Empty;

    // Implicit conversion from FormatInfo to VideoFormat
    public static implicit operator VideoFormat?(FormatInfo? formatInfo)
    {
        if (formatInfo == null) return null;
        return new VideoFormat
        {
            Itag = formatInfo.Itag,
            MimeType = formatInfo.MimeType,
            Quality = 0, // Default value since FormatInfo doesn't have Quality
            Codec = formatInfo.Codec,
            Container = string.Empty, // Default value since FormatInfo doesn't have Container
            Label = formatInfo.QualityLabel // Mapping QualityLabel back to Label
        };
    }
}