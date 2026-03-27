using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.PurchaseOrders;

namespace PeruShopHub.Application.Services;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderListDto>> GetListAsync(
        int page, int pageSize, string? status, string? supplier,
        string sortBy, string sortDir, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> CreateAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> UpdateAsync(Guid id, CreatePurchaseOrderDto dto, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> ReceiveAsync(Guid id, CancellationToken ct = default);

    Task CancelAsync(Guid id, CancellationToken ct = default);

    // Cost CRUD
    Task<PurchaseOrderDetailDto> AddCostAsync(Guid id, CreatePurchaseOrderCostDto dto, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> UpdateCostAsync(Guid id, Guid costId, CreatePurchaseOrderCostDto dto, CancellationToken ct = default);

    Task<PurchaseOrderDetailDto> RemoveCostAsync(Guid id, Guid costId, CancellationToken ct = default);

    // Cost preview
    Task<CostDistributionPreviewDto> GetCostPreviewAsync(Guid id, decimal value, string method, CancellationToken ct = default);
}
