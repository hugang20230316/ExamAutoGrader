using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ExamAutoGrader.Application.Services;

/// <summary>
/// 用户反馈服务 
/// </summary>
[UnitOfWork]
public class FeedbackService : IFeedbackService
{
    private readonly ILogger<FeedbackService> _logger;
    private readonly IFeedbackRecordRepository _feedbackRecordRepository;

    private readonly ILlmService _llmService;
    private readonly IEmbeddingService _embeddingService;

    public FeedbackService(
        IServiceProvider serviceProvider,
        ILogger<FeedbackService> logger,
        IFeedbackRecordRepository feedbackRecordRepository,
        ILlmService llmService,
        IEmbeddingService embeddingService)
    {
        _logger = logger;
        _feedbackRecordRepository = feedbackRecordRepository;
        _llmService = llmService;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// 从用户反馈中学习
    /// </summary>
    public async Task SubmitFeedbackAsync(FeedbackSubmissionDto dto, CancellationToken ct = default)
    {
        // 使用领域模型的工厂方法创建聚合根
        var feedbackRecord = FeedbackRecord.CreateFromFeedback(
            dto.QuestionType,
            dto.Stem,
            dto.Subject,
            dto.StudentAnswer,
            dto.Score,
            dto.ExpectedScore,
            dto.FeedbackComment);
        /*
        // 先生成语义指纹和向量，再保存到数据库
        var fingerprint = await _llmService.GenerateSemanticFingerprintAsync(dto.Stem, dto.Subject, ct);
        var embedding = await _embeddingService.GetEmbeddingAsync(dto.Stem, ct);
        // 设置生成的数据
        feedbackRecord.SemanticFingerprint = fingerprint;
        feedbackRecord.EmbeddingVectorJson = JsonSerializer.Serialize(embedding);
        */

        await _feedbackRecordRepository.AddAsync(feedbackRecord, ct);
        _logger.LogInformation("学习题干 {Stem} 的反馈，记录ID: {RecordId}", dto.Stem, feedbackRecord.Id);
    }

    /// <summary>
    /// 构建学习上下文
    /// </summary>
    public string BuildFeedbackRecordContext(List<FeedbackRecord> relevantRecords)
    {
        if (relevantRecords.Count == 0)
            return "请基于题目内容和学生答案质量进行客观评分。";

        var context = new StringBuilder();
        context.AppendLine("参考以下用户反馈经验：");

        foreach (var record in relevantRecords)
        {
            context.AppendLine($"- {record.FeedbackReason}");
        }

        return context.ToString();
    }

    public async Task<List<FeedbackRecord>> GetRelevantRecordsAsync(Guid? questionId, string stem, string studentAnswer, string subject, EQuestionType? questionType)
    {
        return (await _feedbackRecordRepository.GetPotentialMatchesAsync(questionId, subject, stem, questionType)).ToList();
    }

    public async Task<List<FeedbackRecord>> GetExactAnswerMatchesAsync(string fingerprintOrStem, string studentAnswer)
    {
        if (string.IsNullOrWhiteSpace(studentAnswer))
            return [];

        // 1. 先按语义指纹找相同题目的记录
        var sameQuestionRecords = await _feedbackRecordRepository.GetByFingerprintAsync(fingerprintOrStem);

        // 2. 快速匹配答案
        var exactMatches = sameQuestionRecords.Where(record => IsQuickAnswerMatch(studentAnswer, record.StudentAnswer)).ToList();

        return exactMatches;
    }

    public async Task RecordNewGradingAsync(GradingWithAIItemDto item, GradingItemResultDto result)
    {
        try
        {
            // 生成当前题目的指纹和向量（用于未来匹配）
            var currentFingerprint = await _llmService.GenerateSemanticFingerprintAsync(item.Stem, item.Subject);
            var currentEmbedding = await _embeddingService.GetEmbeddingAsync(item.Stem);

            // 创建新的评分记录（通过 Application 层调用 Domain 实体的静态方法）
            var gradingRecord = FeedbackRecord.CreateFromGradingResult(
                item.QuestionId,
                item.Subject,
                item.Stem,
                item.QuestionType,
                item.StudentAnswer,
                item.TotalScore,
                result.Score,
                result.Comment,
                currentFingerprint,
                currentEmbedding);

            await _feedbackRecordRepository.AddAsync(gradingRecord);

            _logger.LogInformation("记录新评分结果：题号{QuestionNumber}，得分{Score}，记录ID{Id}",item.QuestionNumber, result.Score, gradingRecord.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "记录新评分结果失败：题号{QuestionNumber}", item.QuestionNumber);
        }
    }

    /// <summary>
    /// 快速答案匹配：清理后完全相等 或 包含关系
    /// </summary>
    private bool IsQuickAnswerMatch(string answer1, string answer2)
    {
        if (string.IsNullOrWhiteSpace(answer1) || string.IsNullOrWhiteSpace(answer2))
            return false;

        // 清理文本：移除空格标点，转小写
        var clean1 = new string([.. answer1.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c))]).ToLower();
        var clean2 = new string([.. answer2.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c))]).ToLower();

        // 完全相等 或 互相包含
        return clean1 == clean2 || clean1.Contains(clean2) || clean2.Contains(clean1);
    }

}