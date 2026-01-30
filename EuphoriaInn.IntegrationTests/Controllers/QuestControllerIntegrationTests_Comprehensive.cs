using System.Net;
using EuphoriaInn.IntegrationTests.Helpers;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class QuestControllerIntegrationTests_Comprehensive : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public QuestControllerIntegrationTests_Comprehensive(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Create_Get_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Quest/Create");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Get_WhenAuthenticatedAsDM_ShouldReturnCreateForm()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory);

        // Act
        var response = await client.GetAsync("/Quest/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Create");
        content.Should().Contain("Quest");
    }

    [Fact]
    public async Task Details_WithValidQuestId_ShouldReturnQuestDetails()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "questdm", "questdm@example.com");
        var quest = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Adventure Quest", "Epic adventure");

        // Act
        var response = await _client.GetAsync($"/Quest/Details/{quest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Adventure Quest");
        content.Should().Contain("Epic adventure");
    }

    [Fact]
    public async Task Details_WithInvalidQuestId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/Quest/Details/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Edit_Get_WhenNotQuestOwner_ShouldReturnForbidden()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (_, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory, "originaldm", "originaldm@example.com");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id);

        var (otherClient, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory, "otherdm", "otherdm@example.com");

        // Act
        var response = await otherClient.GetAsync($"/Quest/Edit/{quest.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MyQuests_WhenAuthenticatedAsDM_ShouldReturnDMQuests()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory);

        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "My Quest 1");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "My Quest 2");

        // Act
        var response = await client.GetAsync("/Quest/MyQuests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("My Quest 1");
        content.Should().Contain("My Quest 2");
    }

    [Fact]
    public async Task Signup_Post_WhenAuthenticated_ShouldAddPlayerToQuest()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "signupdm", "signupdm@example.com");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id);

        var (playerClient, player) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "player1", "player1@example.com");

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["questId"] = quest.Id.ToString(),
            ["signupRole"] = "0" // Player role
        });

        // Act
        var response = await playerClient.PostAsync($"/Quest/Signup", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Verify signup was created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        var signup = await context.PlayerSignups
            .FirstOrDefaultAsync(s => s.QuestId == quest.Id && s.PlayerId == player.Id);
        signup.Should().NotBeNull();
    }

    [Fact]
    public async Task Manage_Get_WhenQuestOwner_ShouldReturnManagementPage()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (dmClient, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory);
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id);

        // Add some signups
        var player = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "manageplayer", "manageplayer@example.com");
        await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, player.Id);

        // Act
        var response = await dmClient.GetAsync($"/Quest/Manage/{quest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Manage");
    }

    [Fact]
    public async Task Finalize_Post_WhenQuestOwner_ShouldFinalizeQuest()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (dmClient, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory);
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id);

        var player1 = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "finalizeplayer1", "fp1@example.com");
        var player2 = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "finalizeplayer2", "fp2@example.com");

        var signup1 = await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, player1.Id);
        var signup2 = await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, player2.Id);

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["questId"] = quest.Id.ToString(),
            ["finalizedDate"] = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
            ["selectedPlayerIds"] = $"{signup1.Id},{signup2.Id}"
        });

        // Act
        var response = await dmClient.PostAsync("/Quest/Finalize", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Verify quest was finalized
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        var finalizedQuest = await context.Quests.FindAsync(quest.Id);
        finalizedQuest.Should().NotBeNull();
        finalizedQuest!.IsFinalized.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Post_WhenNotQuestOwner_ShouldReturnForbidden()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (_, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory, "deletedm", "delete@example.com");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id);

        var (otherClient, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory, "otherdeletem", "otherdelete@example.com");

        // Act
        var response = await otherClient.PostAsync($"/Quest/Delete/{quest.Id}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }
}
