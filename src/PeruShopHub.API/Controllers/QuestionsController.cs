using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Questions;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IMarketplaceQuestionService _service;

    public QuestionsController(IMarketplaceQuestionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<QuestionListDto>>> GetQuestions(
        [FromQuery] string? status = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(status, productId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/answer")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<QuestionListDto>> PostAnswer(
        Guid id, [FromBody] PostAnswerRequest request, CancellationToken ct = default)
    {
        var result = await _service.AnswerAsync(id, request, ct);
        return Ok(result);
    }
}
