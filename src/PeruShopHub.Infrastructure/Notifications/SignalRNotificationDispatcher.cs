using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Infrastructure.Notifications;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly PeruShopHubDbContext _db;
    private readonly INotificationHubContext _notificationHub;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(PeruShopHubDbContext db, INotificationHubContext notificationHub, ILogger<SignalRNotificationDispatcher> logger)
    {
        _db = db;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _notificationHub.SendNotificationAsync(new
        {
            notification.Id, notification.Type, notification.Title,
            notification.Description, notification.Timestamp,
            IsRead = false, notification.NavigationTarget
        }, ct);

        _logger.LogInformation("Dispatched notification {Id}: {Title}", notification.Id, notification.Title);
    }

    public async Task BroadcastDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default)
    {
        await _notificationHub.SendDataChangeAsync(entityType, action, entityId, ct);
    }
}
