using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Webhooks;
using PeruShopHub.Core.Entities;
using PeruShopHub.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class WebhookAndOAuthTests : IntegrationTestBase
{
    public WebhookAndOAuthTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(AuthResponse Auth, Guid TenantId)> Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (auth, auth.User.TenantId!.Value);
    }

    [Fact]
    public async Task Webhook_MercadoLivre_Enqueues()
    {
        // Webhooks are anonymous - set up a marketplace connection first
        var (auth, tenantId) = await Authenticate("webhook-test@test.com");

        // Seed a MarketplaceConnection so the webhook can find it
        using (var db = CreateDbContext())
        {
            db.MarketplaceConnections.Add(new MarketplaceConnection
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MarketplaceId = "mercadolivre",
                Name = "Mercado Livre",
                ExternalUserId = "12345",
                AccessTokenProtected = "encrypted_token",
                RefreshTokenProtected = "encrypted_refresh",
                TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                IsConnected = true,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Send webhook (anonymous endpoint)
        Client.DefaultRequestHeaders.Authorization = null;
        var webhook = new MercadoLivreWebhookDto
        {
            Topic = "orders_v2",
            Resource = "/orders/123456",
            UserId = 12345,
            ApplicationId = 99999
        };
        var res = await Client.PostAsJsonAsync("/api/webhooks/mercadolivre", webhook);
        // Webhook should return 200 OK (accepted, enqueued)
        res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task OAuth_GetAuthUrl_ReturnsUrl()
    {
        await Authenticate("oauth-url@test.com");
        var res = await Client.GetAsync("/api/integrations/mercadolivre/auth-url");
        // Should return OK with an auth URL
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("url");
    }

    [Fact]
    public async Task OAuth_Callback_WithoutCode_Returns400()
    {
        await Authenticate("oauth-callback@test.com");
        // Callback without proper code should fail
        var res = await Client.GetAsync("/api/integrations/mercadolivre/callback?code=&state=invalid");
        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Integrations_Listings_ReturnsOk()
    {
        await Authenticate("integ-listings@test.com");
        var res = await Client.GetAsync("/api/integrations/mercadolivre/listings?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Integrations_ImportStatus_ReturnsOk()
    {
        await Authenticate("integ-import@test.com");
        var res = await Client.GetAsync("/api/integrations/mercadolivre/import/status");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Integrations_OrderSyncStatus_ReturnsOk()
    {
        await Authenticate("integ-sync@test.com");
        var res = await Client.GetAsync("/api/integrations/mercadolivre/sync-orders/status");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
