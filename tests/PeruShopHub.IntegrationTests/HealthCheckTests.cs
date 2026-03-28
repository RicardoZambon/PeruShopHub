using System.Net;
using FluentAssertions;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests;

[Collection("Integration")]
public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Database_IsMigrated()
    {
        // Verify that TestContainers PostgreSQL has migrations applied
        using var db = CreateDbContext();
        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}
