using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Mock<ICacheService> _cache;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PeruShopHubDbContext>()
            .UseSqlite(_connection)
            .Options;

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.IsSuperAdmin).Returns(true);
        tenantContext.Setup(t => t.TenantId).Returns(_tenantId);

        _db = new PeruShopHubDbContext(options, tenantContext.Object);
        _db.Database.EnsureCreated();

        _cache = new Mock<ICacheService>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private DashboardService CreateService() => new(_db, _cache.Object);

    private Order SeedOrder(decimal totalAmount, decimal profit, DateTime orderDate, string status = "Pago")
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ExternalOrderId = $"ORD-{Guid.NewGuid():N}",
            BuyerName = "Buyer",
            TotalAmount = totalAmount,
            Profit = profit,
            Status = status,
            OrderDate = orderDate,
            ItemCount = 1,
            CreatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return order;
    }

    private void SeedOrderCost(Guid orderId, string category, decimal value)
    {
        _db.OrderCosts.Add(new OrderCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderId = orderId,
            Category = category,
            Value = value,
            Source = "API"
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    private OrderItem SeedOrderItem(Guid orderId, string sku, string name, int qty, decimal unitPrice, Guid? productId = null)
    {
        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderId = orderId,
            ProductId = productId ?? Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Quantity = qty,
            UnitPrice = unitPrice,
            Subtotal = qty * unitPrice
        };
        _db.OrderItems.Add(item);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return item;
    }

    private void SeedNotification(string type, bool isRead = false)
    {
        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Type = type,
            Title = "Test",
            Description = "Test notification",
            IsRead = isRead,
            Timestamp = DateTime.UtcNow
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    private void SeedClaim(string status = "opened")
    {
        _db.Claims.Add(new MarketplaceClaim
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ExternalId = $"CLM-{Guid.NewGuid():N}",
            ExternalOrderId = "ORD-1",
            Type = "claim",
            Status = status,
            Reason = "defective",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    // --- GetSummaryAsync tests ---
    // Note: GetSummaryAsync internally uses GroupBy + Select chart queries that SQLite
    // cannot translate. Testing cache hit only; full summary needs integration tests.

    [Fact]
    public async Task GetSummary_ReturnsCachedResult_WhenAvailable()
    {
        var cached = new Application.DTOs.Dashboard.DashboardSummaryDto(
            [], [], [], [], []);
        _cache.Setup(c => c.GetAsync<Application.DTOs.Dashboard.DashboardSummaryDto>(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var service = CreateService();
        var result = await service.GetSummaryAsync("hoje");

        result.Should().BeSameAs(cached);
    }

    // Note: GetRevenueProfitChartAsync uses GroupBy + Select that SQLite cannot translate.
    // These need integration tests with PostgreSQL.

    // --- GetCostBreakdownAsync tests ---

    [Fact]
    public async Task GetCostBreakdown_GroupsByCategoryWithPercentages()
    {
        var today = DateTime.UtcNow.Date;
        var order = SeedOrder(300m, 100m, today);
        SeedOrderCost(order.Id, "product_cost", 80m);
        SeedOrderCost(order.Id, "marketplace_commission", 20m);

        var service = CreateService();
        var result = await service.GetCostBreakdownAsync("hoje");

        result.Should().HaveCount(2);
        var productCost = result.First(c => c.Category == "product_cost");
        productCost.Total.Should().Be(80m);
        productCost.Percentage.Should().Be(80m);
        productCost.Color.Should().Be("#5C6BC0");
    }

    [Fact]
    public async Task GetCostBreakdown_UnknownCategory_GetsFallbackColor()
    {
        var today = DateTime.UtcNow.Date;
        var order = SeedOrder(100m, 30m, today);
        SeedOrderCost(order.Id, "custom_cost", 20m);

        var service = CreateService();
        var result = await service.GetCostBreakdownAsync("hoje");

        result.Should().ContainSingle();
        result[0].Color.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCostBreakdown_NoOrders_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetCostBreakdownAsync("hoje");
        result.Should().BeEmpty();
    }

    // --- GetTopProductsAsync tests ---

    [Fact]
    public async Task GetTopProducts_ReturnsTopByProfit()
    {
        var today = DateTime.UtcNow.Date;
        var o1 = SeedOrder(300m, 100m, today);
        SeedOrderItem(o1.Id, "TOP-001", "Top Product", 3, 100m);
        var o2 = SeedOrder(100m, 10m, today);
        SeedOrderItem(o2.Id, "LOW-001", "Low Product", 1, 100m);

        var service = CreateService();
        var result = await service.GetTopProductsAsync(5, "hoje");

        result.Should().HaveCount(2);
        result[0].Sku.Should().Be("TOP-001");
    }

    [Fact]
    public async Task GetTopProducts_LimitWorks()
    {
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 10; i++)
        {
            var order = SeedOrder(100m, 30m, today);
            SeedOrderItem(order.Id, $"P-{i:D3}", $"Product {i}", 1, 100m);
        }

        var service = CreateService();
        var result = await service.GetTopProductsAsync(3, "hoje");

        result.Should().HaveCount(3);
    }

    // --- GetLeastProfitableAsync tests ---

    [Fact]
    public async Task GetLeastProfitable_ReturnsLowestFirst()
    {
        var today = DateTime.UtcNow.Date;
        var o1 = SeedOrder(200m, 100m, today);
        SeedOrderItem(o1.Id, "HIGH-001", "High Margin", 2, 100m);
        var o2 = SeedOrder(200m, 10m, today);
        SeedOrderItem(o2.Id, "LOW-001", "Low Margin", 2, 100m);

        var service = CreateService();
        var result = await service.GetLeastProfitableAsync(5, "hoje");

        result.Should().HaveCount(2);
        result[0].Sku.Should().Be("LOW-001");
    }

    // --- GetPendingActionsAsync tests ---

    [Fact]
    public async Task GetPendingActions_ReturnsUnansweredQuestions()
    {
        SeedNotification("question", isRead: false);
        SeedNotification("question", isRead: false);
        SeedNotification("question", isRead: true); // read, should not count

        var service = CreateService();
        var result = await service.GetPendingActionsAsync();

        var questionAction = result.FirstOrDefault(a => a.Type == "question");
        questionAction.Should().NotBeNull();
        questionAction!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingActions_ReturnsPaidOrders()
    {
        SeedOrder(100m, 30m, DateTime.UtcNow.Date, status: "Pago");
        SeedOrder(100m, 30m, DateTime.UtcNow.Date, status: "Enviado");

        var service = CreateService();
        var result = await service.GetPendingActionsAsync();

        var orderAction = result.FirstOrDefault(a => a.Type == "order");
        orderAction.Should().NotBeNull();
        orderAction!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingActions_ReturnsStockAlerts()
    {
        SeedNotification("stock_alert", isRead: false);

        var service = CreateService();
        var result = await service.GetPendingActionsAsync();

        var stockAction = result.FirstOrDefault(a => a.Type == "stock_alert");
        stockAction.Should().NotBeNull();
        stockAction!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingActions_ReturnsOpenClaims()
    {
        SeedClaim("opened");
        SeedClaim("closed"); // should not count

        var service = CreateService();
        var result = await service.GetPendingActionsAsync();

        var claimAction = result.FirstOrDefault(a => a.Type == "claim");
        claimAction.Should().NotBeNull();
        claimAction!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingActions_NoPending_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetPendingActionsAsync();
        result.Should().BeEmpty();
    }

    // Note: Period parsing, CalculateChangePercent, and GetChangeDirection
    // are tested indirectly via GetSummaryAsync which requires PostgreSQL due to
    // GroupBy chart queries. These should be integration tests.
}
