using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Domain.ValueObjects;

namespace ExamAutoGrader.Domain.Repositories;

/// <summary>
/// 反馈记录仓储接口
/// 定义领域层需要的数据库操作契约
/// </summary>
public interface IFeedbackRecordRepository : IRepository<FeedbackRecord, Guid>
{
    /// <summary>
    /// 获取潜在匹配的题目
    /// </summary>
    Task<IEnumerable<FeedbackRecord>> GetPotentialMatchesAsync(QuestionFingerprint fingerprint);

    /// <summary>
    /// 通过题干和课程获取指纹信息
    /// </summary>
    /// <param name="stem"></param>
    /// <param name="subject"></param>
    /// <returns></returns>
    Task<FeedbackRecord?> GetByStemAndSubjectAsync(string stem, string subject);
}