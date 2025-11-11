using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Domain.Entities;

/// <summary>
/// 考试题目实体类
/// 表示从OCR识别结果中解析出的完整题目信息
/// </summary>
public class ExamQuestion
{
    /// <summary>
    /// 题目编号
    /// 例如："8"
    /// 由AI自动识别或规则推断得出
    /// </summary>
    public string QuestionNumber { get; set; } = string.Empty;

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
    /// 从题目文本中自动提取的分数值
    /// 例如：10分、20分等
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// 题目项集合
    /// 当题目类型为翻译题时，包含多个小题的翻译项
    /// 每个TranslationItem代表一个小题
    /// </summary>
    public List<ExamQuestionItem> Items { get; set; } = new();
}

/// <summary>
/// 翻译题目项实体类
/// 表示翻译题目中的单个小题，包含原文和学生作答
/// </summary>
public class ExamQuestionItem
{
    /// <summary>
    /// 小题编号
    /// 例如：1、2、3等
    /// 对应题目中的(1)、(2)等编号
    /// </summary>
    public string QuestionNumber { get; set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 学生翻译答案
    /// 学生作答的现代汉语翻译内容
    /// 例如："橐驼并不是能够让树木寿命长且茂盛，而是能顺应树木的天性来达到它的本性罢了"
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 标准答案（可选）
    /// 题目的标准参考答案，用于自动评分对比
    /// 在后续评分流程中使用
    /// </summary>
    public string? CorrectAnswer { get; set; }

    /// <summary>
    /// 得分 
    /// 该小题的得分，在评分完成后填充
    /// 初始值为0，评分后更新为实际得分
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// 题目总分
    /// 从题目文本中自动提取的分数值
    /// 例如：10分、20分等
    /// </summary>
    public float TotalScore { get; set; }
}