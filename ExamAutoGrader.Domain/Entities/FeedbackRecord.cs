using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Interfaces;
using System.Text.Json;

namespace ExamAutoGrader.Domain.Entities;

/// <summary>
/// 反馈记录
/// </summary>
public class FeedbackRecord : AggregateRoot<Guid>, IEntity<Guid>, IAuditable, IAggregateRoot<Guid>
{
    public FeedbackRecord(Guid id) : base(id) { }

    /// <summary>
    /// 题目ID
    /// </summary>
    public Guid? QuestionId { get; private set; }

    /// <summary>
    /// 科目
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public EQuestionType? QuestionType { get; private set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; private set; } = string.Empty;

    /// <summary>
    /// 标准答案
    /// 用于答案一致性验证
    /// </summary>
    public string StandardAnswer { get; private set; } = string.Empty;

    /// <summary>
    /// 学生答案
    /// </summary>
    public string StudentAnswer { get; private set; } = string.Empty;

    /// <summary>
    /// AI给分
    /// </summary>
    public float? OriginalScore { get; private set; }

    /// <summary>
    /// 题目总分
    /// </summary>
    public float? Score { get; private set; }

    /// <summary>
    /// 建议评分
    /// </summary>
    public float? ExpectedScore { get; private set; }

    /// <summary>
    /// 反馈说明
    /// </summary>
    public string FeedbackReason { get; private set; } = string.Empty;

    /// <summary>
    /// 语义指纹（由大模型或规则生成）
    /// 示例: "math.derivative.polynomial.at_point"
    /// </summary>
    public string SemanticFingerprint { get; private set; } = string.Empty;

    /// <summary>
    /// 题干的 Embedding 向量（JSON 格式存储）
    /// 示例: "[0.12, -0.45, ..., 0.89]" (1536维)
    /// </summary>
    public string EmbeddingVectorJson { get; private set; } = string.Empty;

    /// <summary>
    /// 评分来源（历史反馈/新评分/AI评分）
    /// </summary>
    public string GradingSource { get; private set; } = "HistoricalFeedback";

    /// <summary>
    /// 是否为自动评分记录（用于区分人工反馈和AI评分）
    /// </summary>
    public bool IsAutoGraded { get; private set; }

    /// <summary>
    /// AI评分置信度（0-1）
    /// </summary>
    public float? AiConfidence { get; private set; }

    /// <summary>
    /// 评分时间戳
    /// </summary>
    public DateTime GradedAt { get; private set; }

    // 工厂方法
    public static FeedbackRecord CreateFromFeedback(
        EQuestionType? questionType,
        string stem,
        string subject,
        string studentAnswer,
        float? score,
        float? expectedScore,
        string feedbackComment)
    {
        var record = new FeedbackRecord(Guid.NewGuid())
        {
            Stem = stem,
            Subject = subject,
            StudentAnswer = studentAnswer,
            Score = score,
            ExpectedScore = expectedScore,
            QuestionType = questionType
        };

        record.GenerateFeedbackReason(record.ExpectedScore, record.StudentAnswer, feedbackComment);

        return record;
    }


    /// <summary>
    /// 从评分结果创建（重构：使用基础参数，不依赖 DTO）
    /// </summary>
    public static FeedbackRecord CreateFromGradingResult(
        Guid? questionId,
        string subject,
        string stem,
        EQuestionType? questionType,
        string studentAnswer,
        float? score,
        float? expectedScore,
        string comment,
        string currentFingerprint,
        float[] currentEmbedding)
    {
        return new FeedbackRecord(Guid.NewGuid())
        { 
            QuestionType = questionType,
            QuestionId = questionId,
            Stem = stem,
            StudentAnswer = studentAnswer,
            Score = score,
            ExpectedScore = expectedScore,
            FeedbackReason = comment,
            GradingSource = "AutoGraded",
            IsAutoGraded = true,
            GradedAt = DateTime.UtcNow,
            SemanticFingerprint = currentFingerprint,
            EmbeddingVectorJson = JsonSerializer.Serialize(currentEmbedding),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expectedScore"></param>
    /// <param name="studentAnswer"></param>
    /// <param name="feedbackComment"></param>
    private void GenerateFeedbackReason(float? expectedScore,string studentAnswer,string feedbackComment)
    {
        FeedbackReason = string.IsNullOrEmpty(feedbackComment) ? $"用户认为应该给{expectedScore}分"
            : $"用户提议当学生答题类似于：{studentAnswer}时，评分建议给:{expectedScore}分 ，原因：{feedbackComment}。";
    }

    protected IEnumerable<object> GetEqualityComponents()
    {
        yield return QuestionId ?? Guid.Empty;
        yield return Subject;
        yield return Stem;
        yield return QuestionType ?? EQuestionType.Unknown;
    }

    public override string ToString()
    {
        return $"{Subject}-{QuestionType}-{Stem}";
    }
}