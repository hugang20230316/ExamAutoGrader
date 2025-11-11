using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExamAutoGrader.Infrastructure.Similarity;

public class DashScopeEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _embeddingModel;
    private readonly ILogger<DashScopeEmbeddingService> _logger;

    public DashScopeEmbeddingService(
        IOptions<DashScopeSettings> options,
        ILogger<DashScopeEmbeddingService> logger,
        HttpClient httpClient)
    {
        var settings = options.Value;
        _apiKey = settings.ApiKey ?? throw new InvalidOperationException("DashScope:ApiKey 未配置");
        _embeddingModel = settings.EmbeddingModel ?? "text-embedding-v2";
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
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
            // 构建请求体
            var requestBody = new
            {
                model = _embeddingModel,
                input = validTexts
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

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
            using var doc = JsonDocument.Parse(responseJson);

            // 提取 embeddings
            var embeddingsArray = doc.RootElement
                .GetProperty("output")
                .GetProperty("embeddings");

            var result = new List<float[]>();
            foreach (var item in embeddingsArray.EnumerateArray())
            {
                var vector = item.GetProperty("embedding").EnumerateArray()
                    .Select(e => e.GetSingle()) // DashScope 返回 float32（Single）
                    .ToArray();
                result.Add(vector);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Embedding 时发生异常");
            return validTexts.Select(_ => Array.Empty<float>()).ToList();
        }
    }
}