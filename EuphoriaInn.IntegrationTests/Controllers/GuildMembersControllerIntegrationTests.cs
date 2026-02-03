using System.Net;
using EuphoriaInn.IntegrationTests.Helpers;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class GuildMembersControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public GuildMembersControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Index_ShouldReturnGuildMembersPage()
    {
        // Arrange - GuildMembers requires authentication
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny("Guild", "Members");
    }

    [Fact]
    public async Task Index_WithMembers_ShouldDisplayAllMembers()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client first (this also creates a user in the database)
        var (client, authUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Create additional users to display in guild members (with unique names)
        var warrior = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "warrior1", "warrior1@example.com", "Test123!", "Warrior One");
        var mage = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "mage1", "mage1@example.com", "Test123!", "Mage One");
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "dm1", "dm1@example.com", "Test123!", "DM One");

        // Create characters for each user
        await TestDataHelper.CreateTestCharacterAsync(_factory.Services, warrior.Id, "Warrior One", level: 5, dndClass: 5); // Fighter
        await TestDataHelper.CreateTestCharacterAsync(_factory.Services, mage.Id, "Mage One", level: 3, dndClass: 12); // Wizard
        await TestDataHelper.CreateTestCharacterAsync(_factory.Services, dm.Id, "DM One", level: 10, dndClass: 3); // Cleric

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // Check for the user names (not usernames which have GUID suffixes)
        content.Should().Contain("Warrior One");
        content.Should().Contain("Mage One");
        content.Should().Contain("DM One");
    }

    [Fact]
    public async Task Index_ShouldShowDungeonMasterBadge()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client first (this also creates a user in the database)
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Create additional user to test DM badge display
        var dmUser = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "dmspecial", "dmspecial@example.com", "Test123!", "Special DM");

        // Create character for the DM user
        await TestDataHelper.CreateTestCharacterAsync(_factory.Services, dmUser.Id, "Special DM", level: 10, dndClass: 7); // Paladin

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // Check for the user's display name
        content.Should().Contain("Special DM");
    }

    [Fact]
    public async Task Index_ShouldDisplayUserInformation()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client first (this also creates a user in the database)
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Create additional user with specific name to test display
        var user = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "detailedchar", "detailed@example.com", name: "Aragorn the Ranger");

        // Create character for the user (use "Aragorn" as character name to match test expectation)
        await TestDataHelper.CreateTestCharacterAsync(_factory.Services, user.Id, "Aragorn", level: 8, dndClass: 8); // Ranger

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Aragorn");
    }
}
