using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;
using StackExchange.Redis;

namespace PeruShopHub.Infrastructure.Cache;

public class RedisWebhookQueueService : IWebhookQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisWebhookQueueService> _logger;
    private static readonly TimeSpan DeduplicationTtl = TimeSpan.FromHours(24);

    public RedisWebhookQueueService(
        IConnectionMultiplexer redis,
        ILogger<RedisWebhookQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task EnqueueAsync(string topic, string payload, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"ml:webhooks:{topic}";
        await db.ListLeftPushAsync(key, payload);
        _logger.LogDebug("Enqueued webhook to {Key}", key);
    }

    public async Task<string?> DequeueAsync(string topic, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"ml:webhooks:{topic}";
        var value = await db.ListRightPopAsync(key);
        return value.IsNull ? null : value.ToString();
    }

    public async Task EnqueueDeadLetterAsync(string payload, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync("ml:webhooks:dead", payload);
        _logger.LogWarning("Webhook moved to dead letter queue");
    }

    public async Task<bool> IsDuplicateAsync(string notificationId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"ml:webhooks:seen:{notificationId}";
        return await db.KeyExistsAsync(key);
    }

    public async Task MarkAsSeenAsync(string notificationId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"ml:webhooks:seen:{notificationId}";
        await db.StringSetAsync(key, "1", DeduplicationTtl);
    }
}
