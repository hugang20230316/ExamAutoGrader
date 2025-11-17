using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamAutoGrader.Application.Services;

[UnitOfWork]
public class GradingService : IGradingService
{
    private readonly ILogger<GradingService> _logger;
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;
    private readonly IFeedbackService _feedbackService;
    private readonly IRepository<GradingRecord, Guid> _gradingRecordRepository;
    private readonly IOCRProcessingService _ocrProcessingService;
    private readonly IQuestionParserService _questionParserService;
    private readonly IHttpClientFactory _httpClientFactory; // 改为工厂
    private readonly string _gradingModelApiUrl; // ← 新增

    public GradingService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<GradingService> logger,
        IFeedbackService knowledgeBase,
        IOCRProcessingService ocrProcessingService,
        IQuestionParserService questionParserService,
        IHttpClientFactory httpClientFactory,
        IRepository<GradingRecord, Guid> gradingRecordRepository)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DashScope:ApiKey"] ?? throw new ArgumentException("DashScope API Key未配置");
        _apiBaseUrl = configuration["DashScope:ApiBaseUrl"] ?? throw new ArgumentException("DashScope APIBaseUrl Key未配置");
        _gradingModelApiUrl = configuration["GradingModel:ApiUrl"] ?? throw new ArgumentException("GradingModel ApiUrl Key未配置");
        _feedbackService = knowledgeBase;
        _ocrProcessingService = ocrProcessingService;
        _questionParserService = questionParserService;
        _gradingRecordRepository = gradingRecordRepository;
    }

    public async Task DBTestAsync()
    {
        // 6. 【可选】记录本次评分到知识库（用于未来匹配）
        await _gradingRecordRepository.AddAsync(GradingRecord.CreateFromGradingResult(new Guid(), "", "", Domain.Enums.EQuestionType.MultipleChoice, "", null, "")); // ✅ 记录新反馈
    }

    public async Task<GradingWithAIResultDto> OCRGradeAsync(IFormFile questionFile, IFormFile answerFile)
    {
        /*
        var ocrQuestionOutput = await _ocrProcessingService.UploadAndRecognizeAsync(questionFile);

        var ocrQuestionParseOutput = await _questionParserService.ParseQuestionFromOCRResultAsync(ocrQuestionOutput.RecognizedText);

        var ocrAnswerOutput = await _ocrProcessingService.UploadAndRecognizeAsync(answerFile);

        var ocrAnswerParseOutput = await _questionParserService.ParseQuestionAnswerFromOCRResultAsync(ocrAnswerOutput.RecognizedText);

        ocrQuestionParseOutput.Items.ForEach(item =>
        {
            var answerItem = ocrQuestionParseOutput.Items.FirstOrDefault(a => a.QuestionNumber == item.QuestionNumber);
            if (answerItem != null)
            {
                item.StudentAnswer = ocrAnswerParseOutput.Items.FirstOrDefault(m => m.QuestionNumber == item.QuestionNumber)?.StudentAnswer;
            }
        });
        */
        var ocrQuestionParseOutput = JsonSerializer.Deserialize<ExamQuestionDto>(@"
        {
            ""QuestionNumber"": ""13"",
            ""QuestionType"": 3,
            ""Stem"": ""把材料中画横线的句子翻译成现代汉语。"",
            ""TotalScore"": 8,
            ""Items"": [
            {
                ""QuestionNumber"": ""13(1)"",
                ""Stem"": ""比年如丹漆、石青之类,所司不究物产,概下郡县征之。"",
                ""TotalScore"": 4,
                ""StudentAnswer"": ""就像近几年丹漆,石这类(物产)专门的官员没有 究查物产, 大概都命令下面的郡县征收。""
            },
            {
                ""QuestionNumber"": ""13(2)"",
                ""Stem"": ""州县劳扰,百姓逃窜。尔其申饬有司,以此为戒。"",
                ""TotalScore"": 4,
                ""StudentAnswer"": ""州县长官骚扰,百姓逃亡流窜。你要向专门的官员 申诉整饬, 把这件事当成教训。""
            }
            ]
        }");

        if (ocrQuestionParseOutput == null || ocrQuestionParseOutput.Items == null)
        {
            throw new Exception("OCR题目解析结果为空");
        }

        foreach (var item in ocrQuestionParseOutput.Items)
        {
            await GradingWithAIAsync(new GradingWithAIItemDto
            {
                QuestionNumber = item.QuestionNumber,
                Stem = item.Stem,
                StudentAnswer = item.StudentAnswer,
                TotalScore = item.TotalScore,
                QuestionType = ocrQuestionParseOutput.QuestionType
            }); // 逐个执行，不并发
        }

        return new GradingWithAIResultDto { };
    }

    public async Task<GradingItemResultDto> GradingWithAIAsync(GradingWithAIItemDto item)
    {
        try
        {
            _logger.LogInformation("开始评分: 题号{QuestionNumber}", item.QuestionNumber);

            // 2. 获取相关反馈记录（多层匹配策略）
            var relevantRecords = await _feedbackService.GetRelevantRecordsAsync(item.QuestionId,
                item.Stem, item.StudentAnswer, item.Subject, item.QuestionType);

            // 4. 构建提示词（包含历史反馈）
            var prompt = BuildGradingPrompt(item.Stem, item.StudentAnswer, item.CorrectAnswer, item.TotalScore, relevantRecords);

            // 5. 调用 AI 评分
            var aiGradingResponse = await CallAiAPI(prompt);
            var aiGradingResult = ParseAiResponse(aiGradingResponse, item.QuestionNumber);

            // 6. 【可选】记录本次评分到知识库（用于未来匹配）
            await _gradingRecordRepository.AddAsync(GradingRecord.CreateFromGradingResult(item.QuestionId, item.Subject, item.Stem,
                item.QuestionType, item.StudentAnswer, aiGradingResult.Score, aiGradingResult.Comment)
            ); // ✅ 记录新反馈

            return ParseAiResponse(aiGradingResponse, item.QuestionNumber);
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


    public async Task<GradingWithAIModelResultDto> GradingWithAIModelAsync(GradingWithAIModelDto item)
    {
        // ✅ 调用的是“训练好的模型”，不是通义千问
        var result = await CallTrainedGradingModelAsync(
            item.Stem,
            item.StudentAnswer,
            item.TotalScore
        );

        // ... 记录结果 ...
        return result;
    }

    public Task<GradingWithAIResultDto> GradeAsync(GradingWithAIResultDto request)
    {
        throw new NotImplementedException();
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
            var feedbackRecordContext = _feedbackService.BuildFeedbackRecordContext(relevantRecords);
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

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await httpClient.PostAsync(_apiBaseUrl, content);

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
    /// AI评分通过模型
    /// </summary>
    /// <param name="stem"></param>
    /// <param name="studentAnswer"></param>
    /// <param name="totalScore"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    private async Task<GradingWithAIModelResultDto> CallTrainedGradingModelAsync(string stem, string studentAnswer, float totalScore)
    {
        using var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(_gradingModelApiUrl, new
        {
            stem,
            student_answer = studentAnswer,
            total_score = totalScore
        });

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"模型服务调用失败: {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<TrainedModelResponse>();
        return result == null ? throw new ApplicationException("模型服务返回空结果")
            : new GradingWithAIModelResultDto
            {
                Score = result.score,
                Comment = result.comment // ← 使用模型生成的真实分析！
            };
    }

    private record TrainedModelResponse(float score, string comment);
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