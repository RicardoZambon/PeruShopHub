using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Claims;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class ClaimService : IClaimService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        PeruShopHubDbContext db,
        IServiceProvider serviceProvider,
        ILogger<ClaimService> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PagedResult<ClaimListDto>> GetListAsync(
        string? status, string? type, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _db.Claims.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(c => c.Type == type);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClaimListDto(
                c.Id,
                c.ExternalId,
                c.OrderId,
                c.ExternalOrderId,
                c.Type,
                c.Status,
                c.Reason,
                c.BuyerName,
                c.ProductName,
                c.Quantity,
                c.Amount,
                c.CreatedAt,
                c.ResolvedAt))
            .ToListAsync(ct);

        return new PagedResult<ClaimListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ClaimDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Reclamação não encontrada.");

        return new ClaimDetailDto(
            c.Id, c.ExternalId, c.OrderId, c.ExternalOrderId,
            c.Type, c.Status, c.Reason, c.BuyerComment, c.SellerComment,
            c.BuyerName, c.Resolution, c.ProductId, c.ProductName,
            c.Quantity, c.Amount, c.CreatedAt, c.ResolvedAt, c.UpdatedAt);
    }

    public async Task<ClaimDetailDto> RespondAsync(Guid id, RespondClaimRequest request, CancellationToken ct = default)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Reclamação não encontrada.");

        claim.SellerComment = request.SellerComment;
        claim.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new ClaimDetailDto(
            claim.Id, claim.ExternalId, claim.OrderId, claim.ExternalOrderId,
            claim.Type, claim.Status, claim.Reason, claim.BuyerComment, claim.SellerComment,
            claim.BuyerName, claim.Resolution, claim.ProductId, claim.ProductName,
            claim.Quantity, claim.Amount, claim.CreatedAt, claim.ResolvedAt, claim.UpdatedAt);
    }

    public async Task<ClaimSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var openCount = await _db.Claims.CountAsync(c => c.Status == "opened", ct);
        var closedCount = await _db.Claims.CountAsync(c => c.Status != "opened", ct);

        // Return rate: claims / total orders in last 30 days
        var since = DateTime.UtcNow.AddDays(-30);
        var totalOrders = await _db.Orders.CountAsync(o => o.CreatedAt >= since, ct);
        var totalClaims = await _db.Claims.CountAsync(c => c.CreatedAt >= since, ct);
        var returnRate = totalOrders > 0 ? Math.Round((decimal)totalClaims / totalOrders * 100, 2) : 0;

        return new ClaimSummaryDto(openCount, closedCount, returnRate);
    }

    public async Task SyncClaimsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId
                && c.MarketplaceId == "mercadolivre"
                && c.IsConnected
                && c.Status == "Active", ct);

        if (connection is null)
        {
            _logger.LogDebug("No active ML connection for tenant {TenantId}. Skipping claim sync", tenantId);
            return;
        }

        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
        if (adapter is null)
        {
            _logger.LogWarning("ML adapter not available for claim sync");
            return;
        }

        await SyncClaimsByStatusAsync(adapter, tenantId, "opened", ct);
        await SyncClaimsByStatusAsync(adapter, tenantId, null, ct); // all recent
    }

    public async Task SyncSingleClaimAsync(string externalClaimId, Guid tenantId, CancellationToken ct = default)
    {
        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
        if (adapter is null)
        {
            _logger.LogWarning("ML adapter not available for single claim sync");
            return;
        }

        var detail = await adapter.GetClaimAsync(externalClaimId, ct);
        await UpsertClaimAsync(detail, tenantId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Synced claim {ClaimId} for tenant {TenantId}", externalClaimId, tenantId);
    }

    private async Task SyncClaimsByStatusAsync(
        IMarketplaceAdapter adapter, Guid tenantId, string? status, CancellationToken ct)
    {
        var offset = 0;
        const int limit = 50;

        do
        {
            ct.ThrowIfCancellationRequested();

            var result = await adapter.SearchClaimsAsync(status, offset, limit, ct);

            foreach (var claim in result.Claims)
            {
                await UpsertClaimAsync(claim, tenantId, ct);
            }

            await _db.SaveChangesAsync(ct);

            if (result.Claims.Count < limit)
                break;

            offset += limit;
        }
        while (true);
    }

    private async Task UpsertClaimAsync(MarketplaceClaimDetail detail, Guid tenantId, CancellationToken ct)
    {
        var existing = await _db.Claims
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ExternalId == detail.ExternalId, ct);

        // Try to link to an order
        Guid? orderId = null;
        if (!string.IsNullOrWhiteSpace(detail.OrderId))
        {
            orderId = await _db.Orders
                .IgnoreQueryFilters()
                .Where(o => o.TenantId == tenantId && o.ExternalOrderId == detail.OrderId)
                .Select(o => o.Id)
                .FirstOrDefaultAsync(ct);
        }

        // Try to link to a product via listing
        Guid? productId = null;
        if (!string.IsNullOrWhiteSpace(detail.ItemId))
        {
            productId = await _db.MarketplaceListings
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId && l.ExternalId == detail.ItemId)
                .Select(l => l.ProductId)
                .FirstOrDefaultAsync(ct);
        }

        if (existing is null)
        {
            var claim = new MarketplaceClaim
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExternalId = detail.ExternalId,
                ExternalOrderId = detail.OrderId ?? string.Empty,
                OrderId = orderId,
                Type = detail.Type,
                Status = detail.Status,
                Reason = detail.Reason,
                BuyerComment = detail.BuyerComment,
                BuyerName = detail.BuyerNickname,
                Resolution = detail.Resolution,
                ProductId = productId,
                ProductName = detail.ItemTitle,
                Quantity = detail.Quantity,
                Amount = detail.ClaimAmount,
                CreatedAt = detail.DateCreated.UtcDateTime,
                ResolvedAt = detail.DateClosed?.UtcDateTime,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Claims.Add(claim);
        }
        else
        {
            existing.Status = detail.Status;
            existing.Resolution = detail.Resolution;
            existing.OrderId = orderId ?? existing.OrderId;
            existing.ProductId = productId ?? existing.ProductId;
            existing.ProductName = detail.ItemTitle ?? existing.ProductName;
            existing.ResolvedAt = detail.DateClosed?.UtcDateTime;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
