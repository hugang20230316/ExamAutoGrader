using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;

    public GradingController(IGradingService gradingService)
    {
        _gradingService = gradingService;
    }

    /// <summary>
    /// 对题目进行评分
    /// </summary>
    [HttpPost("grade")]
    public async Task<ActionResult<GradingResultDto>> Grade([FromBody] GradingExamQuestionDto request)
    {
        var result = await _gradingService.GradeAsync(request);
        return Ok(result);
    }
}