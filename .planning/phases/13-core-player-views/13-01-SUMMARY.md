---
phase: 13-core-player-views
plan: 01
subsystem: testing
tags: [xunit, integration-tests, mobile, wave-0, test-stubs]

# Dependency graph
requires:
  - phase: 12-mobile-infra
    provides: WebApplicationFactoryBase, MobileLayoutTests pattern, AuthenticationHelper, TestDataHelper
provides:
  - MobileViewsTests.cs with 10 test stubs covering HOME-01 through QVIEW-03
  - Nyquist sampling harness for Wave 1 mobile content view implementation
affects: [13-02-plan, 13-03-plan, 13-04-plan]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IClassFixture<WebApplicationFactoryBase> with two-field storage (_factory + _client) for tests needing both seeding and HTTP requests"
    - "GetWithUserAgentAsync(url, userAgent) two-param helper pattern for mobile UA routing tests"
    - "Combine auth header + UA header for authenticated mobile requests: separate authClient for token, _client for sending"

key-files:
  created:
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
  modified: []

key-decisions:
  - "Store _factory as field (not just _client) — HOME-04, QVIEW-01, QVIEW-02 need _factory.Services for seeding data before HTTP requests"
  - "GetWithUserAgentAsync takes url param (unlike MobileLayoutTests which hardcodes '/') — tests cover /, /QuestLog, /Quest/Details/{id}"
  - "Tests start RED by design — Wave 0 goal is zero build errors and test discovery, not green tests"

patterns-established:
  - "Two-param GetWithUserAgentAsync: all url + UA helper calls use (url, userAgent) signature"
  - "Authenticated mobile requests: create authClient for token, use _client for sending with copied Authorization header + UA header"

requirements-completed: [HOME-01, HOME-02, HOME-03, HOME-04, QVIEW-01, QVIEW-02, QVIEW-03]

# Metrics
duration: 4min
completed: 2026-06-24
---

# Phase 13 Plan 01: MobileViewsTests Stubs Summary

**10 integration test stubs covering HOME-01 through QVIEW-03 — compile cleanly, run red against missing mobile views, establish Wave 1 verify gate**

## Performance

- **Duration:** 4 min
- **Started:** 2026-06-24T08:05:00Z
- **Completed:** 2026-06-24T08:09:10Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Created `MobileViewsTests.cs` with 10 test methods covering all 7 Phase 13 requirements
- Zero build errors — file compiles cleanly against existing test infrastructure
- All 10 tests discovered and run by test runner; 8 fail on assertions (mobile views missing), 2 pass (negative UA desktop assertions)
- Wave 1 executors now have an automated verify gate: `dotnet test --filter "FullyQualifiedName~MobileViewsTests"`

## Task Commits

Each task was committed atomically:

1. **Task 1: Create MobileViewsTests.cs with stubs for HOME-01 through QVIEW-03** - `edd035d` (test)

**Plan metadata:** _(pending final docs commit)_

## Files Created/Modified
- `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — 10 integration test stubs for all Phase 13 requirements

## Decisions Made
- Stored `_factory` as a field alongside `_client` because authenticated tests (HOME-04, QVIEW-01, QVIEW-02) need `_factory.Services` to seed test data before making HTTP requests
- `GetWithUserAgentAsync` accepts a `url` parameter (unlike `MobileLayoutTests` which hardcodes `/`) — tests cover three routes: `/`, `/QuestLog`, `/Quest/Details/{id}`
- Tests intentionally start RED — Wave 0 goal is compilation + discovery, not passing assertions

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Known Stubs

The following test methods assert against mobile view markup that does not yet exist (Wave 1 will implement):

| Test | File | Reason |
|------|------|--------|
| MobileHome_MobileUserAgent_RendersCardListNotPosterImages | MobileViewsTests.cs:45 | Index.Mobile.cshtml not yet created (plan 13-02) |
| MobileHome_MobileUserAgent_QuestCardContainsCrAndStatusBadge | MobileViewsTests.cs:62 | Index.Mobile.cshtml not yet created |
| MobileHome_MobileUserAgent_FinalizedQuestShowsDate | MobileViewsTests.cs:76 | Index.Mobile.cshtml not yet created |
| MobileHome_MobileUserAgent_QuestCardLinksToDetails | MobileViewsTests.cs:88 | Index.Mobile.cshtml not yet created |
| MobileHome_AuthenticatedSignedUpPlayer_ShowsSignedUpBadge | MobileViewsTests.cs:100 | Index.Mobile.cshtml not yet created |
| MobileQuestDetails_MobileUserAgent_RendersVoteButtons | MobileViewsTests.cs:122 | Details.Mobile.cshtml not yet created (plan 13-02) |
| MobileQuestDetails_MobileUserAgent_ParticipantListIsStacked | MobileViewsTests.cs:143 | Details.Mobile.cshtml not yet created |
| MobileQuestLog_MobileUserAgent_RendersListWithTitleAndDmName | MobileViewsTests.cs:170 | Index.Mobile.cshtml (QuestLog) not yet created (plan 13-03) |

These stubs are intentional — they exist to provide a RED gate that goes GREEN as Wave 1 plans implement the views.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Wave 0 complete: `MobileViewsTests.cs` in place with all 10 test methods
- Wave 1 plans (13-02, 13-03, 13-04) can now run tests after each task to verify progress
- Plans 13-02 and 13-03 should target 8 tests going green when their views are implemented
- No blockers

---
*Phase: 13-core-player-views*
*Completed: 2026-06-24*
