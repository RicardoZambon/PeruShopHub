using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;
    private readonly ITenantContext _tenantContext;

    public TenantController(ITenantService tenantService, IUserService userService, ITenantContext tenantContext)
    {
        _tenantService = tenantService;
        _userService = userService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<TenantDetailDto>> Get(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _tenantService.GetByIdAsync(_tenantContext.TenantId.Value, ct));
    }

    [HttpPut]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<TenantDetailDto>> Update([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _tenantService.UpdateAsync(_tenantContext.TenantId.Value, request, ct));
    }

    [HttpGet("members")]
    public async Task<ActionResult<IReadOnlyList<UserDetailDto>>> GetMembers(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _userService.GetTenantMembersAsync(_tenantContext.TenantId.Value, ct));
    }

    [HttpPost("members/invite")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<UserDetailDto>> InviteMember([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        var member = await _userService.InviteMemberAsync(_tenantContext.TenantId.Value, request, ct);
        return Created("", member);
    }

    [HttpPut("members/{userId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<UserDetailDto>> UpdateMember(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _userService.UpdateMemberAsync(_tenantContext.TenantId.Value, userId, request, ct));
    }

    [HttpDelete("members/{userId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        await _userService.RemoveMemberAsync(_tenantContext.TenantId.Value, userId, ct);
        return NoContent();
    }

    [HttpPost("members/{userId:guid}/reset-password")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _userService.ResetPasswordAsync(userId, request, ct);
        return NoContent();
    }
}
