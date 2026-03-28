using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ConcurrencyTests : IntegrationTestBase
{
    public ConcurrencyTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var registerRequest = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Product_ConcurrentUpdates_SecondGets409()
    {
        await Authenticate("concurrency-product@test.com");

        // Create a product
        var createDto = new CreateProductDto(
            Sku: "CONC-001", Name: "Concurrent Product", Description: null,
            CategoryId: null, Price: 50.00m, PurchaseCost: 20.00m,
            PackagingCost: 1.00m, StorageCostDaily: null, Supplier: null,
            Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
        var createResponse = await Client.PostAsJsonAsync("/api/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        var originalVersion = product!.Version;

        // First update succeeds with the original version
        var update1 = new UpdateProductDto(
            Sku: null, Name: "Updated by User 1", Description: null,
            CategoryId: null, Price: 60.00m, PurchaseCost: null,
            PackagingCost: null, StorageCostDaily: null, Supplier: null, Status: null,
            IsActive: null, Weight: null, Height: null,
            Width: null, Length: null, Version: originalVersion);
        var response1 = await Client.PutAsJsonAsync($"/api/products/{product.Id}", update1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale version should get 409 Conflict
        var update2 = new UpdateProductDto(
            Sku: null, Name: "Updated by User 2", Description: null,
            CategoryId: null, Price: 70.00m, PurchaseCost: null,
            PackagingCost: null, StorageCostDaily: null, Supplier: null, Status: null,
            IsActive: null, Weight: null, Height: null,
            Width: null, Length: null, Version: originalVersion); // stale version!
        var response2 = await Client.PutAsJsonAsync($"/api/products/{product.Id}", update2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Category_ConcurrentUpdates_SecondGets409()
    {
        await Authenticate("concurrency-category@test.com");

        // Create a category
        var createDto = new CreateCategoryDto("Concurrent Cat", "concurrent-cat", null, null, 1, null);
        var createResponse = await Client.PostAsJsonAsync("/api/categories", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        var originalVersion = category!.Version;

        // First update succeeds
        var update1 = new UpdateCategoryDto(
            Name: "Cat Updated 1", Slug: null, ParentId: null,
            Icon: null, IsActive: null, Order: null,
            SkuPrefix: null, Version: originalVersion);
        var response1 = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", update1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale version → 409
        var update2 = new UpdateCategoryDto(
            Name: "Cat Updated 2", Slug: null, ParentId: null,
            Icon: null, IsActive: null, Order: null,
            SkuPrefix: null, Version: originalVersion);
        var response2 = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", update2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
