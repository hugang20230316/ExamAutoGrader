using ExamAutoGrader.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace ExamAutoGrader.Application.Interfaces;

/// <summary>
/// OCR处理服务接口
/// 应用层服务：协调文件处理和OCR识别业务逻辑
/// </summary>
public interface IOCRProcessingService
{
    /// <summary>
    /// 上传并识别图片文字（边上传边识别）
    /// </summary>
    Task<OCRResultDto> UploadAndRecognizeAsync(IFormFile file);

    /// <summary>
    /// 识别已有文件的文字
    /// </summary>
    Task<OCRResultDto> RecognizeExistingFileAsync(string filePath, string? originalFileName = null);

    /// <summary>
    /// 通过URL识别图片文字
    /// </summary>
    Task<OCRResultDto> RecognizeFromUrlAsync(string imageUrl);

    /// <summary>
    /// 批量识别多个文件
    /// </summary>
    Task<BatchOCRResultDto> BatchRecognizeAsync(List<IFormFile> files);

    /// <summary>
    /// 批量识别已有文件
    /// </summary>
    Task<BatchOCRResultDto> BatchRecognizeExistingFilesAsync(List<FileInfoDto> fileInfos);
}