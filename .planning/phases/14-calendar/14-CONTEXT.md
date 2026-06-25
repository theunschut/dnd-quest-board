# Phase 14: Calendar - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Create a mobile-first calendar experience: (1) `Calendar/Index.Mobile.cshtml` — an agenda list replacing the 7-column desktop grid, and (2) `Views/Shared/_Calendar.Mobile.cshtml` — a touch-friendly shared partial for per-date voting in Quest Details. Also update `Quest/Details.Mobile.cshtml` to replace the AJAX "Update Your Vote" block with the unified calendar partial. Strictly additive — no controllers, ViewModels, repositories, or domain services are modified.

</domain>

<decisions>
## Implementation Decisions

### Month Navigation (Calendar/Index.Mobile.cshtml)
- **D-01:** Single header row: `[<] JUNE 2026 [>]` — Bootstrap chevron buttons flanking the centered month name, all full-width, 44px minimum touch targets on both chevron buttons. Standard mobile calendar navigation pattern.
- **D-02:** Month-name heading uses the same `Model.MonthName` value from `CalendarViewModel` as the desktop view.

### Vote Button Layout (_Calendar.Mobile.cshtml)
- **D-03:** Horizontal three-button row per date: `[✓ Yes] [✗ No] [? Maybe]` — three equal-width buttons in one row, icon + short text label, 44px minimum height. Applied to both initial-vote and update-vote flows.
- **D-04:** The vote buttons render per proposed date entry (not quest-level). Each date shows the day label + quest name + time, followed by the `[Yes] [No] [Maybe]` button row.
- **D-05:** Current vote state is shown by highlighting the active button (pre-checked radio button for update flow), same as the desktop partial's checked state.

### Update Your Vote Unification (CAL-05)
- **D-06:** The "Update Your Vote" AJAX block in `Details.Mobile.cshtml` (lines ~79-97) is removed and replaced with `@await Html.PartialAsync("_Calendar", calendarMonth)` — the same call already used in the "Choose a Date" section. The MobileViewLocationExpander auto-serves `_Calendar.Mobile.cshtml` on mobile.
- **D-07:** `_Calendar.Mobile.cshtml` must handle both vote flows from `_Calendar.cshtml`:
  - **Initial vote** (non-signed-up player): radio button group per date, binds to `DateVotes[@voteIndex].Vote`, uses `VoteIndexLookup` from ViewBag.
  - **Update vote** (signed-up player): radio button group per date with current vote pre-selected, binds to `DateVotes[@updateVoteIndex].Vote`, uses `UpdateVoteIndexLookup` from ViewData.
- **D-08:** The "Update Your Vote" section heading in `Details.Mobile.cshtml` stays as-is (as an `<h6>` wrapper around the partial call), keeping the visual section separation.

### Agenda Layout (Calendar/Index.Mobile.cshtml)
- **D-09:** Only days with at least one quest render (CAL-03). Empty days are skipped entirely — no placeholder rows.
- **D-10:** Each agenda entry format: day label row (e.g. `SATURDAY, JUNE 14` in uppercase, styled like a section header) followed by quest entries below it. Each quest entry shows quest name + time.
- **D-11:** Tapping any quest entry navigates to `Quest/Details/{id}` (CAL-04).
- **D-12:** Finalized/proposed status can be shown via a small colored indicator dot or badge on the quest entry (inline, no sidebar legend).

### Legend
- **D-13:** No legend on mobile. The inline visual indicators (colored dot per quest entry, warning icon) are self-explanatory in context. Keeps the agenda clean.

### CSS
- **D-14:** New per-page CSS file: `calendar.mobile.css`. Loaded via `@section Styles` in `Calendar/Index.Mobile.cshtml`. `_Calendar.Mobile.cshtml` (shared partial) uses the same CSS class names — its host view (`Details.Mobile.cshtml`) already loads `quests.mobile.css`; any calendar-partial-specific mobile rules may go in either file (Claude's discretion, keep classes predictable).
- **D-15:** `mobile.css` baseline rules (44px touch targets) apply to vote buttons. `calendar.mobile.css` provides agenda-specific layout rules only.

### Claude's Discretion
- Exact Bootstrap utility classes for the day-label section header (uppercase, muted text, letter-spacing).
- Whether the warning icon (no building key) appears on the agenda quest entry or is omitted for simplicity.
- The heading text inside the `Details.Mobile.cshtml` "Update Your Vote" wrapper section (e.g. "Your Votes" or "Update Your Vote") — keep existing heading or adjust.
- Empty-state for a month with no quests: show a "No quests this month" message between the nav row and the bottom of the page.

### Requirement Note
- **CAL-05** is listed in ROADMAP.md Phase 14 requirements but not yet in REQUIREMENTS.md. It must be added before or during this phase. CAL-05: "The `_Calendar` partial used inside Quest Details renders as a vertical per-date list with tap-friendly Yes/No/Maybe vote buttons — replacing both the broken desktop grid (Choose a Date) and the Phase 13 simplified quest-level buttons (Update Your Vote)."

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — CAL-01 through CAL-04 (all must be satisfied); CAL-05 needs to be added (see Requirement Note above). Read before implementing to confirm scope.
- `.planning/ROADMAP.md` — Phase 14 goal, success criteria, and CAL-05 implementation notes ("CAL-05 also requires replacing the custom 3-button block in `Details.Mobile.cshtml`…").

### Phase 12 Infrastructure (authoritative for how mobile views hook in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — D-03 (only `mobile.css` loaded by layout), `@section Styles` and `@section Scripts` rendering pattern. MUST read.

### Phase 13 Patterns
- `.planning/phases/13-core-player-views/13-CONTEXT.md` — Per-page CSS file pattern (`home.mobile.css`, `quests.mobile.css`). `calendar.mobile.css` follows the same pattern.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Calendar/Index.cshtml` — Desktop calendar view; source for `CalendarViewModel` usage, month navigation URL pattern, and `_Calendar` partial call.
- `EuphoriaInn.Service/Views/Shared/_Calendar.cshtml` — Desktop calendar partial; authoritative source for vote radio button binding pattern (`VoteIndexLookup`, `UpdateVoteIndexLookup`, `VoteType` enum), status indicators, and the `IsDetailsPage`/`CurrentQuestId` ViewBag flags. MUST read before implementing `_Calendar.Mobile.cshtml`.
- `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` — Mobile quest details view; the "Update Your Vote" section (lines ~79-97) is replaced by D-06; the "Choose a Date" section already calls `@await Html.PartialAsync("_Calendar", calendarMonth)` and is the template for the update section.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` is declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale, spacing. `calendar.mobile.css` follows the same selector patterns.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CalendarViewModel.GetCalendarDays()` — returns all calendar days including empty ones; agenda view filters with `day.QuestsOnDay.Any()`.
- Month navigation pattern from `Index.cshtml`: `Url.Action("Index", new { year = Model.FirstDayOfMonth.AddMonths(-1).Year, month = … })` — reuse directly.
- `_Calendar.cshtml` vote logic — complete radio button binding and ViewBag lookups; `_Calendar.Mobile.cshtml` is a mobile adaptation of this partial (not a rewrite from scratch).
- `Details.Mobile.cshtml` "Choose a Date" section (lines ~101-112) — already calls `@await Html.PartialAsync("_Calendar", calendarMonth)`; the "Update Your Vote" section gets the same treatment.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — load per-page CSS.
- `onclick="window.location.href='@Url.Action(…)'"` — tap navigation pattern from Phase 13.
- `day.QuestsOnDay.Take(3)` cap in desktop partial — for agenda view, show ALL quests per day (no cap; vertical scroll handles it).
- Status classes: `proposed` / `finalized` CSS classes already defined in `calendar.css` — reference these in `calendar.mobile.css` for the colored dot indicator.

### Integration Points
- `_Calendar.Mobile.cshtml` is a shared partial — served automatically by `MobileViewLocationExpander` on mobile. It inherits `IsDetailsPage`, `CurrentQuestId`, `VoteIndexLookup`, `UpdateVoteIndexLookup`, `IsPlayerSignedUp` from ViewBag/ViewData set by the parent view.
- `Calendar/Index.Mobile.cshtml` — same `CalendarViewModel` as `Index.cshtml`; same controller action, no changes needed.
- No new controllers, services, or ViewModels for this phase.

</code_context>

<specifics>
## Specific Ideas

- Day label format: "SATURDAY, JUNE 14" (uppercase day name, comma, month + day number) — matches the CAL-02 requirement exactly.
- Month navigation header layout: single Bootstrap row with `d-flex justify-content-between align-items-center` — left chevron button, centered month name, right chevron button.
- Vote button row per date: `d-flex gap-2` or `btn-group` — three equal-width `flex-fill` buttons, icon + text, 44px height via `min-height` in `calendar.mobile.css`.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 14 scope.

</deferred>

---

*Phase: 14-calendar*
*Context gathered: 2026-06-24*
