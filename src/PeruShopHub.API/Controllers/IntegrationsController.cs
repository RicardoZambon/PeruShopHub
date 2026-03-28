using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize(Roles = "Owner,Admin")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly PeruShopHubDbContext _db;

    public IntegrationsController(IIntegrationService integrationService, PeruShopHubDbContext db)
    {
        _integrationService = integrationService;
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
}
