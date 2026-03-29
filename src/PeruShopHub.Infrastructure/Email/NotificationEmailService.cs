using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Infrastructure.Email;

public class NotificationEmailService : INotificationEmailService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationEmailService> _logger;

    public NotificationEmailService(
        PeruShopHubDbContext db,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationEmailService> logger)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendIfEnabledAsync(Notification notification, CancellationToken ct = default)
    {
        var notificationType = NotificationEmailTemplates.MapNotificationType(notification.Type);
        if (notificationType is null)
        {
            _logger.LogDebug("No email mapping for notification type {Type}, skipping email", notification.Type);
            return;
        }

        // Find all users in this tenant who have email enabled for this notification type
        var tenantUsers = await _db.TenantUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(tu => tu.TenantId == notification.TenantId)
            .Select(tu => tu.UserId)
            .ToListAsync(ct);

        if (tenantUsers.Count == 0) return;

        // Check preferences for each user — if no preference record exists, default is enabled
        var preferences = await _db.NotificationPreferences
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(np => np.TenantId == notification.TenantId
                && tenantUsers.Contains(np.UserId)
                && np.Type == notificationType.Value)
            .ToListAsync(ct);

        var prefsDict = preferences.ToDictionary(p => p.UserId);

        var recipientUserIds = tenantUsers
            .Where(userId =>
            {
                if (prefsDict.TryGetValue(userId, out var pref))
                    return pref.EmailEnabled;
                return true; // default: enabled
            })
            .ToList();

        if (recipientUserIds.Count == 0) return;

        // Get emails for recipients
        var emails = await _db.SystemUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => recipientUserIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Email)
            .ToListAsync(ct);

        if (emails.Count == 0) return;

        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
        var htmlBody = NotificationEmailTemplates.BuildEmailBody(
            notification.Title,
            notification.Description,
            notification.NavigationTarget,
            baseUrl);

        try
        {
            await _emailService.SendAsync(emails, $"PeruShopHub — {notification.Title}", htmlBody, textBody: null, ct);
            _logger.LogInformation("Notification email sent to {Count} recipients for {Type}", emails.Count, notification.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification email for {Type} to {Count} recipients", notification.Type, emails.Count);
        }
    }
}
