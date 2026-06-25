---
phase: 17-character-player-views
plan: 04
subsystem: ui
tags: [mobile, cshtml, css, glass-card, players, dungeon-masters]

# Dependency graph
requires:
  - phase: 17-character-player-views
    provides: Plan 01 integration test stubs (GetMobilePage_PlayersIndex_ReturnsSuccessAndMobileLayout RED)
provides:
  - Players/Index.Mobile.cshtml with two glass card sections and DM tap-navigation
  - players.mobile.css with glass card, parchment text, tappable/non-tappable row styles
  - Desktop Players/Index.cshtml with email column removed from both DM and Player tables
affects: [17-01-tests, 18-dm-editing]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "players.mobile.css pattern: section glass card + parchment name rows with min-height 44px; no-link modifier for non-tappable rows"
    - "DM tap-navigation: onclick window.location.href Url.Action Pattern used in Players/Index.Mobile.cshtml DM rows"

key-files:
  created:
    - EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/players.mobile.css
  modified:
    - EuphoriaInn.Service/Views/Players/Index.cshtml

key-decisions:
  - "players.mobile.css has no @media queries — loaded exclusively by Players/Index.Mobile.cshtml via _Layout.Mobile.cshtml"
  - "Non-tappable player rows use .players-row.no-link modifier (cursor default, no active background) — DM rows use default .players-row (cursor pointer)"
  - "Desktop email removal: w-50 class removed from Name <th> in both tables (Name is now sole column)"

patterns-established:
  - "players-section-card glass card with backdrop-filter blur(15px) — same values as guild-members.mobile.css renamed"
  - "no-link CSS modifier pattern for non-tappable rows — preserves min-height 44px touch target without cursor pointer"

requirements-completed: [PLAYER-01]

# Metrics
duration: 2.5min
completed: 2026-06-25
---

# Phase 17 Plan 04: Players Mobile View and Desktop Email Removal Summary

**Mobile players list with DM tap-navigation glass card sections and email removed from desktop Players/Index**

## Performance

- **Duration:** ~2.5 min
- **Started:** 2026-06-25T07:58:24Z
- **Completed:** 2026-06-25T08:00:49Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Created `players.mobile.css` with glass card container, parchment section heading, tappable row (44px min-height, warm brown divider, active press feedback), non-tappable `.no-link` modifier, parchment name text, and empty state rules — no `@media` queries
- Created `Players/Index.Mobile.cshtml` with two glass card sections: DM rows tap to `DungeonMaster/Profile/{id}` via `onclick`, player rows are non-tappable (`no-link`), name-only display, no email anywhere
- Edited desktop `Players/Index.cshtml` to remove email `<th>` and `<td>` blocks from both DM and Player tables; Name is now the sole column
- Integration test `GetMobilePage_PlayersIndex_ReturnsSuccessAndMobileLayout`: turned GREEN; all 122 integration tests pass

## Task Commits

Each task was committed atomically:

1. **Task 1: Create players.mobile.css** - `4787bb0` (feat)
2. **Task 2: Create Players/Index.Mobile.cshtml and edit Players/Index.cshtml** - `d251f86` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/wwwroot/css/players.mobile.css` — Glass card, parchment text, tappable/non-tappable row styles, empty state; no @media queries
- `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml` — Mobile players list: DM section (tap-navigable to Profile) + Players section (name-only, non-tappable)
- `EuphoriaInn.Service/Views/Players/Index.cshtml` — Desktop players list with email column removed from both DM table and Player table

## Decisions Made

- Non-tappable player rows use `.players-row.no-link` CSS modifier — preserves min-height 44px touch target (from `mobile.css` baseline) without cursor pointer or active background feedback
- Desktop `Players/Index.cshtml` Name `<th>` had `class="w-50"` removed (no longer needed as sole column) — consistent with plan spec
- `players.mobile.css` has no `@media` queries — device targeting is handled at layout-selection layer by Phase 12 expander

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- Transient `MSB3492` MSBuild cache error on `--no-restore` builds: Windows file-locking artifact on `.cache` file. Full build without `--no-restore` confirms 0 C# errors. Pre-existing environment issue, not caused by this plan's changes.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 17 is now complete — all four plans (01-04) done; CHAR-01, CHAR-02, CHAR-03, PLAYER-01 requirements fulfilled
- Phase 18 (DM Editing & Secondary Quest Views) can begin
- All 122 integration tests pass; no regressions introduced

## Known Stubs

None — all mobile view sections are wired to live model data (`Model.DungeonMasters`, `Model.Players`).

## Self-Check: PASSED

- `EuphoriaInn.Service/wwwroot/css/players.mobile.css` exists: FOUND
- `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml` exists: FOUND
- `players.mobile.css` contains `backdrop-filter: blur(15px)`: FOUND
- `players.mobile.css` contains `.players-section-card`: FOUND
- `players.mobile.css` contains `.players-row`: FOUND
- `players.mobile.css` contains `.players-name`: FOUND
- `players.mobile.css` contains `.players-empty-state`: FOUND
- `players.mobile.css` contains `#F4E4BC !important`: FOUND
- `players.mobile.css` contains `no-link`: FOUND
- `players.mobile.css` does NOT contain `@media`: CONFIRMED
- `Index.Mobile.cshtml` contains `@section Styles`: FOUND
- `Index.Mobile.cshtml` contains `players-section-card`: FOUND
- `Index.Mobile.cshtml` contains `players.mobile.css`: FOUND
- `Index.Mobile.cshtml` contains `players-row`: FOUND
- `Index.Mobile.cshtml` contains `players-name`: FOUND
- `Index.Mobile.cshtml` contains `onclick="window.location.href='@Url.Action("Profile", "DungeonMaster"`: FOUND
- `Index.Mobile.cshtml` contains `no-link`: FOUND
- `Index.Mobile.cshtml` does NOT contain `Email`: CONFIRMED
- `Index.Mobile.cshtml` does NOT contain `Layout =`: CONFIRMED
- `Index.Mobile.cshtml` does NOT contain `@inject`: CONFIRMED
- Desktop `Players/Index.cshtml` does NOT contain `dm.Email`: CONFIRMED
- Desktop `Players/Index.cshtml` does NOT contain `player.Email`: CONFIRMED
- Desktop `Players/Index.cshtml` does NOT contain `email-link`: CONFIRMED
- Desktop `Players/Index.cshtml` still contains `DungeonMaster`: FOUND
- Integration test `GetMobilePage_PlayersIndex_ReturnsSuccessAndMobileLayout`: PASSED (GREEN)
- All 122 integration tests: PASSED
- Commit `4787bb0` (Task 1): FOUND
- Commit `d251f86` (Task 2): FOUND

---
*Phase: 17-character-player-views*
*Completed: 2026-06-25*
