using ExamAutoGrader.Application.DTOs;
using ExamAutoGrader.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExamAutoGrader.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _FeedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IFeedbackService FeedbackService,
        ILogger<FeedbackController> logger)
    {
        _FeedbackService = FeedbackService;
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
            await _FeedbackService.SubmitFeedbackAsync(request);
            return Ok(new { success = true, message = "反馈提交成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交反馈失败");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("test-feedback")]
    public async Task<IActionResult> TestFeedback()
    {
        var dto = new FeedbackSubmissionDto
        {
            Stem = "测试题目",
            Subject = "数学",
            StudentAnswer = "测试答案",
            ExpectedScore = 5,
            FeedbackComment = "测试反馈"
        };

        await _FeedbackService.SubmitFeedbackAsync(dto);

        return Ok("反馈提交完成");
    }
}