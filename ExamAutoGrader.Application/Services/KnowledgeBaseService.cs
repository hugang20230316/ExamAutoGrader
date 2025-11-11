using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Attributes;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Repositories;
using ExamAutoGrader.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ExamAutoGrader.Application.Services;

/// <summary>
/// 知识库服务 - 存储用户反馈
/// </summary>
[UnitOfWork]
public class KnowledgeBaseService : ScopedServiceBase, IKnowledgeBaseService
{
    // 保存 Scoped 的 IServiceProvider（当前请求作用域的容器）
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly IFeedbackRecordRepository _feedbackRecordRepository;

    private readonly ILlmService _llmService;
    private readonly IEmbeddingService _embeddingService;

    public KnowledgeBaseService(
        IServiceProvider serviceProvider,
        ILogger<KnowledgeBaseService> logger,
        IFeedbackRecordRepository feedbackRecordRepository,
        ILlmService llmService,
        IEmbeddingService embeddingService) : base(serviceProvider)
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
            dto.ExpectedScore,
            dto.FeedbackComment);

        // 2. 【异步】生成语义指纹和向量（不影响用户响应）
        //_ = Task.Run(async () =>
        //{
            try
            {
            
                await _feedbackRecordRepository.AddAsync(feedbackRecord);
                // 调用大模型（如 GPT-4o / Claude / 本地 LLM）
                var fingerprint = await _llmService.GenerateSemanticFingerprintAsync(dto.Stem, dto.Subject);
                var embedding = await _embeddingService.GetEmbeddingAsync(dto.Stem);

                // 更新数据库
                feedbackRecord.SemanticFingerprint = fingerprint;
                feedbackRecord.EmbeddingVectorJson = JsonSerializer.Serialize(embedding);

                _feedbackRecordRepository.Update(feedbackRecord);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to generate semantic tags for feedback {Id}: {Error}", feedbackRecord.Id, ex.Message);
                // 失败也不影响主流程，后续可重试
            }
        //});

        _logger.LogInformation("学习题干 {Stem} 的反馈，记录ID: {RecordId}", dto.Stem, feedbackRecord.Id);
    }

    /// <summary>
    /// 构建学习上下文
    /// </summary>
    public string BuildFeedbackRecordContext(List<FeedbackRecord> relevantRecords)
    {
        if (!relevantRecords.Any())
            return "请基于题目内容和学生答案质量进行客观评分。";

        var context = new StringBuilder();
        context.AppendLine("参考以下用户反馈经验：");

        foreach (var record in relevantRecords)
        {
            context.AppendLine($"- {record.FeedbackReason}");
        }

        return context.ToString();
    }

    public async Task<List<FeedbackRecord>> GetRelevantRecordsAsync(
        Guid? questionId, string stem, string studentAnswer, string subject, EQuestionType? questionType, CancellationToken ct = default)
    {
        var results = new List<FeedbackRecord>();

        // 1. 优先匹配已知数据，避免直接调用AI
        var sameQuestionRecords = (await _feedbackRecordRepository
            .GetPotentialMatchesAsync(new QuestionFingerprint
            {
                QuestionId = questionId,
                Subject = subject,
                Stem = stem,
                QuestionType = questionType
            })).ToList();

        _logger.LogInformation($"科目{studentAnswer}类型{questionType}找到{sameQuestionRecords.Count}条精确匹配记录");
        return sameQuestionRecords;
    }

    public Task<List<FeedbackRecord>> GetRelevantRecordsAsync(Guid? questionId, string stem, string studentAnswer, string subject, EQuestionType? questionType, string? currentFingerprint = null, float[]? currentEmbedding = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<FeedbackRecord>> GetExactAnswerMatchesAsync(string fingerprintOrStem, string studentAnswer)
    {
        throw new NotImplementedException();
    }

    public async Task RecordNewGradingAsync(GradingExamQuestionItemDto item, GradingItemResultDto result)
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
                result.Score,
                result.Comment,
                currentFingerprint,
                currentEmbedding);

            // 异步保存（不影响当前评分流程）
            await _feedbackRecordRepository.AddAsync(gradingRecord);

            _logger.LogInformation("记录新评分结果：题号{QuestionNumber}，得分{Score}，记录ID{Id}",
                item.QuestionNumber, result.Score, gradingRecord.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "记录新评分结果失败：题号{QuestionNumber}", item.QuestionNumber);
            // 失败不影响当前评分流程
        }
    }

    public Task<FeedbackRecord> GetByStemAndSubjectAsync(string stem, string subject)
    {
        return _feedbackRecordRepository.GetByStemAndSubjectAsync(stem, subject);
    }
}