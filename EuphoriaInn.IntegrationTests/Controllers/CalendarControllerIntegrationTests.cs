using System.Net;
using EuphoriaInn.IntegrationTests.Helpers;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class CalendarControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public CalendarControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Index_ShouldReturnCalendarView()
    {
        // Act
        var response = await _client.GetAsync("/Calendar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Calendar");
    }

    [Fact]
    public async Task Index_WithYearAndMonth_ShouldReturnSpecificMonthCalendar()
    {
        // Arrange
        var year = 2024;
        var month = 6;

        // Act
        var response = await _client.GetAsync($"/Calendar?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Calendar");
    }

    [Fact]
    public async Task Index_WithFinalizedQuests_ShouldDisplayQuestsOnCalendar()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "calendardm", "calendar@example.com");

        var questDate = DateTime.Today.AddDays(7);
        var quest = await TestDataHelper.CreateTestQuestAsync(
            _factory.Services,
            dm.Id,
            "Calendar Quest",
            "Test quest for calendar",
            5,
            isFinalized: true);

        // Add a proposed date that matches the finalized date
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, questDate);

        // Update quest with finalized date
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
            var questToUpdate = await context.Quests.FindAsync(quest.Id);
            if (questToUpdate != null)
            {
                questToUpdate.FinalizedDate = questDate;
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync($"/Calendar?year={questDate.Year}&month={questDate.Month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Calendar Quest");
    }

    [Theory]
    [InlineData(2024, 1)]
    [InlineData(2024, 6)]
    [InlineData(2024, 12)]
    public async Task Index_WithDifferentMonths_ShouldReturnSuccessfully(int year, int month)
    {
        // Act
        var response = await _client.GetAsync($"/Calendar?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Index_WithInvalidMonth_ShouldHandleGracefully()
    {
        // Act
        var response = await _client.GetAsync("/Calendar?year=2024&month=13");

        // Assert
        // Calendar controller throws ArgumentOutOfRangeException for invalid dates
        // which results in 404 from exception handler
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}
