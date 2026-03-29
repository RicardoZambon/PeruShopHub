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
    private readonly IUserDataExportService _exportService;

    public ProfileController(IUserService userService, IFileStorageService fileStorage, IUserDataExportService exportService)
    {
        _userService = userService;
        _fileStorage = fileStorage;
        _exportService = exportService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid GetTenantId() => Guid.Parse(User.FindFirstValue("tenant_id")!);

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

    [HttpPost("export-data")]
    public async Task<ActionResult<UserDataExportDto>> RequestDataExport(CancellationToken ct)
    {
        var result = await _exportService.RequestExportAsync(GetUserId(), GetTenantId(), ct);
        return Ok(result);
    }

    [HttpGet("export-data/{id:guid}")]
    public async Task<ActionResult<UserDataExportDto>> GetExportStatus(Guid id, CancellationToken ct)
    {
        var result = await _exportService.GetExportStatusAsync(id, GetUserId(), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("export-data/{id:guid}/download")]
    public async Task<IActionResult> DownloadExport(Guid id, CancellationToken ct)
    {
        var result = await _exportService.DownloadExportAsync(id, GetUserId(), ct);
        if (result is null) return NotFound(new { message = "Exportação não encontrada ou expirada." });
        return File(result.Value.Data, "application/zip", result.Value.FileName);
    }

    [HttpPost("delete-account")]
    public async Task<ActionResult<AccountDeletionDto>> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken ct)
    {
        var result = await _userService.RequestAccountDeletionAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPost("cancel-deletion")]
    public async Task<IActionResult> CancelDeletion(CancellationToken ct)
    {
        await _userService.CancelAccountDeletionAsync(GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("deletion-status")]
    public async Task<ActionResult<AccountDeletionDto?>> GetDeletionStatus(CancellationToken ct)
    {
        var result = await _userService.GetPendingDeletionAsync(GetUserId(), ct);
        return Ok(result);
    }
}
