using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 用户反馈提交数据传输对象
/// 用于接收用户对AI评分的反馈信息
/// </summary>
public class FeedbackSubmissionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public Guid? QuestionId { get; set; }

    /// <summary>
    /// 题目科目
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    public EQuestionType? QuestionType { get; set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 学生提交的答案内容
    /// 示例："橐驼并不是能够让树木寿命长且茂盛，而是能顺应树木的天性来达到它的本性罢了。"
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// AI给出的原始评分分数
    /// 示例：2 (用户认为此分数不合理)
    /// </summary>
    public float? OriginalScore { get; set; }

    /// <summary>
    /// 用户认为合理的期望分数
    /// 示例：4 (用户认为应该给4分)
    /// </summary>
    public float? ExpectedScore { get; set; }

    /// <summary>
    /// 反馈说明
    /// </summary>
    public string FeedbackComment { get; set; }
}