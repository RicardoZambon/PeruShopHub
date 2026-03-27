using PeruShopHub.Core.Entities;

namespace PeruShopHub.Core.Interfaces;

public interface ICostCalculationService
{
    Task<List<OrderCost>> CalculateOrderCostsAsync(Order order, CancellationToken ct = default);
    Task RecalculateOrderCostsAsync(Guid orderId, CancellationToken ct = default);
    Task ReceivePurchaseOrderAsync(Guid purchaseOrderId, CancellationToken ct = default);
    Task FulfillOrderAsync(Guid orderId, CancellationToken ct = default);
}
