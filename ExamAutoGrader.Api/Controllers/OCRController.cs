using ExamAutoGrader.Api.Common;
using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

/// <summary>
/// OCR识别API控制器
/// 表现层职责：处理HTTP请求，调用应用服务，返回HTTP响应
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OCRController : ControllerBase
{
    private readonly IOCRProcessingService _ocrProcessingService;
    private readonly ILogger<OCRController> _logger;

    public OCRController(
        IOCRProcessingService ocrProcessingService,
        ILogger<OCRController> logger)
    {
        _ocrProcessingService = ocrProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// 上传并识别图片文字 - 边上传边识别
    /// POST: /api/ocr/upload-and-recognize
    /// </summary>
    [HttpPost("upload-and-recognize")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(OCRResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OCRResultDto>> UploadAndRecognize(IFormFile file)
    {
        try
        {
            var result = await _ocrProcessingService.UploadAndRecognizeAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传识别失败");
            return BadRequest(Util.CreateProblemDetails("上传识别失败", ex.Message));
        }
    }

    /// <summary>
    /// 识别已有文件的文字
    /// POST: /api/ocr/recognize-existing
    /// </summary>
    [HttpPost("recognize-existing")]
    [ProducesResponseType(typeof(OCRResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OCRResultDto>> RecognizeExistingFile([FromBody] RecognizeExistingRequestDto request)
    {
        try
        {
            var result = await _ocrProcessingService.RecognizeExistingFileAsync(request.FilePath, request.OriginalFileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "识别已有文件失败");
            return BadRequest(Util.CreateProblemDetails("识别已有文件失败", ex.Message));
        }
    }

    /// <summary>
    /// 通过URL识别图片文字
    /// POST: /api/ocr/recognize-from-url
    /// </summary>
    [HttpPost("recognize-from-url")]
    [ProducesResponseType(typeof(OCRResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OCRResultDto>> RecognizeFromUrl([FromBody] RecognizeFromUrlRequestDto request)
    {
        try
        {
            var result = await _ocrProcessingService.RecognizeFromUrlAsync(request.ImageUrl);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "URL识别失败");
            return BadRequest(Util.CreateProblemDetails("URL识别失败", ex.Message));
        }
    }

    /// <summary>
    /// 批量上传识别
    /// POST: /api/ocr/batch-upload-recognize
    /// </summary>
    [HttpPost("batch-upload-recognize")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(BatchOCRResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchOCRResultDto>> BatchUploadAndRecognize(List<IFormFile> files)
    {
        try
        {
            var result = await _ocrProcessingService.BatchRecognizeAsync(files);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量上传识别失败");
            return BadRequest(Util. CreateProblemDetails("批量上传识别失败", ex.Message));
        }
    }

    /// <summary>
    /// 批量识别已有文件
    /// POST: /api/ocr/batch-recognize-existing
    /// </summary>
    [HttpPost("batch-recognize-existing")]
    [ProducesResponseType(typeof(BatchOCRResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchOCRResultDto>> BatchRecognizeExisting([FromBody] BatchRecognizeExistingRequestDto request)
    {
        try
        {
            var result = await _ocrProcessingService.BatchRecognizeExistingFilesAsync(request.Files);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量识别已有文件失败");
            return BadRequest(Util.CreateProblemDetails("批量识别已有文件失败", ex.Message));
        }
    }
}