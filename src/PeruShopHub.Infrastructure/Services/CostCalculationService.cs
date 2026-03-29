using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private const decimal DefaultTaxRate = 0.06m;
    private const decimal DefaultPaymentFeeRate = 0.0499m;
    private readonly int _averageDaysInStorage;

    public CostCalculationService(
        PeruShopHubDbContext db,
        INotificationDispatcher notifications,
        ILogger<CostCalculationService> logger,
        IConfiguration config)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
        _averageDaysInStorage = config.GetValue<int>("CostSettings:AverageDaysInStorage", 30);
    }

    public async Task<List<OrderCost>> CalculateOrderCostsAsync(Order order, CancellationToken ct = default)
        => await CalculateOrderCostsInternalAsync(order, effectiveDate: null, ct);

    private async Task<List<OrderCost>> CalculateOrderCostsInternalAsync(
        Order order, DateTime? effectiveDate, CancellationToken ct = default)
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
        // When recalculating old orders, use the cost that was effective at the order date
        var historicalCosts = new Dictionary<Guid, decimal>();
        if (effectiveDate.HasValue)
        {
            var variantIds = variants.Select(v => v.Id).ToList();
            historicalCosts = await GetHistoricalCostsAsync(variantIds, effectiveDate.Value, ct);
        }

        decimal totalProductCost = 0m;
        foreach (var item in order.Items)
        {
            if (variantBySku.TryGetValue(item.Sku, out var variant))
            {
                decimal purchaseCost;
                if (effectiveDate.HasValue && historicalCosts.TryGetValue(variant.Id, out var histCost))
                    purchaseCost = histCost;
                else
                    purchaseCost = variant.PurchaseCost ?? 0m;

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

        // ── storage_daily ──────────────────────────────────
        // For Full (fulfillment) products, use accumulated storage costs from StorageCostAccumulation.
        // For others, fall back to the flat daily estimate.
        decimal totalStorageCost = 0m;
        string storageDescription = $"Custo de armazenagem ({_averageDaysInStorage} dias)";

        // Collect unique product IDs to check Full status
        var productIds = variants.Select(v => v.Product.Id).Distinct().ToList();
        var fullProductIds = await _db.MarketplaceListings
            .Where(l => l.FulfillmentType == "fulfillment" && l.ProductId != null && productIds.Contains(l.ProductId.Value))
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToListAsync(ct);

        foreach (var item in order.Items)
        {
            if (variantBySku.TryGetValue(item.Sku, out var storageVariant))
            {
                if (fullProductIds.Contains(storageVariant.Product.Id))
                {
                    // Use actual accumulated storage cost for Full products
                    var latestAccum = await _db.StorageCostAccumulations
                        .Where(s => s.ProductId == storageVariant.Product.Id)
                        .OrderByDescending(s => s.Date)
                        .FirstOrDefaultAsync(ct);

                    if (latestAccum != null)
                    {
                        totalStorageCost += latestAccum.CumulativeCost * item.Quantity;
                        storageDescription = $"Custo de armazenagem Full ({latestAccum.DaysStored} dias acumulados)";
                    }
                }
                else
                {
                    var dailyCost = storageVariant.Product.StorageCostDaily ?? 0m;
                    totalStorageCost += dailyCost * _averageDaysInStorage * item.Quantity;
                }
            }
        }

        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "storage_daily",
            Description = storageDescription,
            Value = totalStorageCost,
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
        var taxRate = await ResolveTaxRateAsync(ct);
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "tax",
            Description = $"Impostos ({taxRate:P0})",
            Value = order.TotalAmount * taxRate,
            Source = "Calculated"
        });

        // ── payment_fee ────────────────────────────────────
        var paymentFeeRate = await ResolvePaymentFeeRateAsync(order.Installments ?? 1, ct);
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "payment_fee",
            Description = $"Taxa de pagamento ({order.Installments ?? 1}x — {paymentFeeRate:P2})",
            Value = order.TotalAmount * paymentFeeRate,
            Source = "Calculated"
        });

        // ── shipping_seller ──────────────────────────────
        // Actual shipping cost comes from the marketplace API; placeholder until webhook populates it
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "shipping_seller",
            Description = "Custo de frete (vendedor)",
            Value = 0m,
            Source = "API"
        });

        // ── fulfillment_fee ──────────────────────────────
        // Fulfillment fee from marketplace (e.g., Mercado Envios Full); populated via API
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "fulfillment_fee",
            Description = "Taxa de fulfillment",
            Value = 0m,
            Source = "API"
        });

        // ── advertising ──────────────────────────────────
        // Advertising cost is manually attributed or imported from Ads API
        costs.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Category = "advertising",
            Description = "Custo de publicidade",
            Value = 0m,
            Source = "Manual"
        });

        // Flag zero-value costs
        foreach (var cost in costs)
        {
            cost.IsZeroValue = cost.Value == 0m;
        }

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

        // Recalculate using cost effective at order time
        var newCosts = await CalculateOrderCostsInternalAsync(order, order.OrderDate, ct);

        // API-sourced costs override calculated ones for the same category
        var apiCostCategories = order.Costs
            .Where(c => c.Source == "API")
            .Select(c => c.Category)
            .ToHashSet();

        foreach (var cost in newCosts)
        {
            if (!apiCostCategories.Contains(cost.Category))
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

            // Broadcast per-product updates so frontend refreshes product data
            foreach (var item in po.Items)
            {
                await _notifications.BroadcastDataChangeAsync("product", "updated", item.ProductId.ToString(), ct);
            }
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task FulfillOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new InvalidOperationException($"Order {orderId} not found.");

            if (order.IsFulfilled)
                throw new InvalidOperationException($"Order {orderId} is already fulfilled.");

            // Look up variants by SKU for stock deduction
            var skus = order.Items.Select(i => i.Sku).Distinct().ToList();
            var variants = await _db.ProductVariants
                .Where(v => skus.Contains(v.Sku))
                .ToListAsync(ct);
            var variantBySku = variants
                .GroupBy(v => v.Sku)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var item in order.Items)
            {
                if (!variantBySku.TryGetValue(item.Sku, out var variant))
                {
                    _logger.LogWarning("No variant found for SKU {Sku} in order {OrderId}. Skipping stock deduction.", item.Sku, orderId);
                    continue;
                }

                variant.Stock = Math.Max(0, variant.Stock - item.Quantity);

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId ?? variant.ProductId,
                    VariantId = variant.Id,
                    Type = "Saída",
                    Quantity = item.Quantity,
                    UnitCost = variant.PurchaseCost ?? 0m,
                    OrderId = orderId,
                    Reason = $"Venda #{order.ExternalOrderId}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            order.IsFulfilled = true;
            order.FulfilledAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Order {OrderId} fulfilled. {ItemCount} items stock deducted.", orderId, order.Items.Count);

            await _notifications.BroadcastDataChangeAsync("Order", "fulfilled", orderId.ToString(), ct);
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

    private async Task<decimal> ResolveTaxRateAsync(CancellationToken ct)
    {
        var profile = await _db.TaxProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (profile is null)
            return DefaultTaxRate;

        return profile.AliquotPercentage / 100m;
    }

    private async Task<decimal> ResolvePaymentFeeRateAsync(int installments, CancellationToken ct)
    {
        // Try specific match: installment count within range
        var rule = await _db.PaymentFeeRules
            .AsNoTracking()
            .Where(r => !r.IsDefault && r.InstallmentMin <= installments && r.InstallmentMax >= installments)
            .FirstOrDefaultAsync(ct);

        if (rule != null)
            return rule.FeePercentage / 100m;

        // Fallback: default rule
        var defaultRule = await _db.PaymentFeeRules
            .AsNoTracking()
            .Where(r => r.IsDefault)
            .FirstOrDefaultAsync(ct);

        if (defaultRule != null)
            return defaultRule.FeePercentage / 100m;

        _logger.LogWarning("No payment fee rule found for {Installments} installments. Using hardcoded fallback rate {Rate}.",
            installments, DefaultPaymentFeeRate);

        return DefaultPaymentFeeRate;
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

    /// <summary>
    /// For each variant, finds the cost that was effective at the given date
    /// by looking at the most recent ProductCostHistory entry on or before that date.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetHistoricalCostsAsync(
        List<Guid> variantIds, DateTime effectiveDate, CancellationToken ct)
    {
        // Get the most recent cost history entry for each variant at or before the effective date
        var histories = await _db.ProductCostHistories
            .Where(h => variantIds.Contains(h.VariantId!.Value) && h.CreatedAt <= effectiveDate)
            .GroupBy(h => h.VariantId!.Value)
            .Select(g => new
            {
                VariantId = g.Key,
                NewCost = g.OrderByDescending(h => h.CreatedAt).First().NewCost
            })
            .ToListAsync(ct);

        return histories.ToDictionary(h => h.VariantId, h => h.NewCost);
    }
}
