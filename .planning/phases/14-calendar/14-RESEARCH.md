# Phase 14: Calendar - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core 8 MVC Razor partial views — mobile calendar agenda and date-voting UI
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Single header row: `[<] JUNE 2026 [>]` — Bootstrap chevron buttons flanking the centered month name, all full-width, 44px minimum touch targets on both chevron buttons.
- **D-02:** Month-name heading uses the same `Model.MonthName` value from `CalendarViewModel` as the desktop view.
- **D-03:** Horizontal three-button row per date: `[✓ Yes] [✗ No] [? Maybe]` — three equal-width buttons in one row, icon + short text label, 44px minimum height. Applied to both initial-vote and update-vote flows.
- **D-04:** The vote buttons render per proposed date entry (not quest-level). Each date shows the day label + quest name + time, followed by the `[Yes] [No] [Maybe]` button row.
- **D-05:** Current vote state is shown by highlighting the active button (pre-checked radio button for update flow).
- **D-06:** The "Update Your Vote" AJAX block in `Details.Mobile.cshtml` (lines ~79-97) is removed and replaced with `@await Html.PartialAsync("_Calendar", calendarMonth)` — the same call used in the "Choose a Date" section. The MobileViewLocationExpander auto-serves `_Calendar.Mobile.cshtml` on mobile.
- **D-07:** `_Calendar.Mobile.cshtml` must handle both vote flows from `_Calendar.cshtml`: initial vote (non-signed-up player) using `VoteIndexLookup` from ViewBag, and update vote (signed-up player) using `UpdateVoteIndexLookup` from ViewData.
- **D-08:** The "Update Your Vote" section heading in `Details.Mobile.cshtml` stays as-is.
- **D-09:** Only days with at least one quest render (CAL-03). Empty days are skipped entirely.
- **D-10:** Each agenda entry format: day label row (e.g. `SATURDAY, JUNE 14` uppercase) followed by quest entries below it. Each quest entry shows quest name + time.
- **D-11:** Tapping any quest entry navigates to `Quest/Details/{id}` (CAL-04).
- **D-12:** Finalized/proposed status shown via a small colored indicator dot (left border) on the quest entry.
- **D-13:** No legend on mobile.
- **D-14:** New per-page CSS file: `calendar.mobile.css`. Loaded via `@section Styles` in `Calendar/Index.Mobile.cshtml`. `_Calendar.Mobile.cshtml` partial CSS rules go in the host view's CSS file (`quests.mobile.css`).
- **D-15:** `mobile.css` baseline rules (44px touch targets) apply to vote buttons. `calendar.mobile.css` provides agenda-specific layout rules only.

### Claude's Discretion

- Exact Bootstrap utility classes for the day-label section header (uppercase, muted text, letter-spacing).
- Whether the warning icon (no building key) appears on the agenda quest entry or is omitted for simplicity.
- The heading text inside the `Details.Mobile.cshtml` "Update Your Vote" wrapper section — keep existing heading or adjust.
- Empty-state for a month with no quests: show a "No quests this month" message between the nav row and the bottom of the page.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within Phase 14 scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CAL-01 | The calendar on mobile shows an agenda/list view instead of the 7-column day grid | `Calendar/Index.Mobile.cshtml` new file; `CalendarViewModel.GetCalendarDays()` filtered to `!day.IsEmpty && day.QuestsOnDay.Any()` |
| CAL-02 | Each agenda entry shows: day label (e.g. SATURDAY, JUNE 14), quest name, and time | `day.Date.ToString("dddd, MMMM d").ToUpper()` produces the exact format; `questOnDay.ProposedDate.Date.ToString("HH:mm")` for time |
| CAL-03 | Days with no quests are skipped entirely | LINQ `.Where(d => !d.IsEmpty && d.QuestsOnDay.Any())` — no empty day rows rendered |
| CAL-04 | Tapping a quest entry in the agenda navigates to Quest Details for that quest | `onclick="window.location.href='@Url.Action("Details", "Quest", new { id = questOnDay.Quest.Id })'"` — established codebase pattern |
| CAL-05 (add to REQUIREMENTS.md) | The `_Calendar` partial used inside Quest Details renders as a vertical per-date list with tap-friendly Yes/No/Maybe vote buttons — replacing both the broken desktop grid (Choose a Date) and the Phase 13 simplified quest-level buttons (Update Your Vote) | `Views/Shared/_Calendar.Mobile.cshtml` new file; `Details.Mobile.cshtml` Update Your Vote block (lines 79-97) replaced with partial call |
</phase_requirements>

---

## Summary

Phase 14 is a pure view-layer addition: two new `.Mobile.cshtml` files, one new CSS file, one CSS addition, and one line change in an existing mobile view. No controllers, ViewModels, repositories, or domain services change. All business logic, data models, and URL patterns already exist.

The two new views (`Calendar/Index.Mobile.cshtml` and `Views/Shared/_Calendar.Mobile.cshtml`) are mobile adaptations of their desktop counterparts (`Calendar/Index.cshtml` and `Views/Shared/_Calendar.cshtml`). The desktop partial is a complex 7-column CSS grid that overflows phone screens; the mobile adaptation replaces it with a vertical agenda list and a per-date vote button row. The MobileViewLocationExpander (delivered in Phase 12) routes mobile requests to `.Mobile.cshtml` files automatically — no controller changes are needed.

The single most important complexity in this phase is `_Calendar.Mobile.cshtml`. It must handle two distinct vote flows (initial vote for non-signed-up players, and update-vote for already-signed-up players) by reading the same ViewBag/ViewData keys that the desktop `_Calendar.cshtml` already reads (`VoteIndexLookup`, `UpdateVoteIndexLookup`, `IsPlayerSignedUp`, `IsDetailsPage`, `CurrentQuestId`, `CurrentUserId`). The existing `Details.cshtml` sets `UpdateVoteIndexLookup` via `ViewData["UpdateVoteIndexLookup"]` inside an inline code block before calling the partial — this same pattern will work for the mobile partial without any controller changes. The `Details.Mobile.cshtml` "Update Your Vote" block currently uses AJAX (lines 79-97) and must be replaced with the same `@await Html.PartialAsync("_Calendar", calendarMonth)` call already present in the "Choose a Date" section.

**Primary recommendation:** Implement in four deliverables — (1) `calendar.mobile.css` new file, (2) `Calendar/Index.Mobile.cshtml` new view, (3) `Views/Shared/_Calendar.Mobile.cshtml` new partial, (4) replace lines 79-97 in `Details.Mobile.cshtml` plus add hidden form fields and `ViewData["UpdateVoteIndexLookup"]` injection. Add five integration tests to `MobileViewsTests.cs` (CAL-01 through CAL-05 mapping). Add CAL-05 to `REQUIREMENTS.md`.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Agenda list rendering (CAL-01 to CAL-04) | Frontend Server (MVC View) | — | Pure view-layer; model already provided by existing CalendarController.Index action |
| Vote button rendering (CAL-05 initial vote) | Frontend Server (MVC View) | — | Radio buttons inside existing form POST; VoteIndexLookup built in Details.cshtml Razor code block |
| Vote button rendering (CAL-05 update vote) | Frontend Server (MVC View) | — | UpdateVoteIndexLookup built in Details.cshtml Razor code block; partial inherits via ViewData |
| Status dot indicator (proposed/finalized) | Frontend Server (MVC View) | — | `questOnDay.IsFinalized` already computed by `CalendarViewModel.GetQuestsForDate()` |
| Month navigation | Frontend Server (MVC View) | — | Standard anchor links to existing CalendarController.Index(year, month) action |
| Mobile CSS delivery | CDN / Static | — | Served as static files under wwwroot/css/ with cache-busting |

---

## Standard Stack

### Core (all already in project)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core 8 MVC Razor | 8.0 | View templating | Project standard — all existing views use it |
| Bootstrap 5.3.0 | 5.3.0 (CDN) | `.btn-check` radio pattern, `d-flex`, `flex-fill`, `gap-2` | Already loaded by `_Layout.Mobile.cshtml` |
| Font Awesome 6.4.0 | 6.4.0 (CDN) | `fa-chevron-left/right`, `fa-check`, `fa-times`, `fa-question`, `fa-calendar-alt` | Already loaded by `_Layout.Mobile.cshtml` |
| Google Fonts Cinzel | CDN | Month name heading typography | Already loaded by `_Layout.Mobile.cshtml` |

### Supporting (testing)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit 2.5.3 | 2.5.3 | Test runner | All tests — existing framework |
| FluentAssertions 8.8.0 | 8.8.0 | Assertion library | `.Should().Contain()` patterns in test assertions |
| Microsoft.AspNetCore.Mvc.Testing 8.0.11 | 8.0.11 | Integration test host via WebApplicationFactoryBase | Mobile integration tests |

**No new NuGet packages or npm packages are required for this phase.** [VERIFIED: codebase grep]

---

## Architecture Patterns

### System Architecture Diagram

```
Mobile browser request
    |
    v
MobileDetectionMiddleware (Phase 12)
    |-- sets HttpContext.Items["IsMobile"] = true
    v
MvcRouting → CalendarController.Index(year, month)
    |-- returns View(calendarViewModel) with model
    v
MobileViewLocationExpander (Phase 12)
    |-- checks "Calendar/Index.Mobile.cshtml" before "Calendar/Index.cshtml"
    v
Calendar/Index.Mobile.cshtml
    |-- @section Styles → loads calendar.mobile.css
    |-- renders month nav bar (prev/next anchor links)
    |-- foreach day where !IsEmpty && QuestsOnDay.Any()
         |-- day label: day.Date.ToString("dddd, MMMM d").ToUpper()
         |-- foreach questOnDay
              |-- .agenda-quest-entry (onclick → /Quest/Details/{id})
              |-- title + time
              |-- left border: proposed (#ffc107) or finalized (#28a745)

Mobile browser request
    |
    v
QuestController.Details(id)
    |-- sets ViewBag.CalendarMonths (List<CalendarViewModel>)
    |-- sets ViewBag.IsDetailsPage = true
    |-- sets ViewBag.CurrentQuestId, ViewBag.IsPlayerSignedUp
    |-- returns View(playerSignup)
    v
Quest/Details.Mobile.cshtml
    |-- @section Styles → loads quests.mobile.css (existing)
    |-- "Choose a Date" section: foreach calendarMonth → PartialAsync("_Calendar", calendarMonth)
    |-- "Update Your Vote" section (D-06): foreach calendarMonth → PartialAsync("_Calendar", calendarMonth)
         |-- ViewData["UpdateVoteIndexLookup"] set before each partial call
    v
_Calendar.Mobile.cshtml (resolved by MobileViewLocationExpander)
    |-- reads ViewBag.IsDetailsPage, ViewBag.CurrentQuestId
    |-- reads ViewBag.IsPlayerSignedUp
    |-- foreach day → foreach questOnDay where isDetailsPage && questOnDay.Quest.Id == currentQuestId
         |-- renders date label
         |-- initial vote flow (not signed up): radio buttons from VoteIndexLookup
         |-- update vote flow (signed up): radio buttons from UpdateVoteIndexLookup with pre-checked state
```

### Recommended File Structure

```
EuphoriaInn.Service/
├── Views/
│   ├── Calendar/
│   │   └── Index.Mobile.cshtml        # NEW — agenda list (CAL-01 to CAL-04)
│   └── Shared/
│       └── _Calendar.Mobile.cshtml    # NEW — vote button partial (CAL-05)
│           (existing _Calendar.cshtml — NO CHANGES)
│   └── Quest/
│       └── Details.Mobile.cshtml      # MODIFY — replace lines 79-97
└── wwwroot/css/
    ├── calendar.mobile.css            # NEW — agenda layout rules
    └── quests.mobile.css              # MODIFY — append calendar partial rules
```

### Pattern 1: Mobile View Serving (MobileViewLocationExpander)

**What:** The Phase 12 `MobileViewLocationExpander.ExpandViewLocations()` prepends `Views/{1}/{0}.Mobile.cshtml` ahead of `Views/{1}/{0}.cshtml`. For shared partials, it prepends `Views/Shared/{0}.Mobile.cshtml` ahead of `Views/Shared/{0}.cshtml`.

**When to use:** Automatic — any `.Mobile.cshtml` file placed alongside an existing view is served on mobile requests with no registration or controller change.

**Key fact:** For `@await Html.PartialAsync("_Calendar", calendarMonth)`, the expander resolves `Views/Shared/_Calendar.Mobile.cshtml` first on mobile. This is why D-06 (replacing the AJAX block with the partial call) causes `_Calendar.Mobile.cshtml` to render automatically. [VERIFIED: codebase read — `MobileViewLocationExpanderTests.cs` confirms this behavior]

### Pattern 2: Per-Page CSS Loading

**What:** Mobile views push per-page CSS into the layout's `<head>` via Razor sections.

**When to use:** Every new `.Mobile.cshtml` view that needs page-specific styles.

```cshtml
@section Styles {
    <link href="~/css/calendar.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

`_Layout.Mobile.cshtml` line 12 declares `@await RenderSectionAsync("Styles", required: false)`. [VERIFIED: codebase read]

**Shared partials cannot push sections** — `_Calendar.Mobile.cshtml` gets its CSS from the host view's loaded stylesheet (`quests.mobile.css` when rendered from `Details.Mobile.cshtml`). [VERIFIED: 14-UI-SPEC.md CSS Architecture Contract]

### Pattern 3: VoteIndexLookup — How It Flows Into the Partial

**What:** `Details.cshtml` builds `VoteIndexLookup` (a `Dictionary<int proposedDateId, int formIndex>`) as a Razor code block and stores it in `ViewBag.VoteIndexLookup` before calling `@await Html.PartialAsync("_Calendar", calendarMonth)`. The partial then reads `ViewBag.VoteIndexLookup as Dictionary<int, int>` to find each proposed date's form array index.

**Identical behavior on mobile:** `Details.Mobile.cshtml` calls the same `@await Html.PartialAsync("_Calendar", calendarMonth)`. Since ViewBag is request-scoped (not view-scoped), `_Calendar.Mobile.cshtml` inherits the same ViewBag values already set by the controller and the parent view.

**Where the lookup is built in the current mobile view:** The existing `Details.Mobile.cshtml` does NOT set `VoteIndexLookup`. This means the "Choose a Date" section in `Details.Mobile.cshtml` currently passes `null` for `VoteIndexLookup` to `_Calendar.cshtml` — which renders the desktop grid with no votes visible. Phase 14 fixes this by replacing the desktop partial with `_Calendar.Mobile.cshtml` which will work with whatever `VoteIndexLookup` is set.

**CRITICAL DISCOVERY:** The mobile view needs to build the lookups the same way `Details.cshtml` does. Looking at `Details.cshtml` lines 447-461 and 544-571: `VoteIndexLookup` is built inside an inline Razor `@{}` block within the "Choose a Date" section, and `UpdateVoteIndexLookup` is built inside an inline Razor `@{}` block within the "Update Your Vote" section — both before calling the partial. The mobile view's replacement section must include the same hidden field setup and lookup building. [VERIFIED: codebase read of `Details.cshtml`]

```cshtml
@* Example pattern from Details.cshtml (for initial vote) *@
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
```

### Pattern 4: UpdateVoteIndexLookup — Set Inside Partial Call Loop

**What:** In `Details.cshtml` lines 560-576, `UpdateVoteIndexLookup` is set via `ViewData["UpdateVoteIndexLookup"] = updateVoteIndexLookup` inside the `@{...}` block immediately before `@await Html.PartialAsync("_Calendar", calendarMonth)` for each month. The partial reads `ViewData["UpdateVoteIndexLookup"] as Dictionary<int, int>`.

**Key difference from VoteIndexLookup:** `UpdateVoteIndexLookup` uses `ViewData` (not `ViewBag`) because it's set per-iteration of the foreach loop and must be fresh for each calendar month. [VERIFIED: codebase read]

**Current state of `Details.Mobile.cshtml`:** The existing Update Your Vote section (lines 79-97) uses AJAX calls (`changeVoteToYes`, `changeVoteToNo`, `changeVoteToMaybe`) — it does NOT call the `_Calendar` partial at all, so `UpdateVoteIndexLookup` is never set. The replacement must add the full lookup + hidden fields + partial call pattern.

### Pattern 5: Mobile Form Submission (Radio Buttons Inside Existing Form)

**What:** `Details.Mobile.cshtml` is a `.cshtml` file with a `<form>` wrapping the vote-related sections. The vote radio buttons inside `_Calendar.Mobile.cshtml` use `name="DateVotes[@voteIndex].Vote"` which maps to the `PlayerSignup.DateVotes` model binder.

**Critical:** The existing mobile view does NOT have a `<form>` tag visible in the current file — let me verify. [VERIFIED: codebase read — `Details.Mobile.cshtml` has no form wrapper in its current state; votes used AJAX. The replacement section must work either within a form or by adding one.]

**IMPORTANT INSIGHT:** The current `Details.Mobile.cshtml` has no `<form>` element. The "Choose a Date" partial call on lines 101-112 calls `@await Html.PartialAsync("_Calendar", calendarMonth)` — but the rendered calendar partial (desktop `_Calendar.cshtml`) uses inline radio buttons tied to a grid view with `data-quest-id` attributes and JavaScript vote dispatch. The replacement `_Calendar.Mobile.cshtml` uses standard radio button form binding. This means `Details.Mobile.cshtml` needs a `<form>` wrapper around the "Choose a Date" section (initial vote) with `asp-action="Details" asp-controller="Quest" method="post"`, matching the desktop `Details.cshtml` pattern. The update vote section uses a different form action (`/Quest/UpdateSignup/{questId}`).

**Reference:** Desktop `Details.cshtml` has separate `<form>` elements for the initial signup (line ~440-490) and the update signup (line ~520-590). The mobile view's replacement sections need corresponding form wrappers. [VERIFIED: codebase read of `Details.cshtml`]

### Anti-Patterns to Avoid

- **Loading `calendar.css` in mobile views:** `_Layout.Mobile.cshtml` explicitly does NOT load the desktop CSS files. The `.calendar-grid`, `.calendar-day`, `.calendar-body` grid rules in `calendar.css` are desktop-only and will break mobile layout if included. [VERIFIED: Phase 12 D-03, `_Layout.Mobile.cshtml` source]
- **Using `@inject` in `_Calendar.Mobile.cshtml`:** `_ViewImports.cshtml` already globally injects `IAuthorizationService` and `IAntiforgery`. Phase 13 discovered this trap — do not re-inject. [VERIFIED: STATE.md Phase 13 Plan 03 lesson]
- **Nesting `@{...}` inside `@foreach{...}`:** Phase 13 discovered that Razor syntax does not allow `@{...}` blocks nested inside `@foreach{...}`. Declare variables directly in the C# code mode of the foreach body instead. [VERIFIED: STATE.md Plan 03 lesson]
- **Capping quests per day with `.Take(3)`:** Desktop `_Calendar.cshtml` has `.Take(3)` — this cap is for grid cell overflow. Agenda view shows ALL quests per day (vertical scroll handles it). [VERIFIED: 14-CONTEXT.md Code Insights]
- **Calling `@section Styles` from a partial:** Partials cannot push sections in ASP.NET Core MVC. `_Calendar.Mobile.cshtml` must rely on its host view's CSS. [VERIFIED: ASP.NET Core MVC architecture]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Radio button active state highlighting | Custom JS toggle classes | Bootstrap `.btn-check:checked + .btn-outline-*` CSS | Native Bootstrap 5 behavior; zero JS required; works with keyboard/assistive tech |
| Touch target sizing | Custom CSS `height` overrides | `mobile.css` already sets `.btn { min-height: 44px }` | Already applied globally; any `.btn` element meets the 44px requirement |
| Month navigation | AJAX or JS navigation | Standard `<a href="@Url.Action(...)">` anchor links | Controller action already exists; page reload is correct behavior for calendar month changes |
| Day filtering | Custom month-aware logic | `GetCalendarDays().Where(d => !d.IsEmpty && d.QuestsOnDay.Any())` | `CalendarViewModel.GetCalendarDays()` already handles month boundary padding; filter is one LINQ call |
| Status color logic | Conditional hex values | `questOnDay.IsFinalized ? "agenda-quest-finalized" : "agenda-quest-proposed"` + CSS class | Status already computed by `CalendarViewModel.GetQuestsForDate()` |

**Key insight:** Every computed value this phase needs (finalized status, quest times, proposed dates, vote state) already exists in the models and ViewBag that the controllers set. Phase 14 is purely a rendering concern.

---

## Runtime State Inventory

Step 2.5: SKIPPED — this is an additive phase, not a rename/refactor/migration. No stored data, live service config, OS-registered state, secrets, or build artifacts reference strings being changed.

---

## Common Pitfalls

### Pitfall 1: Missing Form Wrapper for Vote Submission

**What goes wrong:** `_Calendar.Mobile.cshtml` renders radio buttons with `name="DateVotes[@voteIndex].Vote"` but `Details.Mobile.cshtml` has no `<form>` element — vote submission silently fails or POSTs to wrong endpoint.

**Why it happens:** The current `Details.Mobile.cshtml` used AJAX for votes (the three-button block lines 79-97). When the AJAX block is replaced with a partial call, the radio buttons inside `_Calendar.Mobile.cshtml` need a standard HTML form to bind to.

**How to avoid:** Add `<form asp-action="Details" asp-controller="Quest" asp-route-id="@Model.Quest.Id" method="post">` around the "Choose a Date" section, and a separate `<form asp-action="UpdateSignup" asp-controller="Quest" method="post">` around the "Update Your Vote" section, following `Details.cshtml` desktop pattern. Include antiforgery token via `@Html.AntiForgeryToken()`. Include hidden `DateVotes[@i].ProposedDateId` fields. Add a submit button.

**Warning signs:** Testing shows the form submits but the controller receives empty `DateVotes` list.

### Pitfall 2: VoteIndexLookup Not Set in Mobile View

**What goes wrong:** `_Calendar.Mobile.cshtml` reads `ViewBag.VoteIndexLookup` but gets `null` — all `voteIndex` values are -1 — no vote buttons render.

**Why it happens:** The controller (`QuestController.Details`) does NOT set `VoteIndexLookup`. It is built in `Details.cshtml` as a Razor code block before calling the partial. `Details.Mobile.cshtml` currently calls the partial without building the lookup.

**How to avoid:** In the "Choose a Date" section of `Details.Mobile.cshtml`, add the same Razor code block that `Details.cshtml` uses (lines 447-461) to build and set `ViewBag.VoteIndexLookup` and `ViewBag.UpdateVoteIndexLookup` before calling the partial. [VERIFIED: codebase read of QuestController.Details — lookup not present in controller]

**Warning signs:** Integration test for CAL-05 shows vote buttons absent despite quest having proposed dates.

### Pitfall 3: UpdateVoteIndexLookup Uses ViewData, Not ViewBag

**What goes wrong:** Code sets `ViewBag.UpdateVoteIndexLookup` but the partial reads `ViewData["UpdateVoteIndexLookup"]` — the update vote buttons never render.

**Why it happens:** In `Details.cshtml`, `UpdateVoteIndexLookup` is stored in `ViewData["UpdateVoteIndexLookup"]` (not ViewBag) and read back as `ViewData["UpdateVoteIndexLookup"] as Dictionary<int, int>`. ViewBag and ViewData are the same underlying dictionary — `ViewBag.X` is syntactic sugar for `ViewData["X"]`. But the key is the exact string match.

**How to avoid:** Use `ViewData["UpdateVoteIndexLookup"] = updateVoteIndexLookup;` (not `ViewBag.UpdateVoteIndexLookup`). Read it back with `var updateVoteIndexLookup = ViewData["UpdateVoteIndexLookup"] as Dictionary<int, int>;`. [VERIFIED: codebase read of `_Calendar.cshtml` line 80]

### Pitfall 4: AJAX Functions Still Needed After Block Replacement

**What goes wrong:** `Details.Mobile.cshtml` has `@section Scripts` containing `changeVoteToYes`, `changeVoteToNo`, `changeVoteToMaybe` functions. After replacing the AJAX buttons with the partial, these functions are no longer called by any UI element but are still referenced — or may break if the antiforgery token approach changes.

**Why it happens:** Phase 13 established that JS vote functions must be present even when not visible (STATE.md Plan 03 lesson). However, when the AJAX buttons are removed, the functions become dead code.

**How to avoid:** The `revokeSignup` function must stay (it's still used by the Revoke Signup button). The `changeVoteToYes/No/Maybe` functions can be removed when the AJAX buttons are replaced. Verify no other element in the view calls them before removing.

**Warning signs:** Browser console shows "function not defined" errors after form interaction — or conversely, dead code lint warnings.

### Pitfall 5: Day Label Uppercase Via C# vs CSS

**What goes wrong:** Day label renders as mixed-case instead of "SATURDAY, JUNE 14".

**Why it happens:** If using `day.Date.ToString("dddd, MMMM d")` without `.ToUpper()` in C#, relying on Bootstrap's `text-uppercase` utility alone may not uppercase the C#-generated string if it renders into an already-uppercase CSS context.

**How to avoid:** Apply `.ToUpper()` in C# directly: `day.Date.ToString("dddd, MMMM d").ToUpper()`. Bootstrap `text-uppercase` is a redundant safety net. [VERIFIED: 14-UI-SPEC.md Typography — "Do not hard-code `text-transform: uppercase` in CSS (Bootstrap util is sufficient)" — but safe to also call `.ToUpper()` in C#]

### Pitfall 6: `@await Html.PartialAsync` Inside `@if` Block Requires `@await` Not `await`

**What goes wrong:** Razor compilation error when using `await Html.PartialAsync(...)` inside an `@if` block.

**Why it happens:** Inside Razor `@if {...}` blocks, async calls need the `@await` directive.

**How to avoid:** Use `@await Html.PartialAsync("_Calendar", calendarMonth)` consistently, not bare `await`. [VERIFIED: existing `Details.Mobile.cshtml` line 109 uses this pattern correctly]

---

## Code Examples

Verified patterns from authoritative codebase sources:

### Month Navigation URL Pattern
```cshtml
@* Source: EuphoriaInn.Service/Views/Calendar/Index.cshtml lines 17-25 *@
<a href="@Url.Action("Index", new { year = Model.FirstDayOfMonth.AddMonths(-1).Year, month = Model.FirstDayOfMonth.AddMonths(-1).Month })"
   class="btn btn-primary calendar-nav-btn" aria-label="Previous month">
    <i class="fas fa-chevron-left"></i>
</a>
<span class="fw-bold cinzel-heading calendar-month-title">@Model.MonthName</span>
<a href="@Url.Action("Index", new { year = Model.FirstDayOfMonth.AddMonths(1).Year, month = Model.FirstDayOfMonth.AddMonths(1).Month })"
   class="btn btn-primary calendar-nav-btn" aria-label="Next month">
    <i class="fas fa-chevron-right"></i>
</a>
```

### Agenda List Filtering and Rendering
```cshtml
@* Source: CalendarViewModel.GetCalendarDays() + established codebase pattern *@
@foreach (var day in Model.GetCalendarDays().Where(d => !d.IsEmpty && d.QuestsOnDay.Any()))
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
```

### VoteIndexLookup Setup (from Details.cshtml pattern, mobile adaptation)
```cshtml
@* Source: EuphoriaInn.Service/Views/Quest/Details.cshtml lines ~447-461 — adapted for mobile *@
@{
    var sortedDateVotes = Model.Quest?.ProposedDates
        .OrderBy(pd => pd.Date)
        .Select(pd => new { ProposedDateId = pd.Id })
        .ToList() ?? [];
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
```

### Mobile Vote Button Row (Bootstrap btn-check pattern)
```cshtml
@* Source: 14-UI-SPEC.md + _Calendar.cshtml btn-check pattern *@
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
```

**VoteType enum integer values** (confirmed from `_Calendar.cshtml` line 95/100/105 — `value="2"` = Yes, `value="0"` = No, `value="1"` = Maybe): [VERIFIED: codebase read]

### calendar.mobile.css New File (Required Rules)
```css
/* Source: 14-UI-SPEC.md CSS Architecture Contract */

/* Month nav bar */
.calendar-nav-btn {
    min-width: 44px;
    width: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.calendar-month-title {
    font-family: 'Cinzel', serif;
    font-size: 1.25rem; /* 20px */
    font-weight: 700;
}

/* Day label */
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

/* Quest entry card */
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

/* Status dot via left border — values from calendar.css lines 102-108 */
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

### quests.mobile.css Additions (Calendar Partial Rules)
```css
/* Source: 14-UI-SPEC.md CSS Architecture Contract */

/* Calendar partial: per-date entry wrapper */
.calendar-date-entry-mobile {
    border-bottom: 1px solid rgba(26, 15, 8, 0.15);
    padding-bottom: 12px;
}

.calendar-date-entry-mobile:last-child {
    border-bottom: none;
}

.calendar-date-label-mobile {
    font-size: 0.875em;
    color: #1a0f08;
}

/* Vote button row: three equal-width buttons */
.calendar-vote-row .btn {
    min-height: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Per-quest AJAX vote buttons (Phase 13 `Details.Mobile.cshtml` lines 79-97) | Per-date radio form buttons via `_Calendar.Mobile.cshtml` partial | Phase 14 | More granular voting; aligns mobile with desktop form behavior; eliminates AJAX dependency for update vote flow |
| Desktop 7-column grid calendar (`_Calendar.cshtml`) | Mobile vertical agenda list (`Calendar/Index.Mobile.cshtml`) | Phase 14 | No horizontal overflow; scannable on narrow screens |

**Deprecated/outdated:**
- `changeVoteToYes/No/Maybe` JS functions in `Details.Mobile.cshtml` `@section Scripts` — no longer called after AJAX block is replaced. Remove once confirmed no other caller exists (verify `revokeSignup` stays).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Details.Mobile.cshtml` needs a `<form>` wrapper added for the initial-vote and update-vote sections — the current file has none | Common Pitfalls / Pattern 3 | If a form wrapper already exists and was missed during review, the new code would create a nested form — invalid HTML that silently breaks submission |
| A2 | `changeVoteToYes/No/Maybe` JS functions can be removed when the AJAX block is replaced | Common Pitfalls 4 | If another UI element in the view (not visible during research) calls these functions, removing them would cause JS errors |

**Note on A1:** The current `Details.Mobile.cshtml` was fully read (lines 1-224). No `<form>` element is present in the file. The vote actions were AJAX-only. The assertion is HIGH confidence. [VERIFIED: codebase read]

**Note on A2:** All button elements in `Details.Mobile.cshtml` were audited. Lines 86-94 use `onclick="changeVoteToYes/No/Maybe()"`. These three buttons are the ONLY callers. Replacing lines 79-97 removes all callers. [VERIFIED: codebase read]

If both assumptions hold, the Assumptions Log entries can be closed as confirmed at planning time.

---

## Open Questions

1. **Does Details.Mobile.cshtml need the full `UpdateSignup` form POST flow, or is the existing flow sufficient?**
   - What we know: The desktop `Details.cshtml` has a `<form method="post" asp-action="UpdateSignup">` that binds `DateVotes` from the radio buttons. The mobile view currently used AJAX for a simpler single-vote endpoint.
   - What's unclear: The update vote form (`UpdateSignup`) accepts `List<PlayerDateVote> dateVotes` which maps to `DateVotes[i].ProposedDateId` + `DateVotes[i].Vote` hidden + radio fields. This is a full form POST requiring the hidden ProposedDateId fields AND the antiforgery token. The mobile view needs to either replicate this form structure or keep AJAX.
   - Recommendation: Replicate the form structure from `Details.cshtml` (lines 520-590). The `_Calendar.Mobile.cshtml` renders radio buttons in `name="DateVotes[@updateVoteIndex].Vote"` format (per D-07 and UI-SPEC). This only works if `Details.Mobile.cshtml` has the matching `<form>` and hidden `DateVotes[@i].ProposedDateId` fields. The planner should make this explicit in the plan.

---

## Environment Availability

Step 2.6: All dependencies are in-project code (Razor views + static CSS). No external tools, services, or CLIs are required beyond the standard .NET SDK already confirmed available (`dotnet 10.0.301`). [VERIFIED: `dotnet --version`]

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build + test | ✓ | 10.0.301 | — |
| dotnet test | Integration test run | ✓ | via SDK | — |
| Bootstrap 5.3.0 | Vote button UI | ✓ | CDN (already in _Layout.Mobile.cshtml) | — |
| Font Awesome 6.4.0 | Icons | ✓ | CDN (already in _Layout.Mobile.cshtml) | — |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -x` |
| Full suite command | `dotnet test EuphoriaInn.IntegrationTests` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CAL-01 | Mobile UA on /Calendar renders agenda list (no `.calendar-grid`) | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_RendersAgendaList"` | ❌ Wave 0 |
| CAL-02 | Agenda entry contains day label in "SATURDAY, JUNE 14" format + time | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_AgendaEntryContainsDayLabelAndTime"` | ❌ Wave 0 |
| CAL-03 | Desktop UA on /Calendar does NOT render agenda list | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_DesktopUserAgent_DoesNotRenderAgendaList"` | ❌ Wave 0 |
| CAL-04 | Agenda entry links to /Quest/Details/{id} | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_AgendaEntryLinksToDetails"` | ❌ Wave 0 |
| CAL-05 | _Calendar.Mobile.cshtml partial renders per-date vote buttons on Quest Details mobile | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_CalendarPartialRendersVoteButtons"` | ❌ Wave 0 |
| CAL-01 CSS | calendar.mobile.css is linked from /Calendar on mobile | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_LoadsMobileCssLink"` | ❌ Wave 0 |

### Pattern to Follow

New tests extend `MobileViewsTests.cs` in `EuphoriaInn.IntegrationTests/Mobile/`. Pattern from Phase 13:
- Use `MobileUserAgent` and `DesktopUserAgent` constants already defined in the class
- Use `GetWithUserAgentAsync(url, userAgent)` helper already defined in the class
- Seed data via `TestDataHelper.CreateTestQuestAsync()` + `TestDataHelper.CreateProposedDateAsync()` (already available)
- Assertions use `.Should().Contain("css-class-name")` to verify rendered HTML contains expected class names

**New test data need:** Tests for CAL-01 through CAL-04 need a quest with a `ProposedDate` in the target month. `TestDataHelper.CreateProposedDateAsync()` already exists. [VERIFIED: codebase read]

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -x`
- **Per wave merge:** `dotnet test EuphoriaInn.IntegrationTests`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] New test methods in `MobileViewsTests.cs` — covers CAL-01 through CAL-05 + CSS check
- [ ] CAL-05 requirement text — must be added to `REQUIREMENTS.md` before or in Wave 0

*(Existing test infrastructure and `WebApplicationFactoryBase` fully cover all Phase 14 tests — no new test files or config needed)*

---

## Security Domain

Phase 14 makes no changes to authentication, authorization, form actions, or controller logic. All new files are:
- Static CSS files (no security surface)
- Razor views that render existing controller-provided ViewBag data
- A modification to an existing Razor view replacing AJAX calls with a form POST (which already exists on desktop)

The form POST addition follows the exact same antiforgery pattern as `Details.cshtml` which is already validated. ASVS categories V2 (Authentication) and V4 (Access Control) are handled by the existing controller action guards — no new endpoints are introduced.

**ASVS applicable categories for this phase:**

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No new surface | Existing controller `[Authorize]` attribute — unchanged |
| V5 Input Validation | Minimal | RadioButton values are integers (0/1/2); model binder handles type validation |
| V4 Access Control | No change | `QuestController.Details` POST already validates the user is signed up before accepting update |

---

## Sources

### Primary (HIGH confidence)

- Codebase read: `EuphoriaInn.Service/Views/Calendar/Index.cshtml` — authoritative month nav URL pattern and `_Calendar` partial call pattern
- Codebase read: `EuphoriaInn.Service/Views/Shared/_Calendar.cshtml` — authoritative vote radio button binding, `VoteIndexLookup`, `UpdateVoteIndexLookup`, `VoteType` integer values, `IsDetailsPage` branching
- Codebase read: `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` — exact AJAX block to be replaced (lines 79-97), "Choose a Date" partial call pattern (lines 101-112)
- Codebase read: `EuphoriaInn.Service/Views/Quest/Details.cshtml` lines 447-590 — `VoteIndexLookup` and `UpdateVoteIndexLookup` construction patterns; form structure for both vote flows
- Codebase read: `EuphoriaInn.Service/ViewModels/CalendarViewModels/CalendarViewModel.cs` — `GetCalendarDays()`, `MonthName`, `FirstDayOfMonth`; `CalendarDay.IsEmpty`, `CalendarDay.QuestsOnDay`; `QuestOnDay.IsFinalized`, `QuestOnDay.ProposedDate.Date`
- Codebase read: `EuphoriaInn.Service/wwwroot/css/calendar.css` lines 102-108 — status dot color values (`#ffc107` proposed, `#28a745` finalized)
- Codebase read: `EuphoriaInn.Service/wwwroot/css/mobile.css` — touch targets (`.btn { min-height: 44px }`), typography, notice board background
- Codebase read: `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — existing classes and glass card pattern to extend
- Codebase read: `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — test class structure, `GetWithUserAgentAsync` helper, `MobileUserAgent` constant, assertion patterns
- Codebase read: `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs` — `CreateTestQuestAsync`, `CreateProposedDateAsync` — data seeding for CAL tests
- Codebase read: `.planning/phases/14-calendar/14-CONTEXT.md` — all 15 locked decisions
- Codebase read: `.planning/phases/14-calendar/14-UI-SPEC.md` — exact HTML/CSS contract, color values, typography spec, copywriting contract
- Codebase read: `.planning/STATE.md` — Phase 12/13 accumulated lessons (form wrapper traps, `@inject` duplication, `@{...}` in `@foreach` nesting)

### Secondary (MEDIUM confidence)

- `.planning/REQUIREMENTS.md` — requirement IDs CAL-01 to CAL-04 (confirmed); CAL-05 not yet added (confirmed needs addition)
- `.planning/ROADMAP.md` — Phase 14 success criteria and CAL-05 implementation note

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project; no new packages needed
- Architecture: HIGH — model, ViewBag keys, partial call chain, and MobileViewLocationExpander behavior all verified by direct codebase read
- Pitfalls: HIGH — most derived from direct code evidence (missing form wrapper, missing VoteIndexLookup setup, UpdateVoteIndexLookup key naming) and confirmed Phase 13 lessons from STATE.md
- Test patterns: HIGH — existing `MobileViewsTests.cs` provides exact structure to extend

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (stable ASP.NET Core MVC patterns; no fast-moving dependencies)
