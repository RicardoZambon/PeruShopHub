using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Webhooks;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookQueueService _webhookQueue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WebhooksController(
        IWebhookQueueService webhookQueue,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _webhookQueue = webhookQueue;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Receives Mercado Livre webhook notifications.
    /// Must respond in &lt; 500ms — validates and enqueues only.
    /// </summary>
    [HttpPost("mercadolivre")]
    public async Task<IActionResult> ReceiveMercadoLivreWebhook(
        [FromBody] MercadoLivreWebhookDto webhook,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // IP validation
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (!IsAllowedIp(remoteIp))
        {
            _logger.LogWarning("Webhook rejected: unauthorized IP {RemoteIp}", remoteIp);
            return Ok(); // Return 200 to avoid ML retries from unauthorized sources
        }

        // Structure validation
        if (string.IsNullOrWhiteSpace(webhook.Topic) ||
            string.IsNullOrWhiteSpace(webhook.Resource) ||
            webhook.UserId is null ||
            webhook.ApplicationId is null)
        {
            _logger.LogWarning(
                "Webhook rejected: invalid structure. Topic={Topic}, Resource={Resource}, UserId={UserId}, AppId={AppId}",
                webhook.Topic, webhook.Resource, webhook.UserId, webhook.ApplicationId);
            return Ok(); // Return 200 to avoid ML retries
        }

        // Duplicate detection
        var notificationId = webhook.Id ?? $"{webhook.Topic}:{webhook.Resource}:{webhook.Sent}";
        if (await _webhookQueue.IsDuplicateAsync(notificationId, ct))
        {
            _logger.LogInformation(
                "Webhook duplicate detected: {NotificationId} topic={Topic}",
                notificationId, webhook.Topic);
            sw.Stop();
            _logger.LogInformation(
                "Webhook processed (duplicate) in {ElapsedMs}ms topic={Topic} resource={Resource}",
                sw.ElapsedMilliseconds, webhook.Topic, webhook.Resource);
            return Ok();
        }

        // Mark as seen
        await _webhookQueue.MarkAsSeenAsync(notificationId, ct);

        // Enqueue for async processing
        var payload = JsonSerializer.Serialize(webhook, JsonOptions);
        await _webhookQueue.EnqueueAsync(webhook.Topic, payload, ct);

        sw.Stop();
        _logger.LogInformation(
            "Webhook received and enqueued in {ElapsedMs}ms topic={Topic} resource={Resource} userId={UserId}",
            sw.ElapsedMilliseconds, webhook.Topic, webhook.Resource, webhook.UserId);

        return Ok();
    }

    private bool IsAllowedIp(IPAddress? remoteIp)
    {
        if (remoteIp is null)
            return false;

        var allowedIps = _configuration.GetSection("Webhooks:MercadoLivre:AllowedIPs").Get<string[]>();

        // If no IPs configured, allow all (development mode)
        if (allowedIps is null || allowedIps.Length == 0)
            return true;

        var remoteIpString = remoteIp.MapToIPv4().ToString();

        foreach (var allowed in allowedIps)
        {
            // Support CIDR notation (e.g., "18.231.0.0/16")
            if (allowed.Contains('/'))
            {
                if (IsInCidrRange(remoteIp, allowed))
                    return true;
            }
            else if (remoteIpString == allowed)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInCidrRange(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkAddress) || !int.TryParse(parts[1], out var prefixLength))
            return false;

        var networkBytes = networkAddress.MapToIPv4().GetAddressBytes();
        var addressBytes = address.MapToIPv4().GetAddressBytes();

        if (networkBytes.Length != addressBytes.Length)
            return false;

        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        var networkUint = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);
        var addressUint = BitConverter.ToUInt32(addressBytes.Reverse().ToArray(), 0);

        return (networkUint & mask) == (addressUint & mask);
    }
}
