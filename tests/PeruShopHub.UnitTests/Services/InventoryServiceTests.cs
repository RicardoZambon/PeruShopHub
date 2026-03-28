using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class InventoryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();

    public InventoryServiceTests()
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

    private InventoryService CreateService() => new(_db);

    private (Product product, ProductVariant variant) SeedProductWithVariant(
        string name = "Test Product", string sku = "PROD-001",
        int stock = 10, decimal purchaseCost = 50m)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Sku = sku,
            Name = name,
            Price = 100m,
            PurchaseCost = purchaseCost,
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
            Stock = stock,
            IsActive = true,
            IsDefault = true,
            PurchaseCost = purchaseCost
        };
        _db.ProductVariants.Add(variant);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return (product, variant);
    }

    private StockMovement SeedMovement(Guid productId, Guid? variantId = null,
        string type = "Ajuste", int quantity = 5, DateTime? createdAt = null)
    {
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            VariantId = variantId,
            Type = type,
            Quantity = quantity,
            UnitCost = 50m,
            Reason = "Test",
            CreatedBy = "system",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _db.StockMovements.Add(movement);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return movement;
    }

    // --- GetMovements tests ---
    // Note: GetOverviewAsync uses complex LINQ projections with Sum() subqueries + OrderBy
    // that SQLite cannot translate. Those queries are covered by integration tests.

    [Fact]
    public async Task GetMovements_ReturnsPaginatedResults()
    {
        var (p, v) = SeedProductWithVariant();
        SeedMovement(p.Id, v.Id);
        SeedMovement(p.Id, v.Id, quantity: 10);
        var service = CreateService();

        var result = await service.GetMovementsAsync(null, null, null, null, null, null, 1, 10);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMovements_Pagination_RespectsPageSize()
    {
        var (p, v) = SeedProductWithVariant();
        for (int i = 0; i < 5; i++)
            SeedMovement(p.Id, v.Id, quantity: i + 1, createdAt: DateTime.UtcNow.AddMinutes(-i));
        var service = CreateService();

        var result = await service.GetMovementsAsync(null, null, null, null, null, null, 1, 2);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetMovements_FiltersByProductId()
    {
        var (p1, v1) = SeedProductWithVariant("Product 1", "P1-001");
        var (p2, v2) = SeedProductWithVariant("Product 2", "P2-001");
        SeedMovement(p1.Id, v1.Id);
        SeedMovement(p2.Id, v2.Id);
        var service = CreateService();

        var result = await service.GetMovementsAsync(p1.Id, null, null, null, null, null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items[0].ProductName.Should().Be("Product 1");
    }

    [Fact]
    public async Task GetMovements_FiltersByType()
    {
        var (p, v) = SeedProductWithVariant();
        SeedMovement(p.Id, v.Id, type: "Ajuste");
        SeedMovement(p.Id, v.Id, type: "Entrada");
        var service = CreateService();

        var result = await service.GetMovementsAsync(null, null, "Entrada", null, null, null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items[0].Type.Should().Be("Entrada");
    }

    [Fact]
    public async Task GetMovements_FiltersByDateRange()
    {
        var (p, v) = SeedProductWithVariant();
        SeedMovement(p.Id, v.Id, createdAt: new DateTime(2026, 1, 1));
        SeedMovement(p.Id, v.Id, createdAt: new DateTime(2026, 3, 15));
        SeedMovement(p.Id, v.Id, createdAt: new DateTime(2026, 6, 1));
        var service = CreateService();

        var result = await service.GetMovementsAsync(
            null, null, null, new DateTime(2026, 2, 1), new DateTime(2026, 4, 1), null, 1, 10);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMovements_FiltersByDateFrom()
    {
        var (p, v) = SeedProductWithVariant();
        SeedMovement(p.Id, v.Id, createdAt: new DateTime(2026, 1, 1));
        SeedMovement(p.Id, v.Id, createdAt: new DateTime(2026, 6, 1));
        var service = CreateService();

        var result = await service.GetMovementsAsync(
            null, null, null, new DateTime(2026, 3, 1), null, null, 1, 10);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMovements_OrdersByCreatedAtDescending()
    {
        var (p, v) = SeedProductWithVariant();
        SeedMovement(p.Id, v.Id, quantity: 1, createdAt: new DateTime(2026, 1, 1));
        SeedMovement(p.Id, v.Id, quantity: 99, createdAt: new DateTime(2026, 6, 1));
        var service = CreateService();

        var result = await service.GetMovementsAsync(null, null, null, null, null, null, 1, 10);

        result.Items[0].Quantity.Should().Be(99); // newest first
        result.Items[1].Quantity.Should().Be(1);
    }

    [Fact]
    public async Task GetMovements_NoFilters_ReturnsAll()
    {
        var (p1, v1) = SeedProductWithVariant("Prod 1", "P1");
        var (p2, v2) = SeedProductWithVariant("Prod 2", "P2");
        SeedMovement(p1.Id, v1.Id, type: "Ajuste");
        SeedMovement(p1.Id, v1.Id, type: "Entrada");
        SeedMovement(p2.Id, v2.Id, type: "Ajuste");
        var service = CreateService();

        var result = await service.GetMovementsAsync(null, null, null, null, null, null, 1, 10);

        result.TotalCount.Should().Be(3);
    }

    // --- CreateMovement tests ---

    [Fact]
    public async Task CreateMovement_ValidAdjustment_UpdatesStockAndReturnsDto()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 10);
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 5, "Restock");

        var result = await service.CreateMovementAsync(dto);

        result.Type.Should().Be("Ajuste");
        result.Quantity.Should().Be(5);
        result.Reason.Should().Be("Restock");
        result.ProductName.Should().Be("Widget");

        // Verify stock was updated
        var updatedVariant = await _db.ProductVariants.FindAsync(v.Id);
        updatedVariant!.Stock.Should().Be(15); // 10 + 5
    }

    [Fact]
    public async Task CreateMovement_NegativeQuantity_DecreasesStock()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 10);
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, -3, "Damaged");

        var result = await service.CreateMovementAsync(dto);

        result.Quantity.Should().Be(-3);
        var updatedVariant = await _db.ProductVariants.FindAsync(v.Id);
        updatedVariant!.Stock.Should().Be(7); // 10 - 3
    }

    [Fact]
    public async Task CreateMovement_VariantNotFound_ThrowsNotFoundException()
    {
        var service = CreateService();
        var dto = new StockAdjustmentDto(Guid.NewGuid(), Guid.NewGuid(), 5, "Test");

        var act = () => service.CreateMovementAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateMovement_UsesVariantPurchaseCost()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", purchaseCost: 25m);
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 1, "Test");

        var result = await service.CreateMovementAsync(dto);

        result.UnitCost.Should().Be(25m);
    }

    [Fact]
    public async Task CreateMovement_CreatesStockMovementRecord()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001");
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 7, "Inventory count");

        await service.CreateMovementAsync(dto);

        var movements = await _db.StockMovements.Where(m => m.ProductId == p.Id).ToListAsync();
        movements.Should().HaveCount(1);
        movements[0].Quantity.Should().Be(7);
        movements[0].Reason.Should().Be("Inventory count");
        movements[0].CreatedBy.Should().Be("system");
    }

    [Fact]
    public async Task CreateMovement_ReturnsVariantSku()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001");
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 1, "Test");

        var result = await service.CreateMovementAsync(dto);

        result.Sku.Should().Be("W-001-V1"); // variant SKU
    }

    [Fact]
    public async Task CreateMovement_EmptyReason_ThrowsValidationException()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 10);
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 5, "");

        var act = () => service.CreateMovementAsync(dto);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    // --- GetAllocations tests ---

    [Fact]
    public async Task GetAllocations_ReturnsProductWithVariantsAndAllocations()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 60);
        SeedAllocation(v.Id, "amazon", 30);
        var service = CreateService();

        var result = await service.GetAllocationsAsync(p.Id);

        result.ProductId.Should().Be(p.Id);
        result.ProductName.Should().Be("Widget");
        result.Variants.Should().HaveCount(1);
        var variantDto = result.Variants[0];
        variantDto.TotalStock.Should().Be(100);
        variantDto.TotalAllocated.Should().Be(90);
        variantDto.Unallocated.Should().Be(10);
        variantDto.Allocations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllocations_NoAllocations_ReturnsZeroAllocated()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 50);
        var service = CreateService();

        var result = await service.GetAllocationsAsync(p.Id);

        result.Variants[0].TotalAllocated.Should().Be(0);
        result.Variants[0].Unallocated.Should().Be(50);
        result.Variants[0].Allocations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllocations_ProductNotFound_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.GetAllocationsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- UpdateAllocation tests ---

    [Fact]
    public async Task UpdateAllocation_CreatesNew_WhenNotExists()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("mercadolivre", 50);

        var result = await service.UpdateAllocationAsync(v.Id, dto);

        result.MarketplaceId.Should().Be("mercadolivre");
        result.AllocatedQuantity.Should().Be(50);
        result.ReservedQuantity.Should().Be(0);
        result.VariantSku.Should().Be("W-001-V1");
    }

    [Fact]
    public async Task UpdateAllocation_UpdatesExisting()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 30);
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("mercadolivre", 70);

        var result = await service.UpdateAllocationAsync(v.Id, dto);

        result.AllocatedQuantity.Should().Be(70);
        var allocs = await _db.StockAllocations.Where(a => a.ProductVariantId == v.Id).ToListAsync();
        allocs.Should().HaveCount(1); // no duplicate
    }

    [Fact]
    public async Task UpdateAllocation_ExceedsStock_ThrowsValidationException()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 60);
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("amazon", 50); // 60 + 50 = 110 > 100

        var act = () => service.UpdateAllocationAsync(v.Id, dto);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task UpdateAllocation_ExactStock_Succeeds()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 60);
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("amazon", 40); // 60 + 40 = 100 exactly

        var result = await service.UpdateAllocationAsync(v.Id, dto);

        result.AllocatedQuantity.Should().Be(40);
    }

    [Fact]
    public async Task UpdateAllocation_NegativeQuantity_ThrowsValidationException()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("mercadolivre", -10);

        var act = () => service.UpdateAllocationAsync(v.Id, dto);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task UpdateAllocation_VariantNotFound_ThrowsNotFoundException()
    {
        var service = CreateService();
        var dto = new UpdateStockAllocationDto("mercadolivre", 10);

        var act = () => service.UpdateAllocationAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAllocation_SameMarketplace_AllowsFullStock()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 30);
        var service = CreateService();
        // Update existing marketplace to full stock — should not consider old value
        var dto = new UpdateStockAllocationDto("mercadolivre", 100);

        var result = await service.UpdateAllocationAsync(v.Id, dto);

        result.AllocatedQuantity.Should().Be(100);
    }

    // --- Proportional adjustment on stock decrease ---

    [Fact]
    public async Task CreateMovement_StockDecrease_AdjustsAllocationsProportionally()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 60);
        SeedAllocation(v.Id, "amazon", 40);
        var service = CreateService();
        // Decrease stock by 50 → new stock = 50, but allocations sum to 100
        var dto = new StockAdjustmentDto(p.Id, v.Id, -50, "Lost in warehouse");

        await service.CreateMovementAsync(dto);

        var allocs = await _db.StockAllocations
            .Where(a => a.ProductVariantId == v.Id)
            .OrderByDescending(a => a.AllocatedQuantity)
            .ToListAsync();
        var total = allocs.Sum(a => a.AllocatedQuantity);
        total.Should().BeLessThanOrEqualTo(50);
        // Proportional: ML was 60% → ~30, Amazon was 40% → ~20
        allocs[0].AllocatedQuantity.Should().BeInRange(29, 31);
        allocs[1].AllocatedQuantity.Should().BeInRange(19, 21);
    }

    [Fact]
    public async Task CreateMovement_StockIncrease_DoesNotAdjustAllocations()
    {
        var (p, v) = SeedProductWithVariant("Widget", "W-001", stock: 100);
        SeedAllocation(v.Id, "mercadolivre", 60);
        var service = CreateService();
        var dto = new StockAdjustmentDto(p.Id, v.Id, 50, "New shipment");

        await service.CreateMovementAsync(dto);

        var alloc = await _db.StockAllocations.FirstAsync(a => a.ProductVariantId == v.Id);
        alloc.AllocatedQuantity.Should().Be(60); // unchanged
    }

    private StockAllocation SeedAllocation(Guid variantId, string marketplaceId, int allocated)
    {
        var allocation = new StockAllocation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductVariantId = variantId,
            MarketplaceId = marketplaceId,
            AllocatedQuantity = allocated,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.StockAllocations.Add(allocation);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return allocation;
    }
}
