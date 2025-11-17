using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Application.Services;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExamAutoGrader.Application
{
    public static class ExamAutoGraderApplicationModule
    {
        public static IServiceCollection AddExamAutoGraderApplication(this IServiceCollection services)
        {
            return services;
        }
    }
}