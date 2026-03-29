namespace PeruShopHub.Application.DTOs.Claims;

public record ClaimListDto(
    Guid Id,
    string ExternalId,
    Guid? OrderId,
    string ExternalOrderId,
    string Type,
    string Status,
    string Reason,
    string? BuyerName,
    string? ProductName,
    int Quantity,
    decimal? Amount,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public record ClaimDetailDto(
    Guid Id,
    string ExternalId,
    Guid? OrderId,
    string ExternalOrderId,
    string Type,
    string Status,
    string Reason,
    string? BuyerComment,
    string? SellerComment,
    string? BuyerName,
    string? Resolution,
    Guid? ProductId,
    string? ProductName,
    int Quantity,
    decimal? Amount,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    DateTime UpdatedAt);

public record RespondClaimRequest(string SellerComment);

public record ClaimSummaryDto(int OpenCount, int ClosedCount, decimal ReturnRate);
