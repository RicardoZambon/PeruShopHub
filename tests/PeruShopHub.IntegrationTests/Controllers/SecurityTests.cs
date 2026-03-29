using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

/// <summary>
/// US-080: Security Tests — Tenant Isolation & Auth
/// </summary>
[Collection("Integration")]
public class SecurityTests : IntegrationTestBase
{
    // JWT config matches appsettings.json defaults (used in Testing environment)
    private const string JwtSecret = "CHANGE-THIS-TO-A-SECURE-SECRET-AT-LEAST-32-CHARS";
    private const string JwtIssuer = "PeruShopHub";
    private const string JwtAudience = "PeruShopHub";

    public SecurityTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<AuthResponse> RegisterAndAuthenticate(HttpClient client, string shopName, string email)
    {
        var request = new RegisterRequest(shopName, "Test User", email, "Password123!");
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return auth;
    }

    private static string GenerateToken(
        DateTime expires,
        string? tenantId = null,
        string? role = null,
        bool isSuperAdmin = false)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "sectest@test.com"),
            new("name", "Security Test User"),
            new("is_super_admin", isSuperAdmin.ToString().ToLowerInvariant()),
        };

        if (tenantId is not null)
        {
            claims.Add(new Claim("tenant_id", tenantId));
            claims.Add(new Claim("tenant_role", role ?? "Owner"));
            claims.Add(new Claim(ClaimTypes.Role, role ?? "Owner"));
        }

        if (isSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── AC1: Tenant A product invisible to Tenant B ──────────────────────

    [Fact]
    public async Task TenantA_Product_Invisible_To_TenantB()
    {
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();
        try
        {
            await RegisterAndAuthenticate(clientA, "SecTenantA", $"sectenA-{Guid.NewGuid():N}@test.com");
            await RegisterAndAuthenticate(clientB, "SecTenantB", $"sectenB-{Guid.NewGuid():N}@test.com");

            // Tenant A creates a product
            var dto = new CreateProductDto(
                Sku: "SEC-A-001", Name: "Secret Product A", Description: null,
                CategoryId: null, Price: 100m, PurchaseCost: 40m,
                PackagingCost: 1m, StorageCostDaily: null, Supplier: null,
                Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
            var createResp = await clientA.PostAsJsonAsync("/api/products", dto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = await createResp.Content.ReadFromJsonAsync<ProductDetailDto>();

            // Tenant B cannot see it in list
            var listResp = await clientB.GetAsync("/api/products");
            var list = await listResp.Content.ReadFromJsonAsync<PagedResult<ProductListDto>>();
            list!.Items.Should().NotContain(p => p.Name == "Secret Product A");

            // Tenant B gets 404 for direct access
            var directResp = await clientB.GetAsync($"/api/products/{product!.Id}");
            directResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    // ── AC2: Tenant A order → Tenant B GET → 404 ────────────────────────

    [Fact]
    public async Task TenantA_Order_Returns_404_For_TenantB()
    {
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();
        try
        {
            var authA = await RegisterAndAuthenticate(clientA, "SecOrderTenA", $"secordA-{Guid.NewGuid():N}@test.com");
            await RegisterAndAuthenticate(clientB, "SecOrderTenB", $"secordB-{Guid.NewGuid():N}@test.com");

            // Seed an order directly in DB for Tenant A
            var orderId = Guid.NewGuid();
            using (var db = CreateDbContext())
            {
                db.Orders.Add(new Core.Entities.Order
                {
                    Id = orderId,
                    TenantId = authA.User.TenantId!.Value,
                    ExternalOrderId = "SEC-TEST-ORDER-001",
                    Status = "Pago",
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 150m,
                    BuyerName = "Test Buyer",
                    BuyerEmail = "buyer@test.com",
                });
                await db.SaveChangesAsync();
            }

            // Tenant B tries to get Tenant A's order → 404
            var resp = await clientB.GetAsync($"/api/orders/{orderId}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    // ── AC3: Expired JWT → 401 on all endpoints ─────────────────────────

    [Fact]
    public async Task Expired_JWT_Returns_401()
    {
        // Register to get a valid tenant ID
        var setupClient = Factory.CreateClient();
        var auth = await RegisterAndAuthenticate(setupClient, "SecExpTenant", $"secexp-{Guid.NewGuid():N}@test.com");
        setupClient.Dispose();

        // Create a token that expired 1 hour ago
        var expiredToken = GenerateToken(
            expires: DateTime.UtcNow.AddHours(-1),
            tenantId: auth.User.TenantId!.Value.ToString(),
            role: "Owner");

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        try
        {
            var endpoints = new[]
            {
                ("/api/products", "GET"),
                ("/api/categories", "GET"),
                ("/api/orders", "GET"),
                ("/api/auth/me", "GET"),
                ("/api/settings/tax-rate", "GET"),
                ("/api/dashboard/summary", "GET"),
            };

            foreach (var (path, _) in endpoints)
            {
                var response = await client.GetAsync(path);
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"endpoint {path} should reject expired JWT");
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── AC4: JWT without tenant → 403 on tenant-required endpoints ──────

    [Fact]
    public async Task JWT_Without_Tenant_Returns_403()
    {
        // Create a valid JWT with NO tenant_id claim (non-super-admin)
        var noTenantToken = GenerateToken(
            expires: DateTime.UtcNow.AddMinutes(15),
            tenantId: null,
            role: null);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", noTenantToken);

        try
        {
            var tenantEndpoints = new[]
            {
                "/api/products",
                "/api/categories",
                "/api/orders",
                "/api/dashboard/summary",
            };

            foreach (var path in tenantEndpoints)
            {
                var response = await client.GetAsync(path);
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                    $"endpoint {path} should reject JWT without tenant context");
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── AC5: Non-admin → admin endpoints → 403 ─────────────────────────

    [Fact]
    public async Task Non_Admin_Cannot_Access_Admin_Endpoints()
    {
        // Register creates an "Owner" role user — but not "SuperAdmin"
        var client = Factory.CreateClient();
        try
        {
            await RegisterAndAuthenticate(client, "SecNonAdminShop", $"secnonadm-{Guid.NewGuid():N}@test.com");

            // SuperAdmin-only endpoint
            var adminResp = await client.GetAsync("/api/admin/tenants");
            adminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "Owner role should not access SuperAdmin endpoints");
        }
        finally
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task Unauthenticated_Cannot_Access_Protected_Endpoints()
    {
        var client = Factory.CreateClient();
        // No auth header set
        try
        {
            var endpoints = new[]
            {
                "/api/products",
                "/api/orders",
                "/api/categories",
                "/api/settings/tax-rate",
                "/api/admin/tenants",
            };

            foreach (var path in endpoints)
            {
                var response = await client.GetAsync(path);
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"unauthenticated request to {path} should return 401");
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── AC6: SQL injection in search params → properly escaped ──────────

    [Fact]
    public async Task SQL_Injection_In_Search_Is_Escaped()
    {
        var client = Factory.CreateClient();
        try
        {
            await RegisterAndAuthenticate(client, "SecSqlInjShop", $"secsqli-{Guid.NewGuid():N}@test.com");

            var payloads = new[]
            {
                "'; DROP TABLE products; --",
                "1 OR 1=1",
                "' UNION SELECT * FROM system_users --",
                "Robert'); DROP TABLE orders;--",
                "1; DELETE FROM products WHERE 1=1",
            };

            foreach (var payload in payloads)
            {
                // Search param — EF Core parameterizes these, so they should be safe
                var response = await client.GetAsync($"/api/products?search={Uri.EscapeDataString(payload)}");
                // Should return 200 with empty results, NOT 500 (DB error)
                response.StatusCode.Should().Be(HttpStatusCode.OK,
                    $"SQL injection payload '{payload}' should be safely handled");

                var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductListDto>>();
                result.Should().NotBeNull();
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── AC7: XSS payload in product name → stored safely ────────────────

    [Fact]
    public async Task XSS_Payload_In_Product_Name_Stored_Safely()
    {
        var client = Factory.CreateClient();
        try
        {
            await RegisterAndAuthenticate(client, "SecXssShop", $"secxss-{Guid.NewGuid():N}@test.com");

            var xssPayloads = new[]
            {
                "<script>alert('xss')</script>",
                "<img src=x onerror=alert(1)>",
                "javascript:alert(document.cookie)",
                "<svg onload=alert(1)>",
                "'\"><script>alert(1)</script>",
            };

            foreach (var payload in xssPayloads)
            {
                var dto = new CreateProductDto(
                    Sku: $"XSS-{Guid.NewGuid():N}"[..20],
                    Name: payload,
                    Description: payload,
                    CategoryId: null,
                    Price: 10m, PurchaseCost: 5m,
                    PackagingCost: 0.5m, StorageCostDaily: null, Supplier: null,
                    Weight: 1m, Height: 1m, Width: 1m, Length: 1m);

                var createResp = await client.PostAsJsonAsync("/api/products", dto);
                createResp.StatusCode.Should().Be(HttpStatusCode.Created,
                    $"XSS payload should be accepted and stored safely");

                var product = await createResp.Content.ReadFromJsonAsync<ProductDetailDto>();
                product.Should().NotBeNull();

                // The value is stored as-is (server stores raw, frontend escapes on render)
                // The critical thing is it doesn't cause a 500 or get interpreted server-side
                product!.Name.Should().Be(payload, "value should be stored exactly as submitted");

                // Fetch it back via API — should return the raw value (JSON-encoded)
                var getResp = await client.GetAsync($"/api/products/{product.Id}");
                getResp.StatusCode.Should().Be(HttpStatusCode.OK);
                var fetched = await getResp.Content.ReadFromJsonAsync<ProductDetailDto>();
                fetched!.Name.Should().Be(payload);

                // The JSON response Content-Type should be application/json (not text/html)
                // which means browsers won't interpret the payload as HTML
                getResp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── AC8: CSRF — token-based auth makes CSRF N/A ─────────────────────

    [Fact]
    public async Task Api_Uses_Token_Auth_Not_Cookies_CSRF_Not_Applicable()
    {
        var client = Factory.CreateClient();
        try
        {
            var email = $"seccsrf-{Guid.NewGuid():N}@test.com";
            await RegisterAndAuthenticate(client, "SecCsrfShop", email);

            // Make a state-changing request
            var dto = new CreateProductDto(
                Sku: "CSRF-001", Name: "CSRF Test Product", Description: null,
                CategoryId: null, Price: 10m, PurchaseCost: 5m,
                PackagingCost: 0.5m, StorageCostDaily: null, Supplier: null,
                Weight: 1m, Height: 1m, Width: 1m, Length: 1m);
            var createResp = await client.PostAsJsonAsync("/api/products", dto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify no auth cookies are set — API uses Bearer tokens only
            // A request WITHOUT the Authorization header (but with any cookies) should fail
            var noCookieClient = Factory.CreateClient();
            var noAuthResp = await noCookieClient.GetAsync("/api/products");
            noAuthResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "API should not authenticate via cookies — Bearer token required");
            noCookieClient.Dispose();

            // Verify the API returns JSON content type (not HTML that could be CSRF'd)
            var listResp = await client.GetAsync("/api/products");
            listResp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }
        finally
        {
            client.Dispose();
        }
    }

    // ── Additional: Tampered JWT signature → 401 ────────────────────────

    [Fact]
    public async Task Tampered_JWT_Signature_Returns_401()
    {
        var client = Factory.CreateClient();
        try
        {
            // Create a valid-looking token signed with the wrong key
            var wrongKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("THIS-IS-A-WRONG-SECRET-KEY-32-CHARS!!"));
            var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims: new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) },
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);
            var tamperedToken = new JwtSecurityTokenHandler().WriteToken(token);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
            var resp = await client.GetAsync("/api/products");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            client.Dispose();
        }
    }
}
