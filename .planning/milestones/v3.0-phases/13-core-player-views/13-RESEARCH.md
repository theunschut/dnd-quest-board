# Phase 13: Core Player Views - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core 8 MVC Razor views — mobile-variant additive views
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01** Per-page mobile CSS files: `home.mobile.css`, `quests.mobile.css`, `quest-log.mobile.css`. Each `.Mobile.cshtml` loads its own CSS via `@section Styles { <link href="~/css/{page}.mobile.css" asp-append-version="true" rel="stylesheet" /> }`.

**D-02** `mobile.css` remains the baseline only. Page-specific rules go in the per-page CSS files, not `mobile.css`.

**D-03** Mobile quest cards show required fields only: title, CR, DM name, and status badge. No description on the card.

**D-04** Each card is tap-navigable: DM's own quest → Quest Manage; any other quest → Quest Details. Uses `ViewBag.CurrentUserName` vs `quest.DungeonMaster?.Name` check.

**D-05** Status badge: Open = green (`bg-success`), Finalized = blue/gold (`bg-primary`), past/done = gray (`bg-secondary`). Colors at Claude's discretion within that palette.

**D-06** Signed-up indicator: green pill badge ("✓ Signed up") at top-right of the card. No wax seal imagery.

**D-07** Voting buttons (Yes / No / Maybe) use the same AJAX interaction pattern as the desktop view. No page refresh on vote.

**D-08** Participant list renders as a stacked single-column list (player name + character name + role per row) instead of the horizontal table.

**D-09** Quest Log mobile view shows a vertical scrollable list: title, date (finalized date), DM name. No description. Entries tap-navigate to Quest Log Details.

### Claude's Discretion

- Exact Bootstrap utility classes and color tokens for status badges — follow Bootstrap 5 contextual colors (`success`, `primary`, `secondary`).
- Exact CSS values in per-page CSS files — use `mobile.css` patterns as a guide (44px min-height, 16px font-size).
- Whether the Quest Log mobile view shows a CR badge alongside the title.
- Empty-state markup when no quests exist on the Quest Board or Quest Log.
- Whether the Quest Details mobile view shows the quest description above the voting section or below — place it above.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within Phase 13 scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| HOME-01 | On mobile, the quest board displays a vertical scrollable card list instead of poster/parchment image cards | `Index.Mobile.cshtml` iterates `IEnumerable<Quest>` model; Bootstrap card list replaces `.fantasy-quest-card` poster divs |
| HOME-02 | Each quest entry shows: title, challenge rating, DM name, and status (Open / Finalized / Done); finalized entries include the date | `quest.Title`, `quest.ChallengeRating`, `quest.DungeonMaster?.Name`, `quest.IsFinalized`, `quest.FinalizedDate` — all available on `Quest` model |
| HOME-03 | Each quest entry is tap-navigable to Quest Details, or to Quest Manage for the signed-in DM's own quests | `onclick="window.location.href='...'"`  pattern with `ViewBag.CurrentUserName` vs `quest.DungeonMaster?.Name` check — exact same logic as desktop |
| HOME-04 | If the current user is already signed up for a quest, the entry shows a visual indicator (badge or icon — no wax seal imagery) | `ViewBag.CurrentUserId as int?` + `quest.PlayerSignups.Any(ps => ps.Player.Id == currentUserId.Value)` — exact pattern from desktop |
| QVIEW-01 | Quest Details on mobile shows the quest description and date voting buttons (Yes / No / Maybe) as large tap-friendly controls (minimum 44px) | `@inject` not needed in the view (globally available via `_ViewImports.cshtml`); `tokens.RequestToken` used in fetch calls; voting JS from desktop |
| QVIEW-02 | The participant table on Quest Details is replaced with a stacked list on mobile (player name + character name + role, one item per row) | Desktop table uses `allSelectedParticipants` list of `PlayerSignup`; mobile replaces `<table>` with stacked `<div>` rows |
| QVIEW-03 | Quest Log index on mobile displays past quests as a scannable list with title, date, and DM name | `QuestLogIndexViewModel.CompletedQuests` (type `IEnumerable<Quest>`); `quest.FinalizedDate`, `quest.DungeonMaster?.Name`, `Url.Action("Details", "QuestLog", ...)` |
</phase_requirements>

---

## Summary

Phase 13 creates three `.Mobile.cshtml` view files alongside their existing desktop counterparts. The Phase 12 `MobileViewLocationExpander` will automatically serve the mobile variants when `HttpContext.Items["IsMobile"]` is true — no controller, ViewModel, repository, or domain service changes are needed.

All three views share the same model and ViewBag data as their desktop counterparts. The primary research work confirms the exact data bindings, ViewBag field names/types, AJAX patterns, and the one non-obvious gotcha: `_ViewImports.cshtml` globally injects `IAuthorizationService`, `IUserService`, AND `IAntiforgery` — mobile views must NOT re-inject these, but CAN use `Antiforgery` directly (it is the globally-injected instance). The desktop `Quest/Details.cshtml` has a redundant `@inject Antiforgery` line — do not copy that line into the mobile variant.

Each mobile view loads one per-page CSS file via `@section Styles`. The `_Layout.Mobile.cshtml` already declares `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` at lines 164–165.

**Primary recommendation:** Copy the model/ViewBag access patterns verbatim from the desktop views; replace only the markup structure with mobile-friendly equivalents. Never add new `@inject` directives for services already in `_ViewImports.cshtml`.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Quest card list rendering | Frontend (Razor view) | — | Purely presentation; model already populated by controller |
| Signup detection badge | Frontend (Razor view) | — | `ViewBag.CurrentUserId` + `quest.PlayerSignups` LINQ — evaluated server-side in the view |
| Card tap navigation | Browser (onclick JS) | — | `onclick="window.location.href='...'"` client-side URL redirect |
| AJAX vote actions | Browser (fetch JS) | API (Quest controller) | JS sends DELETE/POST fetch; controller handles persistence |
| Antiforgery token | Frontend (Razor) | — | `tokens.RequestToken` rendered into the view; JS reads from rendered value |
| Participant list | Frontend (Razor view) | — | `allSelectedParticipants` list available on `PlayerSignup` model |
| Status badge logic | Frontend (Razor view) | — | Pure computed display: `quest.IsFinalized` + `quest.FinalizedDate` comparison |
| CSS loading | Frontend (Razor `@section Styles`) | — | Each view pushes its CSS into `_Layout.Mobile.cshtml`'s Styles section |

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5.3.0 | CDN | Badge, card, flex utilities | Already loaded by `_Layout.Mobile.cshtml`; no new install |
| Font Awesome 6.4.0 | CDN | Icons in badges/cards | Already loaded by `_Layout.Mobile.cshtml`; no new install |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ASP.NET Core Tag Helpers | SDK built-in | `asp-append-version`, `asp-action`, etc. | Already registered via `_ViewImports.cshtml` `@addTagHelper` |

### Alternatives Considered
None — this phase is additive Razor markup only; no library decisions needed.

**Installation:**
```bash
# No new packages — all dependencies are already present
```

---

## Architecture Patterns

### System Architecture Diagram

```
Mobile Request (iPhone UA)
        |
        v
MobileDetectionMiddleware
  sets HttpContext.Items["IsMobile"] = true
        |
        v
MobileViewLocationExpander.PopulateValues
  reads HttpContext.Items["IsMobile"]
        |
        v
_ViewStart.cshtml
  Layout = "~/Views/Shared/_Layout.Mobile.cshtml"
        |
        v
View Engine: checks Home/Index.Mobile.cshtml first
  → FOUND (after Phase 13) → renders mobile view
  → NOT FOUND → falls back to Home/Index.cshtml inside mobile layout
        |
        v
_Layout.Mobile.cshtml
  loads mobile.css (baseline)
  @await RenderSectionAsync("Styles") ← page-specific CSS injected here
  @RenderBody()                        ← mobile view content renders here
  @await RenderSectionAsync("Scripts") ← page-specific JS injected here
```

### Recommended Project Structure
```
EuphoriaInn.Service/
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml                   (existing — DO NOT MODIFY)
│   │   └── Index.Mobile.cshtml            (NEW — Phase 13)
│   ├── Quest/
│   │   ├── Details.cshtml                 (existing — DO NOT MODIFY)
│   │   └── Details.Mobile.cshtml          (NEW — Phase 13)
│   └── QuestLog/
│       ├── Index.cshtml                   (existing — DO NOT MODIFY)
│       └── Index.Mobile.cshtml            (NEW — Phase 13)
└── wwwroot/css/
    ├── mobile.css                         (existing baseline — DO NOT MODIFY)
    ├── home.mobile.css                    (NEW — Phase 13)
    ├── quests.mobile.css                  (NEW — Phase 13)
    └── quest-log.mobile.css               (NEW — Phase 13)
```

Also:
```
EuphoriaInn.IntegrationTests/
└── Mobile/
    └── MobileViewsTests.cs                (NEW — Phase 13, extends existing Mobile/ folder)
```

---

## Data Binding Inventory

This section answers all research questions about model types, ViewBag fields, and data structures needed by each mobile view.

### Home/Index.Mobile.cshtml

**Model type:** `IEnumerable<Quest>` [VERIFIED: HomeController.cs line 43]

**ViewBag fields set by HomeController.Index():**

| Field | Type | Value | Line |
|-------|------|-------|------|
| `ViewBag.CurrentUserName` | `string?` | Authenticated user's name, or `null` if anonymous | 41 |
| `ViewBag.CurrentUserId` | `int?` | Authenticated user's Id, or `null` if anonymous | 42 |

**Key data access patterns (from desktop view, verified):**

```cshtml
@* Cast ViewBag fields — both can be null for anonymous users *@
var currentUserId = ViewBag.CurrentUserId as int?;
var currentUserName = ViewBag.CurrentUserName as string;

@* Signup check — safe even if currentUserId is null *@
var isUserSignedUp = currentUserId.HasValue
    && quest.PlayerSignups.Any(ps => ps.Player.Id == currentUserId.Value);

@* Navigation — tap to Manage if own quest, else Details *@
var navUrl = currentUserName != null && currentUserName == quest.DungeonMaster?.Name
    ? Url.Action("Manage", "Quest", new { id = quest.Id })
    : Url.Action("Details", "Quest", new { id = quest.Id });
```

**Quest model fields used for cards:**

| Field | Type | Purpose |
|-------|------|---------|
| `quest.Id` | `int` | URL parameter |
| `quest.Title` | `string` | Card heading |
| `quest.ChallengeRating` | `int` | CR badge |
| `quest.DungeonMaster?.Name` | `string?` | DM row |
| `quest.IsFinalized` | `bool` | Status badge logic |
| `quest.FinalizedDate` | `DateTime?` | Shown on finalized cards (D-09 pattern) |
| `quest.PlayerSignups` | `IList<PlayerSignup>` | Signup count + signup check |

**Status badge logic (copied from Quest/Details.cshtml lines 634–660, verified):**

```cshtml
@{
    string statusBadge;
    string statusIcon;
    string statusText;

    if (quest.IsFinalized && quest.FinalizedDate.HasValue
        && quest.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date)
    {
        statusBadge = "bg-secondary";   // D-05: past/done = gray
        statusIcon = "fas fa-flag-checkered";
        statusText = "Done";
    }
    else if (quest.IsFinalized)
    {
        statusBadge = "bg-primary";     // D-05: Finalized = blue
        statusIcon = "fas fa-check-circle";
        statusText = "Finalized";
    }
    else
    {
        statusBadge = "bg-success";     // D-05: Open = green
        statusIcon = "fas fa-clock";
        statusText = "Open";
    }
}
<span class="badge @statusBadge"><i class="@statusIcon me-1"></i>@statusText</span>
```

Note: Desktop uses `bg-dark` for Done; D-05 maps Done to gray (`bg-secondary`) — use `bg-secondary` in the mobile view.

---

### Quest/Details.Mobile.cshtml

**Model type:** `PlayerSignup` [VERIFIED: Details.cshtml line 6]

**ViewBag fields set by QuestController.Details():**

| Field | Type | Value | Line |
|-------|------|-------|------|
| `ViewBag.IsPlayerSignedUp` | `bool` | Whether current user is signed up | 238 |
| `ViewBag.UserCharacters` | `List<Character>` | Current user's characters | 239 |
| `ViewBag.CanManage` | `bool` | Whether current user can manage this quest (DM or Admin) | 244 |
| `ViewBag.CalendarMonths` | `List<CalendarViewModel>` | Calendar data for proposed date voting | 262 |
| `ViewBag.IsDetailsPage` | `bool` | Always `true` on Details page | 263 |
| `ViewBag.CurrentQuestId` | `int` | Quest ID (same as id route param) | 264 |
| `ViewBag.CurrentUserId` | `int?` | Current user's Id | 265 |

**Antiforgery token pattern — CRITICAL FINDING:**

`_ViewImports.cshtml` globally injects `IAntiforgery` as `Antiforgery` (line 16). The desktop `Details.cshtml` has a redundant `@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery` at line 5 — this is a shadow of the global injection.

The mobile view MUST use the globally-injected `Antiforgery` instance and MUST NOT add another `@inject` line:

```cshtml
@* CORRECT — no @inject needed, Antiforgery is globally injected by _ViewImports.cshtml *@
@{
    ViewData["Title"] = Model.Quest?.Title;
    ViewData["BodyClass"] = "quest-details-page";
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}
```

**Voting JS functions — complete verified source (Details.cshtml lines 861–880):**

```javascript
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
```

The `revokeSignup` function follows the same fetch pattern with method `DELETE` to `/Quest/RevokeSignup/${questId}`.

**AJAX pattern summary:**
- Endpoint: `/Quest/ChangeVoteToYes/{questId}` — POST with form body containing `__RequestVerificationToken`
- Endpoint: `/Quest/RevokeSignup/{questId}` — DELETE with form body containing `__RequestVerificationToken`
- Token rendered inline: `'@tokens.RequestToken'` (Razor server-side interpolation into JS)
- On success: `location.reload()` — no SPA update, full page reload

**Participant list data structure (for QVIEW-02 stacked list):**

Desktop builds these collections (Details.cshtml lines 58–66):

```csharp
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
```

Each `PlayerSignup` in the list exposes:
- `participant.Player.Name` — player display name
- `participant.Character?.Name` — optional character name (null if no character linked)
- `participant.Role` — `SignupRole` enum: `Player`, `AssistantDM`, `Spectator`
- `participant.Player.Id` — for `isCurrentUser` check vs `ViewBag.CurrentUserId`

Mobile stacked list per QVIEW-02 — each row:
```
[Player Name] · [Character Name or "No character"] · [Role badge]
```

**Scope note — what to include in the mobile Details view:**

The desktop Details view is complex (924 lines) covering many states. The mobile view only needs to cover what QVIEW-01 and QVIEW-02 require:

1. Quest description (shown above voting per Claude's discretion)
2. Quest status/summary (DM name, status badge)
3. Date voting section (for users not yet signed up, and for signed-up users updating)
4. Participant list as stacked single-column (QVIEW-02) — when quest is finalized
5. Revoke/Update controls for signed-up users

Features that can be left to desktop (not required by QVIEW-01/02):
- The character avatar thumbnails in participant rows (optional on mobile)
- The Add Character modal
- The DM Controls sidebar panel (the mobile layout is single-column anyway)

---

### QuestLog/Index.Mobile.cshtml

**Model type:** `QuestLogIndexViewModel` [VERIFIED: QuestLog/Index.cshtml line 2]

`QuestLogIndexViewModel` is defined in `EuphoriaInn.Service.ViewModels.QuestLogViewModels`:
```csharp
public class QuestLogIndexViewModel
{
    public IEnumerable<Quest> CompletedQuests { get; set; } = [];
}
```
[VERIFIED: QuestLogIndexViewModel.cs]

**ViewBag fields set by QuestLogController.Index():**

| Field | Type | Value | Line |
|-------|------|-------|------|
| `ViewBag.CanEditRecap` | `bool` | DM or Admin can edit the recap | 56 |

The mobile view (QVIEW-03) does not display recap editing — `ViewBag.CanEditRecap` can be ignored.

**Navigation URL pattern (verified from desktop, QuestLog/Index.cshtml line 22):**
```cshtml
onclick="window.location.href='@Url.Action("Details", "QuestLog", new { id = quest.Id })'"
```

**Quest fields used on the mobile list (QVIEW-03):**

| Field | Type | Purpose |
|-------|------|---------|
| `quest.Id` | `int` | Navigation URL |
| `quest.Title` | `string` | List item heading |
| `quest.FinalizedDate` | `DateTime?` | Displayed as "Completed: MMM dd, yyyy" |
| `quest.DungeonMaster?.Name` | `string?` | DM name row |
| `quest.ChallengeRating` | `int` | Optional CR badge (Claude's discretion) |

**Namespace/using required** (not in `_ViewImports.cshtml`, must be added to mobile view):
```cshtml
@using EuphoriaInn.Service.ViewModels.QuestLogViewModels
@model QuestLogIndexViewModel
```

**Empty state** — desktop uses `.empty-quest-log` div (lines 80–85). Mobile view should have an equivalent empty state (Claude's discretion on markup/styling).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Status badge HTML | Custom CSS classes | Bootstrap `badge bg-success/bg-primary/bg-secondary` | Already in `_Layout.Mobile.cshtml`'s Bootstrap CDN |
| Tap navigation | Custom JS listener | `onclick="window.location.href='...'"` | Established codebase pattern (desktop does this) |
| CSRF token in JS | Manual cookie reading | `'@tokens.RequestToken'` Razor interpolation into `<script>` | Established codebase pattern (desktop does this) |
| Per-page CSS loading | `<link>` in `<head>` directly | `@section Styles { <link href="~/css/..." asp-append-version="true" rel="stylesheet" /> }` | `_Layout.Mobile.cshtml` declares the Styles section; this is the only correct loading point |

---

## Common Pitfalls

### Pitfall 1: Adding @inject for services already in _ViewImports.cshtml
**What goes wrong:** View throws "Duplicate injection" compile error, or silently shadows the global injection with a different instance scope.
**Why it happens:** Desktop `Quest/Details.cshtml` has a redundant `@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery` at line 5 that the mobile view author may copy.
**How to avoid:** Do NOT copy the `@inject` line. `_ViewImports.cshtml` already injects `Antiforgery`, `AuthorizationService`, and `UserService` globally. The mobile view can use all three without any `@inject` declaration.
**Warning signs:** If you add `@inject`, the Razor compiler will either error (duplicate identifier) or compile silently — test the page renders before assuming correctness.

### Pitfall 2: Setting Layout in a mobile view
**What goes wrong:** The mobile view overrides `_ViewStart.cshtml`'s layout selection, either forcing the wrong layout or breaking the mobile detection logic.
**Why it happens:** Copy-paste from layout examples that include `Layout = ...` in the view's `@{ }` block.
**How to avoid:** Never set `Layout` in any `.Mobile.cshtml` view. `_ViewStart.cshtml` handles layout selection for all views.
**Warning signs:** `_ViewStart.cshtml` currently contains `Layout = isMobile ? "~/Views/Shared/_Layout.Mobile.cshtml" : "_Layout"` — any individual view Layout assignment would override this.

### Pitfall 3: Loading desktop CSS in per-page mobile CSS files
**What goes wrong:** Mobile views inherit desktop styles (floats, fixed widths, poster imagery, grid layouts) that break on small screens.
**Why it happens:** Temptation to `@import` or reference `site.css`, `quests.css`, etc. for convenience.
**How to avoid:** Per-page CSS files (`home.mobile.css`, etc.) must contain only mobile-specific rules. `mobile.css` is the only baseline — no desktop CSS files should be referenced.

### Pitfall 4: Forgetting @section Styles placement
**What goes wrong:** Per-page CSS link tag is placed as raw HTML in the view body, rendering it inside `<main>` rather than in `<head>`.
**Why it happens:** Omitting the `@section Styles { }` wrapper.
**How to avoid:** Always wrap the per-page CSS link in `@section Styles { ... }`. The `_Layout.Mobile.cshtml` renders this section at line 164 (inside `<head>` context — actually after `</body>` open but before `</head>` close; see the actual layout file to confirm exact placement).
**Actual placement:** Lines 164–165 of `_Layout.Mobile.cshtml` show `@await RenderSectionAsync("Styles", required: false)` is placed AFTER the `<script>` tags, near `</body>`. This is fine — modern browsers handle late-loaded stylesheets.

### Pitfall 5: Using quest.IsFinalized alone for "Done" status
**What goes wrong:** Past quests (date already elapsed) show "Finalized" instead of "Done", confusing players.
**Why it happens:** Only checking `quest.IsFinalized` without the date comparison.
**How to avoid:** Use the three-way status check (verified from desktop Details.cshtml):
- `IsFinalized && FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date` → Done (`bg-secondary`)
- `IsFinalized` (no date, or future date) → Finalized (`bg-primary`)
- Neither → Open (`bg-success`)

### Pitfall 6: ViewBag.CurrentUserId is int?, not int
**What goes wrong:** NullReferenceException on anonymous user requests, or runtime type mismatch.
**Why it happens:** Casting `ViewBag.CurrentUserId` directly to `int` instead of `int?`.
**How to avoid:** Always: `var currentUserId = ViewBag.CurrentUserId as int?;` then guard with `currentUserId.HasValue` before use.

### Pitfall 7: Quest/Details.cshtml is 924 lines — do not copy blindly
**What goes wrong:** The mobile Details view becomes as complex as the desktop view, undermining the mobile-simplicity goal.
**Why it happens:** Temptation to copy the entire desktop view and "just replace the table".
**How to avoid:** Only implement what QVIEW-01 and QVIEW-02 require. The complex "Join Finalized Quest" flow, character modal, and update-signup calendar are secondary UX — the mobile view must cover them but can simplify (e.g., the character selection form can be a simple select, not the full finalized-quest join panel).

---

## Code Examples

### Pattern 1: Per-page CSS loading in a mobile view
```cshtml
@* VERIFIED: _Layout.Mobile.cshtml lines 164-165 confirm Styles section is declared *@
@section Styles {
    <link href="~/css/home.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

### Pattern 2: Quest card tap navigation (Home)
```cshtml
@* VERIFIED: Home/Index.cshtml line 80 *@
<div class="quest-card"
     onclick="window.location.href='@(ViewBag.CurrentUserName != null && ViewBag.CurrentUserName == quest.DungeonMaster?.Name ? Url.Action("Manage", "Quest", new { id = quest.Id }) : Url.Action("Details", "Quest", new { id = quest.Id }))'">
```

### Pattern 3: Signed-up badge (Home, D-06)
```cshtml
@* Bootstrap badge — no wax seal imagery per D-06 *@
@if (isUserSignedUp)
{
    <span class="badge bg-success position-absolute top-0 end-0 m-2">
        <i class="fas fa-check me-1"></i>Signed up
    </span>
}
```

### Pattern 4: Antiforgery token in mobile Details view
```cshtml
@* CORRECT — Antiforgery is globally injected by _ViewImports.cshtml; no @inject needed *@
@{
    ViewData["Title"] = Model.Quest?.Title;
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}

<script>
    function revokeSignup(questId) {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', '@tokens.RequestToken');
        fetch(`/Quest/RevokeSignup/${questId}`, { method: "DELETE", body: formData })
            .then(res => { if (res.ok) location.reload(); });
    }
</script>
```

### Pattern 5: Stacked participant list (QVIEW-02)
```cshtml
@* Replaces <table class="table table-responsive"> from desktop *@
@foreach (var participant in allSelectedParticipants)
{
    <div class="participant-row d-flex justify-content-between align-items-center py-2 border-bottom">
        <div>
            <span class="fw-bold">@participant.Player.Name</span>
            <br>
            <small class="text-muted">
                @(participant.Character?.Name ?? "No character")
            </small>
        </div>
        <span class="badge @roleBadge">@roleText</span>
    </div>
}
```

### Pattern 6: QuestLog mobile list item (QVIEW-03)
```cshtml
@* Model: QuestLogIndexViewModel; quest is EuphoriaInn.Domain.Models.QuestBoard.Quest *@
@foreach (var quest in Model.CompletedQuests)
{
    <div class="quest-log-list-item"
         onclick="window.location.href='@Url.Action("Details", "QuestLog", new { id = quest.Id })'">
        <div class="d-flex justify-content-between align-items-start">
            <h6 class="mb-1">@quest.Title</h6>
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

---

## CSS Architecture for Per-Page Files

### mobile.css baseline (DO NOT MODIFY)
```css
/* Touch target sizing — INFRA-06 */
.btn, a.nav-link, input, select, textarea, .form-control, .form-select {
    min-height: 44px;
}
body { font-size: 16px; line-height: 1.5; }
.container-fluid { padding-left: 0.5rem; padding-right: 0.5rem; }
.navbar-brand { font-family: 'Cinzel', serif; font-weight: 600; }
.mobile-layout { background-color: #212529; }
.mobile-layout .navbar { min-height: 56px; }
```

### home.mobile.css (new — Phase 13)
Must provide:
- Quest card styles: `min-height` (sufficient for the card content), padding, border-radius, background
- Card text sizing: title (`font-size`), meta fields (smaller)
- Status badge positioning (inline within card)
- Signed-up badge: already handled by Bootstrap `position-absolute top-0 end-0 m-2` if the card has `position: relative`
- Gap between cards: `margin-bottom`

Pattern from `mobile.css` to follow: plain CSS, no `@media` query, no `@import`.

### quests.mobile.css (new — Phase 13)
Must provide:
- Vote button min-height override if `mobile.css` `.btn { min-height: 44px }` is not sufficient for stacked layout
- Participant stacked list row padding
- Quest description text sizing

### quest-log.mobile.css (new — Phase 13)
Must provide:
- List item styles: padding, border-bottom separator
- Text sizing for title, meta fields

---

## _Layout.Mobile.cshtml — Exact Section Declarations

The layout was read in full (168 lines). The critical sections:

```
Line 164: @await RenderSectionAsync("Styles", required: false)
Line 165: @await RenderSectionAsync("Scripts", required: false)
```

Both are placed AFTER the `<script>` tags for jQuery, Bootstrap, and site.js (lines 161–163), inside the `<body>` just before `</body>`. This means per-page CSS will load after body opens — late but functional for mobile views.

The layout loads only:
- Bootstrap 5.3.0 CSS CDN (line 8)
- Font Awesome 6.4.0 CDN (line 9)
- Google Fonts Cinzel CDN (line 10)
- `~/css/mobile.css` with `asp-append-version="true"` (line 11)

No `@inject` directives are in `_Layout.Mobile.cshtml` — all services come from `_ViewImports.cshtml`.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 |
| Config file | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~Mobile"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HOME-01 | Mobile GET `/` renders card list, not poster images | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 |
| HOME-02 | Quest card contains title, CR, DM name, status badge | Integration | same | ❌ Wave 0 |
| HOME-03 | Card tap URL is correct (Details vs Manage) | Integration | same | ❌ Wave 0 |
| HOME-04 | Signed-up quest shows indicator badge | Integration | same | ❌ Wave 0 |
| QVIEW-01 | Quest Details mobile shows description + vote buttons ≥ 44px | Integration + CSS file | same | ❌ Wave 0 |
| QVIEW-02 | Participant table replaced by stacked list | Integration | same | ❌ Wave 0 |
| QVIEW-03 | Quest Log mobile shows list with title, date, DM name | Integration | same | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~Mobile"`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — covers HOME-01 through QVIEW-03

**Wave 0 test template** (based on existing `MobileLayoutTests.cs` pattern):

```csharp
namespace EuphoriaInn.IntegrationTests.Mobile;

public class MobileViewsTests : IClassFixture<WebApplicationFactoryBase>
{
    private const string MobileUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
    private const string DesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly HttpClient _client;

    public MobileViewsTests(WebApplicationFactoryBase factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(HttpResponseMessage Response, string Html)> GetWithUserAgentAsync(
        string url, string userAgent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response, html);
    }

    // HOME-01: card list, not poster images
    [Fact]
    public async Task MobileHome_MobileUserAgent_DoesNotRenderPosterImages()
    {
        var (response, html) = await GetWithUserAgentAsync("/", MobileUserAgent);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        html.Should().NotContain("fantasy-quest-card");   // desktop poster class
        html.Should().NotContain("Blanks w Shadow");      // desktop poster image path
    }
    // ... additional tests per requirement
}
```

**Constraint on integration tests for authenticated pages:** The `WebApplicationFactoryBase` uses a `TestAuthHandler` that can authenticate requests. For HOME-04 (signed-up badge) and QVIEW-02 (participant list), tests may need seeded quest + signup data via `factory.Database`. Follow the pattern from `HomeControllerIntegrationTests.cs` for seeding.

---

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — phase creates Razor views and CSS files only)

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Views are read-only display; auth state is passed via `User.Identity` already managed by Phase 12 layout |
| V3 Session Management | no | No session changes |
| V4 Access Control | no | No new authorization policies; views use same guards as desktop |
| V5 Input Validation | no | No new form inputs (voting uses existing AJAX to existing controller endpoints) |
| V6 Cryptography | no | Antiforgery handled by globally-injected `IAntiforgery`; no new crypto |

**Security note:** The antiforgery token is rendered server-side into `<script>` blocks as `'@tokens.RequestToken'`. This is the established codebase pattern (not a new risk introduced by Phase 13). No new attack surfaces are introduced — all form submissions and AJAX calls go to existing controller endpoints.

---

## Open Questions

1. **The Details.cshtml voting section uses `_Calendar` partial for date voting**
   - What we know: The signup flow (`ViewBag.CalendarMonths` + `@await Html.PartialAsync("_Calendar", calendarMonth)`) is complex. Phase 13 QVIEW-01 requires "date voting buttons (Yes / No / Maybe) as large tap-friendly controls".
   - What's unclear: The `_Calendar` partial is a full month-grid calendar with clickable day cells — not simple Yes/No/Maybe buttons. Whether the mobile view should reuse `_Calendar` partial (which may overflow on mobile) or replace it with a simpler date list is not fully specified by QVIEW-01.
   - Recommendation: Reuse the `_Calendar` partial initially (it renders inside the mobile layout and will at least be functional), but note in the plan that Calendar mobile adaptation is Phase 14 — the calendar partial may render poorly on mobile until then. QVIEW-01 requires voting buttons to be ≥ 44px, which is satisfied by `mobile.css` `.btn { min-height: 44px }` without any calendar changes.

2. **Details.Mobile.cshtml scope — finalized quest "Join" flow**
   - What we know: The desktop Details view has a full "Join Finalized Quest" form with character selection and role selection. QVIEW-01 mentions voting buttons but not this form.
   - What's unclear: Whether the mobile Details view should include the full join-finalized flow.
   - Recommendation: Include it — it is part of the quest details page. Simplify to a single-column layout; the existing Bootstrap `d-grid gap-2` pattern works on mobile without changes.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `bg-secondary` is the correct Bootstrap class for "Done" status on mobile (desktop uses `bg-dark`) | Data Binding Inventory — Status badge logic | Visual inconsistency; low risk (D-05 says "gray", and `bg-secondary` is gray in Bootstrap 5) |
| A2 | The `_Calendar` partial renders inside mobile layout without critical overflow for QVIEW-01 | Open Questions #1 | Voting buttons may be too small on a constrained screen until Phase 14 fixes the calendar |

---

## Sources

### Primary (HIGH confidence)
- `EuphoriaInn.Service/Views/Home/Index.cshtml` — full file read; all ViewBag fields and model bindings verified
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` — full file read; ViewBag fields, Antiforgery pattern, JS functions, participant list structure verified
- `EuphoriaInn.Service/Views/QuestLog/Index.cshtml` — full file read; model type, navigation pattern verified
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — full file read; Styles/Scripts section declarations at lines 164–165 verified
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — full file read; baseline CSS patterns verified
- `EuphoriaInn.Service/Views/_ViewImports.cshtml` — full file read; global `@inject` for `Antiforgery`, `AuthorizationService`, `UserService` verified
- `EuphoriaInn.Service/Views/_ViewStart.cshtml` — full file read; layout selection pattern verified
- `EuphoriaInn.Service/Controllers/QuestBoard/HomeController.cs` — full file read; ViewBag fields verified
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — ViewBag grep; all Details ViewBag fields verified
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestLogController.cs` — ViewBag grep verified
- `EuphoriaInn.Service/ViewModels/QuestLogViewModels/QuestLogIndexViewModel.cs` — full file read; model type verified
- `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` — full file read; model fields verified
- `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs` — full file read; test patterns verified
- `EuphoriaInn.IntegrationTests/Mobile/MobileCssTests.cs` — full file read; CSS test patterns verified
- `EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs` — full file read; unit test patterns verified
- `EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs` — full file read; expander test patterns verified
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` — full file read; test factory setup verified
- `.planning/phases/12-mobile-infrastructure/12-PATTERNS.md` — full file read; established patterns from Phase 12 verified
- `.planning/STATE.md` — full file read; Plan 02 decision (no @inject in mobile layout, _ViewImports handles it) verified

### Secondary (MEDIUM confidence)
None needed — all findings verified from codebase directly.

---

## Metadata

**Confidence breakdown:**
- Data bindings (ViewBag, model types): HIGH — verified by reading all source controllers and views
- Antiforgery pattern: HIGH — verified `_ViewImports.cshtml` global injection; cross-referenced with STATE.md Plan 02 decision
- Test patterns: HIGH — existing Mobile test files read and verified
- CSS architecture: HIGH — `mobile.css` and `_Layout.Mobile.cshtml` read in full
- QVIEW-01 calendar scope: MEDIUM — calendar partial behaviour on mobile not fully tested (Phase 14 concern)

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (30 days — stable codebase, no external dependencies)
