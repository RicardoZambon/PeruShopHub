using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Files;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadDto>> Upload(
        IFormFile file,
        [FromForm] string entityType,
        [FromForm] Guid entityId,
        [FromForm] int sortOrder = 0,
        CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var result = await _fileService.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length,
            entityType, entityId, sortOrder, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FileUploadDto>>> GetFiles(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken ct = default)
    {
        var result = await _fileService.GetByEntityAsync(entityType, entityId, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _fileService.DeleteAsync(id, ct);
        return NoContent();
    }
}
