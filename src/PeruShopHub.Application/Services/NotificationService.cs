using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Notifications;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class NotificationService : INotificationService
{
    private readonly PeruShopHubDbContext _db;

    public NotificationService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetListAsync(CancellationToken ct = default)
    {
        return await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Timestamp)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Description,
                n.Timestamp, n.IsRead, n.NavigationTarget))
            .ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FindAsync([id], ct)
            ?? throw new NotFoundException("Notificação", id);

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
