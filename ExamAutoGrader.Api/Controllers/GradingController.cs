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
    /// AI评分
    /// </summary>
    [HttpPost("grade-ai")]
    public async Task<ActionResult<GradingWithAIResultDto>> GradingWithAI([FromBody] GradingWithAIItemDto request)
    {
        var result = await _gradingService.GradingWithAIAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// AI评分通过模型
    /// </summary>
    [HttpPost("grade-ai-model")]
    public async Task<ActionResult<GradingItemResultDto>> GradingWithAIModel(GradingWithAIModelDto request)
    {
        var result = await _gradingService.GradingWithAIModelAsync(request);
        return Ok(result);
    }


    /// <summary>
    /// 对题目进行评分
    /// </summary>
    /// <param name="questionFile"></param>
    /// <param name="answerFile"></param>
    /// <returns></returns>
    [HttpPost("ocr-grade")]
    public async Task<ActionResult<GradingWithAIResultDto>> OCRGrade(IFormFile questionFile, IFormFile answerFile)
    {
        var result = await _gradingService.OCRGradeAsync(questionFile, answerFile);
        return Ok(result);
    }

    /// <summary>
    /// 对题目进行评分
    /// </summary>
    /// <param name="questionFile"></param>
    /// <param name="answerFile"></param>
    /// <returns></returns>
    [HttpPost("dbtest")]
    public async Task<ActionResult<GradingWithAIResultDto>> DBTest()
    {
        await _gradingService.DBTestAsync();
        return Ok();
    }
}