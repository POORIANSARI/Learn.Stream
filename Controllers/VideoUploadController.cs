using Microsoft.AspNetCore.Mvc;


namespace Learn.Stream.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoUploadController : ControllerBase
{
    private readonly VideoUploadService _uploadService;
    private readonly VideoProcessingService _processingService;
    private readonly ILogger<VideoUploadController> _logger;

    public VideoUploadController(
        VideoUploadService uploadService,
        VideoProcessingService processingService,
        ILogger<VideoUploadController> logger)
    {
        _uploadService = uploadService;
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a video file
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(2_000_000_000)] // 2GB limit
    public async Task<IActionResult> UploadVideo(IFormFile videoFile, [FromForm] VideoUploadRequest request)
    {
        if (videoFile == null || videoFile.Length == 0)
            return BadRequest("No video file provided");

        try
        {
            // Start upload process
            var uploadResult = await _uploadService.UploadVideoAsync(videoFile, request);
            
            if (!uploadResult.Success)
                return BadRequest(uploadResult.ErrorMessage);

            // Queue for processing (async)
            _ = Task.Run(async () => await _processingService.ProcessVideoAsync(uploadResult.VideoId));

            return Ok(new
            {
                videoId = uploadResult.VideoId,
                status = "uploaded",
                message = "Video uploaded successfully. Processing will begin shortly.",
                processingEstimate = uploadResult.EstimatedProcessingTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video");
            return StatusCode(500, "Upload failed");
        }
    }

    /// <summary>
    /// Check processing status
    /// </summary>
    [HttpGet("status/{videoId}")]
    public async Task<IActionResult> GetProcessingStatus(string videoId)
    {
        var status = await _processingService.GetProcessingStatusAsync(videoId);
        return Ok(status);
    }

    /// <summary>
    /// Get video processing details
    /// </summary>
    [HttpGet("details/{videoId}")]
    public async Task<IActionResult> GetVideoDetails(string videoId)
    {
        var details = await _processingService.GetVideoDetailsAsync(videoId);
        if (details == null)
            return NotFound();

        return Ok(details);
    }
}