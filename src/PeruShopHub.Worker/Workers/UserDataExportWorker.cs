using PeruShopHub.Application.Services;

namespace PeruShopHub.Worker.Workers;

public class UserDataExportWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<UserDataExportWorker> _logger;
    private readonly TimeSpan _interval;

    public UserDataExportWorker(IServiceProvider services, IConfiguration config, ILogger<UserDataExportWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(config.GetValue("Workers:DataExport:IntervalSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserDataExportWorker started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var exportService = scope.ServiceProvider.GetRequiredService<IUserDataExportService>();
                await exportService.ProcessPendingExportsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing user data exports");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
