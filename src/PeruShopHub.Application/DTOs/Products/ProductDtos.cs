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
    IReadOnlyList<string> PhotoUrls);

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
    string Sku,
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
    decimal? Length);
