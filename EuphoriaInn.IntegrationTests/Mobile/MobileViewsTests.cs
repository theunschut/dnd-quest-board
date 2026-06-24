using System.Net;
using EuphoriaInn.IntegrationTests.Helpers;

namespace EuphoriaInn.IntegrationTests.Mobile;

/// <summary>
/// Integration test stubs for Phase 13 requirements HOME-01 through QVIEW-03.
/// Tests start RED (mobile views do not exist yet) and go GREEN as Wave 1 plans land.
/// This establishes the Nyquist sampling harness before any implementation.
/// </summary>
public class MobileViewsTests : IClassFixture<WebApplicationFactoryBase>
{
    private const string MobileUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    private const string DesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public MobileViewsTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(HttpResponseMessage Response, string Html)> GetWithUserAgentAsync(string url, string userAgent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response, html);
    }

    /// <summary>
    /// HOME-01: Mobile UA renders the quest-card-mobile list layout instead of poster images.
    /// </summary>
    [Fact]
    public async Task MobileHome_MobileUserAgent_RendersCardListNotPosterImages()
    {
        // Seed a quest so the card list renders (not the empty state)
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_home01", "dm_home01@test.com", name: "DM Home01");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Open Quest Home01");

        var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("quest-card-mobile");
        html.Should().NotContain("fantasy-quest-card");
        html.Should().NotContain("Blanks w Shadow");
    }

    /// <summary>
    /// HOME-01b: Desktop UA does NOT get the mobile card layout.
    /// </summary>
    [Fact]
    public async Task MobileHome_DesktopUserAgent_DoesNotRenderMobileCardList()
    {
        var (response, html) = await GetWithUserAgentAsync("/", DesktopUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("quest-card-mobile");
    }

    /// <summary>
    /// HOME-02: Quest card shows CR badge and status badge.
    /// </summary>
    [Fact]
    public async Task MobileHome_MobileUserAgent_QuestCardContainsCrAndStatusBadge()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_home02", "dm_home02@test.com", name: "DM Home02");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "The Lost Mine", challengeRating: 3);

        var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("The Lost Mine");
        html.Should().Contain("CR 3");
        html.Should().ContainAny("bg-success", "bg-primary", "bg-secondary");
    }

    /// <summary>
    /// HOME-02b: Finalized quest shows the primary badge (date confirmed, future date).
    /// Note: repository filters out finalized quests with null or past FinalizedDate;
    /// a future FinalizedDate is required for the quest to appear on the board.
    /// </summary>
    [Fact]
    public async Task MobileHome_MobileUserAgent_FinalizedQuestShowsDate()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_home02b", "dm_home02b@test.com", name: "DM Home02b");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Finalized Adventure",
            isFinalized: true, finalizedDate: DateTime.UtcNow.AddDays(7));

        var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Finalized Adventure");
        html.Should().Contain("bg-primary");
    }

    /// <summary>
    /// HOME-03: Quest card links to /Quest/Details/{id} (non-DM user navigates to details).
    /// </summary>
    [Fact]
    public async Task MobileHome_MobileUserAgent_QuestCardLinksToDetails()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_home03", "dm_home03@test.com", name: "DM Home03");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Navigation Quest");

        var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain($"/Quest/Details/{quest.Id}");
    }

    /// <summary>
    /// HOME-04: Signed-up badge appears for authenticated player who has signed up for a quest.
    /// </summary>
    [Fact]
    public async Task MobileHome_AuthenticatedSignedUpPlayer_ShowsSignedUpBadge()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_home04", "dm_home04@test.com", name: "DM Home04");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Signup Badge Quest");
        var (authClient, playerUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory, "player_home04", "player_home04@test.com");
        await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, playerUser.Id, isSelected: false);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Signed up");
    }

    /// <summary>
    /// QVIEW-01: Vote buttons (Yes/No/Maybe) are present on Quest Details when viewed on mobile.
    /// Also checks that quests.mobile.css is linked.
    /// </summary>
    [Fact]
    public async Task MobileQuestDetails_MobileUserAgent_RendersVoteButtons()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_qview01", "dm_qview01@test.com", name: "DM Qview01");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Vote Quest");
        var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory, "player_qview01", "player_qview01@test.com");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Details/{quest.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("changeVoteToYes");
        html.Should().Contain("changeVoteToNo");
        html.Should().Contain("changeVoteToMaybe");
        html.Should().Contain("quests.mobile.css");
    }

    /// <summary>
    /// QVIEW-02: Participant list is rendered as stacked rows instead of a responsive table.
    /// </summary>
    [Fact]
    public async Task MobileQuestDetails_MobileUserAgent_ParticipantListIsStacked()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_qview02", "dm_qview02@test.com", name: "DM Qview02");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Stacked Quest", isFinalized: true);
        var player = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "player_qview02a", "player_qview02a@test.com", name: "Player Alpha");
        await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, player.Id, isSelected: true);
        var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory, "player_qview02b", "player_qview02b@test.com");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Details/{quest.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("participant-row");
        html.Should().NotContain("table-responsive");
        html.Should().Contain("Player Alpha");
    }

    /// <summary>
    /// QVIEW-03: Quest Log mobile view renders a list with quest title and DM name.
    /// </summary>
    [Fact]
    public async Task MobileQuestLog_MobileUserAgent_RendersListWithTitleAndDmName()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_qview03", "dm_qview03@test.com", name: "DM Qview03");
        await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Ancient Dungeon", isFinalized: true, finalizedDate: DateTime.UtcNow.AddDays(-2));

        var (response, html) = await GetWithUserAgentAsync("/QuestLog", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("quest-log-item");
        html.Should().Contain("Ancient Dungeon");
        html.Should().Contain("DM Qview03");
    }

    /// <summary>
    /// QVIEW-03b: Mobile Quest Log page includes a link to quest-log.mobile.css.
    /// </summary>
    [Fact]
    public async Task MobileQuestLog_MobileUserAgent_LoadsMobileCssLink()
    {
        var (response, html) = await GetWithUserAgentAsync("/QuestLog", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("quest-log.mobile.css");
    }
}
