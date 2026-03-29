using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.ResponseTemplates;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResponseTemplatesController : ControllerBase
{
    private readonly IResponseTemplateService _service;

    public ResponseTemplatesController(IResponseTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResponseTemplateListDto>>> GetTemplates(
        [FromQuery] string? category = null)
    {
        var result = await _service.GetTemplatesAsync(category);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResponseTemplateDetailDto>> GetTemplate(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ResponseTemplateDetailDto>> CreateTemplate(CreateResponseTemplateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ResponseTemplateDetailDto>> UpdateTemplate(Guid id, UpdateResponseTemplateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/usage")]
    public async Task<IActionResult> IncrementUsage(Guid id)
    {
        await _service.IncrementUsageAsync(id);
        return NoContent();
    }
}
