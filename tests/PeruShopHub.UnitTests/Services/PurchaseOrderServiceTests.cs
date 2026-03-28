using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class PurchaseOrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Mock<ICostCalculationService> _costService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PurchaseOrderServiceTests()
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

    private PurchaseOrderService CreateService() => new(_db, _costService.Object);

    private (Product product, ProductVariant variant) SeedProductWithVariant(
        string name = "Test Product", string sku = "PROD-001")
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Sku = sku,
            Name = name,
            Price = 100m,
            PurchaseCost = 50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = $"{sku}-V1",
            Stock = 10,
            IsActive = true,
            IsDefault = true
        };
        _db.ProductVariants.Add(variant);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return (product, variant);
    }

    private PurchaseOrder SeedPurchaseOrder(string supplier = "Supplier A", string status = "Rascunho",
        Product? product = null, ProductVariant? variant = null)
    {
        var (p, v) = product != null && variant != null ? (product, variant) : SeedProductWithVariant();

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Supplier = supplier,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        var item = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PurchaseOrderId = po.Id,
            ProductId = p.Id,
            VariantId = v.Id,
            Quantity = 5,
            UnitCost = 10m,
            TotalCost = 50m
        };
        po.Items.Add(item);
        po.Subtotal = 50m;
        po.Total = 50m;

        _db.PurchaseOrders.Add(po);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return po;
    }

    // --- GetList tests ---

    [Fact]
    public async Task GetList_ReturnsPaginatedResults()
    {
        var (p, v) = SeedProductWithVariant();
        SeedPurchaseOrder("Supplier A", product: p, variant: v);
        SeedPurchaseOrder("Supplier B", product: p, variant: v);
        var service = CreateService();

        var result = await service.GetListAsync(1, 10, null, null, "createdAt", "asc");

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetList_FilterByStatus()
    {
        var (p, v) = SeedProductWithVariant();
        SeedPurchaseOrder("Supplier A", status: "Rascunho", product: p, variant: v);
        SeedPurchaseOrder("Supplier B", status: "Recebido", product: p, variant: v);
        var service = CreateService();

        var result = await service.GetListAsync(1, 10, "Rascunho", null, "createdAt", "asc");

        result.TotalCount.Should().Be(1);
        result.Items[0].Supplier.Should().Be("Supplier A");
    }

    [Fact]
    public async Task GetList_FilterBySupplier()
    {
        var (p, v) = SeedProductWithVariant();
        SeedPurchaseOrder("Acme Corp", product: p, variant: v);
        SeedPurchaseOrder("Beta Inc", product: p, variant: v);
        var service = CreateService();

        var result = await service.GetListAsync(1, 10, null, "acme", "createdAt", "asc");

        result.TotalCount.Should().Be(1);
        result.Items[0].Supplier.Should().Be("Acme Corp");
    }

    // --- GetById tests ---

    [Fact]
    public async Task GetById_ReturnsDetailWithItems()
    {
        var po = SeedPurchaseOrder();
        var service = CreateService();

        var result = await service.GetByIdAsync(po.Id);

        result.Supplier.Should().Be("Supplier A");
        result.Items.Should().HaveCount(1);
        result.Subtotal.Should().Be(50m);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Create tests ---

    [Fact]
    public async Task Create_ValidPO_ReturnsDetail()
    {
        var (p, v) = SeedProductWithVariant();
        var service = CreateService();
        var dto = new CreatePurchaseOrderDto(
            "New Supplier", "Some notes",
            new List<CreatePurchaseOrderItemDto>
            {
                new(p.Id, v.Id, 3, 20m)
            },
            null);

        var result = await service.CreateAsync(dto);

        result.Supplier.Should().Be("New Supplier");
        result.Status.Should().Be("Rascunho");
        result.Items.Should().HaveCount(1);
        result.Subtotal.Should().Be(60m);
        result.Total.Should().Be(60m);
    }

    [Fact]
    public async Task Create_EmptySupplier_ThrowsValidation()
    {
        var (p, v) = SeedProductWithVariant();
        var service = CreateService();
        var dto = new CreatePurchaseOrderDto(
            "", null,
            new List<CreatePurchaseOrderItemDto> { new(p.Id, v.Id, 1, 10m) },
            null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Supplier");
    }

    [Fact]
    public async Task Create_NoItems_ThrowsValidation()
    {
        var service = CreateService();
        var dto = new CreatePurchaseOrderDto(
            "Supplier", null,
            new List<CreatePurchaseOrderItemDto>(),
            null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Items");
    }

    [Fact]
    public async Task Create_WithCosts_CalculatesAllocations()
    {
        var (p, v) = SeedProductWithVariant();
        var service = CreateService();
        var dto = new CreatePurchaseOrderDto(
            "Supplier", null,
            new List<CreatePurchaseOrderItemDto> { new(p.Id, v.Id, 10, 5m) },
            new List<CreatePurchaseOrderCostDto> { new("Frete", 20m, "by_quantity") });

        var result = await service.CreateAsync(dto);

        result.Subtotal.Should().Be(50m);
        result.AdditionalCosts.Should().Be(20m);
        result.Total.Should().Be(70m);
        result.Items[0].AllocatedAdditionalCost.Should().Be(20m);
        result.Items[0].EffectiveUnitCost.Should().Be(7m); // (50+20)/10
    }

    // --- Update tests ---

    [Fact]
    public async Task Update_NotDraft_ThrowsConflict()
    {
        var po = SeedPurchaseOrder(status: "Recebido");
        var (p, v) = SeedProductWithVariant("P2", "P2-001");
        var service = CreateService();
        var dto = new CreatePurchaseOrderDto(
            "Updated", null,
            new List<CreatePurchaseOrderItemDto> { new(p.Id, v.Id, 1, 10m) },
            null);

        var act = () => service.UpdateAsync(po.Id, dto);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // --- Receive tests ---

    [Fact]
    public async Task Receive_DelegatesToCostService()
    {
        var po = SeedPurchaseOrder(status: "Rascunho");
        var service = CreateService();

        await service.ReceiveAsync(po.Id);

        _costService.Verify(s => s.ReceivePurchaseOrderAsync(po.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Receive_AlreadyReceived_ThrowsConflict()
    {
        var po = SeedPurchaseOrder(status: "Recebido");
        var service = CreateService();

        var act = () => service.ReceiveAsync(po.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // --- Cancel tests ---

    [Fact]
    public async Task Cancel_DraftOrder_SetsCancelado()
    {
        var po = SeedPurchaseOrder(status: "Rascunho");
        var service = CreateService();

        await service.CancelAsync(po.Id);

        var updated = await _db.PurchaseOrders.FindAsync(po.Id);
        updated!.Status.Should().Be("Cancelado");
    }

    [Fact]
    public async Task Cancel_NotDraft_ThrowsValidation()
    {
        var po = SeedPurchaseOrder(status: "Recebido");
        var service = CreateService();

        var act = () => service.CancelAsync(po.Id);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    // --- AddCost tests ---

    [Fact]
    public async Task AddCost_NotFound_ThrowsNotFoundException()
    {
        var service = CreateService();
        var dto = new CreatePurchaseOrderCostDto("Frete", 10m, "by_quantity");

        var act = () => service.AddCostAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddCost_InvalidDistributionMethod_ThrowsValidation()
    {
        var po = SeedPurchaseOrder();
        var service = CreateService();
        var dto = new CreatePurchaseOrderCostDto("Frete", 10m, "invalid_method");

        var act = () => service.AddCostAsync(po.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("DistributionMethod");
    }

    // --- RemoveCost tests ---

    [Fact]
    public async Task RemoveCost_ValidCost_RecalculatesTotals()
    {
        var (p, v) = SeedProductWithVariant();
        // Create PO with a cost directly in DB
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Supplier = "Supplier",
            Status = "Rascunho",
            CreatedAt = DateTime.UtcNow
        };
        var item = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PurchaseOrderId = po.Id,
            ProductId = p.Id,
            VariantId = v.Id,
            Quantity = 5,
            UnitCost = 10m,
            TotalCost = 50m
        };
        var cost = new PurchaseOrderCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PurchaseOrderId = po.Id,
            Description = "Frete",
            Value = 10m,
            DistributionMethod = "by_quantity"
        };
        po.Items.Add(item);
        po.Costs.Add(cost);
        po.Subtotal = 50m;
        po.AdditionalCosts = 10m;
        po.Total = 60m;
        _db.PurchaseOrders.Add(po);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var service = CreateService();

        var result = await service.RemoveCostAsync(po.Id, cost.Id);

        result.AdditionalCosts.Should().Be(0m);
        result.Total.Should().Be(50m);
    }
}
