using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Persistence.Repositories;

/// <summary>
/// 反馈记录仓储实现
/// 在基础设施层实现领域层定义的仓储接口
/// </summary>
public class FeedbackRecordRepository : EfCoreRepository<FeedbackRecord, Guid>, IFeedbackRecordRepository
{
    private readonly ExamAutoGraderDbContext _context;
    private readonly ILogger<FeedbackRecordRepository> _logger;

    public FeedbackRecordRepository(
        ExamAutoGraderDbContext context,
        ILogger<FeedbackRecordRepository> logger)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<FeedbackRecord>> GetPotentialMatchesAsync(Guid? questionId, string subject, string stem, EQuestionType? questionType)
    {
        var query = _context.FeedbackRecords.AsQueryable();
        // 问题ID筛选
        if (questionId.HasValue)
            query = query.Where(x => x.QuestionId == questionId);

        // 科目筛选
        if (!string.IsNullOrEmpty(subject))
            query = query.Where(x => x.Subject == subject);

        // 题目类型筛选
        if (questionType.HasValue)
            query = query.Where(x => x.QuestionType == questionType.Value);

        // 文本长度相似过滤（避免对长度差异过大的文本进行复杂计算）
        query = query.Where(x => Math.Abs(x.Stem.Length - stem.Length) <= stem.Length * 0.5);

        return await query.ToListAsync();
    }

    public async Task<FeedbackRecord> GetByStemAndSubjectAsync(string stem, string subject)
    {
        return await _context.FeedbackRecords.Where(q => q.Stem == stem && q.Subject == subject).FirstOrDefaultAsync();
    }


    public async Task<List<FeedbackRecord>> GetByFingerprintAsync(string fingerprint)
    {
        return await _context.FeedbackRecords.Where(q => q.SemanticFingerprint == fingerprint).ToListAsync();
    }
}