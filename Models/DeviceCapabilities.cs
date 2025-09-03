namespace Learn.Stream.Models;

public class DeviceCapabilities
{
    public int MaxResolutionWidth { get; set; }
    public int MaxResolutionHeight { get; set; }
    public int MaxBitrate { get; set; }
    public List<string> SupportedCodecs { get; set; } = new();
    public bool SupportsHDR { get; set; }
    public bool Supports60fps { get; set; }
}