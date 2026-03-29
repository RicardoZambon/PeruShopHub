using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class NotificationsControllerTests : IntegrationTestBase
{
    public NotificationsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Notifications_GetAll_ReturnsOk()
    {
        await Authenticate("notif-list@test.com");
        var res = await Client.GetAsync("/api/notifications");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Notifications_MarkAllAsRead_ReturnsNoContent()
    {
        await Authenticate("notif-read@test.com");
        var res = await Client.PatchAsync("/api/notifications/read-all", null);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
