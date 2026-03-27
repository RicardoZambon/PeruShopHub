using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/categories/{categoryId:guid}/variation-fields")]
[Authorize]
public class VariationFieldsController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public VariationFieldsController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VariationFieldDto>>> GetFields(Guid categoryId)
    {
        var result = await _categoryService.GetVariationFieldsAsync(categoryId);
        return Ok(result);
    }

    [HttpGet("/api/categories/{categoryId:guid}/variation-fields/inherited")]
    public async Task<ActionResult<IReadOnlyList<InheritedVariationFieldDto>>> GetInheritedFields(Guid categoryId)
    {
        var result = await _categoryService.GetInheritedVariationFieldsAsync(categoryId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VariationFieldDto>> CreateField(
        Guid categoryId, CreateVariationFieldDto dto)
    {
        var result = await _categoryService.CreateVariationFieldAsync(categoryId, dto);
        return CreatedAtAction(nameof(GetFields), new { categoryId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VariationFieldDto>> UpdateField(
        Guid categoryId, Guid id, UpdateVariationFieldDto dto)
    {
        var result = await _categoryService.UpdateVariationFieldAsync(categoryId, id, dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteField(Guid categoryId, Guid id)
    {
        await _categoryService.DeleteVariationFieldAsync(categoryId, id);
        return NoContent();
    }
}
