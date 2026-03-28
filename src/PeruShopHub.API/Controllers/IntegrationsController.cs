using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize(Roles = "Owner,Admin")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;

    public IntegrationsController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
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
}
