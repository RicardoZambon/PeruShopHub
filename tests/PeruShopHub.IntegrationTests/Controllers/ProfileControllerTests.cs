using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Profile;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ProfileControllerTests : IntegrationTestBase
{
    public ProfileControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Profile_GetAndUpdate()
    {
        await Authenticate("profile-test@test.com");

        var getRes = await Client.GetAsync("/api/profile");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateReq = new UpdateProfileRequest("Updated Name");
        var updateRes = await Client.PutAsJsonAsync("/api/profile", updateReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Profile_ChangePassword()
    {
        await Authenticate("profile-pw@test.com");

        var changePwReq = new ChangePasswordRequest("Password123!", "NewPassword456!");
        var res = await Client.PutAsJsonAsync("/api/profile/password", changePwReq);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Profile_DeletionStatus_ReturnsOk()
    {
        await Authenticate("profile-deletion@test.com");
        var res = await Client.GetAsync("/api/profile/deletion-status");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
