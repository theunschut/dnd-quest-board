---
phase: 13-core-player-views
plan: "02"
subsystem: mobile-views
tags:
  - mobile
  - razor-views
  - css
  - home
  - quest-board
dependency_graph:
  requires:
    - 13-01
  provides:
    - home-mobile-card-list
    - home-mobile-css
  affects:
    - MobileViewsTests (HOME-01 through HOME-04 now GREEN)
tech_stack:
  added: []
  patterns:
    - mobile-card-list-layout
    - three-way-status-badge
    - position-absolute-signed-up-badge
    - onclick-tap-navigation
    - section-styles-css-loading
key_files:
  created:
    - EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/home.mobile.css
  modified:
    - EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
decisions:
  - "home.mobile.css uses hex palette (#343a40/#495057) matching mobile.css dark theme; no @media queries — file is mobile-only by definition"
  - "IsFinalized + null FinalizedDate means quest is filtered out by repository (FinalizedDate > oneDayAgo check); tests must seed future FinalizedDate to verify Finalized badge"
  - "TestDataHelper.CreateTestQuestAsync extended with optional FinalizedDate param to support both past-done and future-finalized test scenarios"
metrics:
  duration: "4m 24s"
  completed: "2026-06-24T08:17:00Z"
  tasks_completed: 2
  files_created: 2
  files_modified: 2
---

# Phase 13 Plan 02: Mobile Quest Board View Summary

Mobile Quest Board view with vertical card list replacing poster/parchment grid; CR badge, DM name, three-way status badge, signed-up indicator, and tap navigation.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create home.mobile.css with quest card styles | a063330 | `wwwroot/css/home.mobile.css` |
| 2 | Create Index.Mobile.cshtml mobile quest board view | 47652e0 | `Views/Home/Index.Mobile.cshtml`, `TestDataHelper.cs`, `MobileViewsTests.cs` |

## Verification

All HOME tests GREEN:

```
Passed MobileHome_MobileUserAgent_RendersCardListNotPosterImages
Passed MobileHome_MobileUserAgent_QuestCardContainsCrAndStatusBadge
Passed MobileHome_MobileUserAgent_FinalizedQuestShowsDate
Passed MobileHome_MobileUserAgent_QuestCardLinksToDetails
Passed MobileHome_AuthenticatedSignedUpPlayer_ShowsSignedUpBadge
Passed MobileHome_DesktopUserAgent_DoesNotRenderMobileCardList
```

6/6 HOME tests pass. QVIEW tests (Quest Details + Quest Log) remain RED — implemented in plans 03 and 04.

`dotnet build EuphoriaInn.Service` exits 0.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] HOME-01 test needed a quest seeded to assert quest-card-mobile**
- **Found during:** Task 2 — integration test run
- **Issue:** `MobileHome_MobileUserAgent_RendersCardListNotPosterImages` didn't seed any quest; with an empty database the empty state renders instead of the card list, so `quest-card-mobile` was not present in the HTML
- **Fix:** Added a DM user + open quest seed to the test before the GET request
- **Files modified:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs`
- **Commit:** 47652e0

**2. [Rule 1 - Bug] HOME-02b test seeded isFinalized: true with null FinalizedDate — repository filters it out**
- **Found during:** Task 2 — integration test run
- **Issue:** `QuestRepository.GetQuestsWithSignupsForRoleAsync` applies `q.FinalizedDate > oneDayAgo`; a null FinalizedDate evaluates false in SQL so the quest was excluded from the Home page. The test expected `bg-primary` (Finalized badge) but the quest never appeared in the view.
- **Fix:** Updated test to seed `finalizedDate: DateTime.UtcNow.AddDays(7)` (future date), and extended `TestDataHelper.CreateTestQuestAsync` with an optional `finalizedDate` parameter
- **Files modified:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs`, `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs`
- **Commit:** 47652e0

**3. [Rule 1 - Bug] Razor syntax error — nested @{} inside @foreach**
- **Found during:** Task 2 — dotnet build
- **Issue:** Plan template used `@{ ... }` block inside `@foreach { }` which is invalid Razor syntax (RZ1010: Unexpected "{" after "@")
- **Fix:** Removed the inner `@{ }` wrapper; variables declared directly inside the `@foreach` body in C# code mode
- **Files modified:** `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml`
- **Commit:** 47652e0

## Known Stubs

None. Both files are fully wired:
- `home.mobile.css` provides `.quest-card-mobile`, `.quest-list-mobile`, `.quest-card-title` — all referenced by `Index.Mobile.cshtml`
- `Index.Mobile.cshtml` uses real model data (`quest.Title`, `quest.ChallengeRating`, `quest.DungeonMaster?.Name`, `quest.PlayerSignups`) from the existing `HomeController.Index` action

## Threat Flags

No new threat surface. View-only plan — no new endpoints, no new form inputs. All data access through existing `HomeController` with existing authorization. `Url.Action()` generates server-side URLs; no user-controlled URL construction.

## Self-Check: PASSED

- FOUND: `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml`
- FOUND: `EuphoriaInn.Service/wwwroot/css/home.mobile.css`
- FOUND: commit a063330 (home.mobile.css)
- FOUND: commit 47652e0 (Index.Mobile.cshtml + test fixes)
- FOUND: 6/6 HOME integration tests pass

---
*Phase: 13-core-player-views*
*Completed: 2026-06-24*
