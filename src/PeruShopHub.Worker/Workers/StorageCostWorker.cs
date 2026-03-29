using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Daily worker that calculates storage costs for Full (fulfillment) products.
/// Runs at midnight. Accumulates daily costs per product with penalty multipliers
/// based on days stored.
/// </summary>
public class StorageCostWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StorageCostWorker> _logger;

    // Daily cost by size category (R$/unit/day)
    private static readonly Dictionary<string, decimal> SizeCosts = new()
    {
        ["Pequeno"] = 0.007m,
        ["Medio"] = 0.015m,
        ["Grande"] = 0.035m,
        ["Especial"] = 0.050m,
        ["Extra"] = 0.107m,
    };

    public StorageCostWorker(
        IServiceProvider services,
        ILogger<StorageCostWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StorageCostWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("StorageCostWorker: next run in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunForAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StorageCostWorker cycle");
            }
        }
    }

    private async Task RunForAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        // Get all tenants that have products (any tenant with products)
        var tenantIds = await db.Products
            .IgnoreQueryFilters()
            .Select(p => p.TenantId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("StorageCostWorker: processing {Count} tenants", tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await CalculateStorageCostsForTenantAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StorageCostWorker failed for tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task CalculateStorageCostsForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var today = DateTime.UtcNow.Date;

        // Find products linked to Full listings for this tenant
        var fullProductIds = await db.MarketplaceListings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId
                && l.FulfillmentType == "fulfillment"
                && l.ProductId != null)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (fullProductIds.Count == 0) return;

        var products = await db.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && fullProductIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var product in products)
        {
            // Check if already calculated for today
            var exists = await db.StorageCostAccumulations
                .IgnoreQueryFilters()
                .AnyAsync(s => s.ProductId == product.Id && s.Date == today, ct);

            if (exists) continue;

            var sizeCategory = ClassifySize(product);
            var baseDailyCost = SizeCosts.GetValueOrDefault(sizeCategory, SizeCosts["Medio"]);

            // Get the previous accumulation to determine days stored
            var lastAccum = await db.StorageCostAccumulations
                .IgnoreQueryFilters()
                .Where(s => s.ProductId == product.Id)
                .OrderByDescending(s => s.Date)
                .FirstOrDefaultAsync(ct);

            var daysStored = (lastAccum?.DaysStored ?? 0) + 1;
            var penaltyMultiplier = GetPenaltyMultiplier(daysStored);
            var dailyCost = baseDailyCost * penaltyMultiplier;
            var cumulativeCost = (lastAccum?.CumulativeCost ?? 0m) + dailyCost;

            var accumulation = new StorageCostAccumulation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = product.Id,
                Date = today,
                DailyCost = dailyCost,
                CumulativeCost = cumulativeCost,
                DaysStored = daysStored,
                SizeCategory = sizeCategory,
                PenaltyMultiplier = penaltyMultiplier,
            };

            db.StorageCostAccumulations.Add(accumulation);
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "StorageCostWorker: calculated storage costs for {Count} products in tenant {TenantId}",
            products.Count, tenantId);
    }

    /// <summary>
    /// Classify product size based on dimensions (volume in cm³).
    /// </summary>
    internal static string ClassifySize(Product product)
    {
        var volume = product.Height * product.Width * product.Length;

        return volume switch
        {
            <= 5000m => "Pequeno",     // ≤ 5,000 cm³
            <= 20000m => "Medio",      // ≤ 20,000 cm³
            <= 60000m => "Grande",     // ≤ 60,000 cm³
            <= 120000m => "Especial",  // ≤ 120,000 cm³
            _ => "Extra",
        };
    }

    /// <summary>
    /// Get penalty multiplier based on days in storage.
    /// </summary>
    internal static decimal GetPenaltyMultiplier(int daysStored)
    {
        return daysStored switch
        {
            <= 90 => 1m,
            <= 180 => 2m,
            <= 365 => 3m,
            _ => 4m,
        };
    }
}
