---
phase: 11-navigation-token-generation
plan: "01"
subsystem: api
tags: [hmac-sha256, token-generation, csharp, aspnet-core, integration-testing, unit-testing]

# Dependency graph
requires:
  - phase: 10-admin-settings
    provides: IAdminSettingService, IntegrationSettings.IsConfigured, AdminSettingEntity
provides:
  - IIntegrationTokenService interface and BCL HMAC-SHA256 implementation in Domain layer
  - QuestController.LaunchOmphalos GET action (DungeonMasterOnly) generating signed redirect URLs
  - ViewBag.ShowOmphalosButton set on Details GET and Manage GET
  - DI registration for IIntegrationTokenService (AddTransient)
  - 7 unit tests covering TOKEN-01 through TOKEN-04
  - 6 integration tests covering TOKEN-05, NAV-03, NAV-04, NAV-05
affects:
  - 11-02-PLAN (Wave 2 view wiring depends on ViewBag.ShowOmphalosButton and LaunchOmphalos action existing)
  - Omphalos SSO endpoint (token format contract D-03 is the cross-repo handshake)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - HMAC-SHA256 signed URL generation using BCL HMACSHA256.HashData (static, .NET 5+)
    - Canonical MAC message with alphabetical key ordering locked by cross-repo contract
    - AddTransient for stateless pure-computation domain services
    - ViewBag.ShowOmphalosButton (bool) gating Omphalos UI on quest pages

key-files:
  created:
    - EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs
    - EuphoriaInn.Domain/Services/IntegrationTokenService.cs
    - EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs
    - EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs
  modified:
    - EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
    - EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs

key-decisions:
  - "IntegrationTokenService is stateless pure computation — AddTransient (not AddScoped) correct lifetime"
  - "Username in token uses currentUser.Name.ToLower() (display name), not User.Identity.Name (which is email in this app)"
  - "Uri.EscapeDataString for questTitle (percent-encoding, spaces to %20), NOT HttpUtility.UrlEncode (uses +)"
  - "OmphalosUrl.TrimEnd('/') before appending /api/sso/open-quest prevents double-slash"
  - "ViewBag.ShowOmphalosButton uses OrdinalIgnoreCase for DM name comparison on both Details and Manage GET"
  - "LaunchOmphalos decorated with [Authorize(Policy = DungeonMasterOnly)] for defense in depth before IsConfigured check"

patterns-established:
  - "Internal domain service with no constructor dependencies for pure-computation services"
  - "HMACSHA256.HashData(keyBytes, msgBytes) + Convert.ToHexString(hash).ToLower() for lowercase hex HMAC"
  - "Canonical MAC message: alphabetical key order locked by cross-repo contract (expiry, questId, questTitle, username)"
  - "Integration test: SeedSettingsAsync helper pattern for AdminSettingEntity seeding before HTTP request"

requirements-completed:
  - TOKEN-01
  - TOKEN-02
  - TOKEN-03
  - TOKEN-04
  - TOKEN-05
  - NAV-03
  - NAV-04
  - NAV-05

# Metrics
duration: 4min
completed: "2026-06-18"
---

# Phase 11 Plan 01: Navigation + Token Generation — Backend Summary

**HMAC-SHA256 token service + QuestController.LaunchOmphalos redirect endpoint with signed URL generation (BCL only, zero new NuGet packages)**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-06-18T20:59:13Z
- **Completed:** 2026-06-18T21:02:51Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Domain service `IntegrationTokenService` generates HMAC-SHA256 signed Omphalos SSO URLs using BCL only; canonical message is alphabetical key order per cross-repo contract D-03
- `QuestController.LaunchOmphalos` GET action added: `[Authorize(DungeonMasterOnly)]`, returns 404 when integration not configured, generates signed URL via `IIntegrationTokenService`, returns `302 Redirect`
- `ViewBag.ShowOmphalosButton` wired on both `Details GET` and `Manage GET` actions, gated by `settings.IsConfigured && (isQuestDm || isAdmin)`
- 13 tests total: 7 unit tests (pure function, no mocks) + 6 integration tests (auth, disabled settings, blank URL, redirect structure, query parameter correctness)

## Task Commits

Each task was committed atomically:

1. **Task 1: IIntegrationTokenService + implementation + DI** - `9118f0b` (feat)
2. **Task 2: QuestController extension + integration tests** - `ddd70ea` (feat)

## Files Created/Modified

- `EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs` - Token service contract: `GenerateSignedUrl(string, int, string, string, string)`
- `EuphoriaInn.Domain/Services/IntegrationTokenService.cs` - BCL HMAC-SHA256 implementation: canonical message, lowercase hex sig, trailing slash trim
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` - Added `AddTransient<IIntegrationTokenService, IntegrationTokenService>()`
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` - Extended constructor, ViewBag.ShowOmphalosButton on Details + Manage, LaunchOmphalos action
- `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` - 7 unit tests covering all TOKEN requirements
- `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` - 6 integration tests covering auth + settings gating + redirect correctness

## Decisions Made

None beyond plan — all decisions were pre-locked in CONTEXT.md (D-01 through D-19). Plan executed exactly as specified.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None — all code compiled first time, all tests passed on first run.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Wave 2 (11-02) can begin: `LaunchOmphalos` action exists, `ViewBag.ShowOmphalosButton` is set on quest pages, `IIntegrationTokenService` is registered in DI
- `dotnet build` exits 0, all 114 tests pass (34 unit + 80 integration)
- View wiring (`Details.cshtml`, `Manage.cshtml`, `_Layout.cshtml`, `OmphalosNavItem` ViewComponent) is the sole remaining work for this phase

---
*Phase: 11-navigation-token-generation*
*Completed: 2026-06-18*

## Self-Check: PASSED

All created files verified present. Both task commits verified in git log. Key implementation patterns confirmed (HMACSHA256.HashData, Convert.ToHexString, TrimEnd, AddTransient, LaunchOmphalos, ShowOmphalosButton, currentUser.Name.ToLower()).
