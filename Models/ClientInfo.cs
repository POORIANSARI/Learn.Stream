namespace Learn.Stream.Models;

public class ClientInfo
{
    public bool IsValid { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? UserId { get; set; }
}