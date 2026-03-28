using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Infrastructure.Persistence;
using Xunit;

namespace PeruShopHub.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides access to the test server,
/// an HTTP client, and a scoped DbContext. Uses TestContainers for
/// PostgreSQL 16 and Redis 7.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Creates a new scoped DbContext for test assertions.
    /// Callers should dispose the scope when done.
    /// </summary>
    protected PeruShopHubDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
