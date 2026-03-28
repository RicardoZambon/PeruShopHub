using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_Login_GetToken_AccessProtected()
    {
        // 1. Register a new user/shop
        var registerRequest = new RegisterRequest("Test Shop Auth", "Auth User", "authflow@test.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();
        authResult!.AccessToken.Should().NotBeNullOrEmpty();
        authResult.RefreshToken.Should().NotBeNullOrEmpty();
        authResult.User.Email.Should().Be("authflow@test.com");
        authResult.User.TenantRole.Should().Be("Owner");

        // 2. Access protected endpoint with token
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        var meResponse = await Client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        me.Should().NotBeNull();
        me!.Email.Should().Be("authflow@test.com");
        me.TenantRole.Should().Be("Owner");

        // 3. Login with same credentials
        Client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest("authflow@test.com", "Password123!");
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.User.Email.Should().Be("authflow@test.com");

        // 4. Verify protected endpoint is 401 without token
        Client.DefaultRequestHeaders.Authorization = null;
        var unauthedResponse = await Client.GetAsync("/api/products");
        unauthedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 5. Refresh token flow
        var refreshRequest = new RefreshRequest(loginResult.RefreshToken);
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBe(loginResult.RefreshToken); // rotated

        // 6. Access with refreshed token
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);
        var meAfterRefresh = await Client.GetAsync("/api/auth/me");
        meAfterRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var request = new RegisterRequest("Shop Dup", "Dup User", "duplicate@test.com", "Password123!");
        var response1 = await Client.PostAsJsonAsync("/api/auth/register", request);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        var response2 = await Client.PostAsJsonAsync("/api/auth/register", request);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var registerRequest = new RegisterRequest("Shop Wrong", "Wrong User", "wrongpw@test.com", "Password123!");
        await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("wrongpw@test.com", "WrongPassword!");
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
