using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class LaunchOmphalosIntegrationTests(WebApplicationFactoryBase factory)
    : IClassFixture<WebApplicationFactoryBase>
{
    private readonly HttpClient _client = factory.CreateNonRedirectingClient();

    private async Task SeedSettingsAsync(
        string? url = "https://omphalos.example.com",
        string? secret = "test-secret",
        bool isEnabled = true)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        // Clear any pre-existing settings to avoid PK conflicts
        context.AdminSettings.RemoveRange(context.AdminSettings);
        await context.SaveChangesAsync();
        context.AdminSettings.AddRange(
            new AdminSettingEntity { Key = "IsEnabled", Value = isEnabled.ToString(), UpdatedAt = DateTime.UtcNow },
            new AdminSettingEntity { Key = "OmphalosUrl", Value = url ?? string.Empty, UpdatedAt = DateTime.UtcNow },
            new AdminSettingEntity { Key = "OmphalosSharedSecret", Value = secret ?? string.Empty, UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task LaunchOmphalos_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        var response = await _client.GetAsync("/Quest/LaunchOmphalos/1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LaunchOmphalos_WhenPlayerRole_ShouldReturnForbiddenOrRedirect()
    {
        var (playerClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, "player1", "player1@example.com", roles: ["Player"]);

        var response = await playerClient.GetAsync("/Quest/LaunchOmphalos/1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LaunchOmphalos_WhenIntegrationDisabled_ShouldReturn404()
    {
        await SeedSettingsAsync(isEnabled: false);
        var (dmClient, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);

        var response = await dmClient.GetAsync("/Quest/LaunchOmphalos/1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LaunchOmphalos_WhenOmphalosUrlBlank_ShouldReturn404()
    {
        await SeedSettingsAsync(url: string.Empty);
        var (dmClient, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);

        var response = await dmClient.GetAsync("/Quest/LaunchOmphalos/1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LaunchOmphalos_WhenIntegrationEnabled_ShouldRedirectToOmphalosUrl()
    {
        await SeedSettingsAsync();
        var (dmClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);

        // Create a quest owned by the DM
        var quest = await TestDataHelper.CreateTestQuestAsync(factory.Services, dmUser.Id, title: "Test Quest");

        var response = await dmClient.GetAsync(
            $"/Quest/LaunchOmphalos/{quest.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("https://omphalos.example.com/api/sso/open-quest");
    }

    [Fact]
    public async Task LaunchOmphalos_RedirectUrl_ContainsExpectedQueryParameters()
    {
        await SeedSettingsAsync();
        var (dmClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);
        var quest = await TestDataHelper.CreateTestQuestAsync(factory.Services, dmUser.Id, title: "Dragon Quest");

        var response = await dmClient.GetAsync(
            $"/Quest/LaunchOmphalos/{quest.Id}", TestContext.Current.CancellationToken);

        var location = response.Headers.Location!.ToString();
        var uri = new Uri(location);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        query["questId"].Should().Be(quest.Id.ToString());
        query["sig"].Should().MatchRegex("^[0-9a-f]{64}$");
        var expiry = long.Parse(query["expiry"]!);
        expiry.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        // Username is the DM's Name normalized to lowercase (not email)
        query["username"].Should().NotBeNullOrEmpty();
        query["username"]!.Should().Be(query["username"]!.ToLower());
    }
}
