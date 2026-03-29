using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class StockReconciliationService : IStockReconciliationService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockReconciliationService> _logger;

    public StockReconciliationService(
        PeruShopHubDbContext db,
        IServiceProvider serviceProvider,
        ILogger<StockReconciliationService> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Guid> RunReconciliationAsync(Guid tenantId, int autoCorrectThreshold, CancellationToken ct = default)
    {
        var report = new StockReconciliationReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MarketplaceId = "mercadolivre",
            Status = "Running",
            StartedAt = DateTime.UtcNow,
        };
        _db.StockReconciliationReports.Add(report);
        await _db.SaveChangesAsync(ct);

        try
        {
            // Get all ML-linked listings for this tenant
            var listings = await _db.MarketplaceListings
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId
                    && l.MarketplaceId == "mercadolivre"
                    && l.ProductId != null)
                .Select(l => new
                {
                    l.ExternalId,
                    l.ProductId,
                    ProductName = l.Product!.Name,
                    Variants = l.Product!.Variants.Select(v => new
                    {
                        v.Id,
                        v.Sku,
                        v.Stock,
                        v.ExternalId,
                    }).ToList()
                })
                .ToListAsync(ct);

            if (listings.Count == 0)
            {
                report.Status = "Completed";
                report.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return report.Id;
            }

            // Get allocations for all variant IDs
            var allVariantIds = listings.SelectMany(l => l.Variants.Select(v => v.Id)).Distinct().ToList();
            var allocations = await _db.StockAllocations
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == tenantId
                    && a.MarketplaceId == "mercadolivre"
                    && allVariantIds.Contains(a.ProductVariantId))
                .ToDictionaryAsync(a => a.ProductVariantId, a => a.AllocatedQuantity - a.ReservedQuantity, ct);

            var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
            if (adapter is null)
            {
                report.Status = "Failed";
                report.ErrorMessage = "ML adapter not available";
                report.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return report.Id;
            }

            int matches = 0, discrepancies = 0, autoCorrected = 0, manualReview = 0;
            var items = new List<StockReconciliationReportItem>();

            foreach (var listing in listings)
            {
                MarketplaceProduct? mlProduct = null;
                try
                {
                    mlProduct = await adapter.GetProductAsync(listing.ExternalId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch ML item {ExternalId} during reconciliation", listing.ExternalId);
                }

                // For simple items (no variations), compare at item level
                if (listing.Variants.Count <= 1)
                {
                    var variant = listing.Variants.FirstOrDefault();
                    if (variant is null) continue;

                    var localQty = allocations.TryGetValue(variant.Id, out var allocated)
                        ? Math.Max(0, allocated)
                        : variant.Stock;
                    var mlQty = mlProduct?.AvailableQuantity ?? 0;
                    var diff = localQty - mlQty;

                    var item = CreateReportItem(report, tenantId, variant.Id, variant.Sku,
                        listing.ProductName, listing.ExternalId, localQty, mlQty, diff, autoCorrectThreshold);
                    items.Add(item);
                    CountResolution(item.Resolution, ref matches, ref discrepancies, ref autoCorrected, ref manualReview);

                    if (item.Resolution == "AutoCorrected")
                    {
                        await AutoCorrectStockAsync(tenantId, variant.Id, mlQty, localQty, report.Id, ct);
                    }
                }
                else
                {
                    // For variations, we need item details with per-variation stock
                    MarketplaceItemDetails? details = null;
                    try
                    {
                        details = await adapter.GetItemDetailsAsync(listing.ExternalId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch ML item details {ExternalId} during reconciliation", listing.ExternalId);
                    }

                    foreach (var variant in listing.Variants)
                    {
                        var localQty = allocations.TryGetValue(variant.Id, out var allocated)
                            ? Math.Max(0, allocated)
                            : variant.Stock;

                        var mlQty = 0;
                        if (details is not null && variant.ExternalId is not null)
                        {
                            var mlVariation = details.Variations.FirstOrDefault(
                                v => v.ExternalVariationId == variant.ExternalId);
                            mlQty = mlVariation?.AvailableQuantity ?? 0;
                        }
                        else if (mlProduct is not null && listing.Variants.Count == 1)
                        {
                            mlQty = mlProduct.AvailableQuantity;
                        }

                        var diff = localQty - mlQty;
                        var item = CreateReportItem(report, tenantId, variant.Id, variant.Sku,
                            listing.ProductName, listing.ExternalId, localQty, mlQty, diff, autoCorrectThreshold);
                        items.Add(item);
                        CountResolution(item.Resolution, ref matches, ref discrepancies, ref autoCorrected, ref manualReview);

                        if (item.Resolution == "AutoCorrected")
                        {
                            await AutoCorrectStockAsync(tenantId, variant.Id, mlQty, localQty, report.Id, ct);
                        }
                    }
                }
            }

            // Update report with results
            report.ItemsChecked = items.Count;
            report.Matches = matches;
            report.Discrepancies = discrepancies;
            report.AutoCorrected = autoCorrected;
            report.ManualReviewRequired = manualReview;
            report.Status = "Completed";
            report.CompletedAt = DateTime.UtcNow;

            _db.StockReconciliationReportItems.AddRange(items);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Reconciliation completed for tenant {TenantId}: {ItemsChecked} checked, {Matches} matches, {Discrepancies} discrepancies ({AutoCorrected} auto-corrected, {ManualReview} manual review)",
                tenantId, items.Count, matches, discrepancies, autoCorrected, manualReview);

            return report.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconciliation failed for tenant {TenantId}", tenantId);
            report.Status = "Failed";
            report.ErrorMessage = ex.Message;
            report.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return report.Id;
        }
    }

    public async Task<PagedResult<ReconciliationReportDto>> GetReportsAsync(
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.StockReconciliationReports.AsNoTracking().AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(r => r.StartedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(r => r.StartedAt <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReconciliationReportDto(
                r.Id, r.MarketplaceId, r.ItemsChecked, r.Matches, r.Discrepancies,
                r.AutoCorrected, r.ManualReviewRequired, r.Status, r.ErrorMessage,
                r.StartedAt, r.CompletedAt))
            .ToListAsync(ct);

        return new PagedResult<ReconciliationReportDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ReconciliationReportDetailDto> GetReportDetailAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.StockReconciliationReports.AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException($"Reconciliation report {reportId} not found");

        var items = report.Items
            .OrderByDescending(i => Math.Abs(i.Difference))
            .Select(i => new ReconciliationReportItemDto(
                i.Id, i.ProductVariantId, i.Sku, i.ProductName, i.ExternalId,
                i.LocalQuantity, i.MarketplaceQuantity, i.Difference, i.Resolution, i.Notes))
            .ToList();

        return new ReconciliationReportDetailDto(
            report.Id, report.MarketplaceId, report.ItemsChecked, report.Matches,
            report.Discrepancies, report.AutoCorrected, report.ManualReviewRequired,
            report.Status, report.ErrorMessage, report.StartedAt, report.CompletedAt, items);
    }

    private static StockReconciliationReportItem CreateReportItem(
        StockReconciliationReport report, Guid tenantId, Guid variantId,
        string sku, string productName, string externalId,
        int localQty, int mlQty, int diff, int autoCorrectThreshold)
    {
        string resolution;
        string? notes = null;

        if (diff == 0)
        {
            resolution = "Match";
        }
        else if (Math.Abs(diff) <= autoCorrectThreshold)
        {
            resolution = "AutoCorrected";
            notes = $"Auto-corrected: local {localQty} → {mlQty} (diff: {diff})";
        }
        else
        {
            resolution = "ManualReview";
            notes = $"Large discrepancy: local={localQty}, ML={mlQty}, diff={diff}";
        }

        return new StockReconciliationReportItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = report.Id,
            ProductVariantId = variantId,
            Sku = sku,
            ProductName = productName,
            ExternalId = externalId,
            LocalQuantity = localQty,
            MarketplaceQuantity = mlQty,
            Difference = diff,
            Resolution = resolution,
            Notes = notes,
        };
    }

    private static void CountResolution(string resolution, ref int matches, ref int discrepancies, ref int autoCorrected, ref int manualReview)
    {
        switch (resolution)
        {
            case "Match":
                matches++;
                break;
            case "AutoCorrected":
                discrepancies++;
                autoCorrected++;
                break;
            case "ManualReview":
                discrepancies++;
                manualReview++;
                break;
        }
    }

    private async Task AutoCorrectStockAsync(Guid tenantId, Guid variantId, int mlQty, int localQty, Guid reportId, CancellationToken ct)
    {
        // Update local stock to match ML
        var variant = await _db.ProductVariants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.TenantId == tenantId, ct);

        if (variant is null) return;

        var oldStock = variant.Stock;
        var adjustment = mlQty - localQty;

        // Update variant stock
        variant.Stock = Math.Max(0, variant.Stock + adjustment);

        // Create stock movement for audit trail
        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = variant.ProductId,
            VariantId = variantId,
            Type = "Reconciliação",
            Quantity = adjustment,
            Reason = $"Auto-correção reconciliação ML (local: {localQty}, ML: {mlQty})",
            CreatedBy = "system:reconciliation",
            CreatedAt = DateTime.UtcNow,
        });

        // Update allocation if exists
        var allocation = await _db.StockAllocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId
                && a.ProductVariantId == variantId
                && a.MarketplaceId == "mercadolivre", ct);

        if (allocation is not null)
        {
            allocation.AllocatedQuantity = Math.Max(0, allocation.AllocatedQuantity + adjustment);
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Auto-corrected stock for variant {VariantId}: {OldStock} → {NewStock} (ML: {MlQty})",
            variantId, oldStock, variant.Stock, mlQty);
    }
}
