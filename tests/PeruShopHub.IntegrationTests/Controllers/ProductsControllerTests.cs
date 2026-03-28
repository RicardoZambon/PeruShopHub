using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ProductsControllerTests : IntegrationTestBase
{
    public ProductsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<AuthResponse> RegisterAndAuthenticate(string email)
    {
        var registerRequest = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return auth;
    }

    [Fact]
    public async Task Product_CRUD_FullLifecycle()
    {
        await RegisterAndAuthenticate("product-crud@test.com");

        // 1. Create product
        var createDto = new CreateProductDto(
            Sku: "PROD-001",
            Name: "Test Product",
            Description: "A test product",
            CategoryId: null,
            Price: 99.90m,
            PurchaseCost: 40.00m,
            PackagingCost: 2.50m,
            Supplier: "Test Supplier",
            Weight: 0.5m,
            Height: 10m,
            Width: 20m,
            Length: 30m);

        var createResponse = await Client.PostAsJsonAsync("/api/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Test Product");
        created.Sku.Should().Be("PROD-001");
        created.Price.Should().Be(99.90m);
        var productId = created.Id;

        // 2. Read product
        var getResponse = await Client.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        fetched!.Name.Should().Be("Test Product");

        // 3. Update product
        var updateDto = new UpdateProductDto(
            Sku: null, Name: "Updated Product", Description: null,
            CategoryId: null, Price: 129.90m, PurchaseCost: null,
            PackagingCost: null, Supplier: null, Status: null,
            IsActive: null, Weight: null, Height: null,
            Width: null, Length: null, Version: created.Version);

        var updateResponse = await Client.PutAsJsonAsync($"/api/products/{productId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        updated!.Name.Should().Be("Updated Product");
        updated.Price.Should().Be(129.90m);

        // 4. Delete product
        var deleteResponse = await Client.DeleteAsync($"/api/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify 404 after delete
        var getAfterDelete = await Client.GetAsync($"/api/products/{productId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_WithPagination_ReturnsPagedResult()
    {
        await RegisterAndAuthenticate("product-paging@test.com");

        // Create 3 products
        for (int i = 1; i <= 3; i++)
        {
            var dto = new CreateProductDto(
                Sku: $"PAG-{i:D3}", Name: $"Paged Product {i}", Description: null,
                CategoryId: null, Price: 10m * i, PurchaseCost: 5m,
                PackagingCost: 1m, Supplier: null,
                Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
            await Client.PostAsJsonAsync("/api/products", dto);
        }

        var response = await Client.GetAsync("/api/products?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductListDto>>();
        result.Should().NotBeNull();
        result!.Items.Count.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
    }
}
