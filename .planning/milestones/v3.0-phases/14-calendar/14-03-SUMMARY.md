---
phase: 14-calendar
plan: 03
subsystem: views
tags: [mobile, calendar-partial, vote-buttons, form-binding, integration-tests, antiforgery]

# Dependency graph
requires:
  - phase: 14-calendar
    plan: 01
    provides: CAL-05 test stub (RED)
  - phase: 14-calendar
    plan: 02
    provides: calendar.mobile.css + Index.Mobile.cshtml
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "_Calendar.Mobile.cshtml: per-date vertical list with btn-check radio buttons — both initial vote (VoteIndexLookup) and update vote (UpdateVoteIndexLookup) flows"
    - "ViewData[UpdateVoteIndexLookup] set directly in foreach C# code mode — no nested @{} inside @foreach (Phase 13 lesson enforced)"
    - "CSRF: @Html.AntiForgeryToken() on both form wrappers per T-14-03-02 mitigation"
    - "Dead AJAX functions removed (changeVoteToYes/No/Maybe); revokeSignup preserved"

key-files:
  created:
    - EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml
  modified:
    - EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/quests.mobile.css

key-decisions:
  - "VoteType.Yes=2, VoteType.No=0, VoteType.Maybe=1 — confirmed from _Calendar.cshtml"
  - "No @inject in _Calendar.Mobile.cshtml — globally available via _ViewImports.cshtml"
  - "No @section Styles in _Calendar.Mobile.cshtml — partial cannot push sections; quests.mobile.css covers it"
  - "updateVoteIndexLookup set in foreach body without @{} wrapper — direct C# code mode assignment"

# Metrics
duration: 4min
completed: 2026-06-24
---

# Phase 14 Plan 03: _Calendar.Mobile.cshtml + Details.Mobile Update Summary

**Mobile vote partial created with btn-check radio buttons for initial and update vote flows; AJAX functions replaced by form POSTs; CAL-05 GREEN; all 107 integration tests pass**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-06-24T15:18:21Z
- **Completed:** 2026-06-24T15:22:04Z
- **Tasks:** 2
- **Files created:** 1
- **Files modified:** 2

## Accomplishments

- Created `_Calendar.Mobile.cshtml`: per-date vertical list rendering one date entry per proposed date that matches the current quest. Each entry has a date label (dddd, MMMM d at HH:mm) and a horizontal three-button row (Yes/No/Maybe) using Bootstrap btn-check radio pattern.
- Supports two vote flows:
  - Initial vote: reads `ViewBag.VoteIndexLookup` (Dictionary<int, int>) to index form fields
  - Update vote: reads `ViewData["UpdateVoteIndexLookup"]` (Dictionary<int, int>) with pre-checked state per `VoteType`
- Updated `Details.Mobile.cshtml`:
  - Replaced "Update Your Vote" AJAX block (three onclick buttons) with a form wrapping `_Calendar` partial + UpdateVoteIndexLookup setup
  - Replaced "Choose a Date" partial call (no form) with proper form wrapper + VoteIndexLookup setup + hidden ProposedDateId fields
  - Removed `changeVoteToYes`, `changeVoteToNo`, `changeVoteToMaybe` from @section Scripts
  - Preserved `revokeSignup` function
- Appended `.calendar-date-entry-mobile`, `.calendar-date-label-mobile`, `.calendar-vote-row .btn` rules to `quests.mobile.css`
- CAL-05 test: GREEN (was RED at end of Plan 02)
- QVIEW-01 test: GREEN (asserts `btn-check` — updated in Plan 01)
- All 107 integration tests: GREEN

## Task Commits

Each task was committed atomically:

1. **Task 1: Create _Calendar.Mobile.cshtml + append quests.mobile.css** - `3a41106` (feat)
2. **Task 2: Update Details.Mobile.cshtml** - `d44eb57` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml` — New file: per-date mobile vote partial
- `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` — Modified: form wrappers, partial calls, AJAX removal
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — Modified: calendar partial CSS rules appended

## Decisions Made

- `VoteType.Yes=2, VoteType.No=0, VoteType.Maybe=1` — confirmed from desktop `_Calendar.cshtml` lines 95/100/105
- No `@inject` in the partial — `_ViewImports.cshtml` globally injects `IAntiforgery` and `IAuthorizationService`
- `ViewData["UpdateVoteIndexLookup"]` set directly in foreach C# code mode (no `@{...}` wrapper) — enforces Phase 13 lesson
- CSS appended to `quests.mobile.css` (not a new file) — partial has no `@section Styles`; host view's CSS covers it

## Deviations from Plan

None - plan executed exactly as written.

## Test Results

| Test | Status | Requirement |
|------|--------|-------------|
| MobileCalendar_MobileUserAgent_CalendarPartialRendersVoteButtons | GREEN | CAL-05 |
| MobileQuestDetails_MobileUserAgent_RendersVoteButtons | GREEN | QVIEW-01 |
| All MobileViewsTests (16 tests) | GREEN | All |
| Full integration test suite (107 tests) | GREEN | All |

## Known Stubs

None — `_Calendar.Mobile.cshtml` renders real proposed dates from `CalendarViewModel.GetCalendarDays()`. Both vote flows bind to real form fields using VoteIndexLookup dictionaries.

## Threat Flags

No new threat surface beyond the plan's threat model.

- T-14-03-02 (CSRF): Mitigated — `@Html.AntiForgeryToken()` added to both form wrappers.
- T-14-03-01 (vote tampering): Accepted — integer range validated by model binder; controller validates authorization before processing.
- T-14-03-03 (questId disclosure): Accepted — non-sensitive sequential int; controller validates ownership.
- T-14-03-04 (unauthenticated vote): Accepted — [Authorize] on controller actions; view condition is defense-in-depth.

---

## Self-Check: PASSED

**Files created:**
- `EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml` — FOUND
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` (modified, calendar rules appended) — FOUND
- `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` (modified) — FOUND

**Commits:**
- `3a41106` (Task 1 — _Calendar.Mobile.cshtml + quests.mobile.css) — FOUND
- `d44eb57` (Task 2 — Details.Mobile.cshtml) — FOUND

**Acceptance criteria verified:**
- _Calendar.Mobile.cshtml contains `calendar-date-entry-mobile` — PASS
- _Calendar.Mobile.cshtml contains `calendar-vote-row` — PASS
- _Calendar.Mobile.cshtml contains `btn-check` — PASS
- _Calendar.Mobile.cshtml contains `UpdateVoteIndexLookup` — PASS
- _Calendar.Mobile.cshtml contains `VoteType.Yes` — PASS
- _Calendar.Mobile.cshtml does NOT contain `@inject` — PASS
- _Calendar.Mobile.cshtml does NOT contain `@section Styles` — PASS
- quests.mobile.css contains `.calendar-date-entry-mobile` — PASS
- quests.mobile.css contains `.calendar-vote-row .btn` — PASS
- quests.mobile.css contains `min-height: 44px` — PASS
- Details.Mobile.cshtml does NOT contain `changeVoteToYes` — PASS
- Details.Mobile.cshtml does NOT contain `changeVoteToNo` — PASS
- Details.Mobile.cshtml does NOT contain `changeVoteToMaybe` — PASS
- Details.Mobile.cshtml contains `asp-action="UpdateSignup"` — PASS
- Details.Mobile.cshtml contains `asp-action="Details"` — PASS
- Details.Mobile.cshtml contains `ViewData["UpdateVoteIndexLookup"]` — PASS
- Details.Mobile.cshtml contains `ViewBag.VoteIndexLookup = voteIndexLookup` — PASS
- Details.Mobile.cshtml contains `@Html.AntiForgeryToken()` — PASS
- Details.Mobile.cshtml contains `Html.PartialAsync("_Calendar", calendarMonth)` (twice) — PASS
- Details.Mobile.cshtml contains `revokeSignup` — PASS
- dotnet build EuphoriaInn.Service — 0 errors — PASS
- CAL-05 test GREEN — PASS
- QVIEW-01 test GREEN — PASS
- All 107 integration tests GREEN — PASS

---
*Phase: 14-calendar*
*Completed: 2026-06-24*
