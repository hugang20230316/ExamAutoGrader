namespace ExamAutoGrader.Application.Interfaces;

public interface ILlmService
{
    /// <summary>
    /// 根据题干和科目生成语义指纹（如 "math.derivative.at_point"）
    /// </summary>
    Task<string> GenerateSemanticFingerprintAsync(string stem, string subject, CancellationToken ct = default);
}