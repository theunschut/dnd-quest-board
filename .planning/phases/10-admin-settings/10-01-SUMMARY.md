---
phase: 10-admin-settings
plan: 01
subsystem: database
tags: [ef-core, repository-pattern, integration-tests, key-value-store, admin-settings]

# Dependency graph
requires: []
provides:
  - IAdminSettingService interface (consumed by Phase 11 and Plan 02)
  - IAdminSettingRepository interface
  - AdminSettingService implementation (internal)
  - AdminSettingRepository implementation (internal)
  - AdminSettingEntity EF entity with string PK
  - AdminSettings DB table via EF migration AddAdminSettings
  - IntegrationSettings record (OmphalosUrl, OmphalosSharedSecret, IsEnabled, IsConfigured)
  - 4 passing integration tests covering all D-10 test cases
affects: [10-02, 11-navigation-token]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Key-value store pattern via AdminSettingEntity (string PK, not IEntity)
    - Service with no BaseService/IModel inheritance when domain model lacks int Id
    - Repository with no BaseRepository inheritance when entity lacks IEntity constraint
    - InternalsVisibleTo("EuphoriaInn.IntegrationTests") on Domain and Repository for direct internal class instantiation in tests

key-files:
  created:
    - EuphoriaInn.Domain/Models/IntegrationSettings.cs
    - EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs
    - EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs
    - EuphoriaInn.Domain/Services/AdminSettingService.cs
    - EuphoriaInn.Repository/Entities/AdminSettingEntity.cs
    - EuphoriaInn.Repository/AdminSettingRepository.cs
    - EuphoriaInn.Repository/Properties/AssemblyInfo.cs
    - EuphoriaInn.Repository/Migrations/20260618124958_AddAdminSettings.cs
    - EuphoriaInn.Repository/Migrations/20260618124958_AddAdminSettings.Designer.cs
    - EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs
  modified:
    - EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
    - EuphoriaInn.Domain/Properties/AssemblyInfo.cs
    - EuphoriaInn.Repository/Entities/QuestBoardContext.cs
    - EuphoriaInn.Repository/Extensions/ServiceExtensions.cs
    - EuphoriaInn.Repository/Migrations/QuestBoardContextModelSnapshot.cs
    - EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj

key-decisions:
  - "AdminSettingEntity uses string PK (Key nvarchar(200)) and does not implement IEntity — avoids int Id constraint mismatch"
  - "AdminSettingService does not extend BaseService — IntegrationSettings is not an IModel (no int Id)"
  - "AdminSettingRepository does not extend BaseRepository — AdminSettingEntity does not implement IEntity"
  - "Blank secret on SaveSettingsAsync skips UpsertAsync entirely — preserves existing DB value per D-08"
  - "InternalsVisibleTo added to both Domain and Repository assemblies pointing at IntegrationTests — enables direct instantiation of internal classes in service-layer tests"
  - "EuphoriaInn.Domain added as direct project reference in IntegrationTests.csproj — required for InternalsVisibleTo to resolve AdminSettingService"

patterns-established:
  - "Non-IModel service: when a domain concept lacks int Id, skip IBaseService/BaseService entirely and implement IAdminSettingService directly"
  - "Non-IEntity repository: when an entity has a non-int PK, skip BaseRepository and implement IAdminSettingRepository directly with FindAsync([key]) syntax"
  - "Service-layer integration tests: use TestDatabase helper + direct internal class instantiation (not WebApplicationFactory) for pure domain/repo logic"

requirements-completed: [SETT-06, SETT-07, SETT-08]

# Metrics
duration: 25min
completed: 2026-06-18
---

# Phase 10 Plan 01: Admin Settings Data Layer Summary

**Key-value AdminSettings EF entity + IAdminSettingService/IAdminSettingRepository with blank-secret-preservation logic and 4 passing SQLite integration tests**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-06-18T12:26:00Z
- **Completed:** 2026-06-18T12:51:16Z
- **Tasks:** 2
- **Files modified:** 16

## Accomplishments

- IntegrationSettings record with OmphalosUrl, OmphalosSharedSecret, IsEnabled, and computed IsConfigured property
- IAdminSettingService + AdminSettingService with blank-secret preservation (D-08 compliant)
- AdminSettingEntity with string PK (does not implement IEntity), AdminSettingRepository (does not extend BaseRepository)
- EF migration AddAdminSettings creating AdminSettings table (Key nvarchar(200) PK, Value nvarchar(max), UpdatedAt datetime2)
- 4 integration tests passing: default-when-empty, stored-values-after-save, blank-secret-preserves, second-save-overwrites

## Task Commits

Each task was committed atomically:

1. **Task 1: Domain layer** - `c115a7f` (feat)
2. **Task 2: Repository layer + migration + tests** - `78e67f9` (feat)

**Plan metadata:** (committed with SUMMARY.md)

## Files Created/Modified

- `EuphoriaInn.Domain/Models/IntegrationSettings.cs` - Record with OmphalosUrl, OmphalosSharedSecret, IsEnabled, IsConfigured
- `EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs` - GetValueAsync/UpsertAsync contract
- `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` - GetSettingsAsync/SaveSettingsAsync contract used by Plan 02
- `EuphoriaInn.Domain/Services/AdminSettingService.cs` - Internal implementation; blank secret skips upsert
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` - Added IAdminSettingService Scoped registration
- `EuphoriaInn.Domain/Properties/AssemblyInfo.cs` - Added InternalsVisibleTo("EuphoriaInn.IntegrationTests")
- `EuphoriaInn.Repository/Entities/AdminSettingEntity.cs` - String PK entity, no IEntity
- `EuphoriaInn.Repository/AdminSettingRepository.cs` - Internal; uses FindAsync([key]) syntax; no BaseRepository
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` - Added AdminSettings DbSet
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` - Added IAdminSettingRepository Scoped registration
- `EuphoriaInn.Repository/Properties/AssemblyInfo.cs` - New file; InternalsVisibleTo("EuphoriaInn.IntegrationTests")
- `EuphoriaInn.Repository/Migrations/20260618124958_AddAdminSettings.cs` - EF migration
- `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` - 4 D-10 test cases
- `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` - Added direct Domain project reference

## Decisions Made

- AdminSettingEntity intentionally does not implement IEntity — its PK is `string Key`, not `int Id`. Using IEntity would cause a compile error.
- AdminSettingService does not extend BaseService — IntegrationSettings is not an IModel (no int Id). Custom interface is the right pattern here.
- Blank secret guard: `if (!string.IsNullOrWhiteSpace(secret))` — only calls UpsertAsync when a new secret is provided, preserving the existing DB value otherwise (D-08).
- Added direct `EuphoriaInn.Domain` project reference to IntegrationTests.csproj — required for `InternalsVisibleTo` to allow test code to instantiate `AdminSettingService` (internal class). Transitive references via Service are insufficient for internals access.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added using directive and direct project reference for AdminSettingService in test file**
- **Found during:** Task 2 (integration test creation)
- **Issue:** Test file's `new AdminSettingService(repo)` call failed to compile because (a) `EuphoriaInn.Domain.Services` namespace was not imported and (b) IntegrationTests.csproj had no direct reference to Domain (only transitive), making InternalsVisibleTo ineffective
- **Fix:** Added `using EuphoriaInn.Domain.Services;` to test file; added direct `<ProjectReference>` to Domain in IntegrationTests.csproj; added `EuphoriaInn.Repository/Properties/AssemblyInfo.cs` with `InternalsVisibleTo("EuphoriaInn.IntegrationTests")`
- **Files modified:** AdminSettingServiceTests.cs, EuphoriaInn.IntegrationTests.csproj, EuphoriaInn.Repository/Properties/AssemblyInfo.cs
- **Verification:** `dotnet build` 0 errors; all 4 tests pass
- **Committed in:** `78e67f9` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Required fix — test infrastructure needed to access internal classes across project boundaries. No scope creep.

## Issues Encountered

- Plan test template omitted the `using EuphoriaInn.Domain.Services;` import needed for `new AdminSettingService(repo)`. Resolved by adding the using directive plus making IntegrationTests directly reference EuphoriaInn.Domain.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `IAdminSettingService` is registered in DI and fully tested — Plan 02 (AdminController + settings UI) can proceed immediately
- Phase 11 (Navigation + Token Generation) depends on `IAdminSettingService.GetSettingsAsync().IsConfigured` — interface is ready
- EF migration will be auto-applied on next startup; no manual DB steps needed

---
*Phase: 10-admin-settings*
*Completed: 2026-06-18*
