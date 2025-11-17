using ExamAutoGrader.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExamAutoGrader.Infrastructure.AI;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        IOptions<DashScopeSettings> dashScopeOptions,
        ILogger<LlmService> logger,
        IHttpClientFactory httpClientFactory)
    {
        var settings = dashScopeOptions.Value;
        var apiKey = settings.ApiKey ?? throw new InvalidOperationException("DashScope:ApiKey 未配置");
        _model = settings.Model ?? "qwen-turbo";

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _httpClient.BaseAddress = new Uri(settings.ApiBaseUrl);

        _logger = logger;
    }

    public async Task<string> GenerateSemanticFingerprintAsync(
        string stem,
        string subject,
        CancellationToken ct = default)
    {
        try
        {
            var prompt = $@"
你是一个教育专家，请为以下题目生成一个简洁、结构化的语义指纹（semantic fingerprint）。
要求：
- 使用小写字母和点号分隔，格式：学科.知识点.子类型.编号（可选）
- 只输出指纹，不要解释
- 中文题目也要输出英文指纹

题目：'{stem}'
科目：{subject}
输出：";

            var requestBody = new
            {
                model = _model,
                input = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                },
                parameters = new
                {
                    temperature = 0.3,
                    max_tokens = 50
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("DashScope API 调用失败: {StatusCode}, {Error}",
                    response.StatusCode, errorContent);
                return "unknown";
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return ParseApiResponse(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成语义指纹异常");
            return "unknown";
        }
    }

    private string ParseApiResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("output", out var outputElement) &&
                outputElement.TryGetProperty("text", out var textElement))
            {
                var output = textElement.GetString();
                return ProcessFingerprint(output);
            }

            _logger.LogError("API 响应格式不符合预期，缺少 'output.text' 字段");
            return "unknown";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析 API 响应 JSON 失败");
            return "unknown";
        }
    }

    private string ProcessFingerprint(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            _logger.LogError("API 返回的 text 内容为空");
            return "unknown";
        }

        var fingerprint = output.Trim()
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Trim('\'', '"', '.', ' ');

        _logger.LogInformation("成功生成语义指纹: {Fingerprint}", fingerprint);

        return string.IsNullOrEmpty(fingerprint) ? "unknown" : fingerprint;
    }
}