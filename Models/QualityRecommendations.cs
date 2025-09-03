namespace Learn.Stream.Models;

public class QualityRecommendations
{
    public string RecommendedQuality { get; set; } = string.Empty;
    public List<string> AlternativeQualities { get; set; } = new();
    public double Confidence { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
}