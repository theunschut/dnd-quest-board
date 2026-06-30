using System.Net;
using QuestBoard.IntegrationTests.Helpers;

namespace QuestBoard.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for AdminHandler and DungeonMasterHandler authorization logic.
/// Covers AUTH-02 (AdminOnly policy), AUTH-03 (DungeonMasterOnly policy), AUTH-04 (SuperAdmin bypass).
/// </summary>
public class AdminHandlerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;

    public AdminHandlerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
    }

    // AUTH-02: AdminOnly allows GroupRole.Admin
    [Fact]
    public async Task AdminOnlyPage_WhenUserHasAdminGroupRole_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "adminuser", "admin@test.com", roles: ["Admin"]);

        var response = await client.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-02: AdminOnly denies GroupRole.Player
    [Fact]
    public async Task AdminOnlyPage_WhenUserHasPlayerGroupRole_ShouldDeny()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "playeruser", "player@test.com", roles: ["Player"]);

        var response = await client.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // AUTH-03: DungeonMasterOnly allows GroupRole.DungeonMaster
    [Fact]
    public async Task DungeonMasterOnlyPage_WhenUserHasDMGroupRole_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "dmuser", "dm@test.com", roles: ["DungeonMaster"]);

        var response = await client.GetAsync("/DungeonMaster/EditProfile", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-03: DungeonMasterOnly allows GroupRole.Admin
    [Fact]
    public async Task DungeonMasterOnlyPage_WhenUserHasAdminGroupRole_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "adminfordm", "adminfordm@test.com", roles: ["Admin"]);

        var response = await client.GetAsync("/DungeonMaster/EditProfile", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-03: DungeonMasterOnly denies GroupRole.Player
    [Fact]
    public async Task DungeonMasterOnlyPage_WhenUserHasPlayerGroupRole_ShouldDeny()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "playerformdm", "playerformd@test.com", roles: ["Player"]);

        var response = await client.GetAsync("/DungeonMaster/EditProfile", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // AUTH-04: SuperAdmin bypasses AdminOnly
    [Fact]
    public async Task AdminOnlyPage_WhenSuperAdmin_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedSuperAdminClientAsync(_factory);

        var response = await client.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-04: SuperAdmin bypasses DungeonMasterOnly
    [Fact]
    public async Task DungeonMasterOnlyPage_WhenSuperAdmin_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedSuperAdminClientAsync(_factory);

        var response = await client.GetAsync("/DungeonMaster/EditProfile", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-02: AdminOnly denies unauthenticated users
    [Fact]
    public async Task AdminOnlyPage_WhenNotAuthenticated_ShouldRedirect()
    {
        var unauthClient = _factory.CreateNonRedirectingClient();

        var response = await unauthClient.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }
}
