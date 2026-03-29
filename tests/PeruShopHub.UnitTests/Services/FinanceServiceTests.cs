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

public class FinanceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();

    public FinanceServiceTests()
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
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private FinanceService CreateService() => new(_db);

    private Order SeedOrder(decimal totalAmount, decimal profit, DateTime orderDate, decimal? paymentAmount = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ExternalOrderId = $"ORD-{Guid.NewGuid():N}",
            BuyerName = "Buyer",
            TotalAmount = totalAmount,
            Profit = profit,
            Status = "Pago",
            OrderDate = orderDate,
            ItemCount = 1,
            PaymentAmount = paymentAmount,
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

    // --- GetSummaryAsync tests ---

    [Fact]
    public async Task GetSummary_WithOrders_ReturnsCorrectMetrics()
    {
        var today = DateTime.UtcNow.Date;
        var o1 = SeedOrder(200m, 60m, today.AddHours(1));
        var o2 = SeedOrder(300m, 90m, today.AddHours(2));
        SeedOrderCost(o1.Id, "marketplace_commission", 40m);
        SeedOrderCost(o2.Id, "shipping_seller", 30m);

        var service = CreateService();
        var result = await service.GetSummaryAsync("hoje");

        result.TotalRevenue.Should().Be(500m);
        result.TotalProfit.Should().Be(150m);
        result.TotalCosts.Should().Be(350m); // 500 - 150
        result.AverageMargin.Should().Be(30m); // 150/500*100
        result.AverageTicket.Should().Be(250m); // 500/2
    }

    [Fact]
    public async Task GetSummary_NoOrders_ReturnsZeros()
    {
        var service = CreateService();
        var result = await service.GetSummaryAsync("hoje");

        result.TotalRevenue.Should().Be(0);
        result.TotalProfit.Should().Be(0);
        result.AverageMargin.Should().Be(0);
        result.AverageTicket.Should().Be(0);
    }

    [Fact]
    public async Task GetSummary_CalculatesRevenueChange_WithPreviousPeriod()
    {
        var today = DateTime.UtcNow.Date;
        // Current 7-day period
        SeedOrder(500m, 150m, today.AddDays(-1));
        // Previous 7-day period
        SeedOrder(400m, 100m, today.AddDays(-10));

        var service = CreateService();
        var result = await service.GetSummaryAsync("7dias");

        result.RevenueChange.Should().Be(25m); // (500-400)/400*100
        result.ProfitChange.Should().Be(50m);   // (150-100)/100*100
    }

    [Fact]
    public async Task GetSummary_NoPreviousOrders_Returns100PercentChange()
    {
        var today = DateTime.UtcNow.Date;
        SeedOrder(500m, 150m, today.AddDays(-1));

        var service = CreateService();
        var result = await service.GetSummaryAsync("7dias");

        result.RevenueChange.Should().Be(100m);
        result.ProfitChange.Should().Be(100m);
    }

    [Fact]
    public async Task GetSummary_CostBreakdown_GroupsByCategory()
    {
        var today = DateTime.UtcNow.Date;
        var o1 = SeedOrder(200m, 60m, today);
        SeedOrderCost(o1.Id, "marketplace_commission", 20m);
        SeedOrderCost(o1.Id, "shipping_seller", 10m);
        SeedOrderCost(o1.Id, "marketplace_commission", 5m);

        var service = CreateService();
        var result = await service.GetSummaryAsync("hoje");

        result.CostBreakdown.Should().HaveCount(2);
        var commission = result.CostBreakdown.First(c => c.Category == "marketplace_commission");
        commission.Total.Should().Be(25m);
    }

    [Theory]
    [InlineData("hoje")]
    [InlineData("7dias")]
    [InlineData("30dias")]
    [InlineData("unknown_period")]
    public async Task GetSummary_AllPeriods_DoNotThrow(string period)
    {
        var service = CreateService();
        var act = () => service.GetSummaryAsync(period);
        await act.Should().NotThrowAsync();
    }

    // Note: GetRevenueProfitChartAsync and GetMarginChartAsync use GroupBy + Select
    // projections that SQLite cannot translate. These methods need integration tests
    // with PostgreSQL (see PeruShopHub.IntegrationTests).

    // --- GetReconciliationAsync tests ---

    [Fact]
    public async Task GetReconciliation_Returns12Months()
    {
        var service = CreateService();
        var result = await service.GetReconciliationAsync(2026);

        result.Should().HaveCount(12);
        result[0].Month.Should().Be(1);
        result[0].MonthName.Should().Be("Janeiro");
        result[11].MonthName.Should().Be("Dezembro");
    }

    [Fact]
    public async Task GetReconciliation_WithOrders_CalculatesDifference()
    {
        SeedOrder(500m, 150m, new DateTime(2026, 3, 15), paymentAmount: 480m);
        SeedOrder(300m, 80m, new DateTime(2026, 3, 20), paymentAmount: 300m);

        var service = CreateService();
        var result = await service.GetReconciliationAsync(2026);

        var march = result.First(r => r.Month == 3);
        march.ExpectedRevenue.Should().Be(800m);
        march.DepositedRevenue.Should().Be(780m);
        march.Difference.Should().Be(-20m);
    }

    [Fact]
    public async Task GetReconciliation_NoPaymentAmount_FallsBackToTotalAmount()
    {
        SeedOrder(500m, 150m, new DateTime(2026, 1, 10));

        var service = CreateService();
        var result = await service.GetReconciliationAsync(2026);

        var jan = result.First(r => r.Month == 1);
        jan.DepositedRevenue.Should().Be(500m); // fallback to TotalAmount
        jan.Difference.Should().Be(0m);
    }

    // --- GetSkuProfitabilityFromSourceAsync (via date filter) tests ---

    [Fact]
    public async Task GetSkuProfitability_WithDateFilter_ComputesFromSource()
    {
        var date = DateTime.UtcNow.Date.AddDays(-5);
        var order = SeedOrder(200m, 60m, date);
        SeedOrderItem(order.Id, "SKU-001", "Widget", 2, 100m);
        SeedOrderCost(order.Id, "product_cost", 50m);
        SeedOrderCost(order.Id, "marketplace_commission", 30m);
        SeedOrderCost(order.Id, "shipping_seller", 10m);
        SeedOrderCost(order.Id, "tax", 5m);

        var service = CreateService();
        var result = await service.GetSkuProfitabilityAsync(
            1, 10, "revenue", "desc",
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        result.Items.Should().ContainSingle();
        var sku = result.Items[0];
        sku.Sku.Should().Be("SKU-001");
        sku.UnitsSold.Should().Be(2);
        sku.Revenue.Should().Be(200m);
        sku.Cmv.Should().Be(50m);
        sku.Commissions.Should().Be(30m);
        sku.Shipping.Should().Be(10m);
        sku.Taxes.Should().Be(5m);
    }

    [Fact]
    public async Task GetSkuProfitability_Pagination_Works()
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        for (int i = 0; i < 5; i++)
        {
            var order = SeedOrder(100m, 30m, date);
            SeedOrderItem(order.Id, $"SKU-{i:D3}", $"Product {i}", 1, 100m);
        }

        var service = CreateService();
        var result = await service.GetSkuProfitabilityAsync(
            1, 3, "sku", "asc",
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetSkuProfitability_SearchFilter_FiltersBySku()
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        var o1 = SeedOrder(100m, 30m, date);
        SeedOrderItem(o1.Id, "WIDGET-001", "Widget", 1, 100m);
        var o2 = SeedOrder(100m, 30m, date);
        SeedOrderItem(o2.Id, "GADGET-001", "Gadget", 1, 100m);

        var service = CreateService();
        var result = await service.GetSkuProfitabilityAsync(
            1, 10, "sku", "asc", search: "widget",
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        result.Items.Should().ContainSingle();
        result.Items[0].Sku.Should().Be("WIDGET-001");
    }

    [Fact]
    public async Task GetSkuProfitability_MarginFilter_FiltersCorrectly()
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        // High margin order: 100 revenue, 10 costs = 90 profit = 90% margin
        var o1 = SeedOrder(100m, 90m, date);
        SeedOrderItem(o1.Id, "HIGH-001", "High Margin", 1, 100m);
        SeedOrderCost(o1.Id, "product_cost", 10m);
        // Low margin order: 100 revenue, 95 costs = 5 profit = 5% margin
        var o2 = SeedOrder(100m, 5m, date);
        SeedOrderItem(o2.Id, "LOW-001", "Low Margin", 1, 100m);
        SeedOrderCost(o2.Id, "product_cost", 95m);

        var service = CreateService();
        var result = await service.GetSkuProfitabilityAsync(
            1, 10, "margin", "desc", minMargin: 40m,
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        result.Items.Should().ContainSingle();
        result.Items[0].Sku.Should().Be("HIGH-001");
    }

    [Fact]
    public async Task GetSkuProfitability_ZeroOrderTotal_SkipsCostAllocation()
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        var order = SeedOrder(0m, 0m, date);
        SeedOrderItem(order.Id, "ZERO-001", "Zero Total", 1, 0m);
        SeedOrderCost(order.Id, "product_cost", 10m);

        var service = CreateService();
        var result = await service.GetSkuProfitabilityAsync(
            1, 10, "sku", "asc",
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        result.Items.Should().ContainSingle();
        result.Items[0].Cmv.Should().Be(0m); // cost not allocated due to zero total
    }

    // --- GetAbcCurveAsync tests ---

    [Fact]
    public async Task GetAbcCurve_WithDateFilter_ClassifiesProducts()
    {
        var date = DateTime.UtcNow.Date.AddDays(-5);
        // Product A: 80% of revenue → class A
        var o1 = SeedOrder(800m, 200m, date);
        SeedOrderItem(o1.Id, "TOP-001", "Top Seller", 8, 100m);
        // Product B: 15% → class B
        var o2 = SeedOrder(150m, 30m, date);
        SeedOrderItem(o2.Id, "MID-001", "Mid Seller", 1, 150m);
        // Product C: 5% → class C
        var o3 = SeedOrder(50m, 5m, date);
        SeedOrderItem(o3.Id, "LOW-001", "Low Seller", 1, 50m);

        var service = CreateService();
        var result = await service.GetAbcCurveAsync(date.AddDays(-1), date.AddDays(1));

        result.Should().HaveCount(3);
        result[0].Sku.Should().Be("TOP-001");
        result[0].Classification.Should().Be("A");
        result[2].Classification.Should().Be("C");
    }

    [Fact]
    public async Task GetAbcCurve_EmptyData_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetAbcCurveAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAbcCurve_ZeroRevenue_ReturnsZeroMargin()
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        var order = SeedOrder(0m, 0m, date);
        SeedOrderItem(order.Id, "ZERO-001", "Zero Revenue", 1, 0m);

        var service = CreateService();
        var result = await service.GetAbcCurveAsync(date.AddDays(-1), date.AddDays(1));

        result.Should().ContainSingle();
        result[0].Margin.Should().Be(0m);
        result[0].CumulativePercentage.Should().Be(0m);
    }

    // --- Sorting tests ---

    [Theory]
    [InlineData("margin", "asc")]
    [InlineData("margin", "desc")]
    [InlineData("profit", "asc")]
    [InlineData("revenue", "desc")]
    [InlineData("unitssold", "asc")]
    [InlineData("totalcosts", "desc")]
    [InlineData("sku", "asc")]
    [InlineData("name", "desc")]
    [InlineData("unknown_field", "desc")]
    public async Task GetSkuProfitability_AllSortOptions_DoNotThrow(string sortBy, string sortDir)
    {
        var date = DateTime.UtcNow.Date.AddDays(-3);
        var order = SeedOrder(100m, 30m, date);
        SeedOrderItem(order.Id, "SORT-001", "Sortable", 1, 100m);

        var service = CreateService();
        var act = () => service.GetSkuProfitabilityAsync(
            1, 10, sortBy, sortDir,
            dateFrom: date.AddDays(-1), dateTo: date.AddDays(1));

        await act.Should().NotThrowAsync();
    }
}
