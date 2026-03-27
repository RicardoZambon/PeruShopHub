namespace PeruShopHub.Application.DTOs.Supplies;

public record SupplyListDto(
    Guid Id,
    string Name,
    string Sku,
    string Category,
    decimal UnitCost,
    int Stock,
    int MinimumStock,
    string? Supplier,
    string Status);

public record CreateSupplyDto(
    string Name,
    string Sku,
    string Category,
    decimal UnitCost,
    int Stock,
    int MinimumStock,
    string? Supplier);

public record SupplyDetailDto(
    Guid Id,
    string Name,
    string Sku,
    string Category,
    decimal UnitCost,
    int Stock,
    int MinimumStock,
    string? Supplier,
    string Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version);

public record UpdateSupplyDto(
    string? Name,
    string? Sku,
    string? Category,
    decimal? UnitCost,
    int? Stock,
    int? MinimumStock,
    string? Supplier,
    string? Status,
    int Version);
