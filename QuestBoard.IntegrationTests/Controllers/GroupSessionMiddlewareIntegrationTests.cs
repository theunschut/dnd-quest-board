using QuestBoard.IntegrationTests.Helpers;
using System.Net;

namespace QuestBoard.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for GroupSessionMiddleware (D-09, D-10, D-11): redirects an authenticated
/// user whose group session has expired (no ActiveGroupId) to the group picker, exempts
/// SuperAdmin and the picker/auth/platform/error paths from the redirect, and passes requests
/// through untouched when an active group is present.
///
/// TestGroupContext (MutableGroupContext) is a shared singleton registered on the factory, so
/// each test explicitly sets factory.TestGroupContext.ActiveGroupId at the start and restores
/// it to 1 in a finally block to avoid cross-test bleed (mirrors TenantIsolationTests convention).
/// </summary>
public class GroupSessionMiddlewareIntegrationTests(WebApplicationFactoryBase factory) : IClassFixture<WebApplicationFactoryBase>
{
    // D-09: an authenticated non-SuperAdmin user with no active group session is redirected
    // by the middleware to the hardcoded /groups/pick path before reaching the controller.
    [Fact]
    public async Task AuthenticatedUser_NoActiveGroup_RedirectsToGroupPick()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, "sessionrecoveryuser", "sessionrecovery@example.com", roles: ["Player"]);

        factory.TestGroupContext.ActiveGroupId = null;
        try
        {
            var response = await client.GetAsync("/quests", TestContext.Current.CancellationToken);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            location.Should().Contain("/groups/pick");
        }
        finally
        {
            factory.TestGroupContext.ActiveGroupId = 1;
        }
    }

    // D-09/D-11: SuperAdmin has no group context by design (system-wide access) — the
    // middleware's role check must short-circuit BEFORE the null-group check so a SuperAdmin
    // is never caught by the group-session redirect (avoids a redirect loop for that role).
    [Fact]
    public async Task SuperAdmin_NoActiveGroup_NotRedirectedByMiddleware()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedSuperAdminClientAsync(factory);

        factory.TestGroupContext.ActiveGroupId = null;
        try
        {
            var response = await client.GetAsync("/quests", TestContext.Current.CancellationToken);

            // The key assertion: the middleware must not redirect SuperAdmin to /groups/pick.
            // The request may still 200 (handled by [Authorize]) or redirect elsewhere entirely,
            // but it must never be routed to the group picker by this middleware.
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            location.Should().NotContain("/groups/pick");
        }
        finally
        {
            factory.TestGroupContext.ActiveGroupId = 1;
        }
    }

    // D-10: /groups/pick itself is on the exempt-path list — a user with no active group
    // hitting the picker must never be looped back to the picker by the middleware. The
    // picker controller's own single-group auto-redirect logic (UX-01) sends them onward to
    // the board instead, which is a different mechanism from the middleware under test here.
    [Fact]
    public async Task GroupPickPath_NoActiveGroup_NotLooped()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, "grouppickpathuser", "grouppickpath@example.com", roles: ["Player"]);

        factory.TestGroupContext.ActiveGroupId = null;
        try
        {
            var response = await client.GetAsync("/groups/pick", TestContext.Current.CancellationToken);

            // Never looped back to /groups/pick — either 200 (the picker page itself) or a
            // redirect onward to the board for a single-group user (UX-01), never a redirect
            // whose Location is /groups/pick again.
            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString() ?? string.Empty;
                location.Should().NotContain("/groups/pick");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
        finally
        {
            factory.TestGroupContext.ActiveGroupId = 1;
        }
    }

    // D-09 negative: with an active group present, the middleware passes the request through
    // untouched and the authenticated user reaches the board normally.
    [Fact]
    public async Task AuthenticatedUser_WithActiveGroup_ReachesPage()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, "activegroupuser", "activegroup@example.com", roles: ["Player"]);

        factory.TestGroupContext.ActiveGroupId = 1;
        try
        {
            var response = await client.GetAsync("/quests", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            factory.TestGroupContext.ActiveGroupId = 1;
        }
    }
}
