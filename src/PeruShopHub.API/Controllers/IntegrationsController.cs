using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Listings;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize(Roles = "Owner,Admin")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly IMlListingImportService _importService;
    private readonly IMarketplaceListingService _listingService;
    private readonly ITenantContext _tenantContext;
    private readonly PeruShopHubDbContext _db;

    public IntegrationsController(
        IIntegrationService integrationService,
        IMlListingImportService importService,
        IMarketplaceListingService listingService,
        ITenantContext tenantContext,
        PeruShopHubDbContext db)
    {
        _integrationService = integrationService;
        _importService = importService;
        _listingService = listingService;
        _tenantContext = tenantContext;
        _db = db;
    }

    [HttpGet("{marketplaceId}/auth-url")]
    public async Task<ActionResult<OAuthInitResult>> GetAuthUrl(string marketplaceId, CancellationToken ct)
    {
        var result = await _integrationService.InitiateOAuthAsync(marketplaceId, ct);
        return Ok(result);
    }

    [HttpGet("{marketplaceId}/callback")]
    public async Task<ActionResult<OAuthCallbackResult>> HandleCallback(
        string marketplaceId,
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { message = "Missing code or state parameter." });

        var result = await _integrationService.HandleOAuthCallbackAsync(marketplaceId, code, state, ct);
        return Ok(result);
    }

    [HttpPost("{marketplaceId}/disconnect")]
    public async Task<IActionResult> Disconnect(string marketplaceId, CancellationToken ct)
    {
        await _integrationService.DisconnectAsync(marketplaceId, ct);
        return NoContent();
    }

    [HttpPost("{marketplaceId}/sync")]
    public async Task<IActionResult> TriggerSync(string marketplaceId, CancellationToken ct)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(m => m.MarketplaceId == marketplaceId, ct);

        if (connection == null)
            return NotFound(new { message = "Marketplace connection not found." });

        if (!connection.IsConnected || connection.Status != "Active")
            return BadRequest(new { message = "Marketplace is not connected or not active." });

        // Update LastSyncAt to indicate sync was triggered
        connection.LastSyncAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { lastSyncAt = connection.LastSyncAt });
    }

    [HttpPost("mercadolivre/import")]
    public async Task<ActionResult<ImportJobStatus>> TriggerImport(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null)
            return Unauthorized();

        var status = await _importService.EnqueueImportAsync(_tenantContext.TenantId.Value, ct);
        return Accepted(status);
    }

    [HttpGet("mercadolivre/import/status")]
    public async Task<ActionResult<ImportJobStatus>> GetImportStatus(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null)
            return Unauthorized();

        var status = await _importService.GetImportStatusAsync(_tenantContext.TenantId.Value, ct);
        if (status is null)
            return Ok(new { status = "None" });

        return Ok(status);
    }

    [HttpGet("mercadolivre/listings")]
    public async Task<ActionResult<PagedResult<ListingGridDto>>> GetAllListings(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? syncStatus = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _listingService.GetAllListingsAsync(
            search, status, syncStatus, sortBy, sortDirection, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("mercadolivre/unlinked-items")]
    public async Task<ActionResult<PagedResult<UnlinkedListingDto>>> GetUnlinkedItems(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _listingService.GetUnlinkedListingsAsync("mercadolivre", search, page, pageSize, ct);
        return Ok(result);
    }
}
