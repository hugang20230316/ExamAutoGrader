namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 评分结果DTO
/// </summary>
public class GradingWithAIResultDto
{
    /// <summary>
    /// 评分结果列表
    /// </summary>
    public List<GradingItemResultDto> Results { get; set; } = new();
}

/// <summary>
/// 评分项结果DTO
/// </summary>
public class GradingItemResultDto
{
    /// <summary>
    /// 题号
    /// </summary>
    public string QuestionNumber { get; set; } = string.Empty;

    /// <summary>
    /// 得分
    /// </summary>
    public float? Score { get; set; }

    /// <summary>
    /// 评分说明
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 标记评分来源
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
