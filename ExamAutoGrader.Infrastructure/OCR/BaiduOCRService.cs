using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Infrastructure.Similarity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamAutoGrader.Infrastructure.OCR;

public class BaiduOCRService : IOCRService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly ILogger<BaiduOCRService> _logger;

    private string? _accessToken;
    private DateTime _tokenExpiresAt;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    public BaiduOCRService(HttpClient httpClient, IConfiguration configuration, ILogger<BaiduOCRService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["OCR:Baidu:ApiKey"]
                 ?? throw new ArgumentException("百度OCR API Key未配置");
        _secretKey = configuration["OCR:Baidu:SecretKey"]
                    ?? throw new ArgumentException("百度OCR Secret Key未配置");

        _logger.LogInformation("百度OCR服务初始化完成");
    }

    public async Task<string> RecognizeTextAsync(string imagePath)
    {
        var projectRoot = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(projectRoot, imagePath);

        // 验证文件存在
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"图片文件不存在：{imagePath}");
        }

        _logger.LogInformation("开始OCR识别本地图片：{ImagePath}", imagePath);

        // 获取访问令牌
        await EnsureAccessTokenAsync();

        try
        {
            // 读取图片文件并转换为Base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var imageBase64 = Convert.ToBase64String(imageBytes);

            // 构建请求参数
            var request = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("image", imageBase64),
                new KeyValuePair<string, string>("recognize_granularity", "small"), // 改为small，提高精度
                new KeyValuePair<string, string>("words_type", "handwriting"), // 纯手写模式

                new KeyValuePair<string, string>("language_type", "CHN_ENG"),
                new KeyValuePair<string, string>("detect_direction", "true"),
                new KeyValuePair<string, string>("detect_language", "true"), // 添加语言检测
                new KeyValuePair<string, string>("paragraph", "true"),       // 启用段落识别
                new KeyValuePair<string, string>("probability", "true")      // 获取置信度
            ]);

            // 发送OCR请求
            var response = await _httpClient.PostAsync(
                $"https://aip.baidubce.com/rest/2.0/ocr/v1/handwriting?access_token={_accessToken}",
                request);
       
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OCR API调用失败: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var ocrResponse = JsonSerializer.Deserialize<BaiduOCRResponse>(responseContent);

            if (ocrResponse?.WordsResult == null)
            {
                throw new ApplicationException("OCR返回结果为空");
            }

            // 拼接识别结果
            var rawRecognizedText = string.Join("\n", ocrResponse.WordsResult.Select(w => w.Words));

            // 简单校正：只替换已知错误
            var correctedRecognizedText = SimpleOCRCorrector.CorrectKnownErrors(rawRecognizedText);

            _logger.LogInformation("OCR识别完成，识别出 {LineCount} 行文本", ocrResponse.WordsResult.Count);

            return correctedRecognizedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR识别失败");
            throw;
        }
    }

    /// <summary>
    /// 确保访问令牌有效
    /// </summary>
    private async Task EnsureAccessTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            return;

        await _tokenSemaphore.WaitAsync();
        try
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
                return;

            _logger.LogInformation("获取百度AI访问令牌");

            var tokenUrl = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={_apiKey}&client_secret={_secretKey}";
            var response = await _httpClient.GetAsync(tokenUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("获取百度访问令牌失败，状态码：{StatusCode}，响应：{Response}",
                    response.StatusCode, errorContent);
                throw new ApplicationException("获取OCR访问令牌失败");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<BaiduTokenResponse>(responseContent);

            if (tokenResult?.AccessToken == null)
            {
                _logger.LogError("百度令牌响应格式错误，响应：{Response}", responseContent);
                throw new ApplicationException("OCR令牌响应格式不正确");
            }

            if (!string.IsNullOrEmpty(tokenResult.Error))
            {
                _logger.LogError("百度令牌接口返回错误：{Error} - {ErrorDescription}",
                    tokenResult.Error, tokenResult.ErrorDescription);
                throw new ApplicationException($"获取OCR令牌失败: {tokenResult.ErrorDescription}");
            }

            _accessToken = tokenResult.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn);

            _logger.LogInformation("百度访问令牌获取成功，过期时间：{ExpiresAt}", _tokenExpiresAt);
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }
}

// 数据结构保持不变
public class BaiduOCRRequest
{
    public string ImageUrl { get; set; } = string.Empty;
    public string LanguageType { get; set; } = "CHN_ENG";
    public bool DetectDirection { get; set; } = true;
    public bool Paragraph { get; set; } = false;
    public bool Probability { get; set; } = false;
}

public class BaiduOCRResponse
{
    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }

    [JsonPropertyName("words_result")]
    public List<WordsResult>? WordsResult { get; set; }

    [JsonPropertyName("words_result_num")]
    public int WordsResultNum { get; set; }
}

public class WordsResult
{
    [JsonPropertyName("words")]
    public string Words { get; set; } = string.Empty;
}

public class BaiduTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

public class BaiduErrorResponse
{
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}