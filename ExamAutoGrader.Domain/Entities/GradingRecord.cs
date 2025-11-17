using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Interfaces;

namespace ExamAutoGrader.Domain.Entities;

/// <summary>
/// AI评分记录
/// </summary>
public class GradingRecord : AggregateRoot<Guid>, IEntity<Guid>, IAuditable, IAggregateRoot<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public GradingRecord(Guid id) : base(id) { }

    /// <summary>
    /// 聚合根ID
    /// </summary>
    public Guid Id { get; private set; }

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
    /// 评分原因
    /// </summary>
    public string GradingReason { get; private set; } = string.Empty;

    /// <summary>
    /// 域事件集合
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        if (eventItem != null && !_domainEvents.Contains(eventItem))
        {
            _domainEvents.Add(eventItem);
        }
    }

    public void RemoveDomainEvent(IDomainEvent eventItem)
    {
        if (eventItem != null)
        {
            _domainEvents.Remove(eventItem);
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// 从AI评分结果创建（重构：使用基础参数，不依赖 DTO）
    /// </summary>
    public static GradingRecord CreateFromGradingResult(
        Guid? questionId,
        string subject,
        string stem,
        EQuestionType? questionType,
        string studentAnswer,
        float? score,
        string comment)
    {
        return new GradingRecord(Guid.NewGuid())
        { 
            QuestionType = questionType,
            QuestionId = questionId,
            Stem = stem,
            OriginalScore = score,
            StudentAnswer = studentAnswer,
            GradingReason = comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}