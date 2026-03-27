using PeruShopHub.Application.DTOs.Notifications;

namespace PeruShopHub.Application.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetListAsync(CancellationToken ct = default);
    Task MarkReadAsync(Guid id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}
