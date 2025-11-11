using Microsoft.AspNetCore.Http;

namespace ExamAutoGrader.Domain.Interfaces;

public interface IFileStorageService
{
    Task<FileSaveResult> SaveFileAsync(IFormFile file);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    string GetFileUrl(string filePath);
}

public class FileSaveResult
{
    public string SavedFilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
}