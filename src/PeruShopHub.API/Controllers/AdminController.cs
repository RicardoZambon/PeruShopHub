using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public AdminController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantDetailDto>>> GetTenants(CancellationToken ct)
    {
        return Ok(await _tenantService.GetAllAsync(ct));
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> GetTenant(Guid id, CancellationToken ct)
    {
        return Ok(await _tenantService.GetByIdAsync(id, ct));
    }

    [HttpPut("tenants/{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken ct)
    {
        await _tenantService.SetActiveAsync(id, true, ct);
        return NoContent();
    }

    [HttpPut("tenants/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken ct)
    {
        await _tenantService.SetActiveAsync(id, false, ct);
        return NoContent();
    }
}
