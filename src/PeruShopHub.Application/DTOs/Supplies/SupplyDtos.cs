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

public record UpdateSupplyDto(
    string? Name,
    string? Sku,
    string? Category,
    decimal? UnitCost,
    int? Stock,
    int? MinimumStock,
    string? Supplier,
    string? Status);
