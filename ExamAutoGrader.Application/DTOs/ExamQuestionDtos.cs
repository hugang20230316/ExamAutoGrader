namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 考试题目DTO
/// </summary>
public class ExamQuestionDto
{
    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// 翻译题目项集合
    /// </summary>
    public List<ExamQuestionItemDto> Items { get; set; } = new();
}

/// <summary>
/// 题目项DTO
/// </summary>
public class ExamQuestionItemDto
{
    /// <summary>
    /// 小题编号
    /// </summary>
    public string QuestionNumber { get; set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 学生答案
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分
    /// </summary>
    public int TotalScore { get; set; }
}