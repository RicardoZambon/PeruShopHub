using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class SkuProfitabilityRefreshWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SkuProfitabilityRefreshWorker> _logger;
    private readonly TimeSpan _interval;

    public SkuProfitabilityRefreshWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<SkuProfitabilityRefreshWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:SkuProfitabilityRefresh:IntervalMinutes", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SkuProfitabilityRefreshWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshView(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing sku_profitability materialized view");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RefreshView(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        _logger.LogInformation("Refreshing sku_profitability materialized view...");
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY sku_profitability;", ct);
        _logger.LogInformation("sku_profitability materialized view refreshed successfully");
    }
}
