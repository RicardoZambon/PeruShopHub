namespace PeruShopHub.Application.DTOs.ResponseTemplates;

public record ResponseTemplateListDto(
    Guid Id,
    string Name,
    string Category,
    string Body,
    string? Placeholders,
    int UsageCount,
    int Order,
    bool IsActive);

public record ResponseTemplateDetailDto(
    Guid Id,
    string Name,
    string Category,
    string Body,
    string? Placeholders,
    int UsageCount,
    int Order,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version);

public record CreateResponseTemplateDto(
    string Name,
    string Category,
    string Body,
    string? Placeholders,
    int Order);

public record UpdateResponseTemplateDto(
    string? Name,
    string? Category,
    string? Body,
    string? Placeholders,
    bool? IsActive,
    int? Order,
    int Version);
