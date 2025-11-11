using ExamAutoGrader.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Similarity;

public class SimpleFileStorageService : IFileStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SimpleFileStorageService> _logger;

    public SimpleFileStorageService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SimpleFileStorageService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<FileSaveResult> SaveFileAsync(IFormFile file)
    {
        var basePath = Directory.GetCurrentDirectory();
        var uploadsFolder = Path.Combine(basePath, "wwwroot", "uploads", "ocr-temp");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogInformation("创建文件存储目录：{UploadsFolder}", uploadsFolder);
        }

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = GetFileUrl(filePath);

        return new FileSaveResult
        {
            SavedFilePath = filePath,
            FileName = fileName,
            FileUrl = fileUrl
        };
    }

    public string GetFileUrl(string filePath)
    {
        var basePath = Directory.GetCurrentDirectory();
        var wwwrootPath = Path.Combine(basePath, "wwwroot");

        // 检查文件是否在wwwroot目录下
        if (filePath.StartsWith(wwwrootPath))
        {
            var relativePath = filePath.Replace(wwwrootPath, "").Replace("\\", "/");
            var request = _httpContextAccessor.HttpContext?.Request;

            if (request != null)
            {
                return $"{request.Scheme}://{request.Host}{relativePath}";
            }
        }

        // 如果文件不在wwwroot目录，返回文件路径（用于本地文件识别）
        return filePath;
    }

    // 新增方法：检查文件是否为本地路径
    public bool IsLocalFilePath(string path)
    {
        return Path.IsPathRooted(path) && !path.StartsWith("http");
    }

    // 其他方法保持不变...
    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("已删除文件：{FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "删除文件失败：{FilePath}", filePath);
            throw;
        }
        await Task.CompletedTask;
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }
}