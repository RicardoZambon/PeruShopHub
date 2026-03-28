using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public interface IOrderSyncService
{
    Task<OrderSyncStatus> EnqueueSyncAsync(Guid tenantId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task<OrderSyncStatus?> GetSyncStatusAsync(Guid tenantId, CancellationToken ct = default);
    Task ExecuteSyncAsync(Guid tenantId, CancellationToken ct = default);
}

public record OrderSyncStatus(
    string Status, // Queued, Running, Completed, Failed
    int TotalFound,
    int Processed,
    int Skipped,
    int ErrorCount,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    List<string>? Errors);

public record OrderSyncRequest(
    Guid TenantId,
    DateTime? DateFrom,
    DateTime? DateTo);

public class OrderSyncService : IOrderSyncService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderSyncService> _logger;

    private const string SyncStatusPrefix = "ordersync:ml:";
    private const string SyncQueueKey = "ordersync:ml:queue";
    private const int BatchSize = 50;
    private static readonly TimeSpan StatusTtl = TimeSpan.FromHours(24);

    public OrderSyncService(
        PeruShopHubDbContext db,
        ICacheService cache,
        IServiceProvider serviceProvider,
        ILogger<OrderSyncService> logger)
    {
        _db = db;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<OrderSyncStatus> EnqueueSyncAsync(Guid tenantId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
    {
        var existing = await GetSyncStatusAsync(tenantId, ct);
        if (existing is { Status: "Queued" or "Running" })
            return existing;

        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.MarketplaceId == "mercadolivre" && c.IsConnected && c.Status == "Active", ct)
            ?? throw new InvalidOperationException("Mercado Livre não está conectado ou não está ativo.");

        var status = new OrderSyncStatus("Queued", 0, 0, 0, 0, null, null, null);
        await SetStatusAsync(tenantId, status, ct);

        // Store sync request with date range
        var request = new OrderSyncRequest(tenantId, dateFrom, dateTo);
        await _cache.SetAsync($"{SyncQueueKey}:{tenantId}", request, StatusTtl, ct);

        _logger.LogInformation("ML order sync enqueued for tenant {TenantId}, dateFrom={DateFrom}, dateTo={DateTo}",
            tenantId, dateFrom, dateTo);
        return status;
    }

    public async Task<OrderSyncStatus?> GetSyncStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _cache.GetAsync<OrderSyncStatus>($"{SyncStatusPrefix}{tenantId}", ct);
    }

    public async Task ExecuteSyncAsync(Guid tenantId, CancellationToken ct = default)
    {
        var errors = new List<string>();
        int totalFound = 0, processed = 0, skipped = 0;

        try
        {
            // Get sync request (contains date range)
            var request = await _cache.GetAsync<OrderSyncRequest>($"{SyncQueueKey}:{tenantId}", ct);
            var dateFrom = request?.DateFrom ?? DateTime.UtcNow.AddDays(-90);
            var dateTo = request?.DateTo ?? DateTime.UtcNow;

            var connection = await _db.MarketplaceConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId
                    && c.MarketplaceId == "mercadolivre"
                    && c.IsConnected, ct);

            if (connection is null)
            {
                await SetStatusAsync(tenantId, new OrderSyncStatus("Failed", 0, 0, 0, 1, DateTime.UtcNow, DateTime.UtcNow,
                    ["Conexão com Mercado Livre não encontrada."]), ct);
                return;
            }

            var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
            if (adapter is null)
            {
                await SetStatusAsync(tenantId, new OrderSyncStatus("Failed", 0, 0, 0, 1, DateTime.UtcNow, DateTime.UtcNow,
                    ["Adaptador do Mercado Livre não disponível."]), ct);
                return;
            }

            var startedAt = DateTime.UtcNow;
            await SetStatusAsync(tenantId, new OrderSyncStatus("Running", 0, 0, 0, 0, startedAt, null, null), ct);

            // Step 1: Collect all order IDs via paginated search
            var allOrders = new List<MarketplaceOrder>();
            var offset = 0;
            var from = new DateTimeOffset(dateFrom, TimeSpan.Zero);
            var to = new DateTimeOffset(dateTo, TimeSpan.Zero);

            do
            {
                ct.ThrowIfCancellationRequested();

                var searchResult = await adapter.SearchOrdersPagedAsync(from, to, offset, BatchSize, ct);
                allOrders.AddRange(searchResult.Orders);
                totalFound = searchResult.Total;
                offset += searchResult.Orders.Count;

                await SetStatusAsync(tenantId, new OrderSyncStatus("Running", totalFound, processed, skipped, errors.Count, startedAt, null, null), ct);

                if (searchResult.Orders.Count < BatchSize)
                    break;

            } while (offset < totalFound);

            _logger.LogInformation("ML order sync for tenant {TenantId}: found {Count} orders (total: {Total})",
                tenantId, allOrders.Count, totalFound);

            // Step 2: Process each order
            foreach (var mlOrder in allOrders)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Check if order already exists
                    var exists = await _db.Orders
                        .IgnoreQueryFilters()
                        .AnyAsync(o => o.TenantId == tenantId && o.ExternalOrderId == mlOrder.ExternalOrderId, ct);

                    if (exists)
                    {
                        skipped++;
                        await SetStatusAsync(tenantId, new OrderSyncStatus("Running", totalFound, processed, skipped, errors.Count, startedAt, null, null), ct);
                        continue;
                    }

                    await ProcessSingleOrderAsync(tenantId, adapter, mlOrder.ExternalOrderId, ct);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync ML order {OrderId} for tenant {TenantId}",
                        mlOrder.ExternalOrderId, tenantId);
                    errors.Add($"Pedido {mlOrder.ExternalOrderId}: {ex.Message}");
                }

                await SetStatusAsync(tenantId, new OrderSyncStatus("Running", totalFound, processed, skipped, errors.Count, startedAt, null,
                    errors.Count > 0 ? errors.Take(50).ToList() : null), ct);
            }

            var finalStatus = new OrderSyncStatus("Completed", totalFound, processed, skipped, errors.Count,
                startedAt, DateTime.UtcNow, errors.Count > 0 ? errors.Take(50).ToList() : null);
            await SetStatusAsync(tenantId, finalStatus, ct);

            _logger.LogInformation(
                "ML order sync completed for tenant {TenantId}: {Processed} processed, {Skipped} skipped, {Errors} errors",
                tenantId, processed, skipped, errors.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ML order sync cancelled for tenant {TenantId}", tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML order sync failed for tenant {TenantId}", tenantId);
            errors.Add($"Erro geral: {ex.Message}");
            await SetStatusAsync(tenantId, new OrderSyncStatus("Failed", totalFound, processed, skipped, errors.Count,
                null, DateTime.UtcNow, errors.Take(50).ToList()), ct);
        }
        finally
        {
            await _cache.RemoveAsync($"{SyncQueueKey}:{tenantId}", ct);
        }
    }

    private async Task ProcessSingleOrderAsync(
        Guid tenantId, IMarketplaceAdapter adapter, string orderId, CancellationToken ct)
    {
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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalOrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };

        // Map ML order details to internal Order
        MapOrderDetails(order, orderDetails, tenantId);

        // Find or create customer
        var customerId = await FindOrCreateCustomerAsync(tenantId, orderDetails.Buyer, ct);
        order.CustomerId = customerId;

        _db.Orders.Add(order);

        // Map order items
        await MapOrderItemsAsync(order, orderDetails.Items, tenantId, ct);

        await _db.SaveChangesAsync(ct);

        // Calculate costs
        var costService = _serviceProvider.GetRequiredService<ICostCalculationService>();
        await costService.CalculateOrderCostsAsync(order, ct);

        // Merge API-sourced fee costs
        await MergeApiFeeCostsAsync(order, fees, tenantId, ct);

        // Recalculate profit
        var totalCosts = await _db.OrderCosts
            .IgnoreQueryFilters()
            .Where(c => c.OrderId == order.Id)
            .SumAsync(c => c.Value, ct);
        order.Profit = order.TotalAmount - totalCosts;

        // Handle stock for fulfilled orders
        if (IsFulfilledStatus(orderDetails.Status))
        {
            order.IsFulfilled = true;
            order.FulfilledAt = orderDetails.DateCreated.UtcDateTime;

            try
            {
                await costService.FulfillOrderAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stock decrement failed for historical order {OrderId}", orderId);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogDebug("Synced historical order: ExternalId={ExternalOrderId}, InternalId={OrderId}, TenantId={TenantId}",
            orderId, order.Id, tenantId);
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
        Guid tenantId, MarketplaceBuyer buyer, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(buyer.ExternalId)) return null;

        var customer = await _db.Customers
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
        _db.Customers.Add(customer);
        return customer.Id;
    }

    private async Task MapOrderItemsAsync(
        Order order, IReadOnlyList<MarketplaceOrderItem> mlItems,
        Guid tenantId, CancellationToken ct)
    {
        foreach (var mlItem in mlItems)
        {
            var productId = await _db.Set<MarketplaceListing>()
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

            _db.OrderItems.Add(orderItem);
        }

        order.ItemCount = mlItems.Sum(i => i.Quantity);
    }

    private async Task MergeApiFeeCostsAsync(
        Order order, IReadOnlyList<MarketplaceFee> fees,
        Guid tenantId, CancellationToken ct)
    {
        if (fees.Count == 0) return;

        foreach (var fee in fees)
        {
            var category = MapFeeTypeToCategory(fee.Type);

            var existing = await _db.OrderCosts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "API", ct);

            if (existing is not null)
            {
                existing.Value = Math.Abs(fee.Amount);
            }
            else
            {
                var calculated = await _db.OrderCosts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "Calculated", ct);

                if (calculated is not null)
                {
                    calculated.Value = Math.Abs(fee.Amount);
                    calculated.Source = "API";
                }
                else
                {
                    _db.OrderCosts.Add(new OrderCost
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

        await _db.SaveChangesAsync(ct);
    }

    private static string MapFeeTypeToCategory(string feeType) => feeType.ToLowerInvariant() switch
    {
        "sale_fee" or "marketplace_fee" => "marketplace_commission",
        "shipping" or "shipping_fee" => "shipping_seller",
        "financing_fee" or "financing" => "payment_fee",
        "fixed_fee" => "fixed_fee",
        _ => feeType.ToLowerInvariant()
    };

    private async Task SetStatusAsync(Guid tenantId, OrderSyncStatus status, CancellationToken ct)
    {
        await _cache.SetAsync($"{SyncStatusPrefix}{tenantId}", status, StatusTtl, ct);
    }
}
