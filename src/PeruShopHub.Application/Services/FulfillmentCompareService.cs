using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PeruShopHub.Application.DTOs.Pricing;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class FulfillmentCompareService : IFulfillmentCompareService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IConfiguration _configuration;

    public FulfillmentCompareService(PeruShopHubDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<FulfillmentCompareResult> CompareAsync(FulfillmentCompareRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException($"Produto {request.ProductId} não encontrado.");

        // Daily storage cost from product or config default
        var dailyStorageCost = product.StorageCostDaily
            ?? _configuration.GetValue<decimal>("FulfillmentSettings:DefaultDailyStorageCost", 0.50m);

        // Average days in stock from storage cost accumulation history
        var avgDaysInStock = await GetAvgDaysInStockAsync(product.Id, ct);

        // Fulfillment fee per sale from order history or config default
        var fulfillmentFeePerSale = await GetAvgFulfillmentFeeAsync(product.Id, ct);

        // Own shipping: average shipping cost from historical orders
        var avgShippingCost = await GetAvgShippingCostAsync(product.Id, ct);

        // Packaging cost from product
        var packagingCost = product.PackagingCost;

        // Labor cost per shipment: from request override or config
        var laborCostPerShipment = request.LaborCostPerShipment
            ?? _configuration.GetValue<decimal>("FulfillmentSettings:DefaultLaborCostPerShipment", 5.00m);

        // Full cost = (dailyStorageCost x avgDaysInStock) + fulfillmentFeePerSale
        var storageCostTotal = Math.Round(dailyStorageCost * avgDaysInStock, 4);
        var fullCost = Math.Round(storageCostTotal + fulfillmentFeePerSale, 2);

        // Own cost = avgShippingCost + packagingCost + laborCostPerShipment
        var ownShippingCost = Math.Round(avgShippingCost + packagingCost + laborCostPerShipment, 2);

        // Recommendation
        var savings = Math.Round(Math.Abs(fullCost - ownShippingCost), 2);
        var recommendation = fullCost <= ownShippingCost ? "full" : "own_shipping";

        return new FulfillmentCompareResult(
            ProductId: product.Id,
            ProductName: product.Name,
            ProductSku: product.Sku,
            FullCost: fullCost,
            OwnShippingCost: ownShippingCost,
            Recommendation: recommendation,
            SavingsAmount: savings,
            FullBreakdown: new FulfillmentCostBreakdown(
                DailyStorageCost: dailyStorageCost,
                AvgDaysInStock: avgDaysInStock,
                StorageCostTotal: storageCostTotal,
                FulfillmentFeePerSale: fulfillmentFeePerSale,
                Total: fullCost),
            OwnBreakdown: new OwnShippingCostBreakdown(
                AvgShippingCost: avgShippingCost,
                PackagingCost: packagingCost,
                LaborCostPerShipment: laborCostPerShipment,
                Total: ownShippingCost),
            AvgDaysInStock: avgDaysInStock,
            DailyStorageCost: dailyStorageCost,
            FulfillmentFeePerSale: fulfillmentFeePerSale,
            AvgShippingCost: avgShippingCost,
            PackagingCost: packagingCost,
            LaborCostPerShipment: laborCostPerShipment);
    }

    private async Task<decimal> GetAvgDaysInStockAsync(Guid productId, CancellationToken ct)
    {
        // Use storage cost accumulation history for average days in stock
        var avgDays = await _db.StorageCostAccumulations
            .Where(s => s.ProductId == productId)
            .Select(s => (decimal)s.DaysStored)
            .DefaultIfEmpty(30m) // Default 30 days if no history
            .AverageAsync(ct);

        return Math.Round(avgDays, 1);
    }

    private async Task<decimal> GetAvgFulfillmentFeeAsync(Guid productId, CancellationToken ct)
    {
        // Get average fulfillment_fee from historical order costs for this product's orders
        var orderIds = await _db.OrderItems
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync(ct);

        if (orderIds.Count == 0)
            return _configuration.GetValue<decimal>("FulfillmentSettings:DefaultFulfillmentFee", 12.00m);

        var avgFee = await _db.OrderCosts
            .Where(c => orderIds.Contains(c.OrderId) && c.Category == "fulfillment_fee" && c.Value > 0)
            .Select(c => c.Value)
            .DefaultIfEmpty(0m)
            .AverageAsync(ct);

        return avgFee > 0
            ? Math.Round(avgFee, 2)
            : _configuration.GetValue<decimal>("FulfillmentSettings:DefaultFulfillmentFee", 12.00m);
    }

    private async Task<decimal> GetAvgShippingCostAsync(Guid productId, CancellationToken ct)
    {
        // Get average shipping_seller cost from historical orders
        var orderIds = await _db.OrderItems
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync(ct);

        if (orderIds.Count == 0)
            return _configuration.GetValue<decimal>("FulfillmentSettings:DefaultShippingCost", 20.00m);

        var avgShipping = await _db.OrderCosts
            .Where(c => orderIds.Contains(c.OrderId) && c.Category == "shipping_seller" && c.Value > 0)
            .Select(c => c.Value)
            .DefaultIfEmpty(0m)
            .AverageAsync(ct);

        return avgShipping > 0
            ? Math.Round(avgShipping, 2)
            : _configuration.GetValue<decimal>("FulfillmentSettings:DefaultShippingCost", 20.00m);
    }
}
