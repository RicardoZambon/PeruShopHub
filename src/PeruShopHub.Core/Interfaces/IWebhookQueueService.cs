namespace PeruShopHub.Core.Interfaces;

public interface IWebhookQueueService
{
    /// <summary>
    /// Push a webhook payload to a Redis list for async processing.
    /// </summary>
    Task EnqueueAsync(string topic, string payload, CancellationToken ct = default);

    /// <summary>
    /// Check if a webhook notification ID has already been processed (dedup).
    /// Returns true if it was already seen.
    /// </summary>
    Task<bool> IsDuplicateAsync(string notificationId, CancellationToken ct = default);

    /// <summary>
    /// Mark a notification ID as seen with a 24h TTL.
    /// </summary>
    Task MarkAsSeenAsync(string notificationId, CancellationToken ct = default);
}
