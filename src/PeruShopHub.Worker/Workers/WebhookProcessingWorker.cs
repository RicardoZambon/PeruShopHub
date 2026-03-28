using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Webhooks;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that polls Redis ml:webhooks:* queues and processes webhook payloads.
/// Processes one webhook at a time per topic (ordered processing).
/// After 3 consecutive failures, moves the payload to ml:webhooks:dead.
/// </summary>
public class WebhookProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WebhookProcessingWorker> _logger;
    private readonly TimeSpan _pollInterval;

    private const int MaxRetries = 3;
    private static readonly string[] Topics = ["orders_v2"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public WebhookProcessingWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<WebhookProcessingWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Workers:WebhookProcessing:PollIntervalSeconds", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookProcessingWorker started. Poll interval: {Interval}", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllTopicsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebhookProcessingWorker poll cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessAllTopicsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var webhookQueue = scope.ServiceProvider.GetRequiredService<IWebhookQueueService>();

        foreach (var topic in Topics)
        {
            ct.ThrowIfCancellationRequested();

            // Process one webhook per topic per poll cycle (ordered processing)
            var payload = await webhookQueue.DequeueAsync(topic, ct);
            if (payload is null) continue;

            _logger.LogInformation("Dequeued webhook from {Topic}", topic);

            var success = false;
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var processScope = _services.CreateScope();
                    await ProcessWebhookAsync(processScope.ServiceProvider, topic, payload, ct);
                    success = true;
                    break;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Re-enqueue on shutdown so we don't lose the message
                    await webhookQueue.EnqueueAsync(topic, payload, ct);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Webhook processing attempt {Attempt}/{MaxRetries} failed for topic {Topic}",
                        attempt, MaxRetries, topic);

                    if (attempt < MaxRetries)
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                }
            }

            if (!success)
            {
                _logger.LogError("Webhook processing failed after {MaxRetries} attempts. Moving to dead letter queue", MaxRetries);
                await webhookQueue.EnqueueDeadLetterAsync(payload, ct);
            }
        }
    }

    private async Task ProcessWebhookAsync(IServiceProvider sp, string topic, string payload, CancellationToken ct)
    {
        var webhook = JsonSerializer.Deserialize<MercadoLivreWebhookDto>(payload, JsonOptions);
        if (webhook is null)
        {
            _logger.LogWarning("Failed to deserialize webhook payload");
            return;
        }

        switch (topic)
        {
            case "orders_v2":
                await ProcessOrderWebhookAsync(sp, webhook, ct);
                break;
            default:
                _logger.LogDebug("Unhandled webhook topic: {Topic}", topic);
                break;
        }
    }

    private async Task ProcessOrderWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        // Extract order ID from resource (e.g., "/orders/123456789")
        var orderId = ExtractIdFromResource(webhook.Resource);
        if (orderId is null)
        {
            _logger.LogWarning("Could not extract order ID from resource: {Resource}", webhook.Resource);
            return;
        }

        var db = sp.GetRequiredService<PeruShopHubDbContext>();
        var tokenEncryption = sp.GetRequiredService<ITokenEncryptionService>();

        // Find the ML connection for this user
        var connection = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.MarketplaceId == "mercadolivre"
                && c.ExternalUserId == webhook.UserId.ToString()
                && c.IsConnected
                && c.Status == "Active", ct);

        if (connection is null)
        {
            _logger.LogWarning(
                "No active ML connection found for UserId {UserId}. Skipping order {OrderId}",
                webhook.UserId, orderId);
            return;
        }

        var tenantId = connection.TenantId;

        // Get marketplace adapter and fetch order details
        var adapter = sp.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogError("No adapter registered for marketplace '{Marketplace}'", connection.MarketplaceId);
            return;
        }

        _logger.LogInformation("Processing order webhook: OrderId={OrderId}, TenantId={TenantId}", orderId, tenantId);

        var orderDetails = await adapter.GetOrderDetailsAsync(orderId, ct);

        // Try to fetch fees (may fail for some order states)
        IReadOnlyList<MarketplaceFee> fees = [];
        try
        {
            fees = await adapter.GetOrderFeesAsync(orderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch fees for order {OrderId}", orderId);
        }

        // Check if order already exists (idempotent by ExternalOrderId)
        var existingOrder = await db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.ExternalOrderId == orderId, ct);

        var isNew = existingOrder is null;
        var order = existingOrder ?? new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalOrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };

        // Map ML order details to internal Order
        MapOrderDetails(order, orderDetails, tenantId);

        // Find or create customer
        var customerId = await FindOrCreateCustomerAsync(db, tenantId, orderDetails.Buyer, ct);
        order.CustomerId = customerId;

        if (isNew)
        {
            db.Orders.Add(order);
        }

        // Map order items
        await MapOrderItemsAsync(db, order, orderDetails.Items, tenantId, isNew, ct);

        await db.SaveChangesAsync(ct);

        // Calculate costs
        var costService = sp.GetRequiredService<ICostCalculationService>();
        var calculatedCosts = await costService.CalculateOrderCostsAsync(order, ct);

        // Merge API-sourced fee costs (override calculated ones for the same category)
        await MergeApiFeeCostsAsync(db, order, fees, tenantId, ct);

        // Recalculate profit
        var totalCosts = await db.OrderCosts
            .IgnoreQueryFilters()
            .Where(c => c.OrderId == order.Id)
            .SumAsync(c => c.Value, ct);
        order.Profit = order.TotalAmount - totalCosts;

        // Handle stock decrement for fulfilled orders
        if (IsFulfilledStatus(orderDetails.Status) && !order.IsFulfilled)
        {
            order.IsFulfilled = true;
            order.FulfilledAt = DateTime.UtcNow;

            try
            {
                await costService.FulfillOrderAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stock decrement failed for order {OrderId}", orderId);
            }
        }

        await db.SaveChangesAsync(ct);

        // Create notification
        var action = isNew ? "created" : "updated";
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = "new_sale",
            Title = $"Nova venda: Pedido #{orderId} - R$ {order.TotalAmount:N2}",
            Description = $"Comprador: {order.BuyerName}. {order.ItemCount} item(ns), total R$ {order.TotalAmount:N2}.",
            Timestamp = DateTime.UtcNow,
            NavigationTarget = $"/pedidos/{order.Id}"
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {Action}: ExternalId={ExternalOrderId}, InternalId={OrderId}, TenantId={TenantId}, Amount={Amount}",
            action, orderId, order.Id, tenantId, order.TotalAmount);
    }

    private static void MapOrderDetails(Order order, MarketplaceOrderDetails details, Guid tenantId)
    {
        order.TenantId = tenantId;
        order.BuyerName = details.Buyer.Nickname;
        order.BuyerNickname = details.Buyer.Nickname;
        order.BuyerEmail = details.Buyer.Email;
        order.TotalAmount = details.TotalAmount;
        order.ItemCount = details.Items.Count;
        order.OrderDate = details.DateCreated.UtcDateTime;
        order.Status = MapOrderStatus(details.Status);
        order.LogisticType = details.Shipping is not null ? "mercadolivre" : null;
    }

    private static string MapOrderStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "paid" => "Pago",
        "confirmed" => "Pago",
        "payment_required" => "Aguardando Pagamento",
        "payment_in_process" => "Aguardando Pagamento",
        "cancelled" => "Cancelado",
        "invalid" => "Cancelado",
        _ => "Pago"
    };

    private static bool IsFulfilledStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "paid" => true,
        "confirmed" => true,
        _ => false
    };

    private async Task<Guid?> FindOrCreateCustomerAsync(
        PeruShopHubDbContext db, Guid tenantId, MarketplaceBuyer buyer, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(buyer.ExternalId)) return null;

        // Find existing customer by nickname (ML unique identifier)
        var customer = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Nickname == buyer.Nickname, ct);

        if (customer is not null)
        {
            customer.TotalOrders++;
            customer.LastPurchase = DateTime.UtcNow;
            return customer.Id;
        }

        customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = buyer.Nickname,
            Nickname = buyer.Nickname,
            Email = buyer.Email,
            TotalOrders = 1,
            LastPurchase = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        return customer.Id;
    }

    private async Task MapOrderItemsAsync(
        PeruShopHubDbContext db, Order order, IReadOnlyList<MarketplaceOrderItem> mlItems,
        Guid tenantId, bool isNew, CancellationToken ct)
    {
        if (!isNew)
        {
            // Remove existing items for idempotent update
            var existingItems = await db.OrderItems
                .IgnoreQueryFilters()
                .Where(i => i.OrderId == order.Id)
                .ToListAsync(ct);
            db.OrderItems.RemoveRange(existingItems);
        }

        foreach (var mlItem in mlItems)
        {
            // Try to link to internal product via MarketplaceListing
            var productId = await db.Set<MarketplaceListing>()
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId && l.ExternalId == mlItem.ExternalItemId)
                .Select(l => l.ProductId)
                .FirstOrDefaultAsync(ct);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = order.Id,
                ProductId = productId,
                Name = mlItem.Title,
                Sku = mlItem.ExternalItemId,
                Quantity = mlItem.Quantity,
                UnitPrice = mlItem.UnitPrice,
                Subtotal = mlItem.Quantity * mlItem.UnitPrice
            };

            db.OrderItems.Add(orderItem);
        }

        order.ItemCount = mlItems.Sum(i => i.Quantity);
    }

    private async Task MergeApiFeeCostsAsync(
        PeruShopHubDbContext db, Order order, IReadOnlyList<MarketplaceFee> fees,
        Guid tenantId, CancellationToken ct)
    {
        if (fees.Count == 0) return;

        foreach (var fee in fees)
        {
            var category = MapFeeTypeToCategory(fee.Type);

            // Check if an API cost for this category already exists
            var existing = await db.OrderCosts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "API", ct);

            if (existing is not null)
            {
                existing.Value = Math.Abs(fee.Amount);
            }
            else
            {
                // Check if there's a calculated cost for this category to replace
                var calculated = await db.OrderCosts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "Calculated", ct);

                if (calculated is not null)
                {
                    calculated.Value = Math.Abs(fee.Amount);
                    calculated.Source = "API";
                }
                else
                {
                    db.OrderCosts.Add(new OrderCost
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        OrderId = order.Id,
                        Category = category,
                        Description = $"ML API: {fee.Type}",
                        Value = Math.Abs(fee.Amount),
                        Source = "API"
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string MapFeeTypeToCategory(string feeType) => feeType.ToLowerInvariant() switch
    {
        "sale_fee" or "marketplace_fee" => "marketplace_commission",
        "shipping" or "shipping_fee" => "shipping_seller",
        "financing_fee" or "financing" => "payment_fee",
        "fixed_fee" => "fixed_fee",
        _ => feeType.ToLowerInvariant()
    };

    private static string? ExtractIdFromResource(string? resource)
    {
        if (string.IsNullOrWhiteSpace(resource)) return null;

        // Resource format: "/orders/123456789"
        var parts = resource.TrimStart('/').Split('/');
        return parts.Length >= 2 ? parts[^1] : null;
    }
}
