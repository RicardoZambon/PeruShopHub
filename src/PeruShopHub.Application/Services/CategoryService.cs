using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly PeruShopHubDbContext _db;

    public CategoryService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryListDto>> GetCategoriesAsync(Guid? parentId, CancellationToken ct = default)
    {
        IQueryable<Category> query = _db.Categories.AsNoTracking();

        if (parentId.HasValue)
            query = query.Where(c => c.ParentId == parentId);

        var categoryIds = await query.Select(c => c.Id).ToListAsync(ct);
        var childrenLookup = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId != null && categoryIds.Contains(c.ParentId!.Value))
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToListAsync(ct);

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
            .ToListAsync(ct);

        var result = categories.Select(c => c with
        {
            HasChildren = childrenLookup.Contains(c.Id)
        }).ToList();

        return result;
    }

    public async Task<IReadOnlyList<CategoryListDto>> SearchCategoriesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<CategoryListDto>();

        var term = query.ToLower();

        var matchingIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(term))
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (matchingIds.Count == 0)
            return Array.Empty<CategoryListDto>();

        // Load all categories to reconstruct ancestor chains
        var allCategories = await _db.Categories
            .AsNoTracking()
            .ToListAsync(ct);

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
            .Where(c => c.ParentId != null && resultIds.Contains(c.ParentId!.Value))
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

        return result;
    }

    public async Task<CategoryDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
            throw new NotFoundException("Categoria", id);

        var parentName = category.ParentId.HasValue
            ? await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var childIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == id)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var grandchildParentIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId != null && childIds.Contains(c.ParentId!.Value))
            .Select(c => c.ParentId!.Value)
            .Distinct()
            .ToListAsync(ct);

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
            .ToListAsync(ct);

        var childrenWithFlag = children.Select(c => c with
        {
            HasChildren = grandchildParentIds.Contains(c.Id)
        }).ToList();

        return new CategoryDetailDto(
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
            category.SkuPrefix,
            category.Version);
    }

    public async Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        // Name validation
        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];
        else if (dto.Name.Length > 100)
            errors["Name"] = ["Nome deve ter no máximo 100 caracteres"];
        else if (await _db.Categories.AnyAsync(c => c.Name == dto.Name, ct))
            errors["Name"] = [$"Já existe uma categoria com o nome \"{dto.Name}\""];

        // Slug validation
        if (string.IsNullOrWhiteSpace(dto.Slug))
            errors["Slug"] = ["Slug é obrigatório"];
        else if (await _db.Categories.AnyAsync(c => c.Slug == dto.Slug, ct))
            errors["Slug"] = [$"Já existe uma categoria com o slug \"{dto.Slug}\""];

        // ParentId validation
        if (dto.ParentId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value, ct);
            if (!parentExists)
                errors["ParentId"] = ["Categoria pai não encontrada"];
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

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
        await _db.SaveChangesAsync(ct);

        string? parentName = null;
        if (category.ParentId.HasValue)
        {
            parentName = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        return new CategoryDetailDto(
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
            category.SkuPrefix,
            category.Version);
    }

    public async Task<CategoryDetailDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
            throw new NotFoundException("Categoria", id);

        var errors = new Dictionary<string, List<string>>();

        // Name validation
        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                errors["Name"] = ["Nome é obrigatório"];
            else if (dto.Name.Length > 100)
                errors["Name"] = ["Nome deve ter no máximo 100 caracteres"];
            else if (await _db.Categories.AnyAsync(c => c.Name == dto.Name && c.Id != id, ct))
                errors["Name"] = [$"Já existe uma categoria com o nome \"{dto.Name}\""];
        }

        // Slug validation
        if (dto.Slug is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Slug))
                errors["Slug"] = ["Slug é obrigatório"];
            else if (await _db.Categories.AnyAsync(c => c.Slug == dto.Slug && c.Id != id, ct))
                errors["Slug"] = [$"Já existe uma categoria com o slug \"{dto.Slug}\""];
        }

        // ParentId validation — check existence and circular reference
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
            {
                errors["ParentId"] = ["Uma categoria não pode ser pai de si mesma"];
            }
            else
            {
                var parentExists = await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value, ct);
                if (!parentExists)
                {
                    errors["ParentId"] = ["Categoria pai não encontrada"];
                }
                else
                {
                    // Check for circular reference: walk up from proposed parent
                    var currentParentId = dto.ParentId.Value;
                    var visited = new HashSet<Guid> { id };
                    while (true)
                    {
                        if (visited.Contains(currentParentId))
                        {
                            errors["ParentId"] = ["Referência circular detectada na hierarquia de categorias"];
                            break;
                        }
                        visited.Add(currentParentId);

                        var nextParentId = await _db.Categories.AsNoTracking()
                            .Where(c => c.Id == currentParentId)
                            .Select(c => c.ParentId)
                            .FirstOrDefaultAsync(ct);

                        if (nextParentId is null)
                            break;

                        currentParentId = nextParentId.Value;
                    }
                }
            }
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        _db.Entry(category).Property(c => c.Version).OriginalValue = dto.Version;

        if (dto.Name is not null) category.Name = dto.Name;
        if (dto.Slug is not null) category.Slug = dto.Slug;
        if (dto.ParentId is not null) category.ParentId = dto.ParentId;
        if (dto.Icon is not null) category.Icon = dto.Icon;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;
        if (dto.Order.HasValue) category.Order = dto.Order.Value;
        if (dto.SkuPrefix is not null) category.SkuPrefix = dto.SkuPrefix;

        category.UpdatedAt = DateTime.UtcNow;
        category.Version++;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException();
        }

        string? parentName = null;
        if (category.ParentId.HasValue)
        {
            parentName = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
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
            .ToListAsync(ct);

        return new CategoryDetailDto(
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
            category.SkuPrefix,
            category.Version);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
            throw new NotFoundException("Categoria", id);

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentId == id, ct);
        if (hasChildren)
            throw new ConflictException("Cannot delete a category that has children. Remove child categories first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    // --- Variation Fields ---

    public async Task<IReadOnlyList<VariationFieldDto>> GetVariationFieldsAsync(Guid categoryId, CancellationToken ct = default)
    {
        var fields = await _db.VariationFields
            .AsNoTracking()
            .Where(f => f.CategoryId == categoryId)
            .OrderBy(f => f.Order)
            .Select(f => new VariationFieldDto(
                f.Id, f.CategoryId, f.Name, f.Type, f.Options, f.Required, f.Order))
            .ToListAsync(ct);

        return fields;
    }

    public async Task<IReadOnlyList<InheritedVariationFieldDto>> GetInheritedVariationFieldsAsync(Guid categoryId, CancellationToken ct = default)
    {
        var result = new List<InheritedVariationFieldDto>();

        var currentId = categoryId;
        while (true)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.ParentId, c.Name })
                .FirstOrDefaultAsync(ct);

            if (category?.ParentId is null)
                break;

            currentId = category.ParentId.Value;

            var parentCategory = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync(ct);

            if (parentCategory is null)
                break;

            var parentFields = await _db.VariationFields
                .AsNoTracking()
                .Where(f => f.CategoryId == currentId)
                .OrderBy(f => f.Order)
                .Select(f => new InheritedVariationFieldDto(
                    f.Id, f.CategoryId, f.Name, f.Type, f.Options, f.Required, f.Order,
                    parentCategory.Name, parentCategory.Id))
                .ToListAsync(ct);

            result.InsertRange(0, parentFields);
        }

        return result;
    }

    public async Task<VariationFieldDto> CreateVariationFieldAsync(Guid categoryId, CreateVariationFieldDto dto, CancellationToken ct = default)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId, ct);
        if (!categoryExists)
            throw new NotFoundException("Categoria", categoryId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (dto.Type is not "text" and not "select")
            errors["Type"] = ["Tipo deve ser 'text' ou 'select'"];

        if (dto.Type == "select" && (dto.Options is null || dto.Options.Length < 2))
            errors["Options"] = ["Campos de seleção precisam de pelo menos 2 opções"];

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var allFieldNames = await GetAllFieldNamesForCategory(categoryId, ct: ct);
            if (allFieldNames.Any(n => n.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors["Name"] = [$"Já existe um campo com o nome \"{dto.Name.Trim()}\""];
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var maxOrder = await _db.VariationFields
            .Where(f => f.CategoryId == categoryId)
            .MaxAsync(f => (int?)f.Order, ct) ?? -1;

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
        await _db.SaveChangesAsync(ct);

        return new VariationFieldDto(
            field.Id, field.CategoryId, field.Name, field.Type,
            field.Options, field.Required, field.Order);
    }

    public async Task<VariationFieldDto> UpdateVariationFieldAsync(Guid categoryId, Guid fieldId, UpdateVariationFieldDto dto, CancellationToken ct = default)
    {
        var field = await _db.VariationFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.CategoryId == categoryId, ct);

        if (field is null)
            throw new NotFoundException("Campo de variação", fieldId);

        var errors = new Dictionary<string, List<string>>();

        if (dto.Name is not null && string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];

        if (dto.Type is not null and not "text" and not "select")
            errors["Type"] = ["Tipo deve ser 'text' ou 'select'"];

        var effectiveType = dto.Type ?? field.Type;
        if (effectiveType == "select" && dto.Options is not null && dto.Options.Length < 2)
            errors["Options"] = ["Campos de seleção precisam de pelo menos 2 opções"];

        if (dto.Name is not null && !string.IsNullOrWhiteSpace(dto.Name))
        {
            var allFieldNames = await GetAllFieldNamesForCategory(categoryId, excludeFieldId: fieldId, ct: ct);
            if (allFieldNames.Any(n => n.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors["Name"] = [$"Já existe um campo com o nome \"{dto.Name.Trim()}\""];
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        if (dto.Name is not null) field.Name = dto.Name.Trim();
        if (dto.Type is not null) field.Type = dto.Type;
        if (dto.Options is not null) field.Options = dto.Options;
        if (dto.Required.HasValue) field.Required = dto.Required.Value;
        if (dto.Order.HasValue) field.Order = dto.Order.Value;

        field.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new VariationFieldDto(
            field.Id, field.CategoryId, field.Name, field.Type,
            field.Options, field.Required, field.Order);
    }

    public async Task DeleteVariationFieldAsync(Guid categoryId, Guid fieldId, CancellationToken ct = default)
    {
        var field = await _db.VariationFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.CategoryId == categoryId, ct);

        if (field is null)
            throw new NotFoundException("Campo de variação", fieldId);

        _db.VariationFields.Remove(field);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<string>> GetAllFieldNamesForCategory(Guid categoryId, Guid? excludeFieldId = null, CancellationToken ct = default)
    {
        var query = _db.VariationFields.AsNoTracking().Where(f => f.CategoryId == categoryId);
        if (excludeFieldId.HasValue)
            query = query.Where(f => f.Id != excludeFieldId.Value);

        var names = await query.Select(f => f.Name).ToListAsync(ct);

        // Walk up parent chain for inherited fields
        var currentId = categoryId;
        while (true)
        {
            var parentId = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => c.ParentId)
                .FirstOrDefaultAsync(ct);

            if (parentId is null)
                break;

            currentId = parentId.Value;
            var parentFieldNames = await _db.VariationFields.AsNoTracking()
                .Where(f => f.CategoryId == currentId)
                .Select(f => f.Name)
                .ToListAsync(ct);

            names.AddRange(parentFieldNames);
        }

        return names;
    }
}
