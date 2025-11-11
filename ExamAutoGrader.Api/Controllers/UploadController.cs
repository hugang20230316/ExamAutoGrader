using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

/// <summary>
/// 文件上传API控制器
/// 职责：处理学生答案图片的上传和存储
/// 支持多种图片格式，提供文件验证和安全性检查
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadController> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="environment">宿主环境</param>
    /// <param name="logger">日志记录器</param>
    public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 上传学生答案图片
    /// HTTP POST: /api/upload
    /// 业务流程：接收文件 → 验证 → 保存 → 返回访问URL
    /// </summary>
    /// <param name="file">上传的图片文件</param>
    /// <returns>文件上传结果</returns>
    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadResult>> UploadImage(IFormFile file)
    {
        _logger.LogInformation("收到文件上传请求，文件名：{FileName}，大小：{FileSize}字节",
            file?.FileName, file?.Length);

        // 检查文件是否存在
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("上传文件为空");
            return BadRequest(new ProblemDetails
            {
                Title = "文件为空",
                Detail = "请选择要上传的文件",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 验证文件大小
        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("文件大小超过限制，文件名：{FileName}，大小：{FileSize}字节",
                file.FileName, file.Length);
            return BadRequest(new ProblemDetails
            {
                Title = "文件过大",
                Detail = $"文件大小不能超过 {MaxFileSize / 1024 / 1024}MB",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 验证文件扩展名
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension) || !_allowedExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("文件格式不支持，文件名：{FileName}，扩展名：{FileExtension}",file.FileName, fileExtension);
            return BadRequest(new ProblemDetails
            {
                Title = "文件格式不支持",
                Detail = $"支持的文件格式：{string.Join(", ", _allowedExtensions)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // 生成唯一文件名
            var fileName = GenerateUniqueFileName(fileExtension);
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads");

            // 确保上传目录存在
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation("创建上传目录：{UploadsFolder}", uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("文件上传成功，保存路径：{FilePath}，原始文件名：{OriginalFileName}",filePath, file.FileName);

            // 生成文件访问URL
            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            // 返回上传结果
            var result = new UploadResult
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                Url = fileUrl,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传过程中发生错误，文件名：{FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "文件上传失败",
                Detail = "文件上传过程中发生错误，请稍后重试",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 生成唯一文件名
    /// 使用GUID确保文件名唯一性，避免冲突
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>唯一文件名</returns>
    private string GenerateUniqueFileName(string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{timestamp}_{guid}{extension}";
    }
}

/// <summary>
/// 文件上传结果
/// 标准化文件上传API响应格式
/// </summary>
public class UploadResult
{
    /// <summary>
    /// 服务器生成的文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件访问URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadedAt { get; set; }
}