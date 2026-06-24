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
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, DateTime.UtcNow.AddDays(7));
        var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory, "player_qview01", "player_qview01@test.com");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Details/{quest.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("btn-check");
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

    /// <summary>
    /// CAL-CSS: calendar.mobile.css is linked from /Calendar on mobile.
    /// </summary>
    [Fact]
    public async Task MobileCalendar_MobileUserAgent_LoadsMobileCssLink()
    {
        var (response, html) = await GetWithUserAgentAsync("/Calendar", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("calendar.mobile.css");
    }

    /// <summary>
    /// CAL-01: Mobile UA on /Calendar renders agenda list (contains agenda-quest-entry, no calendar-grid).
    /// </summary>
    [Fact]
    public async Task MobileCalendar_MobileUserAgent_RendersAgendaList()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_cal01", "dm_cal01@test.com", name: "DM Cal01");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Calendar Quest CAL01");
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id,
            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 19, 0, 0, DateTimeKind.Utc));

        var (response, html) = await GetWithUserAgentAsync(
            $"/Calendar?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("agenda-quest-entry");
        html.Should().NotContain("calendar-grid");
    }

    /// <summary>
    /// CAL-02: Agenda entry contains day label in uppercase day-name format and time.
    /// </summary>
    [Fact]
    public async Task MobileCalendar_MobileUserAgent_AgendaEntryContainsDayLabelAndTime()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_cal02", "dm_cal02@test.com", name: "DM Cal02");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Calendar Quest CAL02");
        var knownDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 19, 0, 0, DateTimeKind.Utc);
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, knownDate);

        var (response, html) = await GetWithUserAgentAsync(
            $"/Calendar?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("agenda-day-label");
        // Day label format is "SATURDAY, JUNE 14" — assert at least the time portion is present
        html.Should().Contain("19:00");
    }

    /// <summary>
    /// CAL-03: Desktop UA on /Calendar does NOT render agenda list.
    /// </summary>
    [Fact]
    public async Task MobileCalendar_DesktopUserAgent_DoesNotRenderAgendaList()
    {
        var (response, html) = await GetWithUserAgentAsync("/Calendar", DesktopUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("agenda-quest-entry");
    }

    /// <summary>
    /// CAL-04: Agenda entry links to /Quest/Details/{id}.
    /// </summary>
    [Fact]
    public async Task MobileCalendar_MobileUserAgent_AgendaEntryLinksToDetails()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_cal04", "dm_cal04@test.com", name: "DM Cal04");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Calendar Quest CAL04");
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, DateTime.UtcNow.AddDays(3));

        var (response, html) = await GetWithUserAgentAsync(
            $"/Calendar?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}", MobileUserAgent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain($"/Quest/Details/{quest.Id}");
    }

    /// <summary>
    /// CAL-05: _Calendar.Mobile.cshtml partial renders per-date vote buttons on Quest Details mobile.
    /// Authenticated player not yet signed up — should see btn-check radio inputs.
    /// </summary>
    [Fact]
    public async Task MobileCalendar_MobileUserAgent_CalendarPartialRendersVoteButtons()
    {
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_cal05", "dm_cal05@test.com", name: "DM Cal05");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Calendar Quest CAL05");
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, DateTime.UtcNow.AddDays(5));
        var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "player_cal05", "player_cal05@test.com");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Details/{quest.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("btn-check");
        html.Should().Contain("calendar-date-entry-mobile");
    }

    /// <summary>
    /// DMVIEW-01: Quest Create on mobile renders single-column glass card form (dm-create-card-mobile).
    /// Also checks that dm-create.mobile.css is linked.
    /// </summary>
    [Fact]
    public async Task MobileDmCreate_MobileUserAgent_RendersGlassCardForm()
    {
        var (authClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "dm_dmview01", "dm_dmview01@test.com", roles: new[] { "DungeonMaster" });

        var request = new HttpRequestMessage(HttpMethod.Get, "/Quest/Create");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("dm-create-card-mobile");
        html.Should().Contain("dm-create.mobile.css");
    }

    /// <summary>
    /// DMVIEW-02: Quest Manage on mobile renders condensed vote badges (manage-date-option, dm-vote-summary).
    /// Also checks that dm-manage.mobile.css is linked.
    /// </summary>
    [Fact]
    public async Task MobileDmManage_MobileUserAgent_RendersCondensedVoteBadges()
    {
        var (authClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "dm_dmview02", "dm_dmview02@test.com", roles: new[] { "DungeonMaster" });
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dmUser.Id, "Manage Mobile Quest DM02");
        await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, DateTime.UtcNow.AddDays(7));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Manage/{quest.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("manage-date-option");
        html.Should().Contain("dm-vote-summary");
        html.Should().Contain("dm-manage.mobile.css");
    }

    /// <summary>
    /// DMVIEW-03: DM Profile on mobile renders glass card layout (dm-profile-header-card).
    /// Also checks that dm-profile.mobile.css is linked.
    /// </summary>
    [Fact]
    public async Task MobileDmProfile_MobileUserAgent_RendersGlassCardLayout()
    {
        var (authClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "dm_dmview03", "dm_dmview03@test.com", roles: new[] { "DungeonMaster" });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/DungeonMaster/Profile/{dmUser.Id}");
        request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
        request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("dm-profile-header-card");
        html.Should().Contain("dm-profile.mobile.css");
    }
}
