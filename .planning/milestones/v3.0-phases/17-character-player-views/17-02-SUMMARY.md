---
phase: 17-character-player-views
plan: 02
subsystem: mobile-views
tags: [mobile, razor, css, glass-card, character-detail, integration-tests]

# Dependency graph
requires:
  - phase: 17-character-player-views
    plan: 01
    provides: RED integration test stub GetMobilePage_CharacterDetails_ReturnsSuccessAndMobileLayout
provides:
  - GuildMembers/Details.Mobile.cshtml — mobile character detail view with glass card layout
  - character-detail.mobile.css — portrait sizing, glass card, parchment text for details page
affects: [17-03, 17-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "character-detail.mobile.css: glass card (.character-detail-card) with backdrop-filter, portrait sizing (.character-portrait-mobile max-height 220px), parchment text, row dividers"
    - "Details.Mobile.cshtml: single-column glass card layout — portrait card → info card → owner actions card → back button; @section Styles for CSS injection; no Layout= or @inject"

key-files:
  created:
    - EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css
    - EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml
  modified: []

key-decisions:
  - "Details.Mobile.cshtml follows established @section Styles pattern for CSS injection — consistent with Index.Mobile.cshtml and all prior phase mobile views"
  - "Portrait placeholder uses .character-portrait-placeholder CSS class (not inline style) — consistent with guild-members.mobile.css approach"
  - "Owner actions guard (@if isOwner) wraps entire glass card — matches desktop Details.cshtml isOwner logic"
  - "Delete confirm() uses native browser dialog as specified in D-03 — no custom modal"

requirements-completed: [CHAR-01, CHAR-03]

# Metrics
duration: 2min
completed: 2026-06-25
---

# Phase 17 Plan 02: Character Detail Mobile View Summary

**Glass card mobile character detail page with portrait, badges, info rows, and owner actions — integration test GREEN**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-06-25T07:47:08Z
- **Completed:** 2026-06-25T07:49:28Z
- **Tasks:** 2
- **Files created:** 2

## Accomplishments

- Created `character-detail.mobile.css` with full glass card system: `.character-detail-card` (backdrop-filter blur), `.character-portrait-mobile` (max-height 220px), `.character-portrait-placeholder` (faded parchment), `.character-section-heading` (uppercase 0.875rem), `.character-name-mobile` (1.25rem parchment), form-label and text-muted parchment overrides, row dividers, badge text-shadow suppression, catch-all parchment override for p/a/span
- Created `Details.Mobile.cshtml` with four-section single-column layout: portrait card (image or fa-user fallback, name, owner, status/role badges) → character info card (level, sheet link, class badges, description, backstory) → owner actions card (@if isOwner: Edit btn-warning, ToggleRetirement POST, Delete POST with confirm()) → back button
- Integration test `GetMobilePage_CharacterDetails_ReturnsSuccessAndMobileLayout` turned GREEN (1 passed, 0 failed)

## Task Commits

1. **Task 1: Create character-detail.mobile.css** — `ccecbcc`
2. **Task 2: Create GuildMembers/Details.Mobile.cshtml** — `1d45b37`

## Files Created/Modified

- `EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css` — 82 lines; glass card, portrait sizing, parchment text system, no @media queries
- `EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml` — 131 lines; full mobile character detail view

## Decisions Made

- CSS file loads exclusively via `@section Styles` — consistent with all prior phase mobile CSS patterns (D-13)
- Portrait placeholder class `.character-portrait-placeholder` defined in CSS with `height: 120px` and faded parchment color — mirrors guild-members.mobile.css placeholder approach
- Owner actions guard wraps entire `<div class="character-detail-card">` block so heading is only shown to owners
- No `@media` queries in CSS file; no `min-height: 44px` redefinition (already in mobile.css baseline per D-14)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — all data flows are wired to `Model.*` ViewModel properties; portrait fallback (fa-user icon) is the correct behavior when `Model.ProfilePicture == null`, not a data stub.

## Threat Surface Scan

No new network endpoints, auth paths, or schema changes introduced. All surface stays within the existing trust boundaries documented in the plan's threat model:
- POST forms use `asp-action` tag helper which injects antiforgery tokens (T-17-04 mitigated)
- Owner actions guard `@if (isOwner)` hides UI for non-owners; server-side validation unchanged (T-17-05 mitigated)

## Self-Check: PASSED

- `EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css` exists: FOUND
- `character-detail.mobile.css` contains `backdrop-filter: blur(15px)`: FOUND
- `character-detail.mobile.css` contains `.character-detail-card`: FOUND
- `character-detail.mobile.css` contains `.character-portrait-mobile`: FOUND
- `character-detail.mobile.css` contains `max-height: 220px`: FOUND
- `character-detail.mobile.css` contains `#F4E4BC !important`: FOUND
- `character-detail.mobile.css` contains `text-shadow: none !important`: FOUND
- `character-detail.mobile.css` does NOT contain `@media`: CONFIRMED
- `character-detail.mobile.css` does NOT contain `min-height: 44px`: CONFIRMED
- `EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml` exists: FOUND
- `Details.Mobile.cshtml` contains `@section Styles`: FOUND
- `Details.Mobile.cshtml` contains `character-detail-card`: FOUND
- `Details.Mobile.cshtml` contains `character-portrait-mobile`: FOUND
- `Details.Mobile.cshtml` contains `GetProfilePicture`: FOUND
- `Details.Mobile.cshtml` contains `character-section-heading`: FOUND
- `Details.Mobile.cshtml` contains `ToggleRetirement`: FOUND
- `Details.Mobile.cshtml` contains `Are you sure you want to delete this character`: FOUND
- `Details.Mobile.cshtml` contains `character-detail.mobile.css`: FOUND
- `Details.Mobile.cshtml` does NOT contain `Layout =`: CONFIRMED
- `Details.Mobile.cshtml` does NOT contain `@inject`: CONFIRMED
- `dotnet build EuphoriaInn.Service`: 0 errors: PASSED
- `dotnet build EuphoriaInn.IntegrationTests`: 0 errors: PASSED
- Integration test `GetMobilePage_CharacterDetails_ReturnsSuccessAndMobileLayout`: PASSED (GREEN)
- Commit `ccecbcc` (Task 1): FOUND
- Commit `1d45b37` (Task 2): FOUND

---
*Phase: 17-character-player-views*
*Completed: 2026-06-25*
