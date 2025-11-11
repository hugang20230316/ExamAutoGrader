using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamAutoGrader.Infrastructure.Similarity;

public class AISimilarityService : IAISimilarityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AISimilarityService> _logger;
    private readonly string _apiKey;

    public AISimilarityService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AISimilarityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AI:ApiKey"] ?? throw new ArgumentException("AI API Key未配置");
    }

    public async Task<SimilarityMatchResult> JudgeQuestionAndAnswerSimilarityAsync(
        string currentStem, string currentAnswer,
        string recordStem, string recordAnswer,
        string subject)
    {
        try
        {
            var prompt = BuildDualSimilarityPrompt(currentStem, currentAnswer, recordStem, recordAnswer, subject);
            var response = await CallAISimilarityAPI(prompt);
            return ParseDualAIResponse(response, currentStem, currentAnswer, recordStem, recordAnswer, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI题目和作答相似度判断失败");
            return new SimilarityMatchResult
            {
                SimilarityScore = 0,
                MatchReason = $"AI判断失败: {ex.Message}"
            };
        }
    }

    private string BuildDualSimilarityPrompt(string stem1, string answer1, string stem2, string answer2, string subject)
    {
        return $@"
请作为{subject}学科专家，严格判断以下两个题目及学生作答是否相似：

【题目A】
{stem1}

【题目A的学生作答】
{answer1}

【题目B】  
{stem2}

【题目B的学生作答】
{answer2}

请从题目相似度和作答相似度两个维度综合分析，返回严格的JSON格式：
{{
    ""stemSimilarity"": {{
        ""isSameQuestion"": true/false,
        ""confidence"": 0.95,
        ""reason"": ""题目相似度分析理由""
    }},
    ""answerSimilarity"": {{
        ""isSimilarAnswer"": true/false, 
        ""confidence"": 0.90,
        ""reason"": ""作答相似度分析理由""
    }},
    ""overallSimilarity"": 0.93,
    ""keySimilarities"": [""相似点1"", ""相似点2""],
    ""keyDifferences"": [""差异点1"", ""差异点2""]
}}

只返回JSON：";
    }

    private async Task<string> CallAISimilarityAPI(string prompt)
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

        var response = await _httpClient.PostAsync(
            "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
            content);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<AIAPIResponse>(responseContent);

        return apiResponse?.Output?.Text ?? throw new Exception("AI返回空结果");
    }

    private SimilarityMatchResult ParseDualAIResponse(
        string aiResponse, string currentStem, string currentAnswer,
        string recordStem, string recordAnswer, string subject)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                aiResponse, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                var result = JsonSerializer.Deserialize<AIDualSimilarityResponse>(jsonMatch.Value);
                if (result != null)
                {
                    return new SimilarityMatchResult
                    {
                        SimilarityScore = result.OverallSimilarity,
                        MatchReason = BuildDualMatchReason(result),
                        KeySimilarities = result.KeySimilarities ?? new List<string>(),
                        KeyDifferences = result.KeyDifferences ?? new List<string>(),
                        StemSimilarity = new AISimilarityDetail
                        {
                            IsSameQuestion = result.StemSimilarity.IsSameQuestion,
                            Confidence = result.StemSimilarity.Confidence,
                            Reason = result.StemSimilarity.Reason
                        },
                        AnswerSimilarity = new AISimilarityDetail
                        {
                            IsSimilarAnswer = result.AnswerSimilarity.IsSimilarAnswer,
                            Confidence = result.AnswerSimilarity.Confidence,
                            Reason = result.AnswerSimilarity.Reason
                        },
                        RawAIResponse = aiResponse
                    };
                }
            }

            throw new Exception("解析AI响应失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI相似度响应失败");
            return new SimilarityMatchResult
            {
                SimilarityScore = 0,
                MatchReason = $"解析失败: {ex.Message}",
                RawAIResponse = aiResponse
            };
        }
    }

    private string BuildDualMatchReason(AIDualSimilarityResponse result)
    {
        var reason = new StringBuilder();
        reason.AppendLine($"综合相似度: {result.OverallSimilarity:P0}");
        reason.AppendLine($"题目相似度: {result.StemSimilarity.Confidence:P0} - {result.StemSimilarity.Reason}");
        reason.AppendLine($"作答相似度: {result.AnswerSimilarity.Confidence:P0} - {result.AnswerSimilarity.Reason}");

        if (result.KeySimilarities?.Any() == true)
        {
            reason.AppendLine($"关键相似点: {string.Join("; ", result.KeySimilarities)}");
        }

        return reason.ToString();
    }
}

// API响应模型
public class AIAPIResponse
{
    [JsonPropertyName("output")]
    public AIOutput? Output { get; set; }
}

public class AIOutput
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class AIDualSimilarityResponse
{
    [JsonPropertyName("stemSimilarity")]
    public AISimilarityDetailResponse StemSimilarity { get; set; } = new();

    [JsonPropertyName("answerSimilarity")]
    public AISimilarityDetailResponse AnswerSimilarity { get; set; } = new();

    [JsonPropertyName("overallSimilarity")]
    public double OverallSimilarity { get; set; }

    [JsonPropertyName("keySimilarities")]
    public List<string>? KeySimilarities { get; set; }

    [JsonPropertyName("keyDifferences")]
    public List<string>? KeyDifferences { get; set; }
}

public class AISimilarityDetailResponse
{
    [JsonPropertyName("isSameQuestion")]
    public bool IsSameQuestion { get; set; }

    [JsonPropertyName("isSimilarAnswer")]
    public bool IsSimilarAnswer { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}