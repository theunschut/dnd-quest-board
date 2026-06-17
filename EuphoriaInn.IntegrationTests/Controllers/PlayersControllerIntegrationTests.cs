using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class PlayersControllerIntegrationTests(WebApplicationFactoryBase factory) : IClassFixture<WebApplicationFactoryBase>
{
    // DMPRO-04: DM directory page links to each DM's profile at /DungeonMaster/Profile/{id}
    [Fact]
    public async Task Index_DmDirectory_ContainsProfileLinkForEachDm()
    {
        await TestDataHelper.ClearDatabaseAsync(factory.Services);

        var dmUser = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "directorydm", "directorydm@example.com",
            name: "Directory DM");

        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(factory);

        var response = await client.GetAsync("/Players", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        // D-10: DM name in directory must be wrapped in a link to their profile
        content.Should().Contain($"/DungeonMaster/Profile/{dmUser.Id}");
        content.Should().Contain("Directory DM");
    }
}
