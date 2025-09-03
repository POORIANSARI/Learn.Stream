namespace Learn.Stream.Models;


public class VideoFormat
{
    public int Itag { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int Quality { get; set; }
    public string Codec { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    // Implicit conversion from VideoFormat to FormatInfo
    public static implicit operator FormatInfo?(VideoFormat? videoFormat)
    {
        if (videoFormat == null) return null;
        return new FormatInfo
        {
            Itag = videoFormat.Itag,
            MimeType = videoFormat.MimeType,
            QualityLabel = videoFormat.Label, // Mapping Label to QualityLabel
            Codec = videoFormat.Codec
            // Bitrate and Url will use their default values
        };
    }
}
