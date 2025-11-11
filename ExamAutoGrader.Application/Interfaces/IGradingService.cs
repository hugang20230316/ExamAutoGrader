using ExamAutoGrader.Application.DTOs;

namespace ExamAutoGrader.Application.Interfaces;

public interface IGradingService
{
    /// <summary>
    /// 对题目进行评分
    /// </summary>
    Task<GradingResultDto> GradeAsync(GradingExamQuestionDto request);
}