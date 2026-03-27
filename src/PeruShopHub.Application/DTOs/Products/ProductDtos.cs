namespace PeruShopHub.Application.DTOs.Products;

public record ProductListDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    decimal PurchaseCost,
    decimal PackagingCost,
    string Status,
    bool NeedsReview,
    bool IsActive,
    int VariantCount,
    int Stock,
    decimal? Margin,
    string? PhotoUrl,
    DateTime CreatedAt);

public record ProductDetailDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string? CategoryId,
    decimal Price,
    decimal PurchaseCost,
    decimal PackagingCost,
    string? Supplier,
    string Status,
    bool NeedsReview,
    bool IsActive,
    decimal Weight,
    decimal Height,
    decimal Width,
    decimal Length,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<string> PhotoUrls,
    int Version);

public record ProductVariantDto(
    Guid Id,
    string Sku,
    string Attributes,
    decimal? Price,
    int Stock,
    bool IsActive,
    bool NeedsReview,
    decimal? PurchaseCost,
    decimal? Weight,
    decimal? Height,
    decimal? Width,
    decimal? Length);

public record CreateProductDto(
    string? Sku,
    string Name,
    string? Description,
    string? CategoryId,
    decimal Price,
    decimal PurchaseCost,
    decimal PackagingCost,
    string? Supplier,
    decimal Weight,
    decimal Height,
    decimal Width,
    decimal Length);

public record UpdateProductDto(
    string? Sku,
    string? Name,
    string? Description,
    string? CategoryId,
    decimal? Price,
    decimal? PurchaseCost,
    decimal? PackagingCost,
    string? Supplier,
    string? Status,
    bool? IsActive,
    decimal? Weight,
    decimal? Height,
    decimal? Width,
    decimal? Length,
    int Version);

public record CreateProductVariantDto(
    string Sku,
    string Attributes,
    decimal? Price,
    bool IsActive);

public record UpdateProductVariantDto
{
    public string? Sku { get; init; }
    public decimal? Price { get; init; }
    public bool? IsActive { get; init; }
    public decimal? PurchaseCost { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Height { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
}

public record ProductAnalyticsDto(
    int TotalSales,
    decimal TotalRevenue,
    decimal TotalProfit,
    decimal? Margin,
    decimal? SalesChange,
    decimal? RevenueChange,
    decimal? ProfitChange,
    decimal? MarginChange);

public record ProductRecentOrderDto(
    Guid OrderId,
    DateTime Date,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    decimal Profit);
