using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;

namespace PeruShopHub.Application.Services;

public interface IInventoryService
{
    Task<PagedResult<InventoryItemDto>> GetOverviewAsync(
        int page, int pageSize, string? search,
        string sortBy, string sortDir,
        CancellationToken ct = default);

    Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        Guid? productId, Guid? variantId, string? type,
        DateTime? dateFrom, DateTime? dateTo, string? createdBy,
        int page, int pageSize,
        CancellationToken ct = default);

    Task<byte[]> ExportMovementsToExcelAsync(
        Guid? productId, Guid? variantId, string? type,
        DateTime? dateFrom, DateTime? dateTo, string? createdBy,
        CancellationToken ct = default);

    Task<StockMovementDto> CreateMovementAsync(StockAdjustmentDto dto, CancellationToken ct = default);

    Task<ProductAllocationsDto> GetAllocationsAsync(Guid productId, CancellationToken ct = default);

    Task<StockAllocationDto> UpdateAllocationAsync(Guid variantId, UpdateStockAllocationDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<StockAlertDto>> GetAlertsAsync(CancellationToken ct = default);
}
