using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamAutoGrader.Infrastructure.Parsing;

public class AlibabaAIParserService : IQuestionParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlibabaAIParserService> _logger;
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;

    public AlibabaAIParserService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AlibabaAIParserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AI:ApiKey"] ?? throw new ArgumentException("AI API Key未配置");
        _apiBaseUrl = configuration["AI:ApiBaseUrl"] ?? throw new ArgumentException("AI API BaseUrl未配置");
    }

    public async Task<ExamQuestion> ParseQuestionFromOCRResultAsync(string ocrText)
    {
        try
        {
            var requestBody = new
            {
                model = "qwen-turbo",
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $@"分析以下文本中的考试题目结构，
题目编号（QuestionNumber）(字符串类型)、
题目类型（QuestionType(SingleChoice = 1,MultipleChoice = 2,Subjective = 3,FillInBlank = 4,Other = 0)）、
题干（Stem）、
题目总分（TotalScore）(数字类型)，
小题集合(Items):[{{
    题目编号（QuestionNumber）(字符串类型例:1(1))、
    题干（Stem）、
    学生答案(StudentAnswer)、
    题目总分（TotalScore）(数字类型)                  
}}]
返回JSON格式,确保JSON格式正确：
{ocrText}"
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiBaseUrl, request);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("API调用失败");

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseAIResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI解析失败");
            throw;
        }
    }

    public List<ExamQuestionItem> ExamQuestionItems(string ocrText)
    {
        // 简单返回空列表，完全依赖AI
        return new List<ExamQuestionItem>();
    }

    private ExamQuestion ParseAIResponse(string responseContent)
    {
        try
        {
            var apiResponse = JsonSerializer.Deserialize<AlibabaAIResponse>(responseContent);
            var aiText = apiResponse?.Output?.Text;

            if (string.IsNullOrEmpty(aiText))
                return CreateDefaultQuestion();

            // 提取JSON部分
            var jsonStart = aiText.IndexOf('{');
            var jsonEnd = aiText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<ExamQuestion>(json) ?? CreateDefaultQuestion();
            }

            return CreateDefaultQuestion();
        }
        catch(Exception ex)
        {
            return CreateDefaultQuestion(null,ex.Message);
        }
    }

    private ExamQuestion CreateDefaultQuestion(EQuestionType? questionType = null,string stem = "解析失败")
    {
        return new ExamQuestion
        {
            QuestionType = questionType,
            Stem = stem,
            Items = []
        };
    }
}

// 简化的响应模型
public class AlibabaAIResponse
{
    [JsonPropertyName("output")]
    public Output? Output { get; set; }
}

public class Output
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}