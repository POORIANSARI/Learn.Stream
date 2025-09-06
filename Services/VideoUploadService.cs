namespace Learn.Stream.Controllers;

public class VideoUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideoUploadService> _logger;
    private readonly string _uploadPath;
    private readonly string _tempPath;

    public VideoUploadService(IConfiguration configuration, ILogger<VideoUploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadPath = configuration["VideoStorage:UploadPath"] ?? "uploads";
        _tempPath = configuration["VideoStorage:TempPath"] ?? "temp";
        
        // Ensure directories exist
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_tempPath);
    }

    public async Task<UploadResult> UploadVideoAsync(IFormFile videoFile, VideoUploadRequest request)
    {
        var videoId = Guid.NewGuid().ToString("N");
        var originalFileName = videoFile.FileName;
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();

        // Validate file type
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
        if (!allowedExtensions.Contains(fileExtension))
        {
            return new UploadResult 
            { 
                Success = false, 
                ErrorMessage = $"Unsupported file type: {fileExtension}" 
            };
        }

        try
        {
            // Create video directory structure
            var videoDirectory = Path.Combine(_uploadPath, videoId);
            Directory.CreateDirectory(videoDirectory);

            // Save original file
            var originalFilePath = Path.Combine(videoDirectory, $"original{fileExtension}");
            
            using (var fileStream = new FileStream(originalFilePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(fileStream);
            }

            // Get basic video information
            var videoInfo = await AnalyzeVideoFile(originalFilePath);

            // Create video record in database
            await SaveVideoMetadata(videoId, request, videoInfo, originalFilePath);

            return new UploadResult
            {
                Success = true,
                VideoId = videoId,
                OriginalFilePath = originalFilePath,
                EstimatedProcessingTime = CalculateProcessingTime(videoInfo)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video {VideoId}", videoId);
            return new UploadResult 
            { 
                Success = false, 
                ErrorMessage = "Upload failed: " + ex.Message 
            };
        }
    }

    private async Task<VideoFileInfo> AnalyzeVideoFile(string filePath)
    {
        // In production, use FFprobe or similar to get detailed video information
        var fileInfo = new FileInfo(filePath);
        
        return new VideoFileInfo
        {
            FilePath = filePath,
            FileSize = fileInfo.Length,
            Duration = TimeSpan.FromMinutes(10), // Would be detected from actual file
            Resolution = "1920x1080", // Would be detected
            FrameRate = 30, // Would be detected
            Bitrate = 5000000, // Would be detected
            Codec = "h264", // Would be detected
            HasAudio = true // Would be detected
        };
    }

    private async Task SaveVideoMetadata(string videoId, VideoUploadRequest request, 
        VideoFileInfo videoInfo, string originalFilePath)
    {
        // Save to database - in production, use Entity Framework or similar
        _logger.LogInformation("Saving metadata for video {VideoId}", videoId);
    }

    private TimeSpan CalculateProcessingTime(VideoFileInfo videoInfo)
    {
        // Rough estimate: processing takes about 2x the video duration
        return TimeSpan.FromTicks(videoInfo.Duration.Ticks * 2);
    }
}