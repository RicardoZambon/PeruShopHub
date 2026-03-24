using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public CategoriesController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryListDto>>> GetCategories(
        [FromQuery] Guid? parentId = null)
    {
        IQueryable<Category> query = _db.Categories.AsNoTracking();

        if (parentId.HasValue)
            query = query.Where(c => c.ParentId == parentId);

        var categoryIds = await query.Select(c => c.Id).ToListAsync();
        var childrenLookup = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId != null && categoryIds.Contains(c.ParentId.Value))
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToListAsync();

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ParentId,
                c.Icon,
                c.IsActive,
                c.ProductCount,
                c.Order,
                false))
            .ToListAsync();

        var result = categories.Select(c => c with
        {
            HasChildren = childrenLookup.Contains(c.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDetailDto>> GetCategory(Guid id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        var parentName = category.ParentId.HasValue
            ? await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : null;

        var childIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == id)
            .Select(c => c.Id)
            .ToListAsync();

        var grandchildParentIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId != null && childIds.Contains(c.ParentId.Value))
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToListAsync();

        var children = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == id)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ParentId,
                c.Icon,
                c.IsActive,
                c.ProductCount,
                c.Order,
                false))
            .ToListAsync();

        var childrenWithFlag = children.Select(c => c with
        {
            HasChildren = grandchildParentIds.Contains(c.Id)
        }).ToList();

        var dto = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.ParentId,
            parentName,
            category.Icon,
            category.IsActive,
            category.ProductCount,
            category.Order,
            category.CreatedAt,
            category.UpdatedAt,
            childrenWithFlag);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDetailDto>> CreateCategory(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug,
            ParentId = dto.ParentId,
            Icon = dto.Icon,
            Order = dto.Order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        string? parentName = null;
        if (category.ParentId.HasValue)
        {
            parentName = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        var result = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.ParentId,
            parentName,
            category.Icon,
            category.IsActive,
            category.ProductCount,
            category.Order,
            category.CreatedAt,
            category.UpdatedAt,
            Array.Empty<CategoryListDto>());

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDetailDto>> UpdateCategory(Guid id, UpdateCategoryDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        if (dto.Name is not null) category.Name = dto.Name;
        if (dto.Slug is not null) category.Slug = dto.Slug;
        if (dto.ParentId is not null) category.ParentId = dto.ParentId;
        if (dto.Icon is not null) category.Icon = dto.Icon;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;
        if (dto.Order.HasValue) category.Order = dto.Order.Value;

        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        string? parentName = null;
        if (category.ParentId.HasValue)
        {
            parentName = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        var children = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == id)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ParentId,
                c.Icon,
                c.IsActive,
                c.ProductCount,
                c.Order,
                _db.Categories.Any(gc => gc.ParentId == c.Id)))
            .ToListAsync();

        var result = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.ParentId,
            parentName,
            category.Icon,
            category.IsActive,
            category.ProductCount,
            category.Order,
            category.CreatedAt,
            category.UpdatedAt,
            children);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
            return Conflict(new { message = "Cannot delete a category that has children. Remove child categories first." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
