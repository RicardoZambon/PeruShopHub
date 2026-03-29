using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Infrastructure.Notifications;

/// <summary>
/// Notification dispatcher that saves to DB only (no SignalR).
/// Used in the Worker project which doesn't have a SignalR hub.
/// </summary>
public class DbOnlyNotificationDispatcher : INotificationDispatcher
{
    private readonly PeruShopHubDbContext _db;
    private readonly INotificationEmailService _notificationEmailService;
    private readonly ILogger<DbOnlyNotificationDispatcher> _logger;

    public DbOnlyNotificationDispatcher(
        PeruShopHubDbContext db,
        INotificationEmailService notificationEmailService,
        ILogger<DbOnlyNotificationDispatcher> logger)
    {
        _db = db;
        _notificationEmailService = notificationEmailService;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _notificationEmailService.SendIfEnabledAsync(notification, ct);

        _logger.LogInformation("Dispatched notification {Id}: {Title} (DB only)", notification.Id, notification.Title);
    }

    public Task BroadcastDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Data change: {EntityType} {Action} {EntityId} (no SignalR in Worker)", entityType, action, entityId);
        return Task.CompletedTask;
    }
}
