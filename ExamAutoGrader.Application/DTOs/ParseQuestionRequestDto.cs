using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 解析题目请求DTO
/// </summary>
public class ParseQuestionRequestDto
{
    /// <summary>
    /// OCR识别出的原始文本
    /// </summary>
    public string OCRText { get; set; } = string.Empty;
}

/// <summary>
/// 从图片解析题目请求DTO
/// </summary>
public class ParseFromImageRequestDto
{
    /// <summary>
    /// 图片文件路径
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
}

/// <summary>
/// 提取翻译题目项请求DTO
/// </summary>
public class ExtractTranslationItemsRequestDto
{
    /// <summary>
    /// OCR识别出的原始文本
    /// </summary>
    public string OCRText { get; set; } = string.Empty;
}

/// <summary>
/// 题目解析结果DTO
/// </summary>
public class QuestionParseResultDto
{
    public bool Success { get; set; }
    public string? OCRText { get; set; }
    public ExamQuestionDto? Question { get; set; }
    public string? Error { get; set; }
}
