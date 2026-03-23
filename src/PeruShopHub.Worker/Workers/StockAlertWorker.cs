using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class StockAlertWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockAlertWorker> _logger;
    private readonly TimeSpan _interval;

    public StockAlertWorker(IServiceProvider services, IConfiguration config, ILogger<StockAlertWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:StockAlert:IntervalMinutes", 15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockAlertWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckStockLevels(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error checking stock levels"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckStockLevels(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var lowStock = await db.Supplies
            .Where(s => s.Status == "Ativo" && s.Stock <= s.MinimumStock)
            .ToListAsync(ct);

        foreach (var supply in lowStock)
        {
            var exists = await db.Notifications.AnyAsync(n =>
                n.Type == "stock" && n.Title.Contains(supply.Name) && !n.IsRead, ct);
            if (exists) continue;

            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), Type = "stock",
                Title = $"Estoque baixo: {supply.Name}",
                Description = $"{supply.Name} tem apenas {supply.Stock} unidades (mínimo: {supply.MinimumStock})",
                Timestamp = DateTime.UtcNow, NavigationTarget = "/suprimentos"
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
