using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class TokenRenewalWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenRenewalWorker> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRefreshErrors = 3;

    public TokenRenewalWorker(IServiceProvider services, IConfiguration config, ILogger<TokenRenewalWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:TokenRenewal:IntervalMinutes", 15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenRenewalWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RenewExpiringTokens(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error renewing tokens"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RenewExpiringTokens(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
        var tokenEncryption = scope.ServiceProvider.GetRequiredService<ITokenEncryptionService>();

        var expirationThreshold = DateTime.UtcNow.AddMinutes(30);

        // Query connections expiring within 30 minutes that are still Active
        var connections = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.Status == "Active"
                     && c.TokenExpiresAt != null
                     && c.TokenExpiresAt <= expirationThreshold
                     && c.RefreshTokenProtected != null)
            .ToListAsync(ct);

        if (connections.Count == 0) return;

        _logger.LogInformation("Found {Count} connections with tokens expiring before {Threshold}",
            connections.Count, expirationThreshold);

        foreach (var connection in connections)
        {
            try
            {
                await RenewConnectionToken(db, tokenEncryption, connection, scope.ServiceProvider, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renew token for connection {ConnectionId} (Tenant: {TenantId}, Marketplace: {Marketplace})",
                    connection.Id, connection.TenantId, connection.MarketplaceId);

                connection.RefreshErrorCount++;

                if (connection.RefreshErrorCount >= MaxRefreshErrors)
                {
                    connection.Status = "Error";
                    _logger.LogWarning("Connection {ConnectionId} marked as Error after {Count} consecutive refresh failures",
                        connection.Id, connection.RefreshErrorCount);

                    db.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = connection.TenantId,
                        Type = "token_renewal_failed",
                        Title = "Reconexão necessária",
                        Description = "Sua conexão com o Mercado Livre precisa ser reconectada",
                        Timestamp = DateTime.UtcNow,
                        NavigationTarget = "/integracao/configuracoes"
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task RenewConnectionToken(
        PeruShopHubDbContext db,
        ITokenEncryptionService tokenEncryption,
        MarketplaceConnection connection,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        _logger.LogInformation("Renewing token for connection {ConnectionId} (Tenant: {TenantId}, Marketplace: {Marketplace})",
            connection.Id, connection.TenantId, connection.MarketplaceId);

        var adapter = serviceProvider.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogWarning("No adapter registered for marketplace '{Marketplace}', skipping connection {ConnectionId}",
                connection.MarketplaceId, connection.Id);
            return;
        }

        var refreshToken = tokenEncryption.Decrypt(connection.RefreshTokenProtected!);
        var result = await adapter.RefreshTokenAsync(refreshToken, ct);

        // Success: update tokens and reset error count
        connection.AccessTokenProtected = tokenEncryption.Encrypt(result.AccessToken);
        connection.RefreshTokenProtected = tokenEncryption.Encrypt(result.RefreshToken);
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresInSeconds);
        connection.RefreshErrorCount = 0;

        _logger.LogInformation("Token renewed successfully for connection {ConnectionId}. New expiry: {ExpiresAt}",
            connection.Id, connection.TokenExpiresAt);
    }
}
