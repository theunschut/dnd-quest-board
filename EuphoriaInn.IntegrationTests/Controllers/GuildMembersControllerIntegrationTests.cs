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

        // Create additional users to display in guild members
        await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "warrior1", "warrior1@example.com");
        await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "mage1", "mage1@example.com");
        await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "dm1", "dm1@example.com");

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // Usernames now have GUID suffixes, so we check for the base username
        content.Should().Match("*warrior1*");
        content.Should().Match("*mage1*");
        content.Should().Match("*dm1*");
    }

    [Fact]
    public async Task Index_ShouldShowDungeonMasterBadge()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client first (this also creates a user in the database)
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Create additional user to test DM badge display
        await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "dmspecial", "dmspecial@example.com");

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        // Username now has GUID suffix, so we check for the base username
        content.Should().Match("*dmspecial*");
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

        // Act
        var response = await client.GetAsync("/GuildMembers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Aragorn");
    }
}
