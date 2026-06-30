using System.Net;
using QuestBoard.IntegrationTests.Helpers;

namespace QuestBoard.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for the /platform MVC area authorization.
/// Covers AUTH-05 (SuperAdminOnly policy on platform area) and MGMT-01 (platform area accessible to SuperAdmin).
/// </summary>
public class PlatformAreaIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;

    public PlatformAreaIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
    }

    // AUTH-05 + MGMT-01: SuperAdmin can access the platform area group index
    [Fact]
    public async Task PlatformIndex_WhenSuperAdmin_ShouldReturn200()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedSuperAdminClientAsync(_factory);

        var response = await client.GetAsync("/platform/Group/Index", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AUTH-05: Non-SuperAdmin (regular player) receives 403 or redirect on /platform/*
    [Fact]
    public async Task PlatformIndex_WhenNotSuperAdmin_ShouldDeny()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "regularuser", "regular@test.com", roles: ["Player"]);

        var response = await client.GetAsync("/platform/Group/Index", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // AUTH-05: Unauthenticated users are redirected from /platform/*
    [Fact]
    public async Task PlatformIndex_WhenNotAuthenticated_ShouldRedirect()
    {
        var unauthClient = _factory.CreateNonRedirectingClient();

        var response = await unauthClient.GetAsync("/platform/Group/Index", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    // AUTH-05: Admin (group-scoped) does not get access to platform area
    [Fact]
    public async Task PlatformIndex_WhenAdmin_ShouldDeny()
    {
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "adminuser", "admin@test.com", roles: ["Admin"]);

        var response = await client.GetAsync("/platform/Group/Index", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }
}
