using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExamAutoGrader.Infrastructure.Grading;

public class DashScopeEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _embeddingModel;
    private readonly ILogger<DashScopeEmbeddingService> _logger;

    public DashScopeEmbeddingService(
        IOptions<DashScopeSettings> options,
        ILogger<DashScopeEmbeddingService> logger,
        IHttpClientFactory httpClientFactory) // 使用 IHttpClientFactory
    {
        var settings = options.Value;
        _apiKey = settings.ApiKey ?? throw new InvalidOperationException("DashScope:ApiKey 未配置");
        _embeddingModel = settings.EmbeddingModel ?? "text-embedding-v2";

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));

        var embeddings = await GetEmbeddingsAsync(new[] { text }, ct);
        return embeddings.FirstOrDefault() ?? Array.Empty<float>();
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var validTexts = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (validTexts.Count == 0)
            return [];

        try
        {
            // 构建正确的 DashScope Embedding 请求格式
            var requestBody = new
            {
                model = _embeddingModel,
                input = new
                {
                    texts = validTexts
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("发送 Embedding 请求: {RequestJson}", json);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/embeddings/text-embedding/text-embedding",
                content,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("DashScope Embedding API 失败: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return validTexts.Select(_ => Array.Empty<float>()).ToList();
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("收到 Embedding 响应: {ResponseJson}", responseJson);

            return ParseEmbeddingResponse(responseJson, validTexts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Embedding 时发生异常");
            return validTexts.Select(_ => Array.Empty<float>()).ToList();
        }
    }

    private List<float[]> ParseEmbeddingResponse(string responseJson, int expectedCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // 检查响应格式
            if (!root.TryGetProperty("output", out var outputElement))
            {
                _logger.LogError("API 响应缺少 'output' 字段");
                return Enumerable.Repeat(Array.Empty<float>(), expectedCount).ToList();
            }

            if (!outputElement.TryGetProperty("embeddings", out var embeddingsElement))
            {
                _logger.LogError("API 响应缺少 'embeddings' 字段");
                return Enumerable.Repeat(Array.Empty<float>(), expectedCount).ToList();
            }

            var result = new List<float[]>();
            foreach (var item in embeddingsElement.EnumerateArray())
            {
                if (item.TryGetProperty("embedding", out var embeddingElement))
                {
                    var vector = embeddingElement.EnumerateArray()
                        .Select(e => e.GetSingle())
                        .ToArray();
                    result.Add(vector);
                }
                else
                {
                    _logger.LogWarning("Embedding 项缺少 'embedding' 字段");
                    result.Add(Array.Empty<float>());
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析 Embedding 响应 JSON 失败");
            return Enumerable.Repeat(Array.Empty<float>(), expectedCount).ToList();
        }
    }
}