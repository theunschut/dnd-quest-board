---
phase: 15-dm-views
plan: 03
subsystem: ui
tags: [mobile, glass-card, razor, css, quest-manage, vote-badges]

requires:
  - phase: 15-dm-views/01
    provides: MobileDmManage_MobileUserAgent_RendersCondensedVoteBadges test stub (DMVIEW-02 RED)

provides:
  - Quest/Manage.Mobile.cshtml — glass card, condensed vote badges, stacked player list, verbatim desktop JS
  - EuphoriaInn.Service/wwwroot/css/dm-manage.mobile.css — glass card + parchment + vote badge CSS

affects: []

tech-stack:
  added: []
  patterns: [dm-manage-section-card glass card, dm-vote-summary condensed badge row, date-option class on manage-date-option div for JS compatibility]

key-files:
  created:
    - EuphoriaInn.Service/Views/Quest/Manage.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/dm-manage.mobile.css
  modified: []

key-decisions:
  - "Antiforgery already injected globally via _ViewImports.cshtml — no @inject needed in mobile view"
  - "Both manage-date-option AND date-option classes on date divs — JS closest('.date-option') selector compatibility"
  - "Raw C# variables inside @if(IsFinalized){} without @{} wrapper — Razor requires bare C# in code blocks"

patterns-established:
  - "Inside Razor @if/@else code blocks: write C# directly after HTML output, no @{} wrapper needed"
  - "dm-manage-section-card: same glass card values as other mobile cards"

requirements-completed: [DMVIEW-02]

duration: 20min
completed: 2026-06-24
---

# Phase 15: DM Views — Plan 03 Summary

**Quest Manage mobile view — condensed vote badges replacing per-player name lists, stacked player selection with verbatim desktop JS; DMVIEW-02 integration test GREEN**

## Performance

- **Duration:** ~20 min
- **Completed:** 2026-06-24
- **Tasks:** 2
- **Files modified:** 2 (created)

## Accomplishments
- Created `dm-manage.mobile.css` with glass card, parchment text, vote badge, and player row styles
- Created `Manage.Mobile.cshtml` with condensed vote badges, stacked player selection, verbatim desktop JavaScript
- DMVIEW-02 integration test passes (Passed: 1)

## Task Commits

1. **Task 1: Create dm-manage.mobile.css** - feat(15-03): add dm-manage.mobile.css
2. **Task 2: Create Quest/Manage.Mobile.cshtml** - feat(15-03): add Quest/Manage.Mobile.cshtml

## Files Created/Modified
- `EuphoriaInn.Service/Views/Quest/Manage.Mobile.cshtml` — mobile manage view (490 lines)
- `EuphoriaInn.Service/wwwroot/css/dm-manage.mobile.css` — glass card + parchment CSS (70 lines)

## Decisions Made
- Antiforgery is globally injected via `_ViewImports.cshtml` — did NOT add `@inject` in mobile view (plan specified it but it would cause duplicate injection compile error)
- Added both `manage-date-option` and `date-option` CSS classes to date option divs so desktop JS `closest('.date-option')` still works
- Selected participants in finalized state use `participant.Character?.Name ?? "No character"` for null safety

## Deviations from Plan

### Auto-fixed Issues

**1. Nested @{} in @if block — compile error**
- **Found during:** Task 2, build verification
- **Issue:** Plan template placed `@{` inside `@if (Model.IsFinalized) {}` block; Razor reports RZ1010 (cannot nest @{} inside code block)
- **Fix:** Removed `@{` and `}` wrappers, wrote C# variable declarations directly inside the code block (standard Razor pattern, as used in desktop Manage.cshtml lines 352-368)
- **Verification:** `dotnet build EuphoriaInn.Service` exits 0

## Issues Encountered
None.

## Next Phase Readiness
- Plan 15-04 (DM Profile mobile) is unblocked.

---
*Phase: 15-dm-views*
*Completed: 2026-06-24*

## Self-Check: PASSED
- [x] `Manage.Mobile.cshtml` contains `dm-manage-section-card`
- [x] `Manage.Mobile.cshtml` contains `dm-manage.mobile.css`
- [x] `Manage.Mobile.cshtml` contains `manage-date-option` AND `date-option` on same div
- [x] `Manage.Mobile.cshtml` contains `dm-vote-summary`
- [x] `Manage.Mobile.cshtml` contains `asp-action="Finalize"`
- [x] `Manage.Mobile.cshtml` contains `updatePlayerAvailability`
- [x] `Manage.Mobile.cshtml` does NOT contain `col-md-4` or `col-md-8`
- [x] `Manage.Mobile.cshtml` does NOT contain `table-responsive`
- [x] `dm-manage.mobile.css` contains `backdrop-filter: blur(15px)`
- [x] `dm-manage.mobile.css` does NOT contain `@media`
- [x] DMVIEW-02 integration test: Passed 1/1
