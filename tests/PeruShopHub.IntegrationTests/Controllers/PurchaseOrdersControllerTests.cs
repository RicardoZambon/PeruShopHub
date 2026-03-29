using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Application.Common;
using PeruShopHub.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class PurchaseOrdersControllerTests : IntegrationTestBase
{
    public PurchaseOrdersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task PurchaseOrder_Create_Receive_Flow()
    {
        await Authenticate("po-flow@test.com");

        // Create product first
        var productDto = new CreateProductDto(
            Sku: "PO-PROD-001",
            Name: "PO Test Product",
            Description: null,
            CategoryId: null,
            Price: 50.00m,
            PurchaseCost: 20.00m,
            PackagingCost: 1.00m,
            StorageCostDaily: null,
            Supplier: null,
            Weight: 1m,
            Height: 10m,
            Width: 10m,
            Length: 10m);
        var productRes = await Client.PostAsJsonAsync("/api/products", productDto);
        productRes.EnsureSuccessStatusCode();
        var product = await productRes.Content.ReadFromJsonAsync<ProductDetailDto>();

        // Get the default variant
        Guid variantId;
        using (var db = CreateDbContext())
        {
            var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == product!.Id);
            variantId = variant!.Id;
        }

        // Create PO
        var poDto = new CreatePurchaseOrderDto(
            "Fornecedor Alpha",
            "Test PO",
            new List<CreatePurchaseOrderItemDto> { new(product!.Id, variantId, 10, 18.00m) },
            null
        );
        var poRes = await Client.PostAsJsonAsync("/api/purchase-orders", poDto);
        poRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var po = await poRes.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        po.Should().NotBeNull();
        po!.Supplier.Should().Be("Fornecedor Alpha");
        po.Items.Should().HaveCount(1);

        // Get list
        var listRes = await Client.GetAsync("/api/purchase-orders");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get detail
        var detailRes = await Client.GetAsync($"/api/purchase-orders/{po.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Receive PO
        var receiveRes = await Client.PostAsync($"/api/purchase-orders/{po.Id}/receive", null);
        receiveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var received = await receiveRes.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        received!.Status.Should().Be("Recebido");
    }
}
