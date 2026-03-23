using PeruShopHub.Core.Entities;

namespace PeruShopHub.Core.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken ct = default);
    Task BroadcastDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default);
}
