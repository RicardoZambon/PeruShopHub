using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Infrastructure.Services;

public class CostCalculationService : ICostCalculationService
{
    private readonly PeruShopHubDbContext _db;
    private readonly INotificationDispatcher _notifications;
    private readonly ILogger<CostCalculationService> _logger;

    private const decimal HardcodedFallbackCommissionRate = 0.13m;
    private const decimal TaxRate = 0.06m;

    public CostCalculationService(
        PeruShopHubDbContext db,
        INotificationDispatcher notifications,
        ILogger<CostCalculationService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<List<OrderCost>> CalculateOrderCostsAsync(Order order, CancellationToken ct = default)
    {
        var costs = new List<OrderCost>();

        // Collect all SKUs from order items to batch-lookup variants and products
        var skus = order.Items.Select(i => i.Sku).Distinct().ToList();

        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => skus.Contains(v.Sku))
            .ToListAsync(ct);

        var variantBySku = variants
            .GroupBy(v => v.Sku)
            .ToDictionary(g => g.Key, g => g.First());

        // ── product_cost ─────────────────────────────────────
        decimal totalProductCost = 0m;
        foreach (var item in order.Items)
        {
            if (variantBySku.TryGetValue(item.Sku, out var variant))
            {
                var purchaseCost = variant.PurchaseCost ?? 0m;
                totalProductCost += purchaseCost * item.Quantity;
            }
        }

        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "product_cost",
            Description = "Custo dos produtos",
            Value = totalProductCost,
            Source = "Calculated"
        });

        // ── packaging ────────────────────────────────────────
        decimal totalPackagingCost = 0m;
        foreach (var item in order.Items)
        {
            if (variantBySku.TryGetValue(item.Sku, out var variant))
            {
                totalPackagingCost += variant.Product.PackagingCost * item.Quantity;
            }
        }

        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "packaging",
            Description = "Custo de embalagem",
            Value = totalPackagingCost,
            Source = "Calculated"
        });

        // ── marketplace_commission ───────────────────────────
        var commissionRate = await ResolveCommissionRateAsync(order, ct);

        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "marketplace_commission",
            Description = $"Comissão marketplace ({commissionRate:P1})",
            Value = order.TotalAmount * commissionRate,
            Source = "Calculated"
        });

        // ── fixed_fee ────────────────────────────────────────
        decimal totalFixedFee = 0m;
        foreach (var item in order.Items)
        {
            if (item.UnitPrice < 79m)
            {
                var fee = CalculateFixedFee(item.UnitPrice);
                totalFixedFee += fee * item.Quantity;
            }
        }

        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "fixed_fee",
            Description = "Taxa fixa por item",
            Value = totalFixedFee,
            Source = "Calculated"
        });

        // ── tax ──────────────────────────────────────────────
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "tax",
            Description = $"Impostos ({TaxRate:P0})",
            Value = order.TotalAmount * TaxRate,
            Source = "Calculated"
        });

        return costs;
    }

    public async Task RecalculateOrderCostsAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} not found.");

        // Remove existing calculated costs (preserve Manual/API costs)
        var calculatedCosts = order.Costs.Where(c => c.Source == "Calculated").ToList();
        foreach (var cost in calculatedCosts)
        {
            order.Costs.Remove(cost);
            _db.OrderCosts.Remove(cost);
        }

        // Recalculate
        var newCosts = await CalculateOrderCostsAsync(order, ct);
        foreach (var cost in newCosts)
        {
            order.Costs.Add(cost);
        }

        // Recalculate profit
        order.Profit = order.TotalAmount - order.Costs.Sum(c => c.Value);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Recalculated costs for order {OrderId}. New profit: {Profit}", orderId, order.Profit);
    }

    public async Task ReceivePurchaseOrderAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Variants)
                .Include(p => p.Items).ThenInclude(i => i.Variant)
                .Include(p => p.Costs)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
                ?? throw new InvalidOperationException($"Purchase order {purchaseOrderId} not found.");

            if (po.Status != "Rascunho")
            {
                throw new InvalidOperationException(
                    $"Purchase order {purchaseOrderId} has status '{po.Status}'. Only 'Rascunho' orders can be received.");
            }

            // ── Distribute additional costs ──────────────────
            var sumOfAllItemTotals = po.Items.Sum(i => i.TotalCost);
            var sumOfAllQuantities = po.Items.Sum(i => i.Quantity);

            foreach (var cost in po.Costs)
            {
                switch (cost.DistributionMethod)
                {
                    case "by_value":
                        if (sumOfAllItemTotals > 0)
                        {
                            foreach (var item in po.Items)
                            {
                                item.AllocatedAdditionalCost += cost.Value * (item.TotalCost / sumOfAllItemTotals);
                            }
                        }
                        break;

                    case "by_quantity":
                        if (sumOfAllQuantities > 0)
                        {
                            foreach (var item in po.Items)
                            {
                                item.AllocatedAdditionalCost += cost.Value * ((decimal)item.Quantity / sumOfAllQuantities);
                            }
                        }
                        break;

                    case "manual":
                        // Already allocated client-side
                        break;
                }
            }

            // ── Process each item ────────────────────────────
            foreach (var item in po.Items)
            {
                // Compute effective unit cost
                item.EffectiveUnitCost = item.Quantity > 0
                    ? (item.TotalCost + item.AllocatedAdditionalCost) / item.Quantity
                    : 0m;

                // Weighted average cost for variant
                var variant = item.Variant;
                var previousCost = variant.PurchaseCost ?? 0m;
                var currentQty = variant.Stock;
                var totalQty = currentQty + item.Quantity;

                var newCost = totalQty == 0
                    ? item.EffectiveUnitCost
                    : ((currentQty * previousCost) + (item.Quantity * item.EffectiveUnitCost)) / totalQty;

                variant.PurchaseCost = newCost;
                variant.Stock += item.Quantity;

                // Create ProductCostHistory record
                _db.ProductCostHistories.Add(new ProductCostHistory
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    PreviousCost = previousCost,
                    NewCost = newCost,
                    Quantity = item.Quantity,
                    UnitCostPaid = item.EffectiveUnitCost,
                    PurchaseOrderId = po.Id,
                    Reason = "Recebimento de ordem de compra",
                    CreatedAt = DateTime.UtcNow
                });

                // Create StockMovement
                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Type = "Entrada",
                    Quantity = item.Quantity,
                    UnitCost = item.EffectiveUnitCost,
                    PurchaseOrderId = po.Id,
                    Reason = "Recebimento de ordem de compra",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // ── Update product-level PurchaseCost ────────────
            var productIds = po.Items.Select(i => i.ProductId).Distinct().ToList();
            foreach (var productId in productIds)
            {
                var product = po.Items.First(i => i.ProductId == productId).Product;
                var activeVariants = product.Variants.Where(v => v.Stock > 0).ToList();

                if (activeVariants.Count > 0)
                {
                    var totalStock = activeVariants.Sum(v => v.Stock);
                    product.PurchaseCost = totalStock > 0
                        ? activeVariants.Sum(v => v.Stock * (v.PurchaseCost ?? 0m)) / totalStock
                        : 0m;
                }

                product.UpdatedAt = DateTime.UtcNow;
            }

            // ── Finalize PO ─────────────────────────────────
            po.Status = "Recebido";
            po.ReceivedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Purchase order {PurchaseOrderId} received successfully. {ItemCount} items processed.",
                purchaseOrderId, po.Items.Count);

            await _notifications.BroadcastDataChangeAsync("PurchaseOrder", "received", purchaseOrderId.ToString(), ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<decimal> ResolveCommissionRateAsync(Order order, CancellationToken ct)
    {
        // Try to determine category and listing type from the first item's product
        string? categoryId = null;
        string? listingType = null;
        string marketplace = "mercadolivre";

        var firstItem = order.Items.FirstOrDefault();
        if (firstItem?.ProductId != null)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == firstItem.ProductId, ct);

            categoryId = product?.CategoryId;
        }

        // Try specific match: marketplace + category + listing type
        if (categoryId != null)
        {
            var specificRule = await _db.CommissionRules
                .AsNoTracking()
                .Where(r => r.MarketplaceId == marketplace
                    && r.CategoryPattern == categoryId
                    && (listingType == null || r.ListingType == listingType))
                .FirstOrDefaultAsync(ct);

            if (specificRule != null)
                return specificRule.Rate;

            // Fallback: category only (no listing type)
            var categoryRule = await _db.CommissionRules
                .AsNoTracking()
                .Where(r => r.MarketplaceId == marketplace
                    && r.CategoryPattern == categoryId
                    && r.ListingType == null)
                .FirstOrDefaultAsync(ct);

            if (categoryRule != null)
                return categoryRule.Rate;
        }

        // Fallback: marketplace default (IsDefault=true)
        var defaultRule = await _db.CommissionRules
            .AsNoTracking()
            .Where(r => r.MarketplaceId == marketplace && r.IsDefault)
            .FirstOrDefaultAsync(ct);

        if (defaultRule != null)
            return defaultRule.Rate;

        // Hardcoded fallback
        _logger.LogWarning("No commission rule found for marketplace {Marketplace}. Using hardcoded fallback rate {Rate}.",
            marketplace, HardcodedFallbackCommissionRate);

        return HardcodedFallbackCommissionRate;
    }

    private static decimal CalculateFixedFee(decimal unitPrice)
    {
        return unitPrice switch
        {
            <= 12.50m => unitPrice * 0.50m,
            <= 29m => 6.25m,
            <= 50m => 6.50m,
            <= 79m => 6.75m,
            _ => 0m // Should not reach here since we check < 79 before calling
        };
    }
}
