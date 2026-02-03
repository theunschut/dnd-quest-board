using System.Net;
using EuphoriaInn.IntegrationTests.Helpers;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class HomeControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactoryBase _factory;

    public HomeControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Index_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Index_ShouldReturnHtmlContent()
    {
        // Act
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Index_WithQuests_ShouldDisplayQuestList()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "homedm", "home@example.com");

        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Home Quest 1");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Home Quest 2");

        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Home Quest 1");
        content.Should().Contain("Home Quest 2");
    }

    // REMOVED: Privacy and Error tests - these routes don't exist in HomeController
    // HomeController only has an Index() action
    // If these routes are needed in the future, add Privacy() and Error() actions to HomeController

    [Fact]
    public async Task NonExistentRoute_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/NonExistent/Route");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
