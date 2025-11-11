namespace ExamAutoGrader.Application.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// 获取文本的向量嵌入（float 数组）
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// 批量获取多个文本的嵌入（可选，用于性能优化）
    /// </summary>
    Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
}