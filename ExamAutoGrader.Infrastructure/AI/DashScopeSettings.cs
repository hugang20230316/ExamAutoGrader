namespace ExamAutoGrader.Infrastructure.AI;

public class DashScopeSettings
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "qwen-turbo"; // 或 qwen-plus, qwen-max
    public string EmbeddingModel { get; set; } = "text-embedding-v2"; 

    // API 基础地址（可选，默认值）
    public string ApiBaseUrl { get; set; } = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
}