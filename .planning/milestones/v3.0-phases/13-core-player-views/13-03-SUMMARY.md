---
phase: 13-core-player-views
plan: 03
subsystem: ui
tags: [razor, mobile, asp-net-core, bootstrap5, antiforgery, ajax]

# Dependency graph
requires:
  - phase: 13-01
    provides: MobileViewLocationExpander routes .Mobile.cshtml variants; _Layout.Mobile.cshtml declares Styles/Scripts sections
  - phase: 12
    provides: MobileDetectionMiddleware sets HttpContext.Items["IsMobile"]; mobile.css baseline with 44px .btn min-height

provides:
  - Details.Mobile.cshtml — mobile quest details view with AJAX vote buttons (QVIEW-01) and stacked participant list (QVIEW-02)
  - quests.mobile.css — quest-details-specific mobile styles (participant-list-mobile, participant-row, quest-description-mobile)

affects:
  - 13-04 (QuestLog mobile view — follows same per-page CSS + Mobile.cshtml pattern)
  - verifier (QVIEW-01 and QVIEW-02 integration tests now green)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Per-page mobile CSS loaded via @section Styles { <link href='~/css/quests.mobile.css' ...> } — no @media, no @import"
    - "Antiforgery token from globally-injected Antiforgery (never re-injected in view) used in AJAX fetch calls"
    - "d-grid gap-2 Bootstrap container stacks vote buttons full-width without custom CSS"
    - "allSelectedParticipants LINQ list built in @{} block, rendered as participant-row divs (not table)"

key-files:
  created:
    - EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/quests.mobile.css
  modified: []

key-decisions:
  - "Plan 03: No @inject in Details.Mobile.cshtml — Antiforgery already globally injected via _ViewImports.cshtml line 16; desktop Details.cshtml line 5 @inject is redundant, do not copy it"
  - "Plan 03: participant list condition uses allSelectedParticipants.Count > 0 with IsFinalized check — renders stacked rows for finalized quests with selected players only"
  - "Plan 03: QVIEW-03 (MobileQuestLog) tests remain RED — those require Plan 04 QuestLog/Index.Mobile.cshtml, not in scope here"

patterns-established:
  - "Pattern: quests.mobile.css contains only participant-list and description rules — vote button stacking handled entirely by Bootstrap d-grid gap-2"
  - "Pattern: JS vote functions (changeVoteToYes/No/Maybe/revokeSignup) are in @section Scripts — always rendered, even when buttons are hidden by auth guard"

requirements-completed:
  - QVIEW-01
  - QVIEW-02

# Metrics
duration: 7min
completed: 2026-06-24
---

# Phase 13 Plan 03: Mobile Quest Details View Summary

**Mobile-only `Details.Mobile.cshtml` with AJAX vote buttons stacked via `d-grid gap-2` (QVIEW-01) and participant list as `participant-row` divs replacing the desktop `table-responsive` (QVIEW-02)**

## Performance

- **Duration:** 7 min
- **Started:** 2026-06-24T08:17:00Z
- **Completed:** 2026-06-24T08:23:41Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Created `quests.mobile.css` with `.participant-list-mobile`, `.participant-row`, and `.quest-description-mobile` rules — no @media, no @import
- Created `Details.Mobile.cshtml` with AJAX vote buttons (Yes/No/Maybe) stacked full-width via Bootstrap `d-grid gap-2` — satisfies QVIEW-01
- Participant list renders as stacked `participant-row` divs with player name, character name, and role badge — no `table-responsive` — satisfies QVIEW-02
- Antiforgery token sourced from globally-injected `Antiforgery` — no redundant `@inject` in the view
- QVIEW-01 and QVIEW-02 integration tests: 2/2 pass; all 31 non-QVIEW-03 mobile tests remain green

## Task Commits

1. **Task 1: Create quests.mobile.css** - `e1cae67` (feat)
2. **Task 2: Create Details.Mobile.cshtml** - `47e8ac0` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — participant list container + row styles, quest description block; no @media
- `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` — mobile quest details view with vote buttons, stacked participant list, AJAX fetch functions

## Decisions Made

- No `@inject` in the mobile view — `_ViewImports.cshtml` line 16 globally injects `IAntiforgery Antiforgery`; the desktop `Details.cshtml` has a redundant `@inject` at line 5 that was not copied
- Vote button stacking handled entirely by Bootstrap `d-grid gap-2` on the container — no CSS rule needed in `quests.mobile.css` (`.btn { min-height: 44px }` already in `mobile.css`)
- JS vote functions placed in `@section Scripts` unconditionally — they need to be present even when the vote buttons are hidden (auth guard on the button divs only, not the functions)
- QVIEW-02 participant list shows only when `IsFinalized == true && allSelectedParticipants.Count > 0` — matches the desktop behavior for finalized quests

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- QVIEW-01 and QVIEW-02 complete; the mobile Quest Details view is fully functional
- Plan 04 (QuestLog/Index.Mobile.cshtml) is next — QVIEW-03 tests are RED and ready to turn green
- The 2 failing QVIEW-03 tests (`MobileQuestLog_*`) are expected RED from Plan 01 wave setup; they will go green in Plan 04

## Self-Check

- [x] `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml` exists
- [x] `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` exists
- [x] `git log` shows commits e1cae67 and 47e8ac0
- [x] `dotnet build EuphoriaInn.Service` exits 0
- [x] `dotnet test --filter MobileQuestDetails` — 2/2 pass

## Self-Check: PASSED

---
*Phase: 13-core-player-views*
*Completed: 2026-06-24*
