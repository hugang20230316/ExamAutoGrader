using ExamAutoGrader.Application.DTOs;
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
        _apiKey = configuration["DashScope:ApiKey"] ?? throw new ArgumentException("DashScope API Key未配置");
        _apiBaseUrl = configuration["DashScope:ApiBaseUrl"] ?? throw new ArgumentException("DashScope API BaseUrl未配置");
    }

    public async Task<ExamQuestionDto> ParseQuestionFromOCRResultAsync(string ocrText)
    {
        var prompt = $@"分析以下文本中的考试题目结构：
题目编号（QuestionNumber，字符串类型）、
题目类型（QuestionType：SingleChoice=1,MultipleChoice=2,Subjective=3,FillInBlank=4,Other=0）、
题干（Stem，去掉(数字)分）、
题目总分（TotalScore，数字类型）、
小题集合（Items：[{{题目编号(格式必须是:主题号(小题号))、题干、题目总分}}]）

返回JSON格式：
{ocrText}";

        return await CallAIAndParseAsync<ExamQuestionDto>(prompt, "解析题目失败");
    }

    public async Task<ExamQuestionAnswerDto> ParseQuestionAnswerFromOCRResultAsync(string ocrText)
    {
        var prompt = $@"分析以下文本中的学生答题信息：
题目编号（QuestionNumber，字符串类型，(格式必须是:主题号(小题号))）、
小题集合（Items：[{{题目编号(格式必须是:主题号(小题号)、答题信息（StudentAnswer）}}]）

返回JSON格式：
{ocrText}";

        return await CallAIAndParseAsync<ExamQuestionAnswerDto>(prompt, "解析答案失败");
    }

    private async Task<T> CallAIAndParseAsync<T>(string prompt, string errorMessage) where T : new()
    {
        try
        {
            var requestBody = new
            {
                model = "qwen-turbo",
                input = new
                {
                    messages = new[] { new { role = "user", content = prompt } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiBaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseAIResponse<T>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage);
            return new T();
        }
    }

    private T ParseAIResponse<T>(string responseContent) where T : new()
    {
        try
        {
            var apiResponse = JsonSerializer.Deserialize<AlibabaAIResponse>(responseContent);
            var aiText = apiResponse?.Output?.Text;

            if (string.IsNullOrEmpty(aiText))
                return new T();

            var jsonStart = aiText.IndexOf('{');
            var jsonEnd = aiText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }

            return new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI响应失败");
            return new T();
        }
    }
}

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