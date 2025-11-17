using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 考试题目DTO
/// </summary>
public class ExamQuestionDto
{
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


/// <summary>
/// 考试题目实体类
/// 表示从OCR识别结果中解析出的完整题目信息
/// </summary>
public class ExamParseQuestionDto
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
    public List<ExamParseQuestionItemDto> Items { get; set; } = new();
}

/// <summary>
/// 翻译题目项实体类
/// 表示翻译题目中的单个小题，包含原文和学生作答
/// </summary>
public class ExamParseQuestionItemDto
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
    /// 题目总分
    /// 从题目文本中自动提取的分数值
    /// 例如：10分、20分等
    /// </summary>
    public float TotalScore { get; set; }

    /// <summary>
    /// 学生翻译答案
    /// 学生作答的现代汉语翻译内容
    /// 例如："橐驼并不是能够让树木寿命长且茂盛，而是能顺应树木的天性来达到它的本性罢了"
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;
}

/// <summary>
/// 考试题目实体类
/// 表示从OCR识别结果中解析出的完整题目信息
/// </summary>
public class ExamQuestionAnswerDto
{
    /// <summary>
    /// 题目答案集合
    /// </summary>
    public List<ExamQuestionAnswerItemDto> Items { get; set; } = new();
}

/// <summary>
/// 翻译题目项实体类
/// 表示翻译题目中的单个小题，包含学生作答
/// </summary>
public class ExamQuestionAnswerItemDto
{
    /// <summary>
    /// 小题编号
    /// 例如：1、2、3等
    /// 对应题目中的(1)、(2)等编号
    /// </summary>
    public string QuestionNumber { get; set; }

    /// <summary>
    /// 学生翻译答案
    /// 学生作答的现代汉语翻译内容
    /// 例如："橐驼并不是能够让树木寿命长且茂盛，而是能顺应树木的天性来达到它的本性罢了"
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;
}