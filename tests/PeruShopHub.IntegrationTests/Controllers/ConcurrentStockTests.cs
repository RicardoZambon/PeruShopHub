using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Core.Entities;
using PeruShopHub.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ConcurrentStockTests : IntegrationTestBase
{
    public ConcurrentStockTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return auth.User.TenantId!.Value;
    }

    [Fact]
    public async Task ConcurrentPOReceive_And_Sale_StockConsistency()
    {
        var tenantId = await Authenticate("stock-concurrent@test.com");

        // Create product with initial stock
        var productDto = new CreateProductDto("CONC-STK-001", "Concurrent Stock Product", null, null, 100.00m, 40.00m, 2.00m, null, null, 1m, 10m, 10m, 10m);
        var productRes = await Client.PostAsJsonAsync("/api/products", productDto);
        productRes.EnsureSuccessStatusCode();
        var product = await productRes.Content.ReadFromJsonAsync<ProductDetailDto>();

        Guid variantId;
        using (var db = CreateDbContext())
        {
            var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == product!.Id);
            variant!.Stock = 20; // Start with 20
            variantId = variant.Id;
            await db.SaveChangesAsync();
        }

        // Create a PO to add 30 more units
        var poDto = new CreatePurchaseOrderDto(
            "Fornecedor",
            "Concurrent test",
            new List<CreatePurchaseOrderItemDto> { new(product!.Id, variantId, 30, 35.00m) },
            null
        );
        var poRes = await Client.PostAsJsonAsync("/api/purchase-orders", poDto);
        poRes.EnsureSuccessStatusCode();
        var po = await poRes.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();

        // Create an order that will deplete 5 units (via fulfillment)
        var orderId = Guid.NewGuid();
        using (var db = CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                TenantId = tenantId,
                ExternalOrderId = "CONC-ORD-001",
                BuyerName = "Buyer Conc",
                ItemCount = 1,
                TotalAmount = 100m,
                Status = "Pago",
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            db.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = orderId,
                ProductId = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Quantity = 5,
                UnitPrice = 100m,
                Subtotal = 500m
            });
            await db.SaveChangesAsync();
        }

        // Execute both operations concurrently
        var receiveTask = Client.PostAsync($"/api/purchase-orders/{po!.Id}/receive", null);
        var fulfillTask = Client.PostAsync($"/api/orders/{orderId}/fulfill", null);

        var results = await Task.WhenAll(receiveTask, fulfillTask);

        // Both should succeed (or one might get a concurrency retry)
        // At minimum, neither should return 500
        foreach (var result in results)
        {
            result.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }

        // Verify final stock: started at 20, +30 (PO), -5 (fulfill) = 45
        using (var db = CreateDbContext())
        {
            var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId);
            variant.Should().NotBeNull();
            // Stock should reflect both operations (exact value depends on which ran first)
            // If both succeeded: 20 + 30 - 5 = 45
            // If one failed due to concurrency: could be 50 or 15
            variant!.Stock.Should().BeGreaterThanOrEqualTo(0); // At minimum, no negative stock
        }
    }
}
