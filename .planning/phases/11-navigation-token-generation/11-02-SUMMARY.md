---
phase: 11-navigation-token-generation
plan: "02"
subsystem: ui
tags: [viewcomponent, razor, aspnet-core, bootstrap5, navigation, omphalos-integration]

# Dependency graph
requires:
  - phase: 11-01
    provides: LaunchOmphalos action, ViewBag.ShowOmphalosButton, IAdminSettingService
  - phase: 10-admin-settings
    provides: IAdminSettingService, IntegrationSettings.IsConfigured
provides:
  - OmphalosNavItemViewComponent (first ViewComponent in codebase — auto-discovered by ASP.NET Core convention)
  - Default.cshtml view at Views/Shared/Components/OmphalosNavItem/Default.cshtml
  - DM navbar dropdown "Open Omphalos" link (gated by IsConfigured, opens new tab)
  - Details.cshtml "Open Session Notes" button inside DM Controls card (gated by ShowOmphalosButton)
  - Manage.cshtml "Session Notes" sidebar card with "Open Session Notes" button (gated by ShowOmphalosButton)
affects:
  - NAV-01 through NAV-05 requirements — all now complete
  - Phase 11 phase gate (human-verify checkpoint in 11-03)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - First ASP.NET Core ViewComponent in codebase — auto-discovered by convention (class ends ViewComponent)
    - Dropdown-divider inside ViewComponent Default.cshtml (not in _Layout.cshtml) — prevents orphaned hr when disabled
    - (bool)(ViewBag.ShowOmphalosButton ?? false) null-safe defensive cast for dynamic ViewBag in Razor views
    - target="_blank" rel="noopener noreferrer" on external link (NAV-02 tabnapping prevention)

key-files:
  created:
    - EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs
    - EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml
  modified:
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
    - EuphoriaInn.Service/Views/Quest/Details.cshtml
    - EuphoriaInn.Service/Views/Quest/Manage.cshtml

key-decisions:
  - "Dropdown divider placed inside Default.cshtml (not _Layout.cshtml) — prevents orphaned hr when integration is disabled and ViewComponent returns Content(string.Empty)"
  - "(bool)(ViewBag.ShowOmphalosButton ?? false) null-safe cast — defensive against null ViewBag on non-DM request paths"
  - "No @if wrapper in _Layout.cshtml around Component.InvokeAsync — ViewComponent handles its own visibility gating"

# Metrics
duration: 1min
completed: "2026-06-18"
---

# Phase 11 Plan 02: Navigation + Token Generation — UI Layer Summary

**OmphalosNavItem ViewComponent wiring all Omphalos UI elements: navbar dropdown link, Details DM Controls button, and Manage Session Notes card — all gated by IsConfigured/ShowOmphalosButton**

## Performance

- **Duration:** ~1 min
- **Started:** 2026-06-18T21:07:22Z
- **Completed:** 2026-06-18T21:09:17Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- `OmphalosNavItemViewComponent` created — injects `IAdminSettingService`, returns `Content(string.Empty)` when `!IsConfigured`, returns `View(settings)` with `IntegrationSettings` model when configured
- `Default.cshtml` created at `Views/Shared/Components/OmphalosNavItem/Default.cshtml` — renders dropdown-divider + "Open Omphalos" link with `target="_blank" rel="noopener noreferrer"` (NAV-02)
- `_Layout.cshtml` DM dropdown wired: `@await Component.InvokeAsync("OmphalosNavItem")` inserted after "Edit My Profile" `</li>`, before `</ul>`
- `Details.cshtml` DM Controls card extended: `btn-warning w-100 mt-2` "Open Session Notes" button, gated by `(bool)(ViewBag.ShowOmphalosButton ?? false)`, routes to `LaunchOmphalos` with `Model.Quest?.Id`
- `Manage.cshtml` sidebar extended: new `modern-card` "Session Notes" card after "View Public Page" card, gated by `ShowOmphalosButton`, `btn-warning w-100` button routes to `LaunchOmphalos` with `Model.Id`
- All 114 tests pass (34 unit + 80 integration) — no new tests required for pure view changes

## Task Commits

Each task was committed atomically:

1. **Task 1: OmphalosNavItem ViewComponent + Default.cshtml + _Layout.cshtml wiring** - `679dcad` (feat)
2. **Task 2: Details.cshtml button + Manage.cshtml Session Notes card** - `837740c` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` — ViewComponent: IsConfigured gate, Content(string.Empty) vs View(settings)
- `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` — Navbar dropdown item with divider, external link, fa-external-link-alt icon
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — Added `@await Component.InvokeAsync("OmphalosNavItem")` in DM dropdown
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` — Added ShowOmphalosButton-gated "Open Session Notes" btn-warning in DM Controls card-body
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — Added ShowOmphalosButton-gated "Session Notes" modern-card sidebar card

## Decisions Made

- Dropdown divider placed inside Default.cshtml (not _Layout.cshtml) — prevents orphaned `<hr>` when integration is disabled and ViewComponent returns `Content(string.Empty)`
- `(bool)(ViewBag.ShowOmphalosButton ?? false)` null-safe cast pattern applied consistently in both views — defensive against null ViewBag on non-DM request paths per RESEARCH.md Risk 2
- No `@if` wrapper needed in `_Layout.cshtml` around `Component.InvokeAsync` — ViewComponent handles its own visibility gating

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None — all code compiled first time, all tests passed on first run.

## User Setup Required

None — no configuration required for the UI layer. Integration visibility is controlled by Admin Settings (Phase 10).

## Next Phase Readiness

- Phase 11 is now feature-complete: Wave 1 (backend) and Wave 2 (UI) both delivered
- Human-verify checkpoint: log in as DM with integration configured to verify navbar link and quest page buttons render correctly
- All 114 tests pass; `dotnet build` exits 0

## Known Stubs

None — all UI elements are wired to live data sources (IAdminSettingService via ViewComponent, ViewBag.ShowOmphalosButton from QuestController).

## Threat Flags

No new security surface introduced in this plan beyond what was analyzed in the plan's threat model (T-11-08 through T-11-11). All accepted.

---
*Phase: 11-navigation-token-generation*
*Completed: 2026-06-18*
