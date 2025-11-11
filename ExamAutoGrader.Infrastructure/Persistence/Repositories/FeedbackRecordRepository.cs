using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Repositories;
using ExamAutoGrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ExamAutoGrader.Infrastructure.Persistence.Repositories;

/// <summary>
/// 反馈记录仓储实现
/// 在基础设施层实现领域层定义的仓储接口
/// </summary>
public class FeedbackRecordRepository : EfCoreRepository<FeedbackRecord>, IFeedbackRecordRepository
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

    public async Task<IEnumerable<FeedbackRecord>> GetPotentialMatchesAsync(QuestionFingerprint fingerprint)
    {
        var query = _context.FeedbackRecords.AsQueryable();
        // 问题ID筛选
        if (fingerprint.QuestionId.HasValue)
            query = query.Where(x => x.QuestionId == fingerprint.QuestionId);

        // 科目筛选
        if (!string.IsNullOrEmpty(fingerprint.Subject))
            query = query.Where(x => x.Subject == fingerprint.Subject);

        // 题目类型筛选
        if (fingerprint.QuestionType.HasValue)
            query = query.Where(x => x.QuestionType == fingerprint.QuestionType.Value);

        // 文本长度相似过滤（避免对长度差异过大的文本进行复杂计算）
        query = query.Where(x =>
            Math.Abs(x.Stem.Length - fingerprint.Stem.Length) <= fingerprint.Stem.Length * 0.5);

        return await query.ToListAsync();
    }

    public async Task<FeedbackRecord> GetByStemAndSubjectAsync(string stem, string subject) {
       return await _context.FeedbackRecords
            .Where(q => q.Stem == stem && q.Subject == subject)
            .FirstOrDefaultAsync();
    }
}