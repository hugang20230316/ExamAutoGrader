using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 考试题目DTO
/// </summary>
public class GradingWithAIDto
{
    /// <summary>
    /// 科目
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public EQuestionType? QuestionType { get; set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分
    /// </summary>
    public float TotalScore { get; set; }

    /// <summary>
    /// 翻译题目项集合
    /// </summary>
    public List<GradingWithAIItemDto> Items { get; set; } = new();
}

/// <summary>
/// 评分项 DTO（单个题目的评分请求）
/// </summary>
public class GradingWithAIItemDto
{
    /// <summary>
    /// 题号（如 "1", "2.1", "A-1" 等）
    /// </summary>
    public string QuestionNumber { get; set; } = string.Empty;

    /// <summary>
    /// 题目ID（可选，用于精确匹配）
    /// </summary>
    public Guid? QuestionId { get; set; }

    /// <summary>
    /// 题干（题目内容）
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 学生答案
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 标准答案（可选，用于参考）
    /// </summary>
    public string? CorrectAnswer { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public float TotalScore { get; set; } = 10.0f;

    /// <summary>
    /// 科目
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 题型
    /// </summary>
    public EQuestionType? QuestionType { get; set; }

}