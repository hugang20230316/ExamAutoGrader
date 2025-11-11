using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Parsing;

/// <summary>
/// OCR处理服务实现
/// 重构版本：避免直接依赖IWebHostEnvironment，提高可测试性
/// </summary>
public class OCRProcessingService : IOCRProcessingService
{
    private readonly IOCRService _ocrService;
    private readonly IFileStorageService _fileStorageService; // 使用接口
    private readonly ILogger<OCRProcessingService> _logger;

    public OCRProcessingService(
        IOCRService ocrService,
        IFileStorageService fileStorageService, // 这里应该是IFileStorageService
        ILogger<OCRProcessingService> logger)
    {
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OCRResultDto> UploadAndRecognizeAsync(IFormFile file)
    {
        _logger.LogInformation("开始上传并识别文件：{FileName}", file.FileName);

        // 验证文件
        ValidateFile(file);

        // 保存文件
        var saveResult = await _fileStorageService.SaveFileAsync(file);

        try
        {
            // 构建访问URL并识别
            var recognitionResult = await RecognizeImageAsync(saveResult.FileUrl);

            return new OCRResultDto
            {
                OriginalFileName = file.FileName,
                RecognizedText = recognitionResult.RecognizedText,
                TextLength = recognitionResult.TextLength,
                FileUrl = saveResult.FileUrl,
                FilePath = saveResult.SavedFilePath,
                RecognizedAt = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception)
        {
            // 识别失败时清理文件
            await _fileStorageService.DeleteFileAsync(saveResult.SavedFilePath);
            throw;
        }
    }

    public async Task<OCRResultDto> RecognizeExistingFileAsync(string filePath, string? originalFileName = null)
    {
        _logger.LogInformation("开始识别已有文件：{FilePath}", filePath);

        ValidateFileExtension(filePath);

        // 构建访问URL并识别
        var recognitionResult = await RecognizeImageAsync(filePath);

        return new OCRResultDto
        {
            OriginalFileName = originalFileName ?? Path.GetFileName(filePath),
            RecognizedText = recognitionResult.RecognizedText,
            TextLength = recognitionResult.TextLength,
            FileUrl = filePath,
            FilePath = filePath,
            RecognizedAt = DateTime.UtcNow,
            Success = true
        };
    }

    public async Task<OCRResultDto> RecognizeFromUrlAsync(string imageUrl)
    {
        _logger.LogInformation("开始识别URL图片：{ImageUrl}", imageUrl);

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("图片URL不能为空", nameof(imageUrl));

        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            throw new ArgumentException("图片URL格式无效", nameof(imageUrl));

        var recognitionResult = await RecognizeImageAsync(imageUrl);

        return new OCRResultDto
        {
            RecognizedText = recognitionResult.RecognizedText,
            TextLength = recognitionResult.TextLength,
            FileUrl = imageUrl,
            RecognizedAt = DateTime.UtcNow,
            Success = true
        };
    }

    public async Task<BatchOCRResultDto> BatchRecognizeAsync(List<IFormFile> files)
    {
        _logger.LogInformation("开始批量识别，文件数量：{FileCount}", files.Count);

        var results = new List<OCRResultDto>();
        var savedFiles = new List<string>();

        try
        {
            foreach (var file in files)
            {
                try
                {
                    var result = await UploadAndRecognizeAsync(file);
                    results.Add(result);
                    savedFiles.Add(result.FilePath!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "识别文件失败：{FileName}", file.FileName);
                    results.Add(CreateFailedResult(file.FileName, ex.Message));
                }
            }

            return CreateBatchResult(results, files.Count);
        }
        catch (Exception)
        {
            // 批量处理失败时清理所有已保存的文件
            foreach (var filePath in savedFiles)
            {
                await _fileStorageService.DeleteFileAsync(filePath);
            }
            throw;
        }
    }

    public async Task<BatchOCRResultDto> BatchRecognizeExistingFilesAsync(List<FileInfoDto> fileInfos)
    {
        _logger.LogInformation("开始批量识别已有文件，文件数量：{FileCount}", fileInfos.Count);

        var tasks = fileInfos.Select(async fileInfo =>
        {
            try
            {
                return await RecognizeExistingFileAsync(fileInfo.FilePath, fileInfo.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "识别已有文件失败：{FilePath}", fileInfo.FilePath);
                return CreateFailedResult(fileInfo.OriginalFileName ?? Path.GetFileName(fileInfo.FilePath), ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);
        return CreateBatchResult(results.ToList(), fileInfos.Count);
    }

    #region 私有方法

    private async Task<RecognitionResult> RecognizeImageAsync(string imageUrl)
    {
        _logger.LogDebug("开始OCR识别：{ImageUrl}", imageUrl);
        var recognizedText = await _ocrService.RecognizeTextAsync(imageUrl);
        _logger.LogDebug("OCR识别完成，识别出 {TextLength} 个字符", recognizedText.Length);

        return new RecognitionResult
        {
            RecognizedText = recognizedText,
            TextLength = recognizedText.Length
        };
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("文件不能为空");

        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("文件大小不能超过5MB");

        ValidateFileExtension(file.FileName);
    }

    private void ValidateFileExtension(string fileName)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"不支持的文件格式。支持格式：{string.Join(", ", allowedExtensions)}");
    }

    private OCRResultDto CreateFailedResult(string fileName, string errorMessage) => new()
    {
        OriginalFileName = fileName,
        RecognizedText = $"识别失败：{errorMessage}",
        TextLength = 0,
        Success = false,
        RecognizedAt = DateTime.UtcNow
    };

    private BatchOCRResultDto CreateBatchResult(List<OCRResultDto> results, int totalFiles) => new()
    {
        Results = results,
        TotalFiles = totalFiles,
        SuccessfulRecognitions = results.Count(r => r.Success),
        FailedRecognitions = results.Count(r => !r.Success)
    };

    #endregion
}

// 内部辅助类
internal class RecognitionResult
{
    public string RecognizedText { get; set; } = string.Empty;
    public int TextLength { get; set; }
}