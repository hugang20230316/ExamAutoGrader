namespace ExamAutoGrader.Domain.ValueObjects
{
    /// <summary>
    /// 相似度比较结果值对象
    /// 封装题目相似度比较的详细结果信息
    /// </summary>
    public class SimilarityResult
    {
        /// <summary>
        /// 是否判定为重复或相似题目
        /// true表示找到相似题目，false表示可能是新题目
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// 相似度分数
        /// 范围0-1，1表示完全相似，0表示完全不相似
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// 匹配类型
        /// 描述匹配的级别：精确匹配、高度相似、一般相似、不相似等
        /// </summary>
        public string MatchType { get; set; } = "None";

        /// <summary>
        /// 匹配到的题目ID
        /// 如果找到相似题目，存储匹配题目的ID
        /// </summary>
        public Guid? MatchedQuestionId { get; set; }

        /// <summary>
        /// 匹配原因说明
        /// 详细描述为什么判定为相似，包括使用的算法和关键特征
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}