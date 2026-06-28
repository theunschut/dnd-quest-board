# Phase 14: Calendar — Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 7
**Analogs found:** 7 / 7

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Views/Calendar/Index.Mobile.cshtml` | view (page) | request-response | `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml` + `Views/Calendar/Index.cshtml` | exact (combined) |
| `EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml` | view (partial) | request-response | `EuphoriaInn.Service/Views/Shared/_Calendar.cshtml` | exact |
| `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` | view (page, modify) | request-response | `EuphoriaInn.Service/Views/Quest/Details.cshtml` lines 538–576 | exact |
| `EuphoriaInn.Service/wwwroot/css/calendar.mobile.css` | static asset (CSS) | — | `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` | role-match |
| `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` | static asset (CSS, append) | — | `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` | exact (self-extend) |
| `.planning/REQUIREMENTS.md` | documentation (modify) | — | existing CAL-01…CAL-04 block (lines 32–35) | exact |
| `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` | test (modify) | — | existing methods in the same file | exact |

---

## Pattern Assignments

---

### `EuphoriaInn.Service/Views/Calendar/Index.Mobile.cshtml` (view, request-response)

**Primary analog:** `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml`
**Secondary analog:** `EuphoriaInn.Service/Views/Calendar/Index.cshtml`

**Imports / model declaration** (from `Index.cshtml` lines 1–6; adapt for mobile):
```cshtml
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.CalendarViewModels
@model CalendarViewModel
@{
    ViewData["Title"] = "Quest Calendar";
}
```

**Per-page CSS section** (from `Index.Mobile.cshtml` lines 10–12 — exact pattern):
```cshtml
@section Styles {
    <link href="~/css/calendar.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Month navigation pattern** (from `Index.cshtml` lines 17–25 — copy URL pattern, replace desktop card wrapper with mobile `d-flex`):
```cshtml
<div class="d-flex justify-content-between align-items-center mb-3">
    <a href="@Url.Action("Index", new { year = Model.FirstDayOfMonth.AddMonths(-1).Year, month = Model.FirstDayOfMonth.AddMonths(-1).Month })"
       class="btn btn-primary calendar-nav-btn" aria-label="Previous month">
        <i class="fas fa-chevron-left"></i>
    </a>
    <span class="fw-bold cinzel-heading calendar-month-title">@Model.MonthName</span>
    <a href="@Url.Action("Index", new { year = Model.FirstDayOfMonth.AddMonths(1).Year, month = Model.FirstDayOfMonth.AddMonths(1).Month })"
       class="btn btn-primary calendar-nav-btn" aria-label="Next month">
        <i class="fas fa-chevron-right"></i>
    </a>
</div>
```

**Agenda list rendering pattern** (from `RESEARCH.md` Code Examples — `CalendarViewModel.GetCalendarDays()` filter):
```cshtml
@{
    ViewBag.IsDetailsPage = false;
}
@{
    var agendaDays = Model.GetCalendarDays().Where(d => !d.IsEmpty && d.QuestsOnDay.Any()).ToList();
}
@if (!agendaDays.Any())
{
    <div class="text-center text-muted py-4">
        <i class="fas fa-calendar-times fa-2x mb-2"></i>
        <p>No quests this month.</p>
    </div>
}
else
{
    @foreach (var day in agendaDays)
    {
        <div class="agenda-day-section mb-2">
            <div class="agenda-day-label text-uppercase fw-bold small mb-1">
                @day.Date.ToString("dddd, MMMM d").ToUpper()
            </div>
            @foreach (var questOnDay in day.QuestsOnDay)
            {
                <div class="agenda-quest-entry @(questOnDay.IsFinalized ? "agenda-quest-finalized" : "agenda-quest-proposed")"
                     onclick="window.location.href='@Url.Action("Details", "Quest", new { id = questOnDay.Quest.Id })'">
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="agenda-quest-title fw-bold">@questOnDay.Quest.Title</span>
                        <small class="text-muted ms-2 flex-shrink-0">
                            @questOnDay.ProposedDate.Date.ToString("HH:mm")
                        </small>
                    </div>
                </div>
            }
        </div>
    }
}
```

**Tap navigation pattern** (from `Index.Mobile.cshtml` line 59 — onclick href):
```cshtml
onclick="window.location.href='@Url.Action("Details", "Quest", new { id = questOnDay.Quest.Id })'"
```

**Container wrapper** (from `Index.Mobile.cshtml` line 14 — outer div):
```cshtml
<div class="container-fluid px-2 mt-2">
    ...
</div>
```

**No `@section Scripts` needed** — this view has no JavaScript.

---

### `EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml` (partial, request-response)

**Analog:** `EuphoriaInn.Service/Views/Shared/_Calendar.cshtml` (full read — lines 1–209)

**Imports / model declaration** (from `_Calendar.cshtml` lines 1–8 — copy exactly):
```cshtml
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.CalendarViewModels
@model CalendarViewModel
@{
    var isDetailsPage = ViewBag.IsDetailsPage == true;
    var currentQuestId = ViewBag.CurrentQuestId as int?;
}
```

**ViewBag reading pattern — all keys used** (from `_Calendar.cshtml` lines 79–81):
```cshtml
var isPlayerSignedUp = ViewBag.IsPlayerSignedUp;
var updateVoteIndexLookup = ViewData["UpdateVoteIndexLookup"] as Dictionary<int, int>;
var isUpdateMode = updateVoteIndexLookup != null;
```

**Current user vote lookup** (from `_Calendar.cshtml` lines 73–74):
```cshtml
var currentUserId = ViewBag.CurrentUserId;
var userVote = questOnDay.ProposedDate.PlayerVotes?.FirstOrDefault(v => v.PlayerSignup?.Player?.Id == currentUserId)?.Vote;
```

**Day/quest filter for details page** (from `_Calendar.cshtml` line 77 — critical: only render for the current quest):
```cshtml
@if (isDetailsPage && questOnDay.Quest.Id == currentQuestId)
```

**Initial vote flow — radio buttons** (from `_Calendar.cshtml` lines 86–111 — btn-check pattern; adapt to horizontal `d-flex gap-2` for mobile, adding text labels per D-03):
```cshtml
var voteIndexLookup = ViewBag.VoteIndexLookup as Dictionary<int, int>;
var voteIndex = voteIndexLookup?.ContainsKey(questOnDay.ProposedDate.Id) == true
    ? voteIndexLookup[questOnDay.ProposedDate.Id]
    : -1;

@if (voteIndex >= 0)
{
    <div class="d-flex gap-2 calendar-vote-row">
        <input type="radio" class="btn-check" name="DateVotes[@voteIndex].Vote"
               id="vote_@(voteIndex)_yes" value="2" autocomplete="off">
        <label class="btn btn-outline-success flex-fill" for="vote_@(voteIndex)_yes">
            <i class="fas fa-check me-1"></i>Yes
        </label>
        <input type="radio" class="btn-check" name="DateVotes[@voteIndex].Vote"
               id="vote_@(voteIndex)_no" value="0" autocomplete="off">
        <label class="btn btn-outline-danger flex-fill" for="vote_@(voteIndex)_no">
            <i class="fas fa-times me-1"></i>No
        </label>
        <input type="radio" class="btn-check" name="DateVotes[@voteIndex].Vote"
               id="vote_@(voteIndex)_maybe" value="1" autocomplete="off">
        <label class="btn btn-outline-warning flex-fill" for="vote_@(voteIndex)_maybe">
            <i class="fas fa-question me-1"></i>Maybe
        </label>
    </div>
}
```

**VoteType integer values** (from `_Calendar.cshtml` lines 95/100/105 — confirmed):
- `value="2"` = Yes
- `value="0"` = No
- `value="1"` = Maybe

**Update vote flow — radio buttons with pre-checked state** (from `_Calendar.cshtml` lines 113–144 — adapt to horizontal `d-flex gap-2`):
```cshtml
var updateVoteIndex = updateVoteIndexLookup?.ContainsKey(questOnDay.ProposedDate.Id) == true
    ? updateVoteIndexLookup[questOnDay.ProposedDate.Id]
    : -1;

@if (updateVoteIndex >= 0)
{
    var isCheckedYes = userVote.HasValue && userVote == VoteType.Yes;
    var isCheckedNo = userVote.HasValue && userVote == VoteType.No;
    var isCheckedMaybe = userVote.HasValue && userVote == VoteType.Maybe;

    <div class="d-flex gap-2 calendar-vote-row">
        <input type="radio" class="btn-check" name="DateVotes[@updateVoteIndex].Vote"
               id="update_vote_@(updateVoteIndex)_yes" value="2" autocomplete="off" @(isCheckedYes ? "checked" : "")>
        <label class="btn btn-outline-success flex-fill" for="update_vote_@(updateVoteIndex)_yes">
            <i class="fas fa-check me-1"></i>Yes
        </label>
        <input type="radio" class="btn-check" name="DateVotes[@updateVoteIndex].Vote"
               id="update_vote_@(updateVoteIndex)_no" value="0" autocomplete="off" @(isCheckedNo ? "checked" : "")>
        <label class="btn btn-outline-danger flex-fill" for="update_vote_@(updateVoteIndex)_no">
            <i class="fas fa-times me-1"></i>No
        </label>
        <input type="radio" class="btn-check" name="DateVotes[@updateVoteIndex].Vote"
               id="update_vote_@(updateVoteIndex)_maybe" value="1" autocomplete="off" @(isCheckedMaybe ? "checked" : "")>
        <label class="btn btn-outline-warning flex-fill" for="update_vote_@(updateVoteIndex)_maybe">
            <i class="fas fa-question me-1"></i>Maybe
        </label>
    </div>
}
```

**Date label per entry** (from `RESEARCH.md` Code Examples — adapt `_Calendar.cshtml` day rendering to vertical list):
```cshtml
@foreach (var day in Model.GetCalendarDays())
{
    @foreach (var questOnDay in day.QuestsOnDay)
    {
        @if (isDetailsPage && questOnDay.Quest.Id == currentQuestId)
        {
            <div class="calendar-date-entry-mobile">
                <div class="calendar-date-label-mobile fw-bold small mb-2">
                    @questOnDay.ProposedDate.Date.ToString("dddd, MMMM d").ToUpper()
                    <small class="ms-1 text-muted">@questOnDay.ProposedDate.Date.ToString("HH:mm")</small>
                </div>
                @* vote button block here (initial or update, guarded by isPlayerSignedUp / isUpdateMode) *@
            </div>
        }
    }
}
```

**Anti-pattern — NO `.Take(3)` cap:** Desktop `_Calendar.cshtml` line 34 uses `.Take(3)` for grid overflow. The mobile partial renders all entries; remove that cap entirely.

**Anti-pattern — NO `@inject`:** `_ViewImports.cshtml` already globally injects `IAuthorizationService` and `IAntiforgery`. Do not repeat in the partial (Phase 13 lesson).

**Anti-pattern — NO `@{...}` inside `@foreach`:** Declare variables directly in C# code mode of the foreach body (Phase 13 lesson).

**Anti-pattern — NO `@section Styles`:** Partials cannot push sections. CSS for this partial comes from the host view's loaded stylesheet.

---

### `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` — MODIFY (lines 79–97)

**Analog:** `EuphoriaInn.Service/Views/Quest/Details.cshtml` lines 538–576 (update-vote form pattern)

**Lines to remove** (current `Details.Mobile.cshtml` lines 79–97 — the AJAX block):
```cshtml
@if (User.Identity?.IsAuthenticated == true && isPlayerSignedUp && Model.Quest?.IsFinalized == false)
{
    <div class="quest-section-card-mobile mb-3">
        <h6 class="quest-section-heading mb-3">
            <i class="fas fa-vote-yea me-1"></i>Update Your Vote
        </h6>
        <div class="d-grid gap-2">
            <button class="btn btn-success" onclick="changeVoteToYes(@Model.Quest?.Id)">...</button>
            <button class="btn btn-danger" onclick="changeVoteToNo(@Model.Quest?.Id)">...</button>
            <button class="btn btn-warning" onclick="changeVoteToMaybe(@Model.Quest?.Id)">...</button>
        </div>
    </div>
}
```

**Replacement — update vote section** (from `Details.cshtml` lines 538–576 — compressed for mobile, form wrapping the partial):
```cshtml
@if (User.Identity?.IsAuthenticated == true && isPlayerSignedUp && Model.Quest?.IsFinalized == false)
{
    <div class="quest-section-card-mobile mb-3">
        <h6 class="quest-section-heading mb-3">
            <i class="fas fa-vote-yea me-1"></i>Update Your Vote
        </h6>
        <form asp-action="UpdateSignup" asp-controller="Quest" method="post">
            <input type="hidden" name="questId" value="@Model.Quest?.Id" />
            @Html.AntiForgeryToken()
            @{
                var currentUser = Model.Player;
                var userSignup = Model.Quest?.PlayerSignups.FirstOrDefault(ps => ps.Player.Id == currentUser?.Id);
                var userVotes = userSignup?.DateVotes?.ToDictionary(v => v.ProposedDateId, v => v.Vote) ?? new Dictionary<int, VoteType?>();
                var sortedUpdateDateVotes = Model.Quest?.ProposedDates.OrderBy(pd => pd.Date)
                    .Select(pd => new { ProposedDateId = pd.Id, Vote = userVotes.GetValueOrDefault(pd.Id) })
                    .ToList() ?? [];
                var updateVoteIndexLookup = new Dictionary<int, int>();
                for (var i = 0; i < sortedUpdateDateVotes.Count; i++)
                {
                    updateVoteIndexLookup[sortedUpdateDateVotes[i].ProposedDateId] = i;
                }
            }
            @for (var i = 0; i < sortedUpdateDateVotes.Count; i++)
            {
                <input type="hidden" name="DateVotes[@i].ProposedDateId" value="@sortedUpdateDateVotes[i].ProposedDateId" />
            }
            @foreach (var calendarMonth in ViewBag.CalendarMonths as List<CalendarViewModel> ?? new List<CalendarViewModel>())
            {
                @{
                    ViewData["UpdateVoteIndexLookup"] = updateVoteIndexLookup;
                }
                @await Html.PartialAsync("_Calendar", calendarMonth)
            }
            <button type="submit" class="btn btn-primary w-100 mt-2">
                <i class="fas fa-check me-2"></i>Update Votes
            </button>
        </form>
    </div>
}
```

**"Choose a Date" section also needs a form wrapper** (no form exists in current file — add around lines 101–112, from `Details.cshtml` lines 452–490 pattern):
```cshtml
@if (User.Identity?.IsAuthenticated == true && !isPlayerSignedUp && Model.Quest?.IsFinalized == false)
{
    <div class="quest-section-card-mobile mb-3">
        <h6 class="quest-section-heading mb-2">
            <i class="fas fa-calendar-alt me-1"></i>Choose a Date
        </h6>
        <form asp-action="Details" asp-controller="Quest" asp-route-id="@Model.Quest?.Id" method="post">
            @Html.AntiForgeryToken()
            @{
                var sortedDateVotes = Model.Quest?.ProposedDates.OrderBy(pd => pd.Date)
                    .Select(pd => new { ProposedDateId = pd.Id }).ToList() ?? [];
                var voteIndexLookup = new Dictionary<int, int>();
                for (var i = 0; i < sortedDateVotes.Count; i++)
                {
                    voteIndexLookup[sortedDateVotes[i].ProposedDateId] = i;
                }
                ViewBag.VoteIndexLookup = voteIndexLookup;
            }
            @for (var i = 0; i < sortedDateVotes.Count; i++)
            {
                <input type="hidden" name="DateVotes[@i].ProposedDateId" value="@sortedDateVotes[i].ProposedDateId" />
            }
            @foreach (var calendarMonth in ViewBag.CalendarMonths as List<CalendarViewModel> ?? new List<CalendarViewModel>())
            {
                @await Html.PartialAsync("_Calendar", calendarMonth)
            }
            <button type="submit" class="btn btn-primary w-100 mt-2">
                <i class="fas fa-user-plus me-2"></i>Sign Up with Votes
            </button>
        </form>
    </div>
}
```

**Scripts block change** — remove `changeVoteToYes/No/Maybe` functions; keep only `revokeSignup` (from `Details.Mobile.cshtml` lines 168–179):
```cshtml
@section Scripts {
<script>
    function revokeSignup(questId) {
        if (confirm("Are you sure you want to revoke your signup for this quest? This action cannot be undone.")) {
            const formData = new FormData();
            formData.append('__RequestVerificationToken', '@tokens.RequestToken');
            fetch(`/Quest/RevokeSignup/${questId}`, {
                method: "DELETE",
                body: formData
            }).then(res => {
                if (res.ok) { location.reload(); }
                else { res.text().then(text => { alert(`Failed to revoke signup: ${text}`); }); }
            }).catch(err => { alert("An error occurred while revoking signup."); });
        }
    }
</script>
}
```

---

### `EuphoriaInn.Service/wwwroot/css/calendar.mobile.css` (NEW — CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` (structure pattern)

**File header comment pattern** (from `quests.mobile.css` lines 1–2):
```css
/* Calendar Mobile — calendar.mobile.css */
/* Phase 14: CAL-01, CAL-02, CAL-03, CAL-04 */
```

**Month nav bar rules** (from `RESEARCH.md` Code Examples):
```css
.calendar-nav-btn {
    min-width: 44px;
    width: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.calendar-month-title {
    font-family: 'Cinzel', serif;
    font-size: 1.25rem;
    font-weight: 700;
}
```

**Day label rules**:
```css
.agenda-day-label {
    color: #1a0f08;
    letter-spacing: 0.08em;
    padding-top: 8px;
    border-top: 1px solid rgba(26, 15, 8, 0.15);
}

.agenda-day-section:first-child .agenda-day-label {
    border-top: none;
    padding-top: 0;
}
```

**Quest entry card rules** (left-border status indicator — color values from `calendar.css` lines 102–108):
```css
.agenda-quest-entry {
    background-color: #343a40;
    border-radius: 6px;
    padding: 8px 12px;
    margin-bottom: 8px;
    cursor: pointer;
    border-left: 3px solid #343a40;
}

.agenda-quest-entry:active {
    background-color: #495057;
}

.agenda-quest-proposed {
    border-left-color: #ffc107;
}

.agenda-quest-finalized {
    border-left-color: #28a745;
}

.agenda-quest-title {
    font-size: 1rem;
    color: #F4E4BC;
    text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
}
```

**Note on `.btn` touch targets:** `mobile.css` line 2 already sets `.btn { min-height: 44px }` globally. No override needed here.

---

### `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — MODIFY (append)

**Analog:** `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` (self — append after line 112)

**Section comment pattern** (from `quests.mobile.css` lines 1–3):
```css
/* Calendar partial mobile rules — CAL-05 */
/* Used by _Calendar.Mobile.cshtml when rendered inside Quest Details */
```

**Calendar partial rules to append** (from `RESEARCH.md` Code Examples):
```css
.calendar-date-entry-mobile {
    border-bottom: 1px solid rgba(26, 15, 8, 0.15);
    padding-bottom: 12px;
    margin-bottom: 12px;
}

.calendar-date-entry-mobile:last-child {
    border-bottom: none;
    margin-bottom: 0;
}

.calendar-date-label-mobile {
    font-size: 0.875em;
    color: #F4E4BC;
    text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
}

.calendar-vote-row .btn {
    min-height: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
}
```

---

### `.planning/REQUIREMENTS.md` — MODIFY

**Analog:** Existing CAL-01…CAL-04 block (lines 32–35)

**Lines to insert after line 35** (after `CAL-04`, before `### DM Views`):
```markdown
- [ ] **CAL-05**: The `_Calendar` partial used inside Quest Details renders as a vertical per-date list with tap-friendly Yes/No/Maybe vote buttons — replacing both the broken desktop grid (Choose a Date) and the Phase 13 simplified quest-level buttons (Update Your Vote)
```

---

### `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — MODIFY (append methods)

**Analog:** Existing methods in the same file — exact structure to copy.

**Test class / fixture pattern** (from `MobileViewsTests.cs` lines 11–35 — do not duplicate; append methods only):
```csharp
// Already defined — do NOT re-declare:
// MobileUserAgent, DesktopUserAgent, GetWithUserAgentAsync(url, userAgent)
```

**Data seeding pattern** (from `MobileViewsTests.cs` lines 44–51 — for calendar tests, add a ProposedDate):
```csharp
var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm_cal01", "dm_cal01@test.com", name: "DM Cal01");
var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Calendar Quest");
var proposedDate = await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id,
    new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 19, 0, 0));
```

**Assertion pattern** (from `MobileViewsTests.cs` lines 48–51 — `.Should().Contain(cssClass)`):
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
html.Should().Contain("agenda-quest-entry");
html.Should().NotContain("calendar-grid");
```

**CAL-01 test method** (month nav + agenda class present, grid absent):
```csharp
[Fact]
public async Task MobileCalendar_MobileUserAgent_RendersAgendaList()
{
    var dm = await AuthenticationHelper.CreateTestUserAsync(...);
    var quest = await TestDataHelper.CreateTestQuestAsync(...);
    await TestDataHelper.CreateProposedDateAsync(_factory.Services, quest.Id, DateTime.UtcNow.AddDays(5));

    var (response, html) = await GetWithUserAgentAsync(
        $"/Calendar?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}", MobileUserAgent);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().Contain("agenda-quest-entry");
    html.Should().NotContain("calendar-grid");
}
```

**CAL-02 test method** (day label and time present):
```csharp
[Fact]
public async Task MobileCalendar_MobileUserAgent_AgendaEntryContainsDayLabelAndTime()
{
    // seed quest with known date, assert html contains day.Date.ToString("dddd").ToUpper() fragment
    // e.g. "SATURDAY" or the month name "JUNE"
}
```

**CAL-03 test method** (desktop UA does not render agenda):
```csharp
[Fact]
public async Task MobileCalendar_DesktopUserAgent_DoesNotRenderAgendaList()
{
    var (response, html) = await GetWithUserAgentAsync("/Calendar", DesktopUserAgent);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().NotContain("agenda-quest-entry");
}
```

**CAL-04 test method** (agenda entry links to Quest Details):
```csharp
[Fact]
public async Task MobileCalendar_MobileUserAgent_AgendaEntryLinksToDetails()
{
    // seed quest, assert html contains /Quest/Details/{quest.Id}
}
```

**CAL-05 test method** (vote buttons present in Quest Details via partial):
```csharp
[Fact]
public async Task MobileCalendar_MobileUserAgent_CalendarPartialRendersVoteButtons()
{
    // seed quest + proposed date, authenticated player not yet signed up
    // GET /Quest/Details/{quest.Id} with MobileUserAgent
    // assert html.Should().Contain("btn-check")
    // and html.Should().Contain("calendar-date-entry-mobile")
}
```

**CSS link test method** (calendar.mobile.css linked from /Calendar on mobile):
```csharp
[Fact]
public async Task MobileCalendar_MobileUserAgent_LoadsMobileCssLink()
{
    var (response, html) = await GetWithUserAgentAsync("/Calendar", MobileUserAgent);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().Contain("calendar.mobile.css");
}
```

**Authenticated request pattern** — for CAL-05 (copy from QVIEW-01 lines 143–155):
```csharp
var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory, "player_cal05", "player_cal05@test.com");
var request = new HttpRequestMessage(HttpMethod.Get, $"/Quest/Details/{quest.Id}");
request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
```

**Important:** QVIEW-01 test (line 152) currently asserts `html.Should().Contain("changeVoteToYes")`. After the AJAX block is replaced by the partial, this assertion will fail — the planner must update that test method to assert `html.Should().Contain("btn-check")` instead.

---

## Shared Patterns

### Per-Page CSS Loading
**Source:** `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml` lines 10–12
**Apply to:** `Calendar/Index.Mobile.cshtml` (loads `calendar.mobile.css`)
**Not applicable to:** `_Calendar.Mobile.cshtml` (partial — cannot push sections; relies on host's CSS)
```cshtml
@section Styles {
    <link href="~/css/{page}.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

### ViewBag State Threading Through Partials
**Source:** `EuphoriaInn.Service/Views/Quest/Details.cshtml` lines 453–462, 546–552, 571
**Apply to:** `Details.Mobile.cshtml` replacement sections + `_Calendar.Mobile.cshtml`
```cshtml
@* In parent view — set before calling partial: *@
ViewBag.VoteIndexLookup = voteIndexLookup;
ViewData["UpdateVoteIndexLookup"] = updateVoteIndexLookup;

@* In partial — read back: *@
var voteIndexLookup = ViewBag.VoteIndexLookup as Dictionary<int, int>;
var updateVoteIndexLookup = ViewData["UpdateVoteIndexLookup"] as Dictionary<int, int>;
```
Note: `UpdateVoteIndexLookup` uses `ViewData[key]` (not `ViewBag.X`) — the key string must match exactly.

### Antiforgery Token in Forms
**Source:** `EuphoriaInn.Service/Views/Quest/Details.cshtml` line 538
**Apply to:** Both form wrappers in `Details.Mobile.cshtml`
```cshtml
<form asp-action="..." asp-controller="Quest" method="post">
    @Html.AntiForgeryToken()
    ...
</form>
```

### Bootstrap btn-check Radio Pattern
**Source:** `EuphoriaInn.Service/Views/Shared/_Calendar.cshtml` lines 94–111
**Apply to:** `_Calendar.Mobile.cshtml` vote button rows
- Use `class="btn-check"` + matching `for="..."` label — Bootstrap 5 handles checked-state highlighting natively via CSS, zero JS required.
- `d-flex gap-2` with `flex-fill` labels gives equal-width three-button row on mobile.

### Glass Card Container
**Source:** `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` lines 85–92 (`.quest-section-card-mobile`)
**Apply to:** Both voting section wrappers in `Details.Mobile.cshtml` (already use this class; keep them)
```css
.quest-section-card-mobile {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 12px;
}
```

### Section Heading Typography
**Source:** `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` lines 95–101 (`.quest-section-heading`)
**Apply to:** `h6` elements inside vote sections in `Details.Mobile.cshtml`
```css
.quest-section-heading {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.9), -1px -1px 2px rgba(0, 0, 0, 0.9);
    text-transform: uppercase;
    font-size: 0.85rem;
    letter-spacing: 0.05em;
}
```

### Status Color Values
**Source:** `EuphoriaInn.Service/wwwroot/css/calendar.css` lines 102–108 (verified via RESEARCH.md)
**Apply to:** `calendar.mobile.css` agenda entry left-border colors
- Proposed: `#ffc107`
- Finalized: `#28a745`

---

## No Analog Found

All files have close analogs. No entries in this section.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Views/`, `EuphoriaInn.Service/wwwroot/css/`, `EuphoriaInn.IntegrationTests/Mobile/`, `.planning/`
**Files read:** 10 source files + 2 planning documents
**Pattern extraction date:** 2026-06-24
