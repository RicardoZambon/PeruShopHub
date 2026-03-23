namespace PeruShopHub.Core.Interfaces;

public interface INotificationHubContext
{
    Task SendNotificationAsync(object notification, CancellationToken ct = default);
    Task SendDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default);
}
