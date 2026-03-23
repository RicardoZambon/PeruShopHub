namespace PeruShopHub.Application.DTOs.Search;

public record SearchResultDto(
    string Type,
    Guid Id,
    string Primary,
    string? Secondary,
    string? Route);
