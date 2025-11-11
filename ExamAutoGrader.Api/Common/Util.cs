using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.Api.Common
{
    public static class Util
    {
        public static ProblemDetails CreateProblemDetails(string title, string detail)
        {
            return new()
            {
                Title = title,
                Detail = detail,
                Status = StatusCodes.Status400BadRequest
            };
        }
    }
}