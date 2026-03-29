using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that syncs ML questions every 5 minutes.
/// Iterates all active ML connections and syncs unanswered + recently answered questions.
/// </summary>
public class QuestionSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QuestionSyncWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public QuestionSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<QuestionSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromMinutes(config.GetValue("Workers:QuestionSync:IntervalMinutes", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuestionSyncWorker started. Interval: {Interval}", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuestionSyncWorker");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task SyncAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var tenantIds = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre" && c.IsConnected && c.Status == "Active")
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var syncScope = _services.CreateScope();
                var questionService = syncScope.ServiceProvider.GetRequiredService<IMarketplaceQuestionService>();
                await questionService.SyncQuestionsAsync(tenantId, ct);

                _logger.LogDebug("Synced questions for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync questions for tenant {TenantId}", tenantId);
            }
        }
    }
}
