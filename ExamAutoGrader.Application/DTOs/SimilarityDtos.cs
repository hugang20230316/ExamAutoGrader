using ExamAutoGrader.Domain.Entities;

namespace ExamAutoGrader.Application.DTOs;

public class SimilarityMatchResult
{
    public FeedbackRecord? MatchedRecord { get; set; }
    public double SimilarityScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public bool IsMatch => SimilarityScore >= 0.98;

    public AISimilarityDetail StemSimilarity { get; set; } = new();
    public AISimilarityDetail AnswerSimilarity { get; set; } = new();
    public List<string> KeySimilarities { get; set; } = new();
    public List<string> KeyDifferences { get; set; } = new();
    public string? RawAIResponse { get; set; }
}

public class AISimilarityDetail
{
    public bool IsSameQuestion { get; set; }
    public bool IsSimilarAnswer { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}