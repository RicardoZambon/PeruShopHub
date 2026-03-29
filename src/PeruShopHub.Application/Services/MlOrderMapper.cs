using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Application.Services;

/// <summary>
/// Consolidates ML marketplace order → internal Order mapping logic.
/// Pure mapping methods (no DB dependency) for testability.
/// Used by both WebhookProcessingWorker and OrderSyncService.
/// </summary>
public interface IMlOrderMapper
{
    /// <summary>Maps ML order details onto an Order entity (new or existing).</summary>
    void MapOrderDetails(Order order, MarketplaceOrderDetails details, Guid tenantId);

    /// <summary>Creates OrderItem entities from ML order items.</summary>
    List<OrderItem> MapOrderItems(Order order, IReadOnlyList<MarketplaceOrderItem> items, Guid tenantId);

    /// <summary>Maps ML status string to internal status ("Pago", "Cancelado", etc.).</summary>
    string MapOrderStatus(string mlStatus);

    /// <summary>Determines if the ML status represents a fulfilled/paid order.</summary>
    bool IsFulfilledStatus(string mlStatus);

    /// <summary>Maps ML fee type to internal cost category.</summary>
    string MapFeeTypeToCategory(string feeType);

    /// <summary>Creates OrderCost entities from ML API fees.</summary>
    List<OrderCost> MapFeesToCosts(Order order, IReadOnlyList<MarketplaceFee> fees, Guid tenantId);

    /// <summary>Creates a Customer entity from ML buyer info.</summary>
    Customer MapBuyerToCustomer(MarketplaceBuyer buyer, Guid tenantId);

    /// <summary>Extracts order ID from ML webhook resource path (e.g., "/orders/123").</summary>
    string? ExtractOrderIdFromResource(string? resource);

    /// <summary>Calculates profit: TotalAmount - sum of costs.</summary>
    decimal CalculateProfit(decimal totalAmount, IEnumerable<OrderCost> costs);
}

public class MlOrderMapper : IMlOrderMapper
{
    public void MapOrderDetails(Order order, MarketplaceOrderDetails details, Guid tenantId)
    {
        order.TenantId = tenantId;
        order.ExternalOrderId = details.ExternalOrderId;
        order.BuyerName = details.Buyer.Nickname;
        order.BuyerNickname = details.Buyer.Nickname;
        order.BuyerEmail = details.Buyer.Email;
        order.TotalAmount = details.TotalAmount;
        order.ItemCount = details.Items.Count;
        order.OrderDate = details.DateCreated.UtcDateTime;
        order.Status = MapOrderStatus(details.Status);

        // Shipping info
        if (details.Shipping is not null)
        {
            order.LogisticType = "mercadolivre";
            order.ExternalShippingId = details.Shipping.ExternalShippingId;

            if (!string.IsNullOrWhiteSpace(details.Shipping.ExternalShippingId))
                order.TrackingNumber ??= details.Shipping.ExternalShippingId;
        }

        // Cancelled orders should not be marked as fulfilled
        if (order.Status == "Cancelado")
        {
            order.IsFulfilled = false;
            order.FulfilledAt = null;
        }
    }

    public List<OrderItem> MapOrderItems(Order order, IReadOnlyList<MarketplaceOrderItem> items, Guid tenantId)
    {
        var orderItems = new List<OrderItem>();

        foreach (var mlItem in items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = order.Id,
                Name = mlItem.Title,
                Sku = mlItem.ExternalItemId,
                Quantity = mlItem.Quantity,
                UnitPrice = mlItem.UnitPrice,
                Subtotal = mlItem.Quantity * mlItem.UnitPrice
            };

            orderItems.Add(orderItem);
        }

        order.ItemCount = items.Sum(i => i.Quantity);
        return orderItems;
    }

    public string MapOrderStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "paid" => "Pago",
        "confirmed" => "Pago",
        "payment_required" => "Aguardando Pagamento",
        "payment_in_process" => "Aguardando Pagamento",
        "cancelled" => "Cancelado",
        "invalid" => "Cancelado",
        "partially_refunded" => "Reembolso Parcial",
        _ => "Pago"
    };

    public bool IsFulfilledStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "paid" => true,
        "confirmed" => true,
        _ => false
    };

    public string MapFeeTypeToCategory(string feeType) => feeType.ToLowerInvariant() switch
    {
        "sale_fee" or "marketplace_fee" => "marketplace_commission",
        "shipping" or "shipping_fee" => "shipping_seller",
        "financing_fee" or "financing" => "payment_fee",
        "fixed_fee" => "fixed_fee",
        "fulfillment_fee" or "fulfillment" => "fulfillment_fee",
        _ => feeType.ToLowerInvariant()
    };

    public List<OrderCost> MapFeesToCosts(Order order, IReadOnlyList<MarketplaceFee> fees, Guid tenantId)
    {
        var costs = new List<OrderCost>();

        foreach (var fee in fees)
        {
            var category = MapFeeTypeToCategory(fee.Type);

            costs.Add(new OrderCost
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = order.Id,
                Category = category,
                Description = $"ML API: {fee.Type}",
                Value = Math.Abs(fee.Amount),
                Source = "API"
            });
        }

        return costs;
    }

    public Customer MapBuyerToCustomer(MarketplaceBuyer buyer, Guid tenantId)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = buyer.Nickname,
            Nickname = buyer.Nickname,
            Email = buyer.Email,
            TotalOrders = 1,
            LastPurchase = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string? ExtractOrderIdFromResource(string? resource)
    {
        if (string.IsNullOrWhiteSpace(resource)) return null;

        var parts = resource.TrimStart('/').Split('/');
        return parts.Length >= 2 ? parts[^1] : null;
    }

    public decimal CalculateProfit(decimal totalAmount, IEnumerable<OrderCost> costs)
    {
        return totalAmount - costs.Sum(c => c.Value);
    }
}
