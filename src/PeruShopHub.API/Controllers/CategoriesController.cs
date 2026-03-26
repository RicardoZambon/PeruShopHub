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
                false,
                c.SkuPrefix))
            .ToListAsync();

        var result = categories.Select(c => c with
        {
            HasChildren = childrenLookup.Contains(c.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<CategoryListDto>>> SearchCategories(
        [FromQuery] string q = "")
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<CategoryListDto>());

        var term = q.ToLower();

        // Find all categories matching the search term
        var matchingIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(term))
            .Select(c => c.Id)
            .ToListAsync();

        if (matchingIds.Count == 0)
            return Ok(Array.Empty<CategoryListDto>());

        // Load all categories to reconstruct ancestor chains
        var allCategories = await _db.Categories
            .AsNoTracking()
            .ToListAsync();

        var resultIds = new HashSet<Guid>(matchingIds);

        // For each match, walk up the parent chain and include ancestors
        foreach (var id in matchingIds)
        {
            var current = allCategories.FirstOrDefault(c => c.Id == id);
            while (current?.ParentId != null)
            {
                resultIds.Add(current.ParentId.Value);
                current = allCategories.FirstOrDefault(c => c.Id == current.ParentId.Value);
            }
        }

        // Build HasChildren lookup
        var parentIds = allCategories
            .Where(c => c.ParentId != null && resultIds.Contains(c.ParentId.Value))
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToHashSet();

        var result = allCategories
            .Where(c => resultIds.Contains(c.Id))
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
                parentIds.Contains(c.Id),
                c.SkuPrefix))
            .ToList();

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
                false,
                c.SkuPrefix))
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
            childrenWithFlag,
            category.SkuPrefix);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDetailDto>> CreateCategory(CreateCategoryDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (string.IsNullOrWhiteSpace(dto.Slug))
            errors["Slug"] = ["Slug é obrigatório"];

        if (!string.IsNullOrWhiteSpace(dto.Slug) && await _db.Categories.AnyAsync(c => c.Slug == dto.Slug))
            errors["Slug"] = [$"Já existe uma categoria com o slug \"{dto.Slug}\""];

        if (!string.IsNullOrWhiteSpace(dto.Name) && await _db.Categories.AnyAsync(c => c.Name == dto.Name))
            errors["Name"] = [$"Já existe uma categoria com o nome \"{dto.Name}\""];

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug,
            ParentId = dto.ParentId,
            Icon = dto.Icon,
            Order = dto.Order,
            SkuPrefix = dto.SkuPrefix,
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
            Array.Empty<CategoryListDto>(),
            category.SkuPrefix);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDetailDto>> UpdateCategory(Guid id, UpdateCategoryDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        var errors = new Dictionary<string, string[]>();

        if (dto.Name is not null && string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (dto.Slug is not null && string.IsNullOrWhiteSpace(dto.Slug))
            errors["Slug"] = ["Slug é obrigatório"];

        if (dto.Slug is not null && !string.IsNullOrWhiteSpace(dto.Slug)
            && await _db.Categories.AnyAsync(c => c.Slug == dto.Slug && c.Id != id))
            errors["Slug"] = [$"Já existe uma categoria com o slug \"{dto.Slug}\""];

        if (dto.Name is not null && !string.IsNullOrWhiteSpace(dto.Name)
            && await _db.Categories.AnyAsync(c => c.Name == dto.Name && c.Id != id))
            errors["Name"] = [$"Já existe uma categoria com o nome \"{dto.Name}\""];

        if (errors.Count > 0)
            return BadRequest(new { errors });

        if (dto.Name is not null) category.Name = dto.Name;
        if (dto.Slug is not null) category.Slug = dto.Slug;
        if (dto.ParentId is not null) category.ParentId = dto.ParentId;
        if (dto.Icon is not null) category.Icon = dto.Icon;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;
        if (dto.Order.HasValue) category.Order = dto.Order.Value;
        if (dto.SkuPrefix is not null) category.SkuPrefix = dto.SkuPrefix;

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
                _db.Categories.Any(gc => gc.ParentId == c.Id),
                c.SkuPrefix))
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
            children,
            category.SkuPrefix);

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
