using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Interfaces;

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
    /// <param name="questionId"></param>
    /// <param name="subject"></param>
    /// <param name="stem"></param>
    /// <param name="questionType"></param>
    /// <returns></returns>
    Task<IEnumerable<FeedbackRecord>> GetPotentialMatchesAsync(Guid? questionId,
               string subject,
               string stem,
               EQuestionType? questionType);

    /// <summary>
    /// 通过题干和课程获取指纹信息
    /// </summary>
    /// <param name="stem"></param>
    /// <param name="subject"></param>
    /// <returns></returns>
    Task<FeedbackRecord?> GetByStemAndSubjectAsync(string stem, string subject);

    /// <summary>
    /// 查询列表通过指纹
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<List<FeedbackRecord>> GetByFingerprintAsync(string fingerprint);
}