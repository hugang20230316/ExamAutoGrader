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
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;
    private readonly string _model;
    private readonly ILogger<LlmService> _logger;

    // 如果你仍用 OpenAiSettings，就保留 IOptions<OpenAiSettings>
    public LlmService(
        IOptions<DashScopeSettings> dashScopeOptions, // 👈 推荐改名
        ILogger<LlmService> logger,
        HttpClient httpClient) // 注入 HttpClient（最佳实践）
    {
        var settings = dashScopeOptions.Value;
        _apiKey = settings.ApiKey ?? throw new InvalidOperationException("DashScope:ApiKey 未配置");
        _model = settings.Model ?? "qwen-turbo";
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _logger = logger;
        _apiBaseUrl = settings.ApiBaseUrl;
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

示例：
题目：'求函数 f(x)=x² 在 x=2 处的导数'
科目：数学
输出：math.calculus.derivative.at_point

题目：'{stem}'
科目：{subject}
输出：";

            // 构建 DashScope 请求体
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
                    max_tokens = 100
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 调用 DashScope API
            var response = await _httpClient.PostAsync(
                _apiBaseUrl,
                content,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("DashScope API 调用失败: {StatusCode}, {Error}",
                    response.StatusCode, errorContent);
                return "unknown";
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var output = doc.RootElement
                .GetProperty("output")
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var fingerprint = output?.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("output:", "")
                .Trim('\'', '"', '.', ' ') ?? "unknown";

            return string.IsNullOrEmpty(fingerprint) ? "unknown" : fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成语义指纹异常（通义千问）");
            return "unknown";
        }
    }
}