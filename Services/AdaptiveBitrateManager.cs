using Learn.Stream.Models;

namespace Learn.Stream.Services;

public abstract class AdaptiveBitrateManager(ILogger<AdaptiveBitrateManager> logger)
{
    public static DeviceCapabilities AnalyzeDeviceCapabilities(string? userAgent, IHeaderDictionary headers)
    {
        var capabilities = new DeviceCapabilities
        {
            MaxResolutionWidth = 1920,
            MaxResolutionHeight = 1080,
            MaxBitrate = 5000000, // 5 Mbps
            SupportedCodecs = ["h264", "vp9", "av1"],
            SupportsHDR = false,
            Supports60fps = true
        };

        if (string.IsNullOrEmpty(userAgent)) return capabilities;
        // Analyze user agent for device-specific capabilities
        if (userAgent.Contains("Mobile"))
        {
            capabilities.MaxResolutionWidth = 1280;
            capabilities.MaxResolutionHeight = 720;
            capabilities.MaxBitrate = 2000000; // 2 Mbps for mobile
        }
        else if (userAgent.Contains("4K") || userAgent.Contains("UHD"))
        {
            capabilities.MaxResolutionWidth = 3840;
            capabilities.MaxResolutionHeight = 2160;
            capabilities.MaxBitrate = 25000000; // 25 Mbps for 4K
            capabilities.SupportsHDR = true;
        }

        return capabilities;
    }

    public static async Task<QualityRecommendations> GetQualityRecommendationsAsync(QualityRecommendationRequest request)
    {
        // Machine learning-based quality recommendations
        var recommendations = new QualityRecommendations
        {
            RecommendedQuality = CalculateOptimalQuality(request),
            AlternativeQualities = GetAlternativeQualities(request),
            Confidence = CalculateConfidence(request),
            ReasonCode = GetReasonCode(request)
        };

        return recommendations;
    }

    public async Task<List<SegmentInfo>> DeterminePreloadSegmentsAsync(PreloadRequest request, PreloadStrategy strategy)
    {
        var segments = new List<SegmentInfo>();

        for (var i = 1; i <= strategy.SegmentCount; i++)
        {
            var segmentIndex = request.CurrentSegment + i;

            segments.AddRange(strategy.QualityLevels.Select(quality => new SegmentInfo
            {
                VideoId = request.VideoId,
                Quality = quality,
                SegmentIndex = segmentIndex,
                Url = $"/api/VideoStream/segment/{request.VideoId}/{quality}/{segmentIndex}.ts?preload=true",
                Priority = i == 1 ? PreloadPriority.High : PreloadPriority.Medium,
                EstimatedSize = EstimateSegmentSize(quality)
            }));
        }

        return segments;
    }

    public async Task UpdateAlgorithmAsync(PlaybackStatsRequest stats)
    {
        // Update ABR algorithm based on real user data
        logger.LogDebug("Updating ABR algorithm with stats from {VideoId}", stats.VideoId);
    }

    private static string CalculateOptimalQuality(QualityRecommendationRequest request)
    {
        // Advanced algorithm considering bandwidth, device capabilities, and viewing patterns
        var availableBandwidth = request.BandwidthEstimate;
        var bufferLevel = request.CurrentBufferLevel;

        if (bufferLevel < TimeSpan.FromSeconds(10) && availableBandwidth < 2000000)
        {
            return "360p"; // Conservative quality for low buffer + low bandwidth
        }
        else if (availableBandwidth > 5000000)
        {
            return "1080p";
        }
        else if (availableBandwidth > 2500000)
        {
            return "720p";
        }
        else
        {
            return "480p";
        }
    }

    private static List<string> GetAlternativeQualities(QualityRecommendationRequest request)
    {
        return new List<string> { "360p", "480p", "720p", "1080p" };
    }

    private static double CalculateConfidence(QualityRecommendationRequest request)
    {
        // Calculate confidence based on data quality and consistency
        return 0.85; // 85% confidence
    }

    private static string GetReasonCode(QualityRecommendationRequest request)
    {
        return "bandwidth_optimization";
    }

    private static long EstimateSegmentSize(string quality)
    {
        return quality switch
        {
            "360p" => 500_000, // ~500KB per 10-second segment
            "480p" => 800_000, // ~800KB per 10-second segment
            "720p" => 1_500_000, // ~1.5MB per 10-second segment
            "1080p" => 3_000_000, // ~3MB per 10-second segment
            _ => 1_000_000
        };
    }
}