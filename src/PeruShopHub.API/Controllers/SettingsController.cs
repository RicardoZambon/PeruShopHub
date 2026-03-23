using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public SettingsController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<SystemUserDto>>> GetUsers()
    {
        var users = await _db.SystemUsers
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new SystemUserDto(
                u.Id, u.Email, u.Name, u.Role,
                u.IsActive, u.LastLogin, u.CreatedAt))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("integrations")]
    public async Task<ActionResult<IReadOnlyList<IntegrationDto>>> GetIntegrations()
    {
        var integrations = await _db.MarketplaceConnections
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new IntegrationDto(
                m.Id, m.MarketplaceId, m.Name, m.Logo,
                m.IsConnected, m.SellerNickname, m.LastSyncAt, m.ComingSoon))
            .ToListAsync();

        return Ok(integrations);
    }

    [HttpGet("costs")]
    public ActionResult<object> GetCosts()
    {
        var costs = new
        {
            defaultPackagingCost = 2.50m,
            icmsRate = 6.0m,
            fixedCosts = new[]
            {
                new { id = "1", name = "Internet/Telefone", value = 150.00m },
                new { id = "2", name = "Software/Ferramentas", value = 89.90m }
            }
        };

        return Ok(costs);
    }
}
