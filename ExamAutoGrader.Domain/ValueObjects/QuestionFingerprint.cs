using ExamAutoGrader.Domain.Common;
using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Domain.ValueObjects;

/// <summary>
/// 题目指纹值对象
/// 封装题目的特征信息，用于相似度比较
/// </summary>
public class QuestionFingerprint : ValueObject
{
    /// <summary>
    /// 题目ID（可选，用于精确匹配）
    /// </summary>
    public Guid? QuestionId { get; set; }

    /// <summary>
    /// 科目
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 题干（题目内容）
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 题型
    /// </summary>
    public EQuestionType? QuestionType { get; set; }

    public QuestionFingerprint()
    {
    }

    public QuestionFingerprint(Guid? questionId, string subject, string stem, EQuestionType? questionType)
    {
        QuestionId = questionId;
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Stem = stem ?? throw new ArgumentNullException(nameof(stem));
        QuestionType = questionType;
    }

    protected override IEnumerable<object> GetEqualityComponents()
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
