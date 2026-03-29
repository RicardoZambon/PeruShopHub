using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Profile;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorage;

    public ProfileController(IUserService userService, IFileStorageService fileStorage)
    {
        _userService = userService;
        _fileStorage = fileStorage;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct)
    {
        return Ok(await _userService.GetProfileAsync(GetUserId(), ct));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        return Ok(await _userService.UpdateProfileAsync(GetUserId(), request, ct));
    }

    [HttpPut("email")]
    public async Task<ActionResult<ProfileDto>> UpdateEmail([FromBody] UpdateProfileEmailRequest request, CancellationToken ct)
    {
        return Ok(await _userService.UpdateProfileEmailAsync(GetUserId(), request, ct));
    }

    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _userService.ChangePasswordAsync(GetUserId(), request, ct);
        return NoContent();
    }

    [HttpPost("avatar")]
    public async Task<ActionResult<ProfileDto>> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Arquivo é obrigatório." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Formato inválido. Use JPG, PNG ou WebP." });

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { message = "Arquivo deve ter no máximo 2MB." });

        var userId = GetUserId();

        // Remove old avatar if exists
        var current = await _userService.GetProfileAsync(userId, ct);
        if (!string.IsNullOrEmpty(current.AvatarUrl))
        {
            try { await _fileStorage.DeleteAsync(current.AvatarUrl, ct); } catch { /* ignore */ }
        }

        await using var stream = file.OpenReadStream();
        var storagePath = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType, "avatars", ct);

        return Ok(await _userService.UpdateProfileAvatarAsync(userId, storagePath, ct));
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct)
    {
        var userId = GetUserId();
        var current = await _userService.GetProfileAsync(userId, ct);

        if (!string.IsNullOrEmpty(current.AvatarUrl))
        {
            try { await _fileStorage.DeleteAsync(current.AvatarUrl, ct); } catch { /* ignore */ }
        }

        await _userService.RemoveProfileAvatarAsync(userId, ct);
        return NoContent();
    }
}
