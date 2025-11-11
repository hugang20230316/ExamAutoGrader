using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 考试题目DTO
/// </summary>
public class GradingExamQuestionDto
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
    public List<GradingExamQuestionItemDto> Items { get; set; } = new();
}

/// <summary>
/// 评分项 DTO（单个题目的评分请求）
/// </summary>
public class GradingExamQuestionItemDto
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

    /// <summary>
    /// 额外评分说明（可选，用于特殊评分要求）
    /// </summary>
    public string? GradingInstructions { get; set; }

    /// <summary>
    /// 附件信息（如图片路径、音频链接等，可选）
    /// </summary>
    public List<string> Attachments { get; set; } = new();

    /// <summary>
    /// 学生ID（可选，用于个性化评分）
    /// </summary>
    public Guid? StudentId { get; set; }

    /// <summary>
    /// 题目难度等级（可选，用于评分权重）
    /// </summary>
    public int? DifficultyLevel { get; set; }

    /// <summary>
    /// 题目知识点标签（可选，用于知识图谱）
    /// </summary>
    public List<string> KnowledgePoints { get; set; } = new();

    /// <summary>
    /// 验证数据完整性
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(QuestionNumber) &&
               !string.IsNullOrWhiteSpace(Stem) &&
               !string.IsNullOrWhiteSpace(StudentAnswer) &&
               TotalScore > 0;
    }

    /// <summary>
    /// 获取题目的唯一标识（用于缓存和匹配）
    /// </summary>
    public string GetUniqueKey()
    {
        return $"{Subject}_{QuestionType}_{Stem.GetHashCode()}_{StudentAnswer.GetHashCode()}";
    }
}