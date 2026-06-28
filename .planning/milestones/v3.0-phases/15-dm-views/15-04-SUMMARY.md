---
phase: 15-dm-views
plan: 04
subsystem: ui
tags: [mobile, glass-card, razor, css, dm-profile, quest-history]

requires:
  - phase: 15-dm-views/01
    provides: MobileDmProfile_MobileUserAgent_RendersGlassCardLayout test stub (DMVIEW-03 RED)

provides:
  - DungeonMaster/Profile.Mobile.cshtml — three glass cards, tappable quest history card list, no table
  - EuphoriaInn.Service/wwwroot/css/dm-profile.mobile.css — three glass card classes + parchment text + quest history tap row CSS

affects: []

tech-stack:
  added: []
  patterns: [dm-profile-header-card / dm-profile-bio-card / dm-profile-history-card glass cards, dm-quest-history-item tap row, onclick window.location.href tap navigation]

key-files:
  created:
    - EuphoriaInn.Service/Views/DungeonMaster/Profile.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/dm-profile.mobile.css
  modified: []

key-decisions:
  - "No @inject and no @section Scripts — Profile.cshtml has neither, mobile variant inherits this"
  - "Quest history uses onclick tap navigation (not <a> tags) — consistent with Phase 13 tap pattern"
  - "dm-profile-name class used for both DM name heading AND empty state heading — shares Cinzel font via CSS"

patterns-established:
  - "Three distinct glass card classes per page section (header/bio/history) rather than one monolithic card"
  - "Integration test rebuild required after adding new .Mobile.cshtml — WebApplicationFactory uses compiled output"

requirements-completed: [DMVIEW-03]

duration: 15min
completed: 2026-06-24
---

# Phase 15: DM Views — Plan 04 Summary

**DM Profile mobile view — three glass cards replacing the two-column desktop layout, tappable quest history card list replacing the overflow table; DMVIEW-03 integration test GREEN**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-06-24
- **Tasks:** 2
- **Files modified:** 2 (created)

## Accomplishments
- Created `dm-profile.mobile.css` with three distinct glass card classes (header, bio, history), parchment text, Cinzel for DM name, quest history tap row with cursor pointer, badge shadow suppression
- Created `Profile.Mobile.cshtml` with three glass cards, tappable quest history list (no table), photo as rounded circle, bio with pre-wrap via CSS, conditional Edit Profile button, empty state
- DMVIEW-03 integration test passes (Passed: 1 / Total: 3 including DMVIEW-01 and DMVIEW-02)

## Task Commits

1. **Task 1: Create dm-profile.mobile.css** - feat(15-04): add dm-profile.mobile.css
2. **Task 2: Create DungeonMaster/Profile.Mobile.cshtml** - feat(15-04): add DungeonMaster/Profile.Mobile.cshtml

## Files Created/Modified
- `EuphoriaInn.Service/Views/DungeonMaster/Profile.Mobile.cshtml` — mobile profile view (89 lines)
- `EuphoriaInn.Service/wwwroot/css/dm-profile.mobile.css` — glass card + parchment CSS (130 lines)

## Decisions Made
- Used `fa-4x` for placeholder avatar (80px container) vs desktop `fa-5x` — sized correctly for the constrained container
- `dm-profile-name` class reused for the empty state heading to keep Cinzel font consistent
- No `@section Scripts` — consistent with desktop Profile.cshtml which has no scripts

## Deviations from Plan

### Auto-fixed Issues

**1. Integration test failed on first run (desktop view served instead of mobile)**
- **Found during:** Task 2, test verification
- **Issue:** `dotnet test --no-build` used cached integration test binaries that didn't include the new view file
- **Fix:** Ran `dotnet build EuphoriaInn.IntegrationTests` to rebuild, then re-ran tests with `--no-build` — all 3 DMVIEW tests passed
- **Rule:** After creating new `.Mobile.cshtml` views, always rebuild the integration tests project before testing

## Issues Encountered
None.

## Next Phase Readiness
- All 4 plans in Phase 15 (DM Views) are complete.
- Phase 15 is ready for verification and close-out.

---
*Phase: 15-dm-views*
*Completed: 2026-06-24*

## Self-Check: PASSED
- [x] `Profile.Mobile.cshtml` contains `dm-profile-header-card`
- [x] `Profile.Mobile.cshtml` contains `dm-profile-bio-card`
- [x] `Profile.Mobile.cshtml` contains `dm-profile-history-card`
- [x] `Profile.Mobile.cshtml` contains `dm-profile.mobile.css`
- [x] `Profile.Mobile.cshtml` contains `dm-quest-history-item`
- [x] `Profile.Mobile.cshtml` contains `Url.Action("Details", "Quest"` (tap navigation)
- [x] `Profile.Mobile.cshtml` contains `Url.Action("GetDMProfilePicture"`
- [x] `Profile.Mobile.cshtml` contains `dm-profile-bio-text`
- [x] `Profile.Mobile.cshtml` does NOT contain `table-striped`
- [x] `Profile.Mobile.cshtml` does NOT contain `col-lg-4` or `col-lg-8`
- [x] `Profile.Mobile.cshtml` does NOT contain `@section Scripts`
- [x] `dm-profile.mobile.css` contains `backdrop-filter: blur(15px)` (3 times, once per glass card)
- [x] `dm-profile.mobile.css` does NOT contain `@media`
- [x] DMVIEW-03 integration test: Passed 1/1 (all 3 DMVIEW: Passed 3/3)
