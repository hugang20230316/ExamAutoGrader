namespace ExamAutoGrader.Application.DTOs;

/// <summary>
/// 文件信息数据传输对象
/// </summary>
public class FileInfoDto
{
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
}

/// <summary>
/// 识别已有文件请求数据传输对象
/// </summary>
public class RecognizeExistingRequestDto
{
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
}

/// <summary>
/// URL识别请求数据传输对象
/// </summary>
public class RecognizeFromUrlRequestDto
{
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// 批量识别已有文件请求数据传输对象
/// </summary>
public class BatchRecognizeExistingRequestDto
{
    public List<FileInfoDto> Files { get; set; } = new();
}