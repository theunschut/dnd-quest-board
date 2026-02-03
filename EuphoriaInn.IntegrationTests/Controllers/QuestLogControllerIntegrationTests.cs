using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class QuestLogControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public QuestLogControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Index_ShouldReturnQuestLogPage()
    {
        // Act
        var response = await _client.GetAsync("/QuestLog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny("Quest", "Log");
    }

    [Fact]
    public async Task Index_WithCompletedQuests_ShouldDisplayQuests()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "logdm", "log@example.com");

        var quest1 = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Completed Quest 1", "Description 1", 5, isFinalized: true);
        var quest2 = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Completed Quest 2", "Description 2", 8, isFinalized: true);

        // Set finalized dates
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
            var q1 = await context.Quests.FindAsync(quest1.Id);
            var q2 = await context.Quests.FindAsync(quest2.Id);
            if (q1 != null) q1.FinalizedDate = DateTime.Today.AddDays(-7);
            if (q2 != null) q2.FinalizedDate = DateTime.Today.AddDays(-14);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/QuestLog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Completed Quest 1");
        content.Should().Contain("Completed Quest 2");
    }

    [Fact]
    public async Task Details_WithValidQuestId_ShouldReturnQuestDetails()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "detailslogdm", "detailslog@example.com");

        var quest = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Quest With Details", "Detailed description", 10, isFinalized: true);

        // Set finalized date to at least 1 day in the past
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
            var questToUpdate = await context.Quests.FindAsync(quest.Id);
            if (questToUpdate != null)
            {
                questToUpdate.FinalizedDate = DateTime.UtcNow.AddDays(-2);
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync($"/QuestLog/Details/{quest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Quest With Details");
        content.Should().Contain("Detailed description");
    }

    [Fact]
    public async Task Details_WithInvalidQuestId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/QuestLog/Details/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Index_ShouldOnlyShowFinalizedQuests()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "filterlogdm", "filterlog@example.com");

        var finalizedQuest = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Finalized Quest", "Done", 5, isFinalized: true);
        var activeQuest = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services, dm.Id, "Active Quest", "Not done", 5, isFinalized: false);

        // Set finalized date for the finalized quest
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
            var questToUpdate = await context.Quests.FindAsync(finalizedQuest.Id);
            if (questToUpdate != null)
            {
                questToUpdate.FinalizedDate = DateTime.UtcNow.AddDays(-2);
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync("/QuestLog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Finalized Quest");
        content.Should().NotContain("Active Quest");
    }
}
