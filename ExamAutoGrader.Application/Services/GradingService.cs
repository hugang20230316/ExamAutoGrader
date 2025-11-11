using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using ExamAutoGrader.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamAutoGrader.Application.Services;

public class GradingService : IGradingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GradingService> _logger;
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;
    private readonly IKnowledgeBaseService _knowledgeBase;

    public GradingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GradingService> logger,
        IKnowledgeBaseService knowledgeBase)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AI:ApiKey"] ?? throw new ArgumentException("AI API Key未配置");
        _apiBaseUrl = configuration["AI:ApiBaseUrl"] ?? throw new ArgumentException("AI API BaseUrl未配置");
        _knowledgeBase = knowledgeBase;
    }

    public async Task<GradingResultDto> GradeAsync(GradingExamQuestionDto request)
    {
        var results = new List<GradingItemResultDto>();

        foreach (var item in request.Items)
        {
            var result = await GradeWithAiAsync(item);
            results.Add(result);
        }

        return new GradingResultDto { Results = results };
    }

    private async Task<GradingItemResultDto> GradeWithAiAsync(GradingExamQuestionItemDto item)
    {
        try
        {
            _logger.LogInformation("开始评分: 题号{QuestionNumber}", item.QuestionNumber);

            //1. 从知识库获取
            var existingRecord = await _knowledgeBase.GetByStemAndSubjectAsync(item.Stem, item.Subject);
            string? currentFingerprint = null;
            float[]? currentEmbedding = null;

            if (existingRecord != null)
            {
                currentFingerprint = existingRecord.SemanticFingerprint;
                if (!string.IsNullOrEmpty(existingRecord.EmbeddingVectorJson))
                {
                    currentEmbedding = JsonSerializer.Deserialize<float[]>(existingRecord.EmbeddingVectorJson);
                }
            }

            // 2. 获取相关反馈记录（多层匹配策略）
            var relevantRecords = await _knowledgeBase.GetRelevantRecordsAsync(item.QuestionId,
                item.Stem, item.StudentAnswer, item.Subject, item.QuestionType,
                currentFingerprint, // ✅ 传入当前题目的指纹
                currentEmbedding);

            // 3. 尝试精确答案匹配（性能最高）
            var exactMatch = await TryGetExactAnswerMatchAsync(item.Stem, item.StudentAnswer, currentFingerprint);
            if (exactMatch != null)
            {
                _logger.LogInformation("题号{QuestionNumber}找到精确答案匹配，直接返回历史评分", item.QuestionNumber);
                return new GradingItemResultDto
                {
                    QuestionNumber = item.QuestionNumber,
                    Score = exactMatch.ExpectedScore,
                    Comment = exactMatch.FeedbackReason,
                    Source = "HistoricalExactMatch" // ✅ 标记评分来源
                };
            }

            // 4. 构建提示词（包含历史反馈）
            var prompt = BuildGradingPrompt(item.Stem, item.StudentAnswer, item.CorrectAnswer, item.TotalScore, relevantRecords);

            // 5. 调用 AI 评分
            var response = await CallAiAPI(prompt);
            var result = ParseAiResponse(response, item.QuestionNumber);

            // 6. 【可选】记录本次评分到知识库（用于未来匹配）
            await _knowledgeBase.RecordNewGradingAsync(item, result); // ✅ 记录新反馈

            return ParseAiResponse(response, item.QuestionNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "评分失败: 题号{QuestionNumber}", item.QuestionNumber);
            return new GradingItemResultDto
            {
                QuestionNumber = item.QuestionNumber,
                Score = CalculateBasicScore(item.StudentAnswer, item.TotalScore),
                Comment = "评分系统暂时不可用"
            };
        }
    }

    /// <summary>
    /// 构建提示词 - 使用用户反馈说明
    /// </summary>
    private string BuildGradingPrompt(string stem, string studentAnswer, string? correctAnswer, float totalScore, List<FeedbackRecord> relevantRecords)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("请作为专业阅卷老师进行评分。");

        // 添加用户反馈说明
        if (relevantRecords.Any())
        {
            promptBuilder.AppendLine();
            var feedbackRecordContext = _knowledgeBase.BuildFeedbackRecordContext(relevantRecords);
            promptBuilder.AppendLine(feedbackRecordContext);
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"题目：{stem}");
        promptBuilder.AppendLine($"学生答案：{studentAnswer}");
        promptBuilder.AppendLine($"满分：{totalScore}分");
        promptBuilder.AppendLine("返回JSON：{\"score\": 得分,\"comment\": \"说明\"}");

        return promptBuilder.ToString();
    }

    private async Task<string> CallAiAPI(string prompt)
    {
        var requestBody = new
        {
            model = "qwen-turbo",
            input = new
            {
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            },
            parameters = new
            {
                result_format = "text"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(_apiBaseUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"通义API调用失败: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<AiAPIResponse>(responseContent);

        return apiResponse?.Output?.Text ?? throw new ApplicationException("通义API返回空结果");
    }

    private GradingItemResultDto ParseAiResponse(string responseText, string questionNumber)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                responseText, @"\{.*\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                var result = JsonSerializer.Deserialize<AiGradingResult>(jsonMatch.Value);
                if (result != null)
                {
                    return new GradingItemResultDto
                    {
                        QuestionNumber = questionNumber,
                        Score = result.Score,
                        Comment = result.Comment
                    };
                }
            }

            throw new ApplicationException("解析通义API响应失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析通义评分结果失败");
            throw;
        }
    }

    private float CalculateBasicScore(string studentAnswer, float totalScore)
    {
        // 基础降级评分逻辑
        if (string.IsNullOrWhiteSpace(studentAnswer)) return 0;
        if (studentAnswer.Length < 10) return 1;
        return totalScore / 2; // 返回一半分数作为基础分
    }

    /// <summary>
    /// 尝试获取精确答案匹配（最高优先级）
    /// </summary>
    private async Task<FeedbackRecord?> TryGetExactAnswerMatchAsync(string stem, string studentAnswer, string currentFingerprint)
    {
        // 1. 先按指纹匹配（最快）
        var exactMatches = await _knowledgeBase.GetExactAnswerMatchesAsync(currentFingerprint, studentAnswer);

        if (exactMatches.Any())
        {
            // 返回最高分的匹配（或最近的）
            return exactMatches.OrderByDescending(r => r.ExpectedScore).First();
        }

        // 2. 如果指纹匹配失败，再用题干精确匹配（备选）
        exactMatches = await _knowledgeBase.GetExactAnswerMatchesAsync(stem, studentAnswer);
        return exactMatches.FirstOrDefault();
    }
}

// 通义API响应模型
public class AiAPIResponse
{
    [JsonPropertyName("output")]
    public AiOutput? Output { get; set; }
}

public class AiOutput
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

// 通义评分结果模型
public class AiGradingResult
{
    [JsonPropertyName("questionNumber")]
    public string QuestionNumber { get; set; }

    [JsonPropertyName("score")]
    public float? Score { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}