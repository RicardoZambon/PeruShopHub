using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class TokenRenewalTests : IntegrationTestBase
{
    public TokenRenewalTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ExpiredToken_Refresh_RetrySucceeds()
    {
        // 1. Register and get tokens
        var registerReq = new RegisterRequest("Token Shop", "Token User", "token-renewal@test.com", "Password123!");
        var registerRes = await Client.PostAsJsonAsync("/api/auth/register", registerReq);
        registerRes.EnsureSuccessStatusCode();
        var auth = await registerRes.Content.ReadFromJsonAsync<AuthResponse>();

        // 2. Use an invalid/expired token - should get 401
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "expired.invalid.token");
        var protectedRes = await Client.GetAsync("/api/products");
        protectedRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 3. Refresh the token
        Client.DefaultRequestHeaders.Authorization = null;
        var refreshReq = new RefreshRequest(auth!.RefreshToken);
        var refreshRes = await Client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshRes.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBe(auth.AccessToken);
        refreshed.RefreshToken.Should().NotBe(auth.RefreshToken); // rotated

        // 4. Retry with new token - should succeed
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var retryRes = await Client.GetAsync("/api/products");
        retryRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Old refresh token should be invalidated
        Client.DefaultRequestHeaders.Authorization = null;
        var oldRefreshReq = new RefreshRequest(auth.RefreshToken);
        var oldRefreshRes = await Client.PostAsJsonAsync("/api/auth/refresh", oldRefreshReq);
        oldRefreshRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
