using PeruShopHub.Application.Services;

namespace PeruShopHub.Worker.Workers;

public class AccountDeletionWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AccountDeletionWorker> _logger;
    private readonly TimeSpan _interval;

    public AccountDeletionWorker(IServiceProvider services, IConfiguration config, ILogger<AccountDeletionWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(config.GetValue("Workers:AccountDeletion:IntervalSeconds", 3600));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccountDeletionWorker started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ProcessExpiredDeletionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing expired account deletions");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
