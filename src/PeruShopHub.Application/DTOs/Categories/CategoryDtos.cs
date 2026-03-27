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
    bool HasChildren,
    string? SkuPrefix);

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
    IReadOnlyList<CategoryListDto> Children,
    string? SkuPrefix,
    int Version);

public record CreateCategoryDto(
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    int Order,
    string? SkuPrefix);

public record UpdateCategoryDto(
    string? Name,
    string? Slug,
    Guid? ParentId,
    string? Icon,
    bool? IsActive,
    int? Order,
    string? SkuPrefix,
    int Version);
