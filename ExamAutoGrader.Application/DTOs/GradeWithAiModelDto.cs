namespace ExamAutoGrader.Application.DTOs
{
    /// <summary>
    /// AI评分通过模型DTO
    /// </summary>
    public class GradingWithAIModelDto
    {
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
        public float TotalScore { get; set; }
    }

    /// <summary>
    /// AI评分通过模型结果DTO
    /// </summary>
    public class GradingWithAIModelResultDto
    {
        /// <summary>
        /// 得分
        /// </summary>
        public float? Score { get; set; }

        /// <summary>
        /// 评分说明
        /// </summary>
        public string Comment { get; set; } = string.Empty;
    }
}
