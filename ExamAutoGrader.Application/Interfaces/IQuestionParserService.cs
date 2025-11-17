using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Domain.Entities;

namespace ExamAutoGrader.Application.Interfaces;

public interface IQuestionParserService
{
    /// <summary>
    /// 从OCR识别结果中解析题目信息（通用方法）
    /// </summary>
    /// <param name="ocrText">OCR识别出的原始文本</param>
    /// <returns>结构化的题目信息</returns>
    Task<ExamQuestionDto> ParseQuestionFromOCRResultAsync(string ocrText);

    /// <summary>
    /// 从OCR识别结果中解析答题信息（通用方法）
    /// </summary>
    /// <param name="ocrText">OCR识别出的原始文本</param>
    /// <returns>结构化的题目信息</returns>
    Task<ExamQuestionAnswerDto> ParseQuestionAnswerFromOCRResultAsync(string ocrText);
}