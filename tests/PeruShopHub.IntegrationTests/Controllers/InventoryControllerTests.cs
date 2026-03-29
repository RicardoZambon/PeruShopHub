using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class InventoryControllerTests : IntegrationTestBase
{
    public InventoryControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Inventory_GetAll_ReturnsOk()
    {
        await Authenticate("inventory-list@test.com");
        var res = await Client.GetAsync("/api/inventory?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_StockAdjust_CreatesMovement()
    {
        await Authenticate("inventory-adjust@test.com");

        // Create product first
        var productDto = new CreateProductDto("INV-ADJ-001", "Inventory Adjust Product", null, null, 50.00m, 20.00m, 1.00m, null, null, 1m, 10m, 10m, 10m);
        var productRes = await Client.PostAsJsonAsync("/api/products", productDto);
        productRes.EnsureSuccessStatusCode();
        var product = await productRes.Content.ReadFromJsonAsync<ProductDetailDto>();

        Guid variantId;
        using (var db = CreateDbContext())
        {
            var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == product!.Id);
            variantId = variant!.Id;
        }

        // Adjust stock
        var adjustDto = new StockAdjustmentDto(product!.Id, variantId, 25, "Inventário inicial");
        var adjustRes = await Client.PostAsJsonAsync("/api/inventory/adjust", adjustDto);
        adjustRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify movements
        var movementsRes = await Client.GetAsync($"/api/inventory/movements?productId={product.Id}");
        movementsRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_Alerts_ReturnsOk()
    {
        await Authenticate("inventory-alerts@test.com");
        var res = await Client.GetAsync("/api/inventory/alerts");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_FulfillmentStock_ReturnsOk()
    {
        await Authenticate("inventory-fulfillment@test.com");
        var res = await Client.GetAsync("/api/inventory/fulfillment-stock");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_ReconciliationReports_ReturnsOk()
    {
        await Authenticate("inventory-recon@test.com");
        var res = await Client.GetAsync("/api/inventory/reconciliation-reports?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_MovementsExport_ReturnsExcel()
    {
        await Authenticate("inventory-export@test.com");
        var res = await Client.GetAsync("/api/inventory/movements/export");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Contain("spreadsheet");
    }
}
