using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeruShopHubDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CategoryServiceTests()
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

    private CategoryService CreateService() => new(_db);

    private Category SeedCategory(string name = "Electronics", string slug = "electronics",
        Guid? parentId = null, string? icon = null, string? skuPrefix = null)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = name,
            Slug = slug,
            ParentId = parentId,
            Icon = icon,
            SkuPrefix = skuPrefix,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Categories.Add(category);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return category;
    }

    private VariationField SeedVariationField(Guid categoryId, string name = "Color",
        string type = "select", string[]? options = null, int order = 0)
    {
        var field = new VariationField
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CategoryId = categoryId,
            Name = name,
            Type = type,
            Options = options ?? new[] { "Red", "Blue", "Green" },
            Required = true,
            Order = order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.VariationFields.Add(field);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return field;
    }

    // --- GetCategories tests ---

    [Fact]
    public async Task GetCategories_ReturnsAllRootCategories()
    {
        SeedCategory("Electronics", "electronics");
        SeedCategory("Clothing", "clothing");
        var service = CreateService();

        var result = await service.GetCategoriesAsync(null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategories_FiltersByParentId()
    {
        var parent = SeedCategory("Electronics", "electronics");
        SeedCategory("Phones", "phones", parentId: parent.Id);
        SeedCategory("Laptops", "laptops", parentId: parent.Id);
        SeedCategory("Clothing", "clothing");
        var service = CreateService();

        var result = await service.GetCategoriesAsync(parent.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.ParentId.Should().Be(parent.Id));
    }

    [Fact]
    public async Task GetCategories_SetsHasChildrenFlag()
    {
        var parent = SeedCategory("Electronics", "electronics");
        SeedCategory("Phones", "phones", parentId: parent.Id);
        var service = CreateService();

        var result = await service.GetCategoriesAsync(null);

        var electronics = result.First(c => c.Name == "Electronics");
        electronics.HasChildren.Should().BeTrue();
    }

    // --- SearchCategories tests ---

    [Fact]
    public async Task SearchCategories_FindsMatchingCategories()
    {
        SeedCategory("Electronics", "electronics");
        SeedCategory("Clothing", "clothing");
        var service = CreateService();

        var result = await service.SearchCategoriesAsync("elect");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task SearchCategories_IncludesAncestorsOfMatches()
    {
        var root = SeedCategory("Electronics", "electronics");
        var phones = SeedCategory("Phones", "phones", parentId: root.Id);
        SeedCategory("Smartphones", "smartphones", parentId: phones.Id);
        var service = CreateService();

        var result = await service.SearchCategoriesAsync("smartphone");

        result.Should().HaveCount(3); // Smartphones + Phones + Electronics
    }

    [Fact]
    public async Task SearchCategories_EmptyQuery_ReturnsEmpty()
    {
        SeedCategory("Electronics", "electronics");
        var service = CreateService();

        var result = await service.SearchCategoriesAsync("");

        result.Should().BeEmpty();
    }

    // --- GetById tests ---

    [Fact]
    public async Task GetById_ReturnsCategoryWithChildren()
    {
        var parent = SeedCategory("Electronics", "electronics");
        SeedCategory("Phones", "phones", parentId: parent.Id);
        var service = CreateService();

        var result = await service.GetByIdAsync(parent.Id);

        result.Name.Should().Be("Electronics");
        result.Children.Should().HaveCount(1);
        result.Children[0].Name.Should().Be("Phones");
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
    public async Task Create_ValidCategory_ReturnsDetail()
    {
        var service = CreateService();
        var dto = new CreateCategoryDto("Electronics", "electronics", null, "laptop", 0, "ELEC");

        var result = await service.CreateAsync(dto);

        result.Name.Should().Be("Electronics");
        result.Slug.Should().Be("electronics");
        result.SkuPrefix.Should().Be("ELEC");
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsValidationException()
    {
        SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new CreateCategoryDto("Electronics", "electronics-2", null, null, 0, null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Create_DuplicateSlug_ThrowsValidationException()
    {
        SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new CreateCategoryDto("Different Name", "electronics", null, null, 0, null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Slug");
    }

    [Fact]
    public async Task Create_InvalidParent_ThrowsValidationException()
    {
        var service = CreateService();
        var dto = new CreateCategoryDto("Phones", "phones", Guid.NewGuid(), null, 0, null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("ParentId");
    }

    [Fact]
    public async Task Create_EmptyName_ThrowsValidationException()
    {
        var service = CreateService();
        var dto = new CreateCategoryDto("", "some-slug", null, null, 0, null);

        var act = () => service.CreateAsync(dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    // --- Update tests ---

    [Fact]
    public async Task Update_ValidUpdate_ReturnsUpdatedCategory()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new UpdateCategoryDto("Updated Electronics", null, null, null, null, null, null, 0);

        var result = await service.UpdateAsync(category.Id, dto);

        result.Name.Should().Be("Updated Electronics");
    }

    [Fact]
    public async Task Update_SelfReference_ThrowsValidationException()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new UpdateCategoryDto(null, null, category.Id, null, null, null, null, 0);

        var act = () => service.UpdateAsync(category.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("ParentId");
    }

    [Fact]
    public async Task Update_CircularReference_ThrowsValidationException()
    {
        // Create chain: A -> B -> C, then try to set A.Parent = C (circular)
        var catA = SeedCategory("A", "a");
        var catB = SeedCategory("B", "b", parentId: catA.Id);
        var catC = SeedCategory("C", "c", parentId: catB.Id);
        var service = CreateService();
        var dto = new UpdateCategoryDto(null, null, catC.Id, null, null, null, null, 0);

        var act = () => service.UpdateAsync(catA.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("ParentId");
    }

    [Fact]
    public async Task Update_DuplicateSlug_ThrowsValidationException()
    {
        SeedCategory("Electronics", "electronics");
        var other = SeedCategory("Clothing", "clothing");
        var service = CreateService();
        var dto = new UpdateCategoryDto(null, "electronics", null, null, null, null, null, 0);

        var act = () => service.UpdateAsync(other.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Slug");
    }

    // --- Delete tests ---

    [Fact]
    public async Task Delete_ValidCategory_Removes()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();

        await service.DeleteAsync(category.Id);

        var exists = await _db.Categories.AnyAsync(c => c.Id == category.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        var service = CreateService();

        var act = () => service.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_HasChildren_ThrowsConflictException()
    {
        var parent = SeedCategory("Electronics", "electronics");
        SeedCategory("Phones", "phones", parentId: parent.Id);
        var service = CreateService();

        var act = () => service.DeleteAsync(parent.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // --- Variation Field tests ---

    [Fact]
    public async Task CreateVariationField_ValidField_ReturnsDto()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new CreateVariationFieldDto("Color", "select", new[] { "Red", "Blue" }, true);

        var result = await service.CreateVariationFieldAsync(category.Id, dto);

        result.Name.Should().Be("Color");
        result.Type.Should().Be("select");
        result.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task CreateVariationField_DuplicateNameInCategory_ThrowsValidation()
    {
        var category = SeedCategory("Electronics", "electronics");
        SeedVariationField(category.Id, "Color");
        var service = CreateService();
        var dto = new CreateVariationFieldDto("Color", "select", new[] { "Red", "Blue" }, true);

        var act = () => service.CreateVariationFieldAsync(category.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task CreateVariationField_DuplicateNameFromAncestor_ThrowsValidation()
    {
        var parent = SeedCategory("Electronics", "electronics");
        SeedVariationField(parent.Id, "Voltage");
        var child = SeedCategory("Phones", "phones", parentId: parent.Id);
        var service = CreateService();
        var dto = new CreateVariationFieldDto("Voltage", "text", null, false);

        var act = () => service.CreateVariationFieldAsync(child.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task CreateVariationField_InvalidType_ThrowsValidation()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new CreateVariationFieldDto("Color", "invalid", null, true);

        var act = () => service.CreateVariationFieldAsync(category.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Type");
    }

    [Fact]
    public async Task CreateVariationField_SelectWithFewerThan2Options_ThrowsValidation()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();
        var dto = new CreateVariationFieldDto("Color", "select", new[] { "Red" }, true);

        var act = () => service.CreateVariationFieldAsync(category.Id, dto);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Options");
    }

    [Fact]
    public async Task GetInheritedVariationFields_WalksUpParentChain()
    {
        var root = SeedCategory("Electronics", "electronics");
        SeedVariationField(root.Id, "Voltage", order: 0);
        var child = SeedCategory("Phones", "phones", parentId: root.Id);
        var service = CreateService();

        var result = await service.GetInheritedVariationFieldsAsync(child.Id);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Voltage");
        result[0].InheritedFrom.Should().Be("Electronics");
    }

    [Fact]
    public async Task DeleteVariationField_ValidField_Removes()
    {
        var category = SeedCategory("Electronics", "electronics");
        var field = SeedVariationField(category.Id, "Color");
        var service = CreateService();

        await service.DeleteVariationFieldAsync(category.Id, field.Id);

        var exists = await _db.VariationFields.AnyAsync(f => f.Id == field.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteVariationField_NotFound_ThrowsNotFoundException()
    {
        var category = SeedCategory("Electronics", "electronics");
        var service = CreateService();

        var act = () => service.DeleteVariationFieldAsync(category.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
