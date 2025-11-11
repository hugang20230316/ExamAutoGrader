namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// OCR识别结果数据传输对象
/// </summary>
public class OCRResultDto
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string RecognizedText { get; set; } = string.Empty;
    public int TextLength { get; set; }
    public string? FileUrl { get; set; }
    public string? FilePath { get; set; }
    public DateTime RecognizedAt { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 批量OCR识别结果数据传输对象
/// </summary>
public class BatchOCRResultDto
{
    public List<OCRResultDto> Results { get; set; } = new();
    public int TotalFiles { get; set; }
    public int SuccessfulRecognitions { get; set; }
    public int FailedRecognitions { get; set; }
}