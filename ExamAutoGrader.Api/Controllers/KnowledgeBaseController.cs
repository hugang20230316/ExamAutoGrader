using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<KnowledgeBaseController> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// 提交AI评分反馈
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmissionDto request)
    {
        try
        {
            await _knowledgeBaseService.SubmitFeedbackAsync(request);
            return Ok(new { success = true, message = "反馈提交成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交反馈失败");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}