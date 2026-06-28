# Phase 13: Core Player Views - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 7 (3 views, 3 CSS files, 1 test file)
**Analogs found:** 7 / 7

---

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Home/Index.cshtml` | exact |
| `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Quest/Details.cshtml` | exact |
| `EuphoriaInn.Service/Views/QuestLog/Index.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/QuestLog/Index.cshtml` | exact |
| `EuphoriaInn.Service/wwwroot/css/home.mobile.css` | static/CSS | — | `EuphoriaInn.Service/wwwroot/css/mobile.css` | role-match |
| `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` | static/CSS | — | `EuphoriaInn.Service/wwwroot/css/mobile.css` | role-match |
| `EuphoriaInn.Service/wwwroot/css/quest-log.mobile.css` | static/CSS | — | `EuphoriaInn.Service/wwwroot/css/mobile.css` | role-match |
| `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` | test | request-response | `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs` | exact |

---

## Pattern Assignments

### `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/Home/Index.cshtml`

**Header pattern** (analog lines 1-5) — copy `@using` and `@model` declarations verbatim; add `@section Styles` block; do NOT set `Layout`:
```cshtml
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Domain.Models.QuestBoard
@model IEnumerable<Quest>
@{
    ViewData["Title"] = "Quest Board";
    ViewData["BodyClass"] = "home-page";
}
@section Styles {
    <link href="~/css/home.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**No `@inject` needed** — `_ViewImports.cshtml` (line 16) already globally injects `Antiforgery`, `AuthorizationService`, and `UserService`. Do NOT add `@inject` lines in the mobile view.

**ViewBag access pattern** (analog lines 69-70) — always cast to nullable types:
```cshtml
@{
    var currentUserId = ViewBag.CurrentUserId as int?;
    var currentUserName = ViewBag.CurrentUserName as string;
}
```

**Signed-up detection pattern** (analog line 70):
```cshtml
var isUserSignedUp = currentUserId.HasValue
    && quest.PlayerSignups.Any(ps => ps.Player.Id == currentUserId.Value);
```

**Tap navigation pattern** (analog line 80) — reuse verbatim inside the card element:
```cshtml
onclick="window.location.href='@(ViewBag.CurrentUserName != null && ViewBag.CurrentUserName == quest.DungeonMaster?.Name
    ? Url.Action("Manage", "Quest", new { id = quest.Id })
    : Url.Action("Details", "Quest", new { id = quest.Id }))'"
```

**Status badge three-way logic** (sourced from RESEARCH.md Data Binding Inventory — verified against Details.cshtml lines 634–660) — use this exact check inside the `@foreach`:
```cshtml
@{
    string statusBadge;
    string statusIcon;
    string statusText;

    if (quest.IsFinalized && quest.FinalizedDate.HasValue
        && quest.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date)
    {
        statusBadge = "bg-secondary";
        statusIcon = "fas fa-flag-checkered";
        statusText = "Done";
    }
    else if (quest.IsFinalized)
    {
        statusBadge = "bg-primary";
        statusIcon = "fas fa-check-circle";
        statusText = "Finalized";
    }
    else
    {
        statusBadge = "bg-success";
        statusIcon = "fas fa-clock";
        statusText = "Open";
    }
}
<span class="badge @statusBadge"><i class="@statusIcon me-1"></i>@statusText</span>
```

**Signed-up badge pattern** (D-06 — Bootstrap badge, no wax seal):
```cshtml
@if (isUserSignedUp)
{
    <span class="badge bg-success position-absolute top-0 end-0 m-2">
        <i class="fas fa-check me-1"></i>Signed up
    </span>
}
```
Requires `position: relative` on the card container — set in `home.mobile.css`.

**Quest card fields to render** (analog lines 98-139, mobile subset per D-03): title, CR, DM name, status badge only. Omit description, poster images, and wax seal imagery.

**Empty state pattern** (analog has no empty state — use Bootstrap empty-state pattern matching the quest log analog):
```cshtml
@if (!Model.Any())
{
    <div class="text-center text-muted py-5">
        <i class="fas fa-scroll fa-3x mb-3"></i>
        <h3>No Quests Available</h3>
        <p>Check back later for new adventures.</p>
    </div>
}
```

---

### `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/Quest/Details.cshtml`

**CRITICAL: No `@inject` line** — `_ViewImports.cshtml` (line 16) globally injects `IAntiforgery` as `Antiforgery`. The desktop analog has a redundant `@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery` at line 5. Do NOT copy this line — it will either duplicate-inject or shadow the global instance.

**Header pattern** (analog lines 1-12, with `@inject` line removed and `@section Styles` added):
```cshtml
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Domain.Models.QuestBoard
@using EuphoriaInn.Service.ViewModels.CalendarViewModels
@model PlayerSignup
@{
    ViewData["Title"] = Model.Quest?.Title;
    ViewData["BodyClass"] = "quest-details-page";
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}
@section Styles {
    <link href="~/css/quests.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Participant list data construction** (analog lines 58-66) — copy verbatim into a Razor code block:
```cshtml
@{
    var selectedPlayers = Model.Quest?.PlayerSignups
        .Where(ps => ps.IsSelected && ps.Role == SignupRole.Player)
        .OrderBy(ps => ps.SignupTime).ToList() ?? [];
    var selectedAssistants = Model.Quest?.PlayerSignups
        .Where(ps => ps.IsSelected && ps.Role == SignupRole.AssistantDM)
        .OrderBy(ps => ps.SignupTime).ToList() ?? [];
    var selectedSpectators = Model.Quest?.PlayerSignups
        .Where(ps => ps.IsSelected && ps.Role == SignupRole.Spectator)
        .OrderBy(ps => ps.SignupTime).ToList() ?? [];
    var allSelectedParticipants = new List<PlayerSignup>();
    allSelectedParticipants.AddRange(selectedPlayers);
    allSelectedParticipants.AddRange(selectedAssistants);
    allSelectedParticipants.AddRange(selectedSpectators);
}
```

**Stacked participant list** (QVIEW-02 — replaces `<table>` from analog lines 86-145):
```cshtml
@foreach (var participant in allSelectedParticipants)
{
    var isCurrentUser = participant.Player.Id == (ViewBag.CurrentUserId as int?);
    string roleBadge = participant.Role == SignupRole.Player ? "bg-primary"
        : participant.Role == SignupRole.AssistantDM ? "bg-warning text-dark"
        : "bg-secondary";
    string roleText = participant.Role == SignupRole.Player ? "Player"
        : participant.Role == SignupRole.AssistantDM ? "Asst. DM"
        : "Spectator";

    <div class="participant-row d-flex justify-content-between align-items-center py-2 border-bottom @(isCurrentUser ? "bg-light" : "")">
        <div>
            <span class="fw-bold">@participant.Player.Name</span>
            @if (isCurrentUser)
            {
                <span class="badge bg-info ms-1">You</span>
            }
            <br>
            <small class="text-muted">@(participant.Character?.Name ?? "No character")</small>
        </div>
        <span class="badge @roleBadge">@roleText</span>
    </div>
}
```

**AJAX voting JS functions** (analog lines 838-881) — copy verbatim inside `@section Scripts { <script> ... </script> }`. The `@tokens.RequestToken` Razor interpolation is the key pattern:
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

    function changeVoteToYes(questId) {
        if (confirm("Change your vote to Yes and join this quest?")) {
            const formData = new FormData();
            formData.append('__RequestVerificationToken', '@tokens.RequestToken');
            fetch(`/Quest/ChangeVoteToYes/${questId}`, {
                method: "POST",
                body: formData
            }).then(res => {
                if (res.ok) { location.reload(); }
                else { res.text().then(text => { alert(`Failed to change vote: ${text}`); }); }
            }).catch(err => { alert("An error occurred while changing vote."); });
        }
    }
</script>
}
```

**Revoke signup button** (analog line 601) — copy and adapt for single-column layout:
```cshtml
@if (User.Identity?.IsAuthenticated == true && (bool)ViewBag.IsPlayerSignedUp)
{
    <button type="button" class="btn btn-danger w-100 mt-2"
            onclick="revokeSignup(@ViewContext.RouteData.Values["id"])">
        <i class="fas fa-times me-2"></i>Revoke Signup
    </button>
}
```

**Calendar partial reuse** (analog line 481) — reuse as-is for QVIEW-01; wrap in `@section Scripts` if the partial requires JS:
```cshtml
@foreach (var calendarMonth in ViewBag.CalendarMonths as List<CalendarViewModel> ?? new List<CalendarViewModel>())
{
    @await Html.PartialAsync("_Calendar", calendarMonth)
}
```
Note: Calendar partial may render sub-optimally on mobile until Phase 14; `mobile.css` `.btn { min-height: 44px }` satisfies the 44px QVIEW-01 requirement without calendar changes.

**CanManage guard** (analog line 611):
```cshtml
@if ((bool)ViewBag.CanManage)
{
    @* DM controls — link to Manage page instead of inline panel *@
    <a asp-controller="Quest" asp-action="Manage" asp-route-id="@Model.Quest?.Id"
       class="btn btn-warning w-100 mb-2">
        <i class="fas fa-cogs me-2"></i>Manage Quest
    </a>
}
```

---

### `EuphoriaInn.Service/Views/QuestLog/Index.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/QuestLog/Index.cshtml`

**Header pattern** (analog lines 1-5) — requires the `@using` for `QuestLogIndexViewModel` because it is not in `_ViewImports.cshtml`:
```cshtml
@using EuphoriaInn.Service.ViewModels.QuestLogViewModels
@model QuestLogIndexViewModel
@{
    ViewData["Title"] = "Quest Log";
}
@section Styles {
    <link href="~/css/quest-log.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Tap navigation pattern** (analog line 22) — copy verbatim:
```cshtml
onclick="window.location.href='@Url.Action("Details", "QuestLog", new { id = quest.Id })'"
```

**List item fields** (analog lines 16-75, mobile subset per D-09 — title, finalized date, DM name only; no truncated description):
```cshtml
@foreach (var quest in Model.CompletedQuests)
{
    <div class="quest-log-list-item"
         onclick="window.location.href='@Url.Action("Details", "QuestLog", new { id = quest.Id })'">
        <div class="d-flex justify-content-between align-items-start">
            <h6 class="mb-1 fw-bold">@quest.Title</h6>
            <span class="badge bg-secondary">CR @quest.ChallengeRating</span>
        </div>
        <small class="text-muted d-block">
            <i class="fas fa-calendar-check me-1"></i>
            @(quest.FinalizedDate?.ToString("MMM dd, yyyy") ?? "Unknown Date")
        </small>
        <small class="text-muted">
            <i class="fas fa-crown me-1"></i>
            @quest.DungeonMaster?.Name
        </small>
    </div>
}
```

**Empty state pattern** (analog lines 79-85 — copy class name for CSS consistency):
```cshtml
@if (Model.CompletedQuests?.Any() != true)
{
    <div class="empty-quest-log text-center py-5">
        <i class="fas fa-book-open fa-3x mb-3 text-muted"></i>
        <h3 class="text-muted">No Completed Quests Yet</h3>
        <p class="text-muted">Completed quests will appear here once they have been undertaken and finished.</p>
    </div>
}
```

---

### `EuphoriaInn.Service/wwwroot/css/home.mobile.css` (static/CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/mobile.css`

**CSS authoring rules** (from mobile.css and Phase 12 D-02):
- Plain CSS only — no `@media` queries, no `@import`, no preprocessor syntax
- No reference to desktop CSS files (`site.css`, `quests.css`, etc.)
- This file is ONLY loaded on mobile requests via `_Layout.Mobile.cshtml`

**Pattern to follow** (mobile.css lines 1-37 — plain selector-block structure):
```css
/* Quest Board Mobile — home.mobile.css */

/* Card container */
.quest-board-mobile {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    padding: 0.5rem 0;
}

/* Individual quest card — must have position: relative for signed-up badge */
.mobile-quest-card {
    position: relative;
    background-color: #2c2c2c;
    border-radius: 0.5rem;
    padding: 1rem;
    cursor: pointer;
    border-left: 4px solid #6c757d;
    min-height: 80px;
}

/* Status border colors — mirror quests.css .quest-status-* pattern */
.mobile-quest-card.status-open    { border-left-color: #198754; }
.mobile-quest-card.status-final   { border-left-color: #0d6efd; }
.mobile-quest-card.status-done    { border-left-color: #6c757d; }

/* Card title */
.mobile-quest-card-title {
    font-size: 1rem;
    font-weight: 600;
    margin-bottom: 0.25rem;
    color: #fff;
}

/* Meta fields (DM, CR) */
.mobile-quest-card-meta {
    font-size: 0.875rem;
    color: #adb5bd;
}
```

---

### `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` (static/CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/mobile.css`

**Pattern to follow** (same plain CSS, no media queries):
```css
/* Quest Details Mobile — quests.mobile.css */

/* Vote buttons — stacked, full-width, 44px min-height already from mobile.css .btn rule */
.vote-btn-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.vote-btn-group .btn {
    width: 100%;
    /* min-height: 44px already guaranteed by mobile.css */
}

/* Participant stacked list */
.participant-row {
    font-size: 0.9rem;
}

/* Quest description block */
.quest-description-mobile {
    font-size: 1rem;
    line-height: 1.6;
    margin-bottom: 1.5rem;
    color: #dee2e6;
}
```

---

### `EuphoriaInn.Service/wwwroot/css/quest-log.mobile.css` (static/CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/mobile.css`

**Pattern to follow** (same plain CSS):
```css
/* Quest Log Mobile — quest-log.mobile.css */

/* List item */
.quest-log-list-item {
    padding: 0.875rem 0.5rem;
    border-bottom: 1px solid #343a40;
    cursor: pointer;
}

.quest-log-list-item:last-child {
    border-bottom: none;
}

.quest-log-list-item h6 {
    font-size: 1rem;
    color: #fff;
}

.quest-log-list-item small {
    font-size: 0.8125rem;
}
```

---

### `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` (test, request-response)

**Analog:** `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs`

**Class declaration pattern** (analog lines 1-27) — copy namespace, `IClassFixture`, UA constants, `HttpClient` field, and constructor verbatim:
```csharp
using System.Net;

namespace EuphoriaInn.IntegrationTests.Mobile;

public class MobileViewsTests : IClassFixture<WebApplicationFactoryBase>
{
    private const string MobileUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
    private const string DesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly HttpClient _client;
    private readonly WebApplicationFactoryBase _factory;

    public MobileViewsTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
```

**UA-parameterised HTTP helper** (analog lines 29-36, extended to accept URL per RESEARCH.md template):
```csharp
private async Task<(HttpResponseMessage Response, string Html)> GetWithUserAgentAsync(
    string url, string userAgent)
{
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
    var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
    var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    return (response, html);
}
```

**Test method pattern** (analog lines 38-52) — copy `[Fact]`, arrange/act/assert structure with `Should().Contain()` / `Should().NotContain()`:
```csharp
[Fact]
public async Task MobileHome_MobileUserAgent_DoesNotRenderPosterImages()
{
    // Arrange + Act
    var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().NotContain("fantasy-quest-card");   // desktop poster class
    html.Should().NotContain("Blanks w Shadow");       // desktop poster image path
}
```

**Seeding pattern for authenticated tests** (from `HomeControllerIntegrationTests.cs` lines 43-57 and `TestDataHelper.cs` lines 38-61) — for HOME-04 (signed-up badge) and QVIEW-02 tests:
```csharp
// Create a DM user and quest
var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "dm", "dm@test.com");
var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Test Quest");

// Create a player user and signup
var player = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "player", "player@test.com");
await TestDataHelper.CreatePlayerSignupAsync(_factory.Services, quest.Id, player.Id, isSelected: true);
```

**Authenticated client pattern** (from `AuthenticationHelper.cs` lines 51-85):
```csharp
var (authClient, playerUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
    _factory, "player", "player@test.com");
// Then use authClient instead of _client for requests requiring authentication
var request = new HttpRequestMessage(HttpMethod.Get, "/");
request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
// Add auth header manually when combining UA + auth:
request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
```

**CSS file existence assertion pattern** (from `MobileCssTests.cs` — referenced in RESEARCH.md):
```csharp
[Fact]
public async Task MobileQuestBoard_MobileUserAgent_LoadsMobileCssLink()
{
    var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().Contain("home.mobile.css");
}
```

---

## Shared Patterns

### `@section Styles` CSS Loading
**Source:** `_Layout.Mobile.cshtml` lines 164-165 (confirmed placement: after `<script>` tags, before `</body>`)
**Apply to:** All three `.Mobile.cshtml` views
```cshtml
@section Styles {
    <link href="~/css/{page}.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```
Rule: `asp-append-version="true"` is mandatory for cache busting — matches the `mobile.css` link pattern in `_Layout.Mobile.cshtml` line 11.

### No Layout Assignment in Mobile Views
**Source:** `_ViewStart.cshtml` (Phase 12 pattern — isMobile conditional)
**Apply to:** All three `.Mobile.cshtml` views
Never set `Layout = ...` in any mobile view. `_ViewStart.cshtml` selects `_Layout.Mobile.cshtml` automatically when `HttpContext.Items["IsMobile"]` is true.

### No Duplicate `@inject` Directives
**Source:** `_ViewImports.cshtml` lines 14-16
**Apply to:** All three `.Mobile.cshtml` views
```cshtml
@inject IAuthorizationService AuthorizationService    // already global
@inject IUserService UserService                      // already global
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery  // already global
```
Desktop `Details.cshtml` has a redundant `@inject Antiforgery` at line 5 — do NOT copy it. All three services are available in mobile views without any `@inject` declaration.

### `ViewBag.CurrentUserId` Null-Safe Cast
**Source:** `EuphoriaInn.Service/Views/Home/Index.cshtml` line 69
**Apply to:** `Index.Mobile.cshtml`, `Details.Mobile.cshtml`
```cshtml
var currentUserId = ViewBag.CurrentUserId as int?;
```
Always `as int?`, never direct cast to `int`. Then guard with `currentUserId.HasValue` before use.

### Tap Navigation via `onclick`
**Source:** `EuphoriaInn.Service/Views/Home/Index.cshtml` line 80, `EuphoriaInn.Service/Views/QuestLog/Index.cshtml` line 22
**Apply to:** `Index.Mobile.cshtml`, `QuestLog/Index.Mobile.cshtml`
```cshtml
onclick="window.location.href='@Url.Action("Action", "Controller", new { id = quest.Id })'"
```

### Mobile CSS File Authoring Rules
**Source:** `EuphoriaInn.Service/wwwroot/css/mobile.css` (complete — 37 lines)
**Apply to:** `home.mobile.css`, `quests.mobile.css`, `quest-log.mobile.css`
- Plain CSS only — no `@media`, no `@import`, no SCSS
- These files are ONLY served on mobile requests; no desktop CSS compensation needed
- Follow `mobile.css` selector-block structure and color palette (`#212529` background, `#adb5bd` muted text)
- Touch targets: `mobile.css` already sets `.btn { min-height: 44px }` — per-page CSS only needs to override layout (stacking, width), not the height

### Integration Test: UA Header Injection
**Source:** `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs` lines 29-36
**Apply to:** `MobileViewsTests.cs`
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, url);
request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
```
Use `TryAddWithoutValidation` (not `Add`) to avoid header validation exceptions on UA strings.

### Integration Test: HTML Assertion
**Source:** `EuphoriaInn.IntegrationTests/Controllers/HomeControllerIntegrationTests.cs` lines 54-57
**Apply to:** `MobileViewsTests.cs`
```csharp
var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
content.Should().Contain("expected-string");
content.Should().NotContain("absent-string");
```

---

## No Analog Found

All seven files have analogs. No entries.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Views/`, `EuphoriaInn.Service/wwwroot/css/`, `EuphoriaInn.IntegrationTests/Mobile/`, `EuphoriaInn.IntegrationTests/Controllers/`, `EuphoriaInn.IntegrationTests/Helpers/`
**Key source files read:**
- `EuphoriaInn.Service/Views/Home/Index.cshtml` (complete — 143 lines)
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` (lines 1-12 header; lines 825-924 JS section; grep for all key patterns)
- `EuphoriaInn.Service/Views/QuestLog/Index.cshtml` (complete — 87 lines)
- `EuphoriaInn.Service/Views/_ViewImports.cshtml` (complete — 16 lines)
- `EuphoriaInn.Service/wwwroot/css/mobile.css` (complete — 37 lines)
- `EuphoriaInn.Service/wwwroot/css/quests.css` (lines 1-60)
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` (lines 155-168)
- `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs` (complete — 115 lines)
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` (complete — 146 lines)
- `EuphoriaInn.IntegrationTests/Controllers/HomeControllerIntegrationTests.cs` (complete — 73 lines)
- `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs` (lines 1-80)
- `EuphoriaInn.IntegrationTests/Helpers/AuthenticationHelper.cs` (complete — 228 lines)
- `.planning/phases/12-mobile-infrastructure/12-PATTERNS.md` (complete — 515 lines)
**Pattern extraction date:** 2026-06-24
