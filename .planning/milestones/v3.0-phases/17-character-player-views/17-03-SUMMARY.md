---
phase: 17-character-player-views
plan: 03
subsystem: mobile-views
tags: [mobile, razor, css, glass-card, character-forms, integration-tests]

# Dependency graph
requires:
  - phase: 17-character-player-views
    plan: 01
    provides: RED integration test stubs GetMobilePage_CharacterCreate and GetMobilePage_CharacterEdit
provides:
  - GuildMembers/Create.Mobile.cshtml — mobile character create form with stacked class entries
  - GuildMembers/Edit.Mobile.cshtml — mobile character edit form with portrait thumbnail
  - character-form.mobile.css — glass card, parchment labels, stacked class-entry layout for form views
affects: [17-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "character-form.mobile.css: glass card (.character-form-card) with backdrop-filter 16px padding, parchment text, faded parchment hints, stacked class-entry (row-gap 0.5rem), full-width file input block"
    - "Create.Mobile.cshtml and Edit.Mobile.cshtml: single-column glass card forms with profile picture at top, col-12 stacked class entries, JS innerHTML template uses col-12, @section Styles + Scripts unconditional"

key-files:
  created:
    - EuphoriaInn.Service/wwwroot/css/character-form.mobile.css
    - EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml
    - EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml
  modified: []

key-decisions:
  - "character-form.mobile.css uses 16px padding (vs 12px in detail CSS) for extra form breathing room — matches account.mobile.css pattern for form containers"
  - "Create.Mobile.cshtml profile picture section is at top per D-05 — full-width, not side-by-side as in desktop col-md-4/8 split"
  - "Edit.Mobile.cshtml portrait thumbnail centered (text-center) above file input per D-05 and UI-SPEC"
  - "Both views @section Scripts unconditional outside any @if block — ensures JS available regardless of auth state"
  - "Variable classData assigned directly in @for body without @{} wrapper — Phase 13 pattern for direct C# code mode"

requirements-completed: [CHAR-02, CHAR-03]

# Metrics
duration: 3min
completed: 2026-06-25
---

# Phase 17 Plan 03: Character Form Mobile Views Summary

**Glass card mobile character create and edit forms with stacked col-12 class entries and JS innerHTML template using col-12 layout — both integration tests GREEN**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-25T07:52:06Z
- **Completed:** 2026-06-25T07:55:13Z
- **Tasks:** 2
- **Files created:** 3

## Accomplishments

- Created `character-form.mobile.css` with glass card container (16px padding, backdrop-filter), parchment text system for headings/form-labels/catch-all, faded parchment for hints, stacked class-entry layout (row-gap: 0.5rem, btn-danger min-height: 44px), and full-width file input block. No @media queries.
- Created `Create.Mobile.cshtml` — single-column glass card form with profile picture upload at top (D-05), character name, level, status/role selects, classes section with stacked col-12 entries (D-06), sheet link, description, backstory, submit (btn-warning) + cancel (btn-secondary to Index) buttons. JS innerHTML template uses col-12 (D-07). @section Styles links character-form.mobile.css.
- Created `Edit.Mobile.cshtml` — identical structure to Create with differences: hidden Id field, portrait thumbnail above file input (centered, max-width 200px), asp-action="Edit", classes hint includes @Model.Level, role hint references Backup, Save Changes (fa-save) button, cancel links to Details (not Index), classIndex initialized from Model.Classes.Count.
- Integration tests `GetMobilePage_CharacterCreate_ReturnsSuccessAndMobileLayout` and `GetMobilePage_CharacterEdit_ReturnsSuccessAndMobileLayout` both turned GREEN.

## Task Commits

1. **Task 1: Create character-form.mobile.css** — `392f623`
2. **Task 2: Create Create.Mobile.cshtml and Edit.Mobile.cshtml** — `a5a23c8`

## Files Created/Modified

- `EuphoriaInn.Service/wwwroot/css/character-form.mobile.css` — 59 lines; glass card, parchment text system, stacked class-entry rules, no @media queries
- `EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml` — 162 lines; full mobile character create form
- `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml` — 166 lines; full mobile character edit form with portrait thumbnail

## Decisions Made

- Glass card uses 16px padding (not 12px) — forms need more internal breathing room than detail views; matches account.mobile.css convention for form containers
- Profile picture section placed at top of form per D-05 — on mobile, the portrait is the visual anchor before text fields
- Edit portrait thumbnail wrapped in `text-center` div — centered display consistent with UI-SPEC contract
- Both @section Scripts blocks unconditional (outside any @if) — file validation + class add/remove JS must be available regardless of auth state (Phase 13 established pattern)
- `var classData` assigned directly in @for body code mode, no @{} wrapper — established pattern from Phase 13 to avoid Razor syntax nesting issues

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — all data flows wired to CharacterViewModel properties. Profile picture fallback (no thumbnail shown when ProfilePicture == null) is correct behavior, not a stub.

## Threat Surface Scan

No new network endpoints introduced. Trust boundaries unchanged:
- T-17-08 (antiforgery): `asp-action` tag helpers inject `__RequestVerificationToken` automatically — both form views inherit protection
- T-17-09 (file upload): Client-side JS validates ALLOWED_TYPES (jpeg/png/gif) and MAX_FILE_SIZE (5MB) — same as desktop Create/Edit; server-side validation unchanged
- T-17-10 (hidden Id field): `asp-for="Id"` can be tampered; server-side Edit action re-validates character ownership — accepted disposition unchanged

## Self-Check: PASSED

- `EuphoriaInn.Service/wwwroot/css/character-form.mobile.css` exists: FOUND
- `character-form.mobile.css` contains `backdrop-filter: blur(15px)`: FOUND
- `character-form.mobile.css` contains `.character-form-card`: FOUND
- `character-form.mobile.css` contains `.class-entry .row`: FOUND
- `character-form.mobile.css` contains `row-gap: 0.5rem`: FOUND
- `character-form.mobile.css` contains `#F4E4BC !important`: FOUND
- `character-form.mobile.css` contains `text-shadow: none !important`: FOUND
- `character-form.mobile.css` does NOT contain `@media`: CONFIRMED
- `EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml` exists: FOUND
- `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml` exists: FOUND
- `Create.Mobile.cshtml` contains `@section Styles`: FOUND
- `Create.Mobile.cshtml` contains `character-form-card`: FOUND
- `Create.Mobile.cshtml` contains `character-form.mobile.css`: FOUND
- `Create.Mobile.cshtml` contains `col-12` (3+ occurrences): FOUND
- `Create.Mobile.cshtml` contains `profilePictureInput`: FOUND
- `Create.Mobile.cshtml` does NOT contain `col-md-5` or `col-md-4` or `col-md-3`: CONFIRMED
- `Edit.Mobile.cshtml` contains `asp-action="Edit"`: FOUND
- `Edit.Mobile.cshtml` contains `asp-for="Id"`: FOUND
- `Edit.Mobile.cshtml` contains `GetProfilePicture`: FOUND
- `Edit.Mobile.cshtml` contains `Details`: FOUND
- `Edit.Mobile.cshtml` contains `Save Changes`: FOUND
- `Edit.Mobile.cshtml` does NOT contain `Layout =`: CONFIRMED
- `Edit.Mobile.cshtml` does NOT contain `@inject`: CONFIRMED
- `dotnet build EuphoriaInn.Service`: 0 errors: PASSED
- `dotnet build EuphoriaInn.IntegrationTests`: 0 errors: PASSED
- Integration test `GetMobilePage_CharacterCreate_ReturnsSuccessAndMobileLayout`: PASSED (GREEN)
- Integration test `GetMobilePage_CharacterEdit_ReturnsSuccessAndMobileLayout`: PASSED (GREEN)
- Commit `392f623` (Task 1): FOUND
- Commit `a5a23c8` (Task 2): FOUND

---
*Phase: 17-character-player-views*
*Completed: 2026-06-25*
