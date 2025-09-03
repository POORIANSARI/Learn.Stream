namespace Learn.Stream.Models;

public class CdnEndpoint
{
    public string Url { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Priority { get; set; }
}