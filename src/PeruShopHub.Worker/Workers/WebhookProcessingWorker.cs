using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Webhooks;
using PeruShopHub.Core.Entities;
using PeruShopHub.Application.Services;
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
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookProcessingWorker> _logger;
    private readonly TimeSpan _pollInterval;

    private const int MaxRetries = 3;
    private static readonly string[] Topics = ["orders_v2", "items", "shipments", "payments", "questions", "messages"];

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
        _config = config;
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
            case "items":
                await ProcessItemWebhookAsync(sp, webhook, ct);
                break;
            case "shipments":
                await ProcessShipmentWebhookAsync(sp, webhook, ct);
                break;
            case "payments":
                await ProcessPaymentWebhookAsync(sp, webhook, ct);
                break;
            case "questions":
                await ProcessQuestionWebhookAsync(sp, webhook, ct);
                break;
            case "messages":
                await ProcessMessageWebhookAsync(sp, webhook, ct);
                break;
            default:
                _logger.LogDebug("Unhandled webhook topic: {Topic}", topic);
                break;
        }
    }

    private async Task ProcessOrderWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        var mapper = sp.GetRequiredService<IMlOrderMapper>();

        // Extract order ID from resource (e.g., "/orders/123456789")
        var orderId = mapper.ExtractOrderIdFromResource(webhook.Resource);
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
        bool billingFetched = false;
        try
        {
            fees = await adapter.GetOrderFeesAsync(orderId, ct);
            billingFetched = fees.Count > 0;
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
        mapper.MapOrderDetails(order, orderDetails, tenantId);

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
        await MergeApiFeeCostsAsync(db, mapper, order, fees, tenantId, ct);

        // Track billing fetch status
        if (billingFetched)
        {
            order.BillingFetchedAt = DateTime.UtcNow;
        }
        order.BillingRetryCount = billingFetched ? 0 : 1;

        // Recalculate profit
        var totalCosts = await db.OrderCosts
            .IgnoreQueryFilters()
            .Where(c => c.OrderId == order.Id)
            .SumAsync(c => c.Value, ct);
        order.Profit = order.TotalAmount - totalCosts;

        // Handle stock decrement for fulfilled orders
        if (mapper.IsFulfilledStatus(orderDetails.Status) && !order.IsFulfilled)
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
        PeruShopHubDbContext db, IMlOrderMapper mapper, Order order, IReadOnlyList<MarketplaceFee> fees,
        Guid tenantId, CancellationToken ct)
    {
        if (fees.Count == 0) return;

        foreach (var fee in fees)
        {
            var category = mapper.MapFeeTypeToCategory(fee.Type);

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

    // ── Items webhook ─────────────────────────────────────────

    private async Task ProcessItemWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        // Extract item ID from resource (e.g., "/items/MLB123456")
        var itemId = ExtractIdFromResource(webhook.Resource);
        if (itemId is null)
        {
            _logger.LogWarning("Could not extract item ID from resource: {Resource}", webhook.Resource);
            return;
        }

        var db = sp.GetRequiredService<PeruShopHubDbContext>();

        var connection = await FindConnectionAsync(db, webhook.UserId, ct);
        if (connection is null)
        {
            _logger.LogWarning("No active ML connection for UserId {UserId}. Skipping item {ItemId}", webhook.UserId, itemId);
            return;
        }

        var adapter = sp.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogError("No adapter registered for marketplace '{Marketplace}'", connection.MarketplaceId);
            return;
        }

        _logger.LogInformation("Processing items webhook: ItemId={ItemId}, TenantId={TenantId}", itemId, connection.TenantId);

        var itemDetails = await adapter.GetItemDetailsAsync(itemId, ct);

        // Update MarketplaceListing
        var listing = await db.Set<MarketplaceListing>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l =>
                l.TenantId == connection.TenantId
                && l.MarketplaceId == "mercadolivre"
                && l.ExternalId == itemId, ct);

        if (listing is not null)
        {
            var previousLocalQuantity = listing.AvailableQuantity;

            listing.Title = itemDetails.Title;
            listing.Status = itemDetails.Status;
            listing.Price = itemDetails.Price;
            listing.AvailableQuantity = itemDetails.AvailableQuantity;
            listing.ThumbnailUrl = itemDetails.ThumbnailUrl;
            listing.UpdatedAt = DateTime.UtcNow;

            // If linked to a product, update the product's price and status
            if (listing.ProductId.HasValue)
            {
                var product = await db.Products
                    .IgnoreQueryFilters()
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == listing.ProductId.Value, ct);

                if (product is not null)
                {
                    product.Price = itemDetails.Price;
                    product.Status = itemDetails.Status switch
                    {
                        "active" => "Ativo",
                        "paused" => "Pausado",
                        "closed" or "inactive" => "Inativo",
                        _ => product.Status
                    };
                    product.UpdatedAt = DateTime.UtcNow;

                    // ── Stock reconciliation: detect external stock changes ──
                    await ReconcileStockFromMlAsync(db, connection.TenantId, listing, product, itemDetails, ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Item updated: ExternalId={ItemId}, TenantId={TenantId}, Status={Status}, Price={Price}, Stock={Stock}",
            itemId, connection.TenantId, itemDetails.Status, itemDetails.Price, itemDetails.AvailableQuantity);
    }

    /// <summary>
    /// Compares ML available_quantity with local allocated stock.
    /// If ML changed externally, creates StockMovement adjustments and updates local stock.
    /// Does NOT trigger stock push back to ML (prevents infinite loop).
    /// </summary>
    private async Task ReconcileStockFromMlAsync(
        PeruShopHubDbContext db,
        Guid tenantId,
        MarketplaceListing listing,
        Product product,
        MarketplaceItemDetails itemDetails,
        CancellationToken ct)
    {
        var discrepancyThreshold = _config.GetValue("Workers:StockSync:DiscrepancyThreshold", 5);
        var variants = product.Variants.ToList();

        if (itemDetails.Variations.Count > 0)
        {
            // Variation-level reconciliation
            foreach (var mlVariation in itemDetails.Variations)
            {
                if (mlVariation.ExternalVariationId is null) continue;

                var variant = variants.FirstOrDefault(v => v.ExternalId == mlVariation.ExternalVariationId);
                if (variant is null) continue;

                var allocation = await db.StockAllocations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a =>
                        a.TenantId == tenantId
                        && a.ProductVariantId == variant.Id
                        && a.MarketplaceId == "mercadolivre", ct);

                var localAvailable = allocation is not null
                    ? allocation.AllocatedQuantity - allocation.ReservedQuantity
                    : variant.Stock;

                var mlQuantity = mlVariation.AvailableQuantity;
                var difference = mlQuantity - localAvailable;

                if (difference == 0) continue;

                ReconcileVariantStock(db, tenantId, product, variant, allocation, localAvailable, mlQuantity, difference, discrepancyThreshold);
            }
        }
        else
        {
            // Item-level reconciliation (no variations — use default variant)
            var variant = variants.FirstOrDefault(v => v.IsDefault) ?? variants.FirstOrDefault();
            if (variant is null) return;

            var allocation = await db.StockAllocations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a =>
                    a.TenantId == tenantId
                    && a.ProductVariantId == variant.Id
                    && a.MarketplaceId == "mercadolivre", ct);

            var localAvailable = allocation is not null
                ? allocation.AllocatedQuantity - allocation.ReservedQuantity
                : variant.Stock;

            var mlQuantity = itemDetails.AvailableQuantity;
            var difference = mlQuantity - localAvailable;

            if (difference == 0) return;

            ReconcileVariantStock(db, tenantId, product, variant, allocation, localAvailable, mlQuantity, difference, discrepancyThreshold);
        }
    }

    /// <summary>
    /// Adjusts local stock for a single variant based on ML discrepancy.
    /// Creates StockMovement and updates allocation/variant stock.
    /// </summary>
    private void ReconcileVariantStock(
        PeruShopHubDbContext db,
        Guid tenantId,
        Product product,
        ProductVariant variant,
        StockAllocation? allocation,
        int localAvailable,
        int mlQuantity,
        int difference,
        int discrepancyThreshold)
    {
        var absDifference = Math.Abs(difference);

        if (absDifference > discrepancyThreshold)
        {
            _logger.LogWarning(
                "Stock discrepancy alert: variant={VariantId}, sku={Sku}, product={ProductName}, " +
                "localAvailable={Local}, mlQuantity={Ml}, difference={Diff}, threshold={Threshold}",
                variant.Id, variant.Sku, product.Name, localAvailable, mlQuantity, difference, discrepancyThreshold);
        }

        // Create StockMovement to record the adjustment
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            VariantId = variant.Id,
            Type = "Ajuste",
            Quantity = difference,
            Reason = "Sync ML",
            CreatedBy = "webhook",
            CreatedAt = DateTime.UtcNow
        };
        db.StockMovements.Add(movement);

        // Update variant stock
        variant.Stock += difference;

        // Update allocation if it exists
        if (allocation is not null)
        {
            allocation.AllocatedQuantity += difference;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation(
            "Stock reconciled from ML: variant={VariantId}, sku={Sku}, " +
            "localBefore={Local}, mlQuantity={Ml}, adjustment={Diff}",
            variant.Id, variant.Sku, localAvailable, mlQuantity, difference);

        // NOTE: We intentionally do NOT enqueue stock sync back to ML here
        // to prevent an infinite loop (ML change → local update → push to ML → ML webhook → ...)
    }

    // ── Shipments webhook ─────────────────────────────────────

    private async Task ProcessShipmentWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        // Extract shipment ID from resource (e.g., "/shipments/12345678")
        var shipmentId = ExtractIdFromResource(webhook.Resource);
        if (shipmentId is null)
        {
            _logger.LogWarning("Could not extract shipment ID from resource: {Resource}", webhook.Resource);
            return;
        }

        var db = sp.GetRequiredService<PeruShopHubDbContext>();

        var connection = await FindConnectionAsync(db, webhook.UserId, ct);
        if (connection is null)
        {
            _logger.LogWarning("No active ML connection for UserId {UserId}. Skipping shipment {ShipmentId}", webhook.UserId, shipmentId);
            return;
        }

        var adapter = sp.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogError("No adapter registered for marketplace '{Marketplace}'", connection.MarketplaceId);
            return;
        }

        _logger.LogInformation("Processing shipment webhook: ShipmentId={ShipmentId}, TenantId={TenantId}", shipmentId, connection.TenantId);

        var shipment = await adapter.GetShipmentDetailsAsync(shipmentId, ct);

        // Find the order linked to this shipment
        var order = await db.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.TenantId == connection.TenantId
                && o.ExternalShippingId == shipmentId, ct);

        // If not found by ExternalShippingId, try by order ID from the shipment
        if (order is null && shipment.OrderId.HasValue)
        {
            order = await db.Orders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o =>
                    o.TenantId == connection.TenantId
                    && o.ExternalOrderId == shipment.OrderId.Value.ToString(), ct);
        }

        if (order is null)
        {
            _logger.LogWarning(
                "No order found for shipment {ShipmentId} (OrderId={OrderId}), TenantId={TenantId}",
                shipmentId, shipment.OrderId, connection.TenantId);
            return;
        }

        // Update shipping fields
        order.ExternalShippingId = shipmentId;
        order.ShippingStatus = MapShippingStatus(shipment.Status);
        order.Carrier = shipment.Carrier ?? order.Carrier;
        order.LogisticType = shipment.ServiceName ?? order.LogisticType ?? "mercadolivre";

        if (!string.IsNullOrWhiteSpace(shipment.TrackingNumber))
            order.TrackingNumber = shipment.TrackingNumber;

        if (!string.IsNullOrWhiteSpace(shipment.TrackingUrl))
            order.TrackingUrl = shipment.TrackingUrl;

        // Update order status based on shipping status
        var newStatus = shipment.Status.ToLowerInvariant() switch
        {
            "shipped" or "active" => "Enviado",
            "delivered" => "Entregue",
            "not_delivered" or "returned_to_sender" => "Devolvido",
            _ => null
        };

        if (newStatus is not null && order.Status != "Cancelado")
            order.Status = newStatus;

        // Update shipping_seller cost from real shipment data
        if (shipment.ShippingCost.HasValue && shipment.ShippingCost.Value > 0)
        {
            var shippingCost = await db.OrderCosts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c =>
                    c.OrderId == order.Id && c.Category == "shipping_seller", ct);

            if (shippingCost is not null)
            {
                shippingCost.Value = shipment.ShippingCost.Value;
                shippingCost.Source = "API";
                shippingCost.Description = $"Frete real: {shipment.Carrier ?? "Mercado Envios"}";
            }
            else
            {
                db.OrderCosts.Add(new OrderCost
                {
                    Id = Guid.NewGuid(),
                    TenantId = connection.TenantId,
                    OrderId = order.Id,
                    Category = "shipping_seller",
                    Description = $"Frete real: {shipment.Carrier ?? "Mercado Envios"}",
                    Value = shipment.ShippingCost.Value,
                    Source = "API"
                });
            }

            // Recalculate profit
            var totalCosts = await db.OrderCosts
                .IgnoreQueryFilters()
                .Where(c => c.OrderId == order.Id)
                .SumAsync(c => c.Value, ct);
            order.Profit = order.TotalAmount - totalCosts;
        }

        await db.SaveChangesAsync(ct);

        // Create notification for shipping events
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = connection.TenantId,
            Type = "shipping_update",
            Title = $"Atualização de envio: Pedido #{order.ExternalOrderId}",
            Description = $"Status: {order.ShippingStatus}. Rastreio: {order.TrackingNumber ?? "N/A"}",
            Timestamp = DateTime.UtcNow,
            NavigationTarget = $"/pedidos/{order.Id}"
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Shipment updated: ShipmentId={ShipmentId}, OrderId={OrderId}, Status={Status}, Tracking={Tracking}",
            shipmentId, order.ExternalOrderId, shipment.Status, shipment.TrackingNumber);
    }

    // ── Payments webhook ──────────────────────────────────────

    private async Task ProcessPaymentWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        // Extract payment ID from resource (e.g., "/collections/12345678")
        var paymentId = ExtractIdFromResource(webhook.Resource);
        if (paymentId is null)
        {
            _logger.LogWarning("Could not extract payment ID from resource: {Resource}", webhook.Resource);
            return;
        }

        var db = sp.GetRequiredService<PeruShopHubDbContext>();

        var connection = await FindConnectionAsync(db, webhook.UserId, ct);
        if (connection is null)
        {
            _logger.LogWarning("No active ML connection for UserId {UserId}. Skipping payment {PaymentId}", webhook.UserId, paymentId);
            return;
        }

        var adapter = sp.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogError("No adapter registered for marketplace '{Marketplace}'", connection.MarketplaceId);
            return;
        }

        _logger.LogInformation("Processing payment webhook: PaymentId={PaymentId}, TenantId={TenantId}", paymentId, connection.TenantId);

        var payment = await adapter.GetPaymentDetailsAsync(paymentId, ct);

        // Find the order linked to this payment
        Order? order = null;
        if (payment.OrderId.HasValue)
        {
            order = await db.Orders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o =>
                    o.TenantId == connection.TenantId
                    && o.ExternalOrderId == payment.OrderId.Value.ToString(), ct);
        }

        if (order is null)
        {
            _logger.LogWarning(
                "No order found for payment {PaymentId} (OrderId={OrderId}), TenantId={TenantId}",
                paymentId, payment.OrderId, connection.TenantId);
            return;
        }

        // Update payment fields
        order.PaymentMethod = MapPaymentMethod(payment.PaymentMethodId, payment.PaymentTypeId);
        order.Installments = payment.Installments;
        order.PaymentAmount = payment.TransactionAmount;
        order.PaymentStatus = MapPaymentStatus(payment.Status);

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment updated: PaymentId={PaymentId}, OrderId={OrderId}, Status={Status}, Amount={Amount}",
            paymentId, order.ExternalOrderId, payment.Status, payment.TransactionAmount);
    }

    // ── Questions webhook ──────────────────────────────────────

    private async Task ProcessQuestionWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        var questionId = ExtractIdFromResource(webhook.Resource);
        if (questionId is null)
        {
            _logger.LogWarning("Could not extract question ID from resource: {Resource}", webhook.Resource);
            return;
        }

        var db = sp.GetRequiredService<PeruShopHubDbContext>();

        var connection = await FindConnectionAsync(db, webhook.UserId, ct);
        if (connection is null)
        {
            _logger.LogWarning("No active ML connection for UserId {UserId}. Skipping question {QuestionId}", webhook.UserId, questionId);
            return;
        }

        _logger.LogInformation("Processing question webhook: QuestionId={QuestionId}, TenantId={TenantId}", questionId, connection.TenantId);

        var questionService = sp.GetRequiredService<IMarketplaceQuestionService>();
        await questionService.SyncSingleQuestionAsync(questionId, connection.TenantId, ct);

        _logger.LogInformation("Question webhook processed: QuestionId={QuestionId}", questionId);
    }

    // ── Messages webhook ─────────────────────────────────────

    private async Task ProcessMessageWebhookAsync(IServiceProvider sp, MercadoLivreWebhookDto webhook, CancellationToken ct)
    {
        var db = sp.GetRequiredService<PeruShopHubDbContext>();

        var connection = await FindConnectionAsync(db, webhook.UserId, ct);
        if (connection is null)
        {
            _logger.LogWarning("No active ML connection for UserId {UserId}. Skipping message webhook", webhook.UserId);
            return;
        }

        _logger.LogInformation("Processing messages webhook: Resource={Resource}, TenantId={TenantId}",
            webhook.Resource, connection.TenantId);

        // Messages webhook usually contains pack ID in resource
        // For now, log the event — full sync will be handled by the message service
        // when ML adapter supports GET /messages/packs/{packId}/sellers/{sellerId}
        _logger.LogInformation("Message webhook received for tenant {TenantId}. Resource: {Resource}",
            connection.TenantId, webhook.Resource);
    }

    // ── Shared helpers ────────────────────────────────────────

    private async Task<MarketplaceConnection?> FindConnectionAsync(
        PeruShopHubDbContext db, long? userId, CancellationToken ct)
    {
        if (userId is null) return null;

        return await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.MarketplaceId == "mercadolivre"
                && c.ExternalUserId == userId.ToString()
                && c.IsConnected
                && c.Status == "Active", ct);
    }

    private static string? ExtractIdFromResource(string? resource)
    {
        if (string.IsNullOrWhiteSpace(resource)) return null;
        var parts = resource.TrimStart('/').Split('/');
        return parts.Length >= 2 ? parts[^1] : null;
    }

    private static string MapShippingStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "pending" => "Pendente",
        "handling" or "ready_to_ship" => "Em preparação",
        "shipped" or "active" => "Em trânsito",
        "delivered" => "Entregue",
        "not_delivered" => "Não entregue",
        "returned_to_sender" or "returned_to_agency" => "Devolvido",
        "cancelled" => "Cancelado",
        _ => mlStatus
    };

    private static string MapPaymentStatus(string mlStatus) => mlStatus.ToLowerInvariant() switch
    {
        "approved" => "Aprovado",
        "pending" or "in_process" or "in_mediation" => "Pendente",
        "authorized" => "Autorizado",
        "refunded" => "Reembolsado",
        "cancelled" or "rejected" => "Cancelado",
        "charged_back" => "Estornado",
        _ => mlStatus
    };

    private static string MapPaymentMethod(string? methodId, string? typeId)
    {
        return methodId?.ToLowerInvariant() switch
        {
            "pix" => "Pix",
            "account_money" => "Mercado Pago",
            "bolbradesco" or "boleto" => "Boleto",
            "master" or "visa" or "elo" or "amex" or "hipercard" => $"Cartão ({methodId})",
            _ when typeId == "credit_card" => "Cartão de Crédito",
            _ when typeId == "debit_card" => "Cartão de Débito",
            _ => methodId ?? "Desconhecido"
        };
    }

}
