using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests;

[Collection("Integration")]
public class TokenEncryptionIntegrationTests : IntegrationTestBase
{
    public TokenEncryptionIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task SaveConnection_ReloadFromDb_TokensDecryptCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var accessToken = "APP_USR-test-access-token-12345";
        var refreshToken = "TG-test-refresh-token-67890";

        // Encrypt and save
        using (var scope = Factory.Services.CreateScope())
        {
            var encryption = scope.ServiceProvider.GetRequiredService<ITokenEncryptionService>();
            var db = scope.ServiceProvider.GetRequiredService<PeruShopHub.Infrastructure.Persistence.PeruShopHubDbContext>();

            var connection = new MarketplaceConnection
            {
                Id = connectionId,
                TenantId = tenantId,
                MarketplaceId = "mercadolivre",
                Name = "Mercado Livre",
                IsConnected = true,
                Status = "Active",
                AccessTokenProtected = encryption.Encrypt(accessToken),
                RefreshTokenProtected = encryption.Encrypt(refreshToken),
                TokenExpiresAt = DateTime.UtcNow.AddHours(6),
                ExternalUserId = "ML-12345",
                SellerNickname = "TestSeller"
            };

            db.MarketplaceConnections.Add(connection);
            await db.SaveChangesAsync();
        }

        // Reload from DB and decrypt
        using (var scope = Factory.Services.CreateScope())
        {
            var encryption = scope.ServiceProvider.GetRequiredService<ITokenEncryptionService>();
            var db = scope.ServiceProvider.GetRequiredService<PeruShopHub.Infrastructure.Persistence.PeruShopHubDbContext>();

            var loaded = await db.MarketplaceConnections
                .IgnoreQueryFilters()
                .FirstAsync(c => c.Id == connectionId);

            loaded.AccessTokenProtected.Should().NotBeNullOrEmpty();
            loaded.RefreshTokenProtected.Should().NotBeNullOrEmpty();

            // Encrypted values should not match plain text
            loaded.AccessTokenProtected.Should().NotBe(accessToken);
            loaded.RefreshTokenProtected.Should().NotBe(refreshToken);

            // Decrypt should return original tokens
            encryption.Decrypt(loaded.AccessTokenProtected!).Should().Be(accessToken);
            encryption.Decrypt(loaded.RefreshTokenProtected!).Should().Be(refreshToken);
        }
    }
}
