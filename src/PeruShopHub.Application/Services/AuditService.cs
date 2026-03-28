using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.AuditLogs;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class AuditService : IAuditService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public AuditService(PeruShopHubDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId, object? oldValue, object? newValue, CancellationToken ct = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;
        var userName = user?.FindFirstValue("name")
            ?? user?.FindFirstValue(ClaimTypes.Email)
            ?? "Sistema";

        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue is string s1 ? s1 : (oldValue != null ? JsonSerializer.Serialize(oldValue, JsonOptions) : null),
            NewValue = newValue is string s2 ? s2 : (newValue != null ? JsonSerializer.Serialize(newValue, JsonOptions) : null),
            CreatedAt = DateTime.UtcNow,
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetListAsync(
        int page, int pageSize,
        string? entityType, Guid? entityId,
        DateTime? dateFrom, DateTime? dateTo,
        Guid? userId,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        if (dateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= dateTo.Value);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserName, a.Action,
                a.EntityType, a.EntityId,
                a.OldValue, a.NewValue, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
