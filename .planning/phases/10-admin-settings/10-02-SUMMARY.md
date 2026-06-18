---
phase: 10-admin-settings
plan: 02
subsystem: web-layer
tags: [admin-settings, mvc, razor-views, integration-tests, omphalos]

# Dependency graph
requires:
  - 10-01 (IAdminSettingService, IntegrationSettings record, DI registration)
provides:
  - SettingsViewModel with [Url]/[StringLength] annotations
  - AdminController Settings GET and POST actions injecting IAdminSettingService
  - Settings.cshtml Razor view with modern-card layout, password masking, PRG feedback
  - Admin navbar dropdown Settings link (fa-plug icon, after Quest Management with divider)
  - SETT-07 integration tests: unauthenticated access Theory + non-admin Fact
affects: [11-navigation-token]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - PRG (Post/Redirect/Get) pattern for Settings form with TempData["SuccessMessage"]
    - Password field never pre-populated on GET — blank submit preserves existing DB value (D-08/D-09)
    - "[ValidateAntiForgeryToken] on Settings POST — consistent with all other AdminController POST actions"

key-files:
  created:
    - EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs
    - EuphoriaInn.Service/Views/Admin/Settings.cshtml
  modified:
    - EuphoriaInn.Service/Controllers/Admin/AdminController.cs
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
    - EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs

key-decisions:
  - "SettingsViewModel has no [Required] on OmphalosSharedSecret — blank = preserve existing (D-08 contract from Plan 01)"
  - "Settings GET action intentionally does NOT set model.OmphalosSharedSecret — type=password with autocomplete=off prevents browser pre-population (D-09)"
  - "Settings POST calls SaveSettingsAsync(model.OmphalosUrl, model.OmphalosSharedSecret, model.IsEnabled) — null/empty secret flows through to service which guards it (D-08)"
  - "[Authorize(Policy=AdminOnly)] inherited at class level — no per-action attribute needed on Settings actions"

# Metrics
duration: 3min
completed: 2026-06-18
---

# Phase 10 Plan 02: Admin Settings Web Layer Summary

**SettingsViewModel, AdminController Settings GET/POST, Settings.cshtml with password masking, admin navbar dropdown link, and 2 SETT-07 integration tests; all 74 integration tests green**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-18T12:54:57Z
- **Completed:** 2026-06-18T12:58:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- SettingsViewModel with `[Url]`, `[StringLength(2000)]` on OmphalosUrl and `[StringLength(500)]` on OmphalosSharedSecret; no `[Required]` on secret (D-08)
- AdminController extended: `IAdminSettingService adminSettingService` added to primary constructor
- Settings GET: populates OmphalosUrl and IsEnabled, never sets OmphalosSharedSecret (D-09)
- Settings POST: `[ValidateAntiForgeryToken]`, calls `SaveSettingsAsync`, PRG with TempData["SuccessMessage"]
- Settings.cshtml: modern-card layout, `type="password" autocomplete="off"` on secret field, hint "Leave blank to keep the existing value.", TempData feedback alerts, validation summary
- `_Layout.cshtml`: Integration Settings link (fa-plug icon) with dropdown-divider inserted after Quest Management in Admin dropdown
- SETT-07 tests: `[InlineData("/Admin/Settings")]` added to unauthenticated Theory; new `Settings_WhenNotAdmin_ShouldReturnForbiddenOrRedirect` Fact for Player-role access check
- Full integration test suite: 74/74 passing

## Task Commits

1. **Task 1: SettingsViewModel and AdminController Settings actions** - `90b29aa` (feat)
2. **Task 2: Settings.cshtml view, navbar link, and SETT-07 integration test** - `646e037` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs` — new file; [Url]/[StringLength] annotations, no [Required] on secret
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — constructor + Settings GET/POST actions
- `EuphoriaInn.Service/Views/Admin/Settings.cshtml` — new file; full form with password masking and PRG feedback
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — Integration Settings link inserted in Admin dropdown
- `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — InlineData + new Fact for SETT-07

## Decisions Made

- OmphalosSharedSecret has no `[Required]` attribute — by design, blank = preserve existing secret (D-08 established in Plan 01 service layer)
- GET action leaves `model.OmphalosSharedSecret` null so it never appears in the rendered page source (D-09 requirement)
- `[Authorize(Policy="AdminOnly")]` is class-level on AdminController; Settings actions inherit it automatically — no per-action attribute needed
- Integration Settings link placed after Quest Management with a `<hr class="dropdown-divider">` — consistent with Bootstrap dropdown pattern used elsewhere in the app

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — all fields wire to actual model/service data. OmphalosSharedSecret intentionally loads empty (D-09 security requirement, not a stub).

## Threat Flags

All surfaces covered by the plan's threat model (T-10-02-01 through T-10-02-05). No new unmodeled surfaces introduced.

## Self-Check: PASSED

Files created:
- `EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs` — FOUND
- `EuphoriaInn.Service/Views/Admin/Settings.cshtml` — FOUND

Commits:
- `90b29aa` — FOUND
- `646e037` — FOUND

Tests: 74/74 integration tests passing.

---
*Phase: 10-admin-settings*
*Completed: 2026-06-18*
