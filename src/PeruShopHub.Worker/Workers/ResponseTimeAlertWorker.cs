using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class ResponseTimeAlertWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ResponseTimeAlertWorker> _logger;
    private readonly TimeSpan _interval;

    private const int DefaultQuestionThresholdHours = 4;
    private const int DefaultMessageThresholdHours = 12;

    public ResponseTimeAlertWorker(IServiceProvider services, IConfiguration config, ILogger<ResponseTimeAlertWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:ResponseTimeAlert:IntervalMinutes", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ResponseTimeAlertWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckResponseTimes(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error checking response times"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckResponseTimes(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
        var notificationDispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        // Get all tenants
        var tenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await CheckTenantResponseTimes(db, notificationDispatcher, tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking response times for tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task CheckTenantResponseTimes(
        PeruShopHubDbContext db,
        INotificationDispatcher notificationDispatcher,
        Guid tenantId,
        CancellationToken ct)
    {
        // Get tenant-specific thresholds or defaults
        var settings = await db.ResponseTimeSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        var questionThresholdHours = settings?.QuestionThresholdHours ?? DefaultQuestionThresholdHours;
        var messageThresholdHours = settings?.MessageThresholdHours ?? DefaultMessageThresholdHours;

        var now = DateTime.UtcNow;

        // Check unanswered questions older than threshold
        var questionCutoff = now.AddHours(-questionThresholdHours);
        var unansweredQuestions = await db.MarketplaceQuestions
            .IgnoreQueryFilters()
            .Where(q => q.TenantId == tenantId
                && q.Status == "UNANSWERED"
                && q.QuestionDate <= questionCutoff)
            .Select(q => new { q.Id, q.QuestionDate })
            .ToListAsync(ct);

        if (unansweredQuestions.Count > 0)
        {
            var oldest = unansweredQuestions.Min(q => q.QuestionDate);
            var ageHours = (int)(now - oldest).TotalHours;

            // Check if a similar unread notification already exists
            var existsQuestion = await db.Notifications
                .IgnoreQueryFilters()
                .AnyAsync(n => n.TenantId == tenantId
                    && n.Type == "unanswered_question"
                    && !n.IsRead, ct);

            if (!existsQuestion)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = "unanswered_question",
                    Title = "Perguntas sem resposta",
                    Description = $"{unansweredQuestions.Count} pergunta(s) sem resposta há mais de {questionThresholdHours}h. A mais antiga tem {ageHours}h.",
                    NavigationTarget = "/perguntas?status=unanswered",
                    Timestamp = DateTime.UtcNow,
                };

                await notificationDispatcher.DispatchAsync(notification, ct);

                _logger.LogInformation(
                    "Unanswered question alert for tenant {TenantId}: {Count} questions, oldest {AgeHours}h",
                    tenantId, unansweredQuestions.Count, ageHours);
            }
        }

        // Check unread buyer messages older than threshold
        var messageCutoff = now.AddHours(-messageThresholdHours);
        var unreadMessages = await db.MarketplaceMessages
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == tenantId
                && m.SenderType == "buyer"
                && !m.IsRead
                && m.SentAt <= messageCutoff)
            .Select(m => new { m.Id, m.SentAt })
            .ToListAsync(ct);

        if (unreadMessages.Count > 0)
        {
            var oldest = unreadMessages.Min(m => m.SentAt);
            var ageHours = (int)(now - oldest).TotalHours;

            var existsMessage = await db.Notifications
                .IgnoreQueryFilters()
                .AnyAsync(n => n.TenantId == tenantId
                    && n.Type == "unanswered_message"
                    && !n.IsRead, ct);

            if (!existsMessage)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = "unanswered_message",
                    Title = "Mensagens não lidas",
                    Description = $"{unreadMessages.Count} mensagem(ns) de compradores não lida(s) há mais de {messageThresholdHours}h. A mais antiga tem {ageHours}h.",
                    NavigationTarget = "/mensagens?filter=unread",
                    Timestamp = DateTime.UtcNow,
                };

                await notificationDispatcher.DispatchAsync(notification, ct);

                _logger.LogInformation(
                    "Unread message alert for tenant {TenantId}: {Count} messages, oldest {AgeHours}h",
                    tenantId, unreadMessages.Count, ageHours);
            }
        }
    }
}
