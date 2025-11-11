namespace ExamAutoGrader.Application.Interfaces;
public interface IOCRService
{
    Task<string> RecognizeTextAsync(string imagePath);
}