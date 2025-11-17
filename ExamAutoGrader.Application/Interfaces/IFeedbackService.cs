using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;

namespace ExamAutoGrader.Application.Interfaces
{
    public interface IFeedbackService
    {
        /// <summary>
        /// 从用户反馈中学习
        /// </summary>
        Task SubmitFeedbackAsync(FeedbackSubmissionDto dto, CancellationToken ct = default);

        /// <summary>
        /// 获取相关反馈记录
        /// </summary>
        Task<List<FeedbackRecord>> GetRelevantRecordsAsync(
            Guid? questionId,
            string stem,
            string studentAnswer,
            string subject,
            EQuestionType? questionType); 


        /// <summary>
        /// 尝试获取精确答案匹配（用于快速返回）
        /// </summary>
        Task<List<FeedbackRecord>> GetExactAnswerMatchesAsync(string fingerprintOrStem, string studentAnswer);

        /// <summary>
        /// 构建上下文
        /// </summary>
        string BuildFeedbackRecordContext(List<FeedbackRecord> relevantRecords);

        /// <summary>
        /// 记录新的评分结果（用于未来匹配）
        /// </summary>
        Task RecordNewGradingAsync(GradingWithAIItemDto item, GradingItemResultDto result);
    }
}
