namespace PeruShopHub.Application.DTOs.Categories;

public record VariationFieldDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Type,
    string[] Options,
    bool Required,
    int Order);

public record CreateVariationFieldDto(
    string Name,
    string Type,
    string[] Options,
    bool Required);

public record UpdateVariationFieldDto(
    string? Name,
    string? Type,
    string[]? Options,
    bool? Required,
    int? Order);

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
