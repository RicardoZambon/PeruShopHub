using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.DTOs.Orders;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Mock<ICostCalculationService> _costService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public OrderServiceTests()
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

        _costService = new Mock<ICostCalculationService>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private OrderService CreateService() => new(_db, _costService.Object, new Mock<IAuditService>().Object);

    private Order SeedOrder(string externalId = "ML-12345", string buyerName = "João Silva",
        decimal totalAmount = 200m, string status = "Pago", DateTime? orderDate = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ExternalOrderId = externalId,
            BuyerName = buyerName,
            TotalAmount = totalAmount,
            Profit = 50m,
            Status = status,
            OrderDate = orderDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ItemCount = 1,
            PaymentMethod = "credit_card",
            Installments = 3,
            PaymentAmount = totalAmount
        };
        _db.Orders.Add(order);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return order;
    }

    private void SeedOrderItem(Guid orderId, string name = "Test Product", int quantity = 1, decimal unitPrice = 100m)
    {
        _db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderId = orderId,
            Name = name,
            Sku = "SKU-001",
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = quantity * unitPrice
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    private OrderCost SeedOrderCost(Guid orderId, string category = "shipping", decimal value = 15m, string source = "Manual")
    {
        var cost = new OrderCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderId = orderId,
            Category = category,
            Description = "Test cost",
            Value = value,
            Source = source
        };
        _db.OrderCosts.Add(cost);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return cost;
    }

    // --- GetList tests ---

    [Fact]
    public async Task GetList_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 5; i++)
            SeedOrder(externalId: $"ML-{i:D5}");

        var service = CreateService();
        var result = await service.GetListAsync(1, 3, null, null, null, null, "orderdate", "desc");

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetList_SearchByBuyerName_FiltersCorrectly()
    {
        SeedOrder(externalId: "ML-001", buyerName: "Maria Santos");
        SeedOrder(externalId: "ML-002", buyerName: "João Silva");

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, "maria", null, null, null, "orderdate", "desc");

        result.Items.Should().ContainSingle();
        result.Items[0].BuyerName.Should().Be("Maria Santos");
    }

    [Fact]
    public async Task GetList_SearchByExternalId_FiltersCorrectly()
    {
        SeedOrder(externalId: "ML-99999");
        SeedOrder(externalId: "ML-00001");

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, "99999", null, null, null, "orderdate", "desc");

        result.Items.Should().ContainSingle();
        result.Items[0].ExternalOrderId.Should().Be("ML-99999");
    }

    [Fact]
    public async Task GetList_StatusFilter_FiltersCorrectly()
    {
        SeedOrder(externalId: "ML-P", status: "Pago");
        SeedOrder(externalId: "ML-E", status: "Enviado");

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, null, "Enviado", null, null, "orderdate", "desc");

        result.Items.Should().ContainSingle();
        result.Items[0].Status.Should().Be("Enviado");
    }

    [Fact]
    public async Task GetList_DateRangeFilter_FiltersCorrectly()
    {
        var oldDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recentDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        SeedOrder(externalId: "ML-OLD", orderDate: oldDate);
        SeedOrder(externalId: "ML-NEW", orderDate: recentDate);

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "orderdate", "desc");

        result.Items.Should().ContainSingle();
        result.Items[0].ExternalOrderId.Should().Be("ML-NEW");
    }

    // --- GetById tests ---

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsDetailWithFinancials()
    {
        var order = SeedOrder(totalAmount: 200m);
        SeedOrderItem(order.Id, quantity: 2, unitPrice: 100m);
        SeedOrderCost(order.Id, "commission", 26m);
        SeedOrderCost(order.Id, "shipping", 15m);

        var service = CreateService();
        var result = await service.GetByIdAsync(order.Id);

        result.Id.Should().Be(order.Id);
        result.Revenue.Should().Be(200m);
        result.TotalCosts.Should().Be(41m);
        result.Profit.Should().Be(159m);
        result.Margin.Should().BeApproximately(79.5m, 0.1m);
    }

    [Fact]
    public async Task GetById_NonExistent_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_BuildsTimeline_PagoStatus()
    {
        var order = SeedOrder(status: "Pago");
        var service = CreateService();

        var result = await service.GetByIdAsync(order.Id);

        result.Shipping.Timeline.Should().HaveCount(4);
        result.Shipping.Timeline![0].Status.Should().Be("Pedido realizado");
        result.Shipping.Timeline[1].Description.Should().Be("Concluido");
        result.Shipping.Timeline[2].Description.Should().Be("Pendente"); // Enviado
        result.Shipping.Timeline[3].Description.Should().Be("Pendente"); // Entregue
    }

    [Fact]
    public async Task GetById_BuildsTimeline_CanceladoStatus()
    {
        var order = SeedOrder(status: "Cancelado");
        var service = CreateService();

        var result = await service.GetByIdAsync(order.Id);

        result.Shipping.Timeline![1].Description.Should().Be("Cancelado");
    }

    [Fact]
    public async Task GetById_BuildsTimeline_EntregueStatus()
    {
        var order = SeedOrder(status: "Entregue");
        var service = CreateService();

        var result = await service.GetByIdAsync(order.Id);

        result.Shipping.Timeline![2].Description.Should().Be("Concluido"); // Enviado
        result.Shipping.Timeline[3].Description.Should().Be("Concluido"); // Entregue
    }

    [Theory]
    [InlineData("Pago", "Aprovado")]
    [InlineData("Enviado", "Aprovado")]
    [InlineData("Entregue", "Aprovado")]
    [InlineData("Cancelado", "Cancelado")]
    [InlineData("Devolvido", "Devolvido")]
    [InlineData("Pendente", "Pendente")]
    public async Task GetById_DerivesPaymentStatus_Correctly(string orderStatus, string expectedPaymentStatus)
    {
        var order = SeedOrder(status: orderStatus);
        var service = CreateService();

        var result = await service.GetByIdAsync(order.Id);

        result.Payment.Status.Should().Be(expectedPaymentStatus);
    }

    // --- AddCost tests ---

    [Fact]
    public async Task AddCost_ValidRequest_AddsCostToOrder()
    {
        var order = SeedOrder();
        var service = CreateService();
        var request = new CreateOrderCostRequest("advertising", "Google Ads", 50m);

        var result = await service.AddCostAsync(order.Id, request);

        result.Category.Should().Be("advertising");
        result.Value.Should().Be(50m);
        result.Source.Should().Be("Manual");
    }

    [Fact]
    public async Task AddCost_NonExistentOrder_ThrowsNotFoundException()
    {
        var service = CreateService();
        var request = new CreateOrderCostRequest("shipping", null, 10m);

        var act = () => service.AddCostAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddCost_EmptyCategory_ThrowsValidationException()
    {
        var order = SeedOrder();
        var service = CreateService();
        var request = new CreateOrderCostRequest("", null, 10m);

        var act = () => service.AddCostAsync(order.Id, request);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Category");
    }

    [Fact]
    public async Task AddCost_ZeroValue_ThrowsValidationException()
    {
        var order = SeedOrder();
        var service = CreateService();
        var request = new CreateOrderCostRequest("shipping", null, 0m);

        var act = () => service.AddCostAsync(order.Id, request);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Value");
    }

    // --- UpdateCost tests ---

    [Fact]
    public async Task UpdateCost_ValidRequest_UpdatesCost()
    {
        var order = SeedOrder();
        var cost = SeedOrderCost(order.Id, "shipping", 15m);
        var service = CreateService();
        var request = new UpdateOrderCostRequest("shipping_updated", "Express", 25m);

        var result = await service.UpdateCostAsync(order.Id, cost.Id, request);

        result.Category.Should().Be("shipping_updated");
        result.Value.Should().Be(25m);
    }

    [Fact]
    public async Task UpdateCost_NonExistent_ThrowsNotFoundException()
    {
        var order = SeedOrder();
        var service = CreateService();
        var request = new UpdateOrderCostRequest("x", null, 10m);

        var act = () => service.UpdateCostAsync(order.Id, Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- DeleteCost tests ---

    [Fact]
    public async Task DeleteCost_ExistingCost_RemovesCost()
    {
        var order = SeedOrder();
        var cost = SeedOrderCost(order.Id);
        var service = CreateService();

        await service.DeleteCostAsync(order.Id, cost.Id);

        var exists = await _db.OrderCosts.AnyAsync(c => c.Id == cost.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCost_NonExistent_ThrowsNotFoundException()
    {
        var order = SeedOrder();
        var service = CreateService();

        var act = () => service.DeleteCostAsync(order.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Fulfill tests ---

    [Fact]
    public async Task Fulfill_DelegatesToCostService()
    {
        var orderId = Guid.NewGuid();
        var service = CreateService();

        await service.FulfillAsync(orderId);

        _costService.Verify(s => s.FulfillOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- RecalculateCosts tests ---

    [Fact]
    public async Task RecalculateCosts_ExistingOrder_DelegatesToCostService()
    {
        var order = SeedOrder();
        var service = CreateService();

        await service.RecalculateCostsAsync(order.Id);

        _costService.Verify(s => s.RecalculateOrderCostsAsync(order.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateCosts_NonExistentOrder_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.RecalculateCostsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
