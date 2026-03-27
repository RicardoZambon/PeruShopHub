using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/categories/{categoryId:guid}/variation-fields")]
[Authorize]
public class VariationFieldsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public VariationFieldsController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VariationFieldDto>>> GetFields(Guid categoryId)
    {
        var fields = await _db.VariationFields
            .AsNoTracking()
            .Where(f => f.CategoryId == categoryId)
            .OrderBy(f => f.Order)
            .Select(f => new VariationFieldDto(
                f.Id, f.CategoryId, f.Name, f.Type, f.Options, f.Required, f.Order))
            .ToListAsync();

        return Ok(fields);
    }

    [HttpGet("/api/categories/{categoryId:guid}/variation-fields/inherited")]
    public async Task<ActionResult<IReadOnlyList<InheritedVariationFieldDto>>> GetInheritedFields(Guid categoryId)
    {
        var result = new List<InheritedVariationFieldDto>();

        // Walk up the parent chain
        var currentId = categoryId;
        while (true)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.ParentId, c.Name })
                .FirstOrDefaultAsync();

            if (category?.ParentId is null)
                break;

            currentId = category.ParentId.Value;

            var parentCategory = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();

            if (parentCategory is null)
                break;

            var parentFields = await _db.VariationFields
                .AsNoTracking()
                .Where(f => f.CategoryId == currentId)
                .OrderBy(f => f.Order)
                .Select(f => new InheritedVariationFieldDto(
                    f.Id, f.CategoryId, f.Name, f.Type, f.Options, f.Required, f.Order,
                    parentCategory.Name, parentCategory.Id))
                .ToListAsync();

            result.InsertRange(0, parentFields);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VariationFieldDto>> CreateField(
        Guid categoryId, CreateVariationFieldDto dto)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
            return NotFound(new { message = "Category not found" });

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (dto.Type is not "text" and not "select")
            errors["Type"] = ["Tipo deve ser 'text' ou 'select'"];

        if (dto.Type == "select" && (dto.Options is null || dto.Options.Length < 2))
            errors["Options"] = ["Campos de seleção precisam de pelo menos 2 opções"];

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var allFieldNames = await GetAllFieldNamesForCategory(categoryId);
            if (allFieldNames.Any(n => n.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors["Name"] = [$"Já existe um campo com o nome \"{dto.Name.Trim()}\""];
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var maxOrder = await _db.VariationFields
            .Where(f => f.CategoryId == categoryId)
            .MaxAsync(f => (int?)f.Order) ?? -1;

        var field = new VariationField
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = dto.Name.Trim(),
            Type = dto.Type,
            Options = dto.Options ?? Array.Empty<string>(),
            Required = dto.Required,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.VariationFields.Add(field);
        await _db.SaveChangesAsync();

        var result = new VariationFieldDto(
            field.Id, field.CategoryId, field.Name, field.Type,
            field.Options, field.Required, field.Order);

        return CreatedAtAction(nameof(GetFields), new { categoryId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VariationFieldDto>> UpdateField(
        Guid categoryId, Guid id, UpdateVariationFieldDto dto)
    {
        var field = await _db.VariationFields
            .FirstOrDefaultAsync(f => f.Id == id && f.CategoryId == categoryId);

        if (field is null)
            return NotFound();

        var errors = new Dictionary<string, string[]>();

        if (dto.Name is not null && string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (dto.Type is not null and not "text" and not "select")
            errors["Type"] = ["Tipo deve ser 'text' ou 'select'"];

        var effectiveType = dto.Type ?? field.Type;
        if (effectiveType == "select" && dto.Options is not null && dto.Options.Length < 2)
            errors["Options"] = ["Campos de seleção precisam de pelo menos 2 opções"];

        if (dto.Name is not null && !string.IsNullOrWhiteSpace(dto.Name))
        {
            var allFieldNames = await GetAllFieldNamesForCategory(categoryId, excludeFieldId: id);
            if (allFieldNames.Any(n => n.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors["Name"] = [$"Já existe um campo com o nome \"{dto.Name.Trim()}\""];
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        if (dto.Name is not null) field.Name = dto.Name.Trim();
        if (dto.Type is not null) field.Type = dto.Type;
        if (dto.Options is not null) field.Options = dto.Options;
        if (dto.Required.HasValue) field.Required = dto.Required.Value;
        if (dto.Order.HasValue) field.Order = dto.Order.Value;

        field.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new VariationFieldDto(
            field.Id, field.CategoryId, field.Name, field.Type,
            field.Options, field.Required, field.Order));
    }

    private async Task<List<string>> GetAllFieldNamesForCategory(Guid categoryId, Guid? excludeFieldId = null)
    {
        // Own fields
        var query = _db.VariationFields.AsNoTracking().Where(f => f.CategoryId == categoryId);
        if (excludeFieldId.HasValue)
            query = query.Where(f => f.Id != excludeFieldId.Value);

        var names = await query.Select(f => f.Name).ToListAsync();

        // Walk up parent chain for inherited fields
        var currentId = categoryId;
        while (true)
        {
            var parentId = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => c.ParentId)
                .FirstOrDefaultAsync();

            if (parentId is null)
                break;

            currentId = parentId.Value;
            var parentFieldNames = await _db.VariationFields.AsNoTracking()
                .Where(f => f.CategoryId == currentId)
                .Select(f => f.Name)
                .ToListAsync();

            names.AddRange(parentFieldNames);
        }

        return names;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteField(Guid categoryId, Guid id)
    {
        var field = await _db.VariationFields
            .FirstOrDefaultAsync(f => f.Id == id && f.CategoryId == categoryId);

        if (field is null)
            return NotFound();

        _db.VariationFields.Remove(field);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record InheritedVariationFieldDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Type,
    string[] Options,
    bool Required,
    int Order,
    string InheritedFrom,
    Guid InheritedFromId);
