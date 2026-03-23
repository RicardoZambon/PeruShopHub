namespace PeruShopHub.Application.DTOs.Categories;

public record CategoryListDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    bool IsActive,
    int ProductCount,
    int Order,
    bool HasChildren);

public record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string? ParentName,
    string? Icon,
    bool IsActive,
    int ProductCount,
    int Order,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<CategoryListDto> Children);

public record CreateCategoryDto(
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    int Order);

public record UpdateCategoryDto(
    string? Name,
    string? Slug,
    Guid? ParentId,
    string? Icon,
    bool? IsActive,
    int? Order);
