using PeruShopHub.Core.Entities;

namespace PeruShopHub.Core.Interfaces;

public interface INotificationEmailService
{
    Task SendIfEnabledAsync(Notification notification, CancellationToken ct = default);
}
