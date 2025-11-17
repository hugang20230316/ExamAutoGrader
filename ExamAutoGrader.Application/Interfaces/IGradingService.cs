using ExamAutoGrader.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace ExamAutoGrader.Application.Interfaces;

public interface IGradingService
{
    /// <summary>
    /// AI评分
    /// </summary>
    Task<GradingItemResultDto> GradingWithAIAsync(GradingWithAIItemDto request);

    /// <summary>
    /// AI评分通过模型
    /// </summary>
    Task<GradingWithAIModelResultDto> GradingWithAIModelAsync(GradingWithAIModelDto item);

    /// <summary>
    /// 扫描件进行OCR识别并评分
    /// </summary>
    /// <param name="questionFile"></param>
    /// <param name="answerFile"></param>
    /// <returns></returns>
    Task<GradingWithAIResultDto> OCRGradeAsync(IFormFile questionFile, IFormFile answerFile);

    Task DBTestAsync();
}