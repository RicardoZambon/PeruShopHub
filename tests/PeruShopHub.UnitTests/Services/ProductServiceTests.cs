using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Mock<ICacheService> _cache;
    private readonly Mock<INotificationDispatcher> _dispatcher;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProductServiceTests()
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
        _dispatcher = new Mock<INotificationDispatcher>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private ProductService CreateService() => new(_db, _cache.Object, _dispatcher.Object);

    private Product SeedProduct(string sku = "PROD-001", string name = "Test Product", decimal price = 100m,
        decimal purchaseCost = 50m, decimal packagingCost = 5m, string status = "Ativo")
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Sku = sku,
            Name = name,
            Price = price,
            PurchaseCost = purchaseCost,
            PackagingCost = packagingCost,
            Status = status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return product;
    }

    private Category SeedCategory(string name = "Electronics", string? skuPrefix = "ELEC", Guid? parentId = null)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = name,
            Slug = name.ToLower(),
            SkuPrefix = skuPrefix,
            ParentId = parentId
        };
        _db.Categories.Add(category);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return category;
    }

    // --- Create tests ---

    [Fact]
    public async Task Create_ValidProduct_ReturnsDetailDto()
    {
        var service = CreateService();
        var dto = new CreateProductDto("SKU-001", "New Product", "Desc", null, 99.90m, 40m, 3m, null, "Supplier", 0.5m, 10m, 20m, 30m);

        var result = await service.CreateAsync(dto);

        result.Sku.Should().Be("SKU-001");
        result.Name.Should().Be("New Product");
        result.Price.Should().Be(99.90m);
        result.PurchaseCost.Should().Be(40m);
        result.PackagingCost.Should().Be(3m);
    }

    [Fact]
    public async Task Create_AutoGeneratesSku_WhenSkuIsNull()
    {
        var category = SeedCategory(skuPrefix: "ELEC");
        var service = CreateService();
        var dto = new CreateProductDto(null, "Auto SKU Product", null, category.Id.ToString(), 50m, 20m, 2m, null, null, 0m, 0m, 0m, 0m);

        var result = await service.CreateAsync(dto);

        result.Sku.Should().StartWith("ELEC-");
    }

    [Fact]
    public async Task Create_AutoIncrementsSku_WhenExistingSkusExist()
    {
        var category = SeedCategory(skuPrefix: "ELEC");
        SeedProduct(sku: "ELEC-001");
        SeedProduct(sku: "ELEC-002");
        var service = CreateService();
        var dto = new CreateProductDto(null, "Third Product", null, category.Id.ToString(), 50m, 20m, 2m, null, null, 0m, 0m, 0m, 0m);

        var result = await service.CreateAsync(dto);

        result.Sku.Should().Be("ELEC-003");
    }

    [Fact]
    public async Task Create_DuplicateSku_ThrowsValidationException()
    {
        SeedProduct(sku: "DUP-001");
        var service = CreateService();
        var dto = new CreateProductDto("DUP-001", "Duplicate", null, null, 10m, 5m, 1m, null, null, 0m, 0m, 0m, 0m);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Sku");
    }

    [Fact]
    public async Task Create_EmptyName_ThrowsValidationException()
    {
        var service = CreateService();
        var dto = new CreateProductDto("SKU-X", "", null, null, 10m, 5m, 1m, null, null, 0m, 0m, 0m, 0m);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Create_NegativeCosts_ThrowsValidationException()
    {
        var service = CreateService();
        var dto = new CreateProductDto("SKU-X", "Product", null, null, -1m, -1m, -1m, null, null, 0m, 0m, 0m, 0m);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Price");
        ex.Which.Errors.Should().ContainKey("PurchaseCost");
        ex.Which.Errors.Should().ContainKey("PackagingCost");
    }

    [Fact]
    public async Task Create_InvalidCategoryId_ThrowsValidationException()
    {
        var service = CreateService();
        var dto = new CreateProductDto("SKU-X", "Product", null, Guid.NewGuid().ToString(), 10m, 5m, 1m, null, null, 0m, 0m, 0m, 0m);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("CategoryId");
    }

    [Fact]
    public async Task Create_BroadcastsNotification()
    {
        var service = CreateService();
        var dto = new CreateProductDto("SKU-N", "Notified", null, null, 10m, 5m, 1m, null, null, 0m, 0m, 0m, 0m);

        await service.CreateAsync(dto);

        _dispatcher.Verify(d => d.BroadcastDataChangeAsync("product", "created", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- GetById tests ---

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsDetailDto()
    {
        var product = SeedProduct();
        var service = CreateService();

        var result = await service.GetByIdAsync(product.Id);

        result.Id.Should().Be(product.Id);
        result.Sku.Should().Be("PROD-001");
        result.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetById_NonExistent_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Update tests ---

    [Fact]
    public async Task Update_ValidFields_UpdatesProduct()
    {
        var product = SeedProduct();
        var service = CreateService();
        var dto = new UpdateProductDto(null, "Updated Name", null, null, 150m, null, null, null, null, null, null, null, null, null, null, product.Version);

        var result = await service.UpdateAsync(product.Id, dto);

        result.Name.Should().Be("Updated Name");
        result.Price.Should().Be(150m);
        result.Version.Should().Be(product.Version + 1);
    }

    [Fact]
    public async Task Update_NonExistent_ThrowsNotFoundException()
    {
        var service = CreateService();
        var dto = new UpdateProductDto(null, "X", null, null, null, null, null, null, null, null, null, null, null, null, null, 0);

        var act = () => service.UpdateAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Delete tests ---

    [Fact]
    public async Task Delete_NoOrders_HardDeletes()
    {
        var product = SeedProduct();
        var service = CreateService();

        await service.DeleteAsync(product.Id);

        var exists = await _db.Products.AnyAsync(p => p.Id == product.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithOrders_SoftDeletes()
    {
        var product = SeedProduct();
        // Create an order referencing the product
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ExternalOrderId = "ORD-001",
            BuyerName = "Buyer",
            TotalAmount = 100m,
            Status = "Pago",
            OrderDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ItemCount = 1
        };
        _db.Orders.Add(order);
        _db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderId = order.Id,
            ProductId = product.Id,
            Name = "Test",
            Sku = "PROD-001",
            Quantity = 1,
            UnitPrice = 100m,
            Subtotal = 100m
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var service = CreateService();
        await service.DeleteAsync(product.Id);

        var p = await _db.Products.FirstAsync(p => p.Id == product.Id);
        p.IsActive.Should().BeFalse();
        p.Status.Should().Be("Excluído");
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- GetList tests ---

    [Fact]
    public async Task GetList_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 5; i++)
            SeedProduct(sku: $"LIST-{i:D3}", name: $"Product {i}");

        var service = CreateService();
        var result = await service.GetListAsync(1, 3, null, null, null, "name", "asc");

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetList_SearchFilter_FiltersResults()
    {
        SeedProduct(sku: "ABC-001", name: "Widget Alpha");
        SeedProduct(sku: "XYZ-001", name: "Gadget Beta");

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, "widget", null, null, "name", "asc");

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Widget Alpha");
    }

    [Fact]
    public async Task GetList_StatusFilter_FiltersResults()
    {
        SeedProduct(sku: "A-001", name: "Active Product", status: "Ativo");
        SeedProduct(sku: "I-001", name: "Inactive Product", status: "Inativo");

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, null, "Ativo", null, "name", "asc");

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Active Product");
    }

    [Fact]
    public async Task GetList_CategoryFilter_IncludesDescendants()
    {
        var parent = SeedCategory("Parent", "PAR");
        var child = SeedCategory("Child", "CHI", parent.Id);

        var p1 = SeedProduct(sku: "PAR-001", name: "Parent Product");
        // Assign category
        var prod1 = await _db.Products.FindAsync(p1.Id);
        prod1!.CategoryId = parent.Id.ToString();
        var p2 = SeedProduct(sku: "CHI-001", name: "Child Product");
        var prod2 = await _db.Products.FindAsync(p2.Id);
        prod2!.CategoryId = child.Id.ToString();
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var service = CreateService();
        var result = await service.GetListAsync(1, 10, null, null, parent.Id, "name", "asc");

        result.Items.Should().HaveCount(2);
    }

    // --- GetNextSku tests ---

    [Fact]
    public async Task GetNextSku_WithPrefix_ReturnsNextSku()
    {
        var category = SeedCategory(skuPrefix: "ELEC");
        SeedProduct(sku: "ELEC-005");
        var service = CreateService();

        var result = await service.GetNextSkuAsync(category.Id);

        result.Should().Be("ELEC-006");
    }

    [Fact]
    public async Task GetNextSku_NullCategoryId_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetNextSkuAsync(null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextSku_CategoryNoPrefix_ReturnsNull()
    {
        var category = SeedCategory(skuPrefix: null);
        var service = CreateService();

        var result = await service.GetNextSkuAsync(category.Id);

        result.Should().BeNull();
    }
}
