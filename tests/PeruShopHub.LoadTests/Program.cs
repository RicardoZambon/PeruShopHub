using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace PeruShopHub.LoadTests;

/// <summary>
/// Load tests for PeruShopHub API endpoints.
/// Targets: webhook processing, order detail fetches, product creates.
///
/// Usage:
///   dotnet run --project tests/PeruShopHub.LoadTests -- [--base-url http://localhost:5062]
///
/// Requires the API to be running. Set BASE_URL env var or pass --base-url argument.
/// </summary>
public class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task Main(string[] args)
    {
        var baseUrl = GetBaseUrl(args);
        Console.WriteLine($"Load test target: {baseUrl}");

        // Setup: register a user and get auth token for authenticated endpoints
        var (token, tenantId) = await SetupAuthAsync(baseUrl);
        Console.WriteLine("Auth setup complete.");

        // Seed some orders for the order detail test
        var orderIds = await SeedOrdersAsync(baseUrl, token);
        Console.WriteLine($"Seeded/found {orderIds.Count} orders.");

        // Seed some products for the product GET tests
        var productIds = await SeedProductsAsync(baseUrl, token, 5);
        Console.WriteLine($"Seeded {productIds.Count} products.");

        using var httpClient = new HttpClient();
        var webhookCounter = 0;
        var productCounter = 0;

        // Scenario 1: 100 concurrent webhook POSTs
        var webhookScenario = Scenario.Create("webhook_processing", async context =>
        {
            var id = Interlocked.Increment(ref webhookCounter);
            var payload = new
            {
                _id = $"load-test-{id}-{context.InvocationNumber}",
                topic = "orders_v2",
                resource = $"/orders/{10000 + id}",
                user_id = 999999L,
                application_id = 888888L,
                sent = DateTime.UtcNow.ToString("o"),
                attempts = 1,
                received = DateTime.UtcNow.ToString("o")
            };

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/webhooks/mercadolivre")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(
                    JsonSerializer.Serialize(payload, JsonOptions),
                    Encoding.UTF8,
                    "application/json"));

            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Scenario 2: 50 concurrent order detail GETs
        var orderScenario = Scenario.Create("order_detail_fetch", async context =>
        {
            if (orderIds.Count == 0)
                return Response.Fail(statusCode: "NO_ORDERS", message: "No orders seeded");

            var orderId = orderIds[(int)(context.InvocationNumber % orderIds.Count)];

            var request = Http.CreateRequest("GET", $"{baseUrl}/api/orders/{orderId}")
                .WithHeader("Authorization", $"Bearer {token}");

            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Scenario 3: 20 concurrent product creates
        var productScenario = Scenario.Create("product_create", async context =>
        {
            var id = Interlocked.Increment(ref productCounter);
            var product = new
            {
                sku = $"LOAD-{id:D6}",
                name = $"Load Test Product {id}",
                description = "Created during load test",
                price = 99.90m,
                purchaseCost = 40.00m,
                packagingCost = 2.50m,
                weight = 0.5m,
                height = 10m,
                width = 20m,
                length = 30m
            };

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/products")
                .WithHeader("Authorization", $"Bearer {token}")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(
                    JsonSerializer.Serialize(product, JsonOptions),
                    Encoding.UTF8,
                    "application/json"));

            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        NBomberRunner
            .RegisterScenarios(webhookScenario, orderScenario, productScenario)
            .WithReportFolder("load-test-results")
            .WithReportFormats(
                NBomber.Contracts.Stats.ReportFormat.Txt,
                NBomber.Contracts.Stats.ReportFormat.Html)
            .Run();
    }

    private static string GetBaseUrl(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--base-url")
                return args[i + 1].TrimEnd('/');
        }

        var envUrl = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrEmpty(envUrl))
            return envUrl.TrimEnd('/');

        return "http://localhost:5062";
    }

    private static async Task<(string Token, Guid TenantId)> SetupAuthAsync(string baseUrl)
    {
        using var client = new HttpClient();
        var email = $"loadtest-{Guid.NewGuid():N}@test.com";
        var registerPayload = new
        {
            shopName = "Load Test Shop",
            ownerName = "Load Tester",
            email,
            password = "Password123!"
        };

        var response = await client.PostAsJsonAsync($"{baseUrl}/api/auth/register", registerPayload);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("accessToken").GetString()!;
        var tenantId = doc.RootElement.GetProperty("user").GetProperty("tenantId").GetString()!;
        return (token, Guid.Parse(tenantId));
    }

    private static async Task<List<Guid>> SeedOrdersAsync(string baseUrl, string token)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{baseUrl}/api/orders?page=1&pageSize=50");
        if (!response.IsSuccessStatusCode)
            return new List<Guid>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var ids = new List<Guid>();
        if (doc.RootElement.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idProp))
                    ids.Add(Guid.Parse(idProp.GetString()!));
            }
        }

        return ids;
    }

    private static async Task<List<Guid>> SeedProductsAsync(string baseUrl, string token, int count)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            var product = new
            {
                sku = $"SEED-{Guid.NewGuid():N}"[..16],
                name = $"Seed Product {i + 1}",
                description = "Seeded for load test",
                price = 50.00m + i * 10,
                purchaseCost = 20.00m,
                packagingCost = 1.50m,
                weight = 0.3m,
                height = 5m,
                width = 10m,
                length = 15m
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/api/products", product);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id", out var idProp))
                    ids.Add(Guid.Parse(idProp.GetString()!));
            }
        }

        return ids;
    }
}
