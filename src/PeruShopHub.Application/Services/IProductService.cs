using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Products;

namespace PeruShopHub.Application.Services;

public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetListAsync(
        int page, int pageSize, string? search, string? status,
        Guid? categoryId, string sortBy, string sortDir,
        CancellationToken ct = default);

    Task<ProductDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<string?> GetNextSkuAsync(Guid? categoryId, CancellationToken ct = default);

    Task<ProductDetailDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);

    Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Variants
    Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(Guid productId, CancellationToken ct = default);
    Task<ProductVariantDto> CreateVariantAsync(Guid productId, CreateProductVariantDto dto, CancellationToken ct = default);
    Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken ct = default);
    Task DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken ct = default);

    // Analytics
    Task<ProductAnalyticsDto> GetAnalyticsAsync(Guid id, int days, CancellationToken ct = default);
    Task<PagedResult<ProductRecentOrderDto>> GetRecentOrdersAsync(Guid id, int days, int page, int pageSize, CancellationToken ct = default);

    // Cost history
    Task<PagedResult<ProductCostHistoryDto>> GetCostHistoryAsync(Guid id, int page, int pageSize, CancellationToken ct = default);
}
