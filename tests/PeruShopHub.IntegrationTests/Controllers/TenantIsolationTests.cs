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
public class TenantIsolationTests : IntegrationTestBase
{
    public TenantIsolationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<AuthResponse> RegisterAndAuthenticate(HttpClient client, string shopName, string email)
    {
        var registerRequest = new RegisterRequest(shopName, "Test User", email, "Password123!");
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return auth;
    }

    [Fact]
    public async Task TenantA_Data_Invisible_To_TenantB()
    {
        // Create two separate HTTP clients (two separate tenants)
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();

        try
        {
            // Register Tenant A
            await RegisterAndAuthenticate(clientA, "Tenant A Shop", "tenantA@test.com");

            // Register Tenant B
            await RegisterAndAuthenticate(clientB, "Tenant B Shop", "tenantB@test.com");

            // Tenant A creates a product
            var productDto = new CreateProductDto(
                Sku: "TENA-001", Name: "Tenant A Product", Description: null,
                CategoryId: null, Price: 50.00m, PurchaseCost: 20.00m,
                PackagingCost: 1.00m, StorageCostDaily: null, Supplier: null,
                Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
            var createResponse = await clientA.PostAsJsonAsync("/api/products", productDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var tenantAProduct = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

            // Tenant B creates a product
            var productDtoB = new CreateProductDto(
                Sku: "TENB-001", Name: "Tenant B Product", Description: null,
                CategoryId: null, Price: 75.00m, PurchaseCost: 30.00m,
                PackagingCost: 1.50m, StorageCostDaily: null, Supplier: null,
                Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
            var createResponseB = await clientB.PostAsJsonAsync("/api/products", productDtoB);
            createResponseB.StatusCode.Should().Be(HttpStatusCode.Created);

            // Tenant B lists products — should NOT see Tenant A's product
            var listResponse = await clientB.GetAsync("/api/products");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<ProductListDto>>();
            result.Should().NotBeNull();
            result!.Items.Should().NotContain(p => p.Name == "Tenant A Product");
            result.Items.Should().Contain(p => p.Name == "Tenant B Product");

            // Tenant B tries to get Tenant A's product by ID — should get 404
            var crossTenantResponse = await clientB.GetAsync($"/api/products/{tenantAProduct!.Id}");
            crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Tenant A should see only their product
            var listResponseA = await clientA.GetAsync("/api/products");
            var resultA = await listResponseA.Content.ReadFromJsonAsync<PagedResult<ProductListDto>>();
            resultA!.Items.Should().Contain(p => p.Name == "Tenant A Product");
            resultA.Items.Should().NotContain(p => p.Name == "Tenant B Product");
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }
}
