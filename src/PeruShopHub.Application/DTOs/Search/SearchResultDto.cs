namespace PeruShopHub.Application.DTOs.Search;

public record SearchResultDto(
    string EntityType,
    Guid Id,
    string Title,
    string? Subtitle,
    string? NavigationTarget);
