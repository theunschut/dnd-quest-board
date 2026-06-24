---
phase: 14-calendar
plan: 01
subsystem: testing
tags: [integration-tests, xunit, calendar, mobile, requirements]

# Dependency graph
requires:
  - phase: 13-core-player-views
    provides: MobileViewsTests.cs structure, AuthenticationHelper, TestDataHelper, GetWithUserAgentAsync helper
provides:
  - CAL-05 requirement definition in REQUIREMENTS.md
  - 6 MobileCalendar_* integration test stubs (RED) for CAL-CSS, CAL-01 through CAL-05
  - QVIEW-01 assertion updated from changeVoteToYes to btn-check (forward-compatible with Plan 03)
affects: [14-calendar/14-02, 14-calendar/14-03]

# Tech tracking
tech-stack:
  added: []
  patterns: [Nyquist sampling harness established before implementation; tests start RED by design]

key-files:
  created: []
  modified:
    - .planning/REQUIREMENTS.md
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs

key-decisions:
  - "QVIEW-01 now asserts btn-check (not changeVoteToYes) — forward-compatible with Plan 03 AJAX removal"
  - "CreateProposedDateAsync added to QVIEW-01 so vote buttons render once Plan 03 replaces the AJAX block"
  - "Each CAL test uses unique username prefix (dm_cal01, dm_cal02, etc.) to avoid fixture collision"

patterns-established:
  - "Phase 14 test stubs: 6 MobileCalendar_* methods discoverable before views exist"

requirements-completed: [CAL-01, CAL-02, CAL-03, CAL-04, CAL-05]

# Metrics
duration: 3min
completed: 2026-06-24
---

# Phase 14 Plan 01: Calendar Test Stubs + CAL-05 Requirement Summary

**6 MobileCalendar_* integration test stubs and CAL-05 requirement added; QVIEW-01 forward-patched from changeVoteToYes to btn-check**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-24T15:27:43Z
- **Completed:** 2026-06-24T15:30:03Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added CAL-05 requirement to REQUIREMENTS.md (definition + traceability row)
- Added 6 new MobileCalendar_* test methods covering CAL-CSS, CAL-01 through CAL-05
- Updated QVIEW-01 assertion from Phase 13 AJAX pattern to Plan 03's btn-check pattern
- Added CreateProposedDateAsync seed to QVIEW-01 so vote buttons render once Plan 03 ships
- All 6 new tests are discoverable; integration test project compiles clean (0 errors)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CAL-05 to REQUIREMENTS.md** - `f33cc76` (docs)
2. **Task 2: Add CAL test stubs and fix QVIEW-01** - `b64a1f7` (test)

**Plan metadata:** (added in final commit)

## Files Created/Modified

- `.planning/REQUIREMENTS.md` - Added CAL-05 requirement definition and traceability row
- `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` - 6 new MobileCalendar_* test methods; QVIEW-01 updated

## Decisions Made

- QVIEW-01 updated now (Plan 01) rather than in Plan 03, because the test file is a Plan 01 deliverable and keeping the assertion green through Plan 02 avoids a transient RED state on the wrong commit.
- CreateProposedDateAsync seed added to QVIEW-01 so the test remains valid after the AJAX block is removed in Plan 03 and the partial requires a proposed date to render vote buttons.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 02 (CSS + agenda view) can start immediately — test harness is in place
- Plan 03 (partial + Details update) will make QVIEW-01 and the 5 CAL tests green
- All 6 MobileCalendar_* tests currently RED as expected (views not yet created)

---
*Phase: 14-calendar*
*Completed: 2026-06-24*
