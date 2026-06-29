using QuestBoard.IntegrationTests.Helpers;
using System.Net;

namespace QuestBoard.IntegrationTests.Controllers;

public class DungeonMasterControllerIntegrationTests(WebApplicationFactoryBase factory) : IClassFixture<WebApplicationFactoryBase>
{
    // DMPRO-01: Profile page returns 200 for a valid DM user id
    [Fact]
    public async Task Profile_WithValidDmUserId_ReturnsOk()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, user) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, roles: ["DungeonMaster"]);

        var response = await client.GetAsync($"/DungeonMaster/Profile/{user.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain(user.Name);
    }

    // DMPRO-01: Profile page renders placeholder state when DM has no saved profile yet (no 404)
    [Fact]
    public async Task Profile_WithNoSavedProfile_RendersPlaceholderNotNotFound()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, user) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, roles: ["DungeonMaster"]);

        // DM has never saved a profile — profile row does not exist in DB
        var response = await client.GetAsync($"/DungeonMaster/Profile/{user.Id}", TestContext.Current.CancellationToken);

        // Must NOT return 404 — D-03 requires graceful null handling
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain("No bio provided yet.");
    }

    // DMPRO-01: Profile page returns 404 for a non-existent user id
    [Fact]
    public async Task Profile_WithNonExistentUserId_ReturnsNotFound()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(factory);

        var response = await client.GetAsync("/DungeonMaster/Profile/999999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DMPRO-02: DM can GET EditProfile for their own profile without being redirected or forbidden
    [Fact]
    public async Task EditProfile_OwnProfile_ReturnsOk()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var (client, user) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, roles: ["DungeonMaster"]);

        var response = await client.GetAsync($"/DungeonMaster/EditProfile/{user.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain("Edit DM Profile");
    }

    // DMPRO-03: Admin can GET EditProfile for another DM's profile
    [Fact]
    public async Task EditProfile_AdminEditingOtherDm_ReturnsOk()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var targetDm = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "targetdm", "targetdm@example.com");

        var (adminClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, roles: ["Admin"]);

        var response = await adminClient.GetAsync($"/DungeonMaster/EditProfile/{targetDm.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // DMPRO-03: Non-admin DM gets 403 when trying to edit another DM's profile
    [Fact]
    public async Task EditProfile_NonAdminDmEditingOtherDm_ReturnsForbidden()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);
        var otherDm = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "otherdm", "otherdm@example.com");

        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            factory, roles: ["DungeonMaster"]);

        var response = await client.GetAsync($"/DungeonMaster/EditProfile/{otherDm.Id}", TestContext.Current.CancellationToken);

        // Forbid() with Identity.Application scheme redirects to /Account/AccessDenied (302)
        // This is the standard project behavior — mirrors QuestControllerIntegrationTests pattern
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }
}
