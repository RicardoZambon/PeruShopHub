using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Orders;

namespace PeruShopHub.Application.Services;

public interface IOrderService
{
    Task<PagedResult<OrderListDto>> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateTime? dateFrom, DateTime? dateTo,
        string sortBy, string sortDir, CancellationToken ct = default);

    Task<OrderDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OrderCostDto> AddCostAsync(Guid orderId, CreateOrderCostRequest request, CancellationToken ct = default);
    Task<OrderCostDto> UpdateCostAsync(Guid orderId, Guid costId, UpdateOrderCostRequest request, CancellationToken ct = default);
    Task DeleteCostAsync(Guid orderId, Guid costId, CancellationToken ct = default);

    Task FulfillAsync(Guid orderId, CancellationToken ct = default);
    Task RecalculateCostsAsync(Guid orderId, CancellationToken ct = default);
}
