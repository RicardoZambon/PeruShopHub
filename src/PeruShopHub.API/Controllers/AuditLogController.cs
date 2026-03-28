using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.AuditLogs;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize(Roles = "Owner,Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
    {
        var result = await _auditService.GetListAsync(
            page, pageSize, entityType, entityId,
            dateFrom, dateTo, userId, ct);
        return Ok(result);
    }
}
