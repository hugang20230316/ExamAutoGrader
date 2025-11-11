using ExamAutoGrader.Api.Common;
using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

/// <summary>
/// AI题目解析API控制器
/// 表现层职责：处理AI解析相关HTTP请求，调用应用服务，返回HTTP响应
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OCRParseController : ControllerBase
{
    private readonly IQuestionParserService _questionParsingService;
    private readonly ILogger<OCRParseController> _logger;

    public OCRParseController(
        IQuestionParserService questionParsingService,
        ILogger<OCRParseController> logger)
    {
        _questionParsingService = questionParsingService;
        _logger = logger;
    }

    /// <summary>
    /// 解析OCR文本为结构化题目
    /// POST: /api/ai/parse-question
    /// </summary>
    [HttpPost("parse-question")]
    [ProducesResponseType(typeof(QuestionParseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionParseResultDto>> ParseQuestion([FromBody] ParseQuestionRequestDto request)
    {
        try
        {
            var result = await _questionParsingService.ParseQuestionFromOCRResultAsync(request.OCRText);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI解析题目失败");
            return BadRequest(Util.CreateProblemDetails("AI解析题目失败", ex.Message));
        }
    }

}