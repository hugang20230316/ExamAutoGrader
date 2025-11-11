using ExamAutoGrader.Application.DTOs;

namespace ExamAutoGrader.Application.Interfaces;

public interface IAISimilarityService
{
    Task<SimilarityMatchResult> JudgeQuestionAndAnswerSimilarityAsync(
        string currentStem, string currentAnswer,
        string recordStem, string recordAnswer,
        string subject);
}