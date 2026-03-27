using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Files;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly IFileStorageService _storage;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public FilesController(PeruShopHubDbContext db, IFileStorageService storage) { _db = db; _storage = storage; }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadDto>> Upload(IFormFile file, [FromForm] string entityType, [FromForm] Guid entityId, [FromForm] int sortOrder = 0, CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest("Empty file");
        if (file.Length > MaxFileSize) return BadRequest("File exceeds 5MB limit");
        if (!AllowedTypes.Contains(file.ContentType)) return BadRequest("File type not allowed");

        await using var stream = file.OpenReadStream();
        var storagePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType, entityType, ct);

        var upload = new FileUpload
        {
            Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId,
            FileName = file.FileName, StoragePath = storagePath,
            ContentType = file.ContentType, SizeBytes = file.Length, SortOrder = sortOrder
        };

        _db.FileUploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        return Ok(new FileUploadDto(upload.Id, _storage.GetPublicUrl(upload.StoragePath), upload.FileName, upload.ContentType, upload.SizeBytes, upload.SortOrder));
    }

    [HttpGet]
    public async Task<ActionResult<List<FileUploadDto>>> GetFiles([FromQuery] string entityType, [FromQuery] Guid entityId, CancellationToken ct)
    {
        var files = await _db.FileUploads
            .Where(f => f.EntityType == entityType && f.EntityId == entityId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FileUploadDto(f.Id, _storage.GetPublicUrl(f.StoragePath), f.FileName, f.ContentType, f.SizeBytes, f.SortOrder))
            .ToListAsync(ct);
        return Ok(files);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var file = await _db.FileUploads.FindAsync([id], ct);
        if (file is null) return NotFound();
        await _storage.DeleteAsync(file.StoragePath, ct);
        _db.FileUploads.Remove(file);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
