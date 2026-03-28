using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace PeruShopHub.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that replaces PostgreSQL and Redis
/// connections with TestContainers instances.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("perushophub_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PeruShopHubDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Add DbContext with TestContainers PostgreSQL
            services.AddDbContext<PeruShopHubDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace Redis connection string
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _redis.GetConnectionString();
                options.InstanceName = "perushophub_test:";
            });

            // Replace SignalR Redis backplane with in-memory
            // (TestContainers Redis is available but SignalR backplane is not needed in tests)
            var signalRDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("SignalR") == true
                         && d.ServiceType.FullName?.Contains("Redis") == true)
                .ToList();
            foreach (var descriptor in signalRDescriptors)
                services.Remove(descriptor);

            services.AddSignalR();

            // Build the service provider and run migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
            db.Database.Migrate();
        });
    }
}
