---
phase: 07-dm-profile-page
plan: 01
subsystem: data-service-layer
tags: [ef-core, automapper, repository, service, migration, dm-profile]
dependency_graph:
  requires: []
  provides:
    - IDungeonMasterProfileService
    - IDungeonMasterProfileRepository
    - DungeonMasterProfile (domain model)
    - DungeonMasterProfileEntity (EF entity)
    - DungeonMasterProfileImageEntity (EF entity)
    - GetQuestsByDungeonMasterAsync (IQuestService/IQuestRepository/QuestService/QuestRepository)
    - Migration AddDMProfileSystem
  affects:
    - EuphoriaInn.Domain (new model, interfaces, service, ServiceExtensions)
    - EuphoriaInn.Repository (new entities, repository, EntityProfile, QuestBoardContext, ServiceExtensions)
    - EuphoriaInn.Domain/Services/QuestService (new method)
    - EuphoriaInn.Repository/QuestRepository (new method)
tech_stack:
  added: []
  patterns:
    - CharacterImageEntity PK=FK pattern replicated for DungeonMasterProfileImageEntity
    - ValueGeneratedNever on DungeonMasterProfileEntity.Id (= UserId)
    - Lazy-create upsert pattern (service creates profile entity on first save)
    - BaseService<TModel> / BaseRepository<TModel, TEntity> extension pattern
key_files:
  created:
    - EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs
    - EuphoriaInn.Repository/Entities/DungeonMasterProfileImageEntity.cs
    - EuphoriaInn.Domain/Models/DungeonMasterProfile.cs
    - EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs
    - EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileRepository.cs
    - EuphoriaInn.Repository/DungeonMasterProfileRepository.cs
    - EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs
    - EuphoriaInn.Repository/Migrations/20260617191315_AddDMProfileSystem.cs
  modified:
    - EuphoriaInn.Repository/Entities/QuestBoardContext.cs
    - EuphoriaInn.Repository/Automapper/EntityProfile.cs
    - EuphoriaInn.Repository/Extensions/ServiceExtensions.cs
    - EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
    - EuphoriaInn.Domain/Interfaces/IQuestService.cs
    - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Repository/QuestRepository.cs
decisions:
  - "DungeonMasterProfileEntity.Id uses DatabaseGeneratedOption.None + ValueGeneratedNever() so EF does not auto-assign identity — Id equals UserId (1:1 extension of AspNetUsers)"
  - "UserEntity to DungeonMasterProfileEntity configured with OnDelete(DeleteBehavior.Cascade) — safe single path; avoids orphaned profile rows on user deletion"
  - "ViewModelProfile.cs ViewModel mappings deferred to Plan 02 — DMProfileViewModel and QuestSummaryViewModel do not exist in Wave 1; adding them now would break the build"
  - "GetQuestsByDungeonMasterAsync implemented in both tasks atomically — IQuestService update in Task 1 broke QuestService compilation, so IQuestRepository and QuestRepository were also updated in Task 1 to maintain a buildable state"
metrics:
  duration_minutes: 15
  completed_date: "2026-06-17T19:14:10Z"
  tasks_completed: 2
  files_changed: 18
---

# Phase 07 Plan 01: DM Profile Data and Service Layer Summary

**One-liner:** Two EF Core entities for DM profile storage (ProfileEntity with lazy-create, ImageEntity with PK=FK), full service/repository stack with upsert and image upload patterns, and GetQuestsByDungeonMasterAsync wired through all four layers.

## What Was Built

Complete data and service layer for the DM Profile subsystem. Plan 02 (web layer) can now compile — all interfaces, domain models, repositories, and service contracts exist and are DI-registered.

### Task 1: Entities, Domain Model, Interfaces, AutoMapper Profiles

- `DungeonMasterProfileEntity` — `[Table("DungeonMasterProfiles")]`, `Id` is PK with `DatabaseGeneratedOption.None` (= UserId), `Bio` varchar 2000 nullable, nav to `DungeonMasterProfileImageEntity`
- `DungeonMasterProfileImageEntity` — `[Table("DungeonMasterProfileImages")]`, `Id` is both PK and FK (`[ForeignKey(nameof(DungeonMasterProfile))]`) to profile entity, `byte[] ImageData`
- `DungeonMasterProfile` domain model — `IModel`, `Id/Bio/ProfilePicture`
- `IDungeonMasterProfileService` — extends `IBaseService<DungeonMasterProfile>`, adds `GetProfileByUserIdAsync`, `UpsertProfileAsync`, `GetProfilePictureAsync`
- `IDungeonMasterProfileRepository` — extends `IBaseRepository<DungeonMasterProfile>`, adds `GetProfileByUserIdAsync`, `GetProfilePictureAsync`, `UpsertProfileImageAsync`
- `IQuestService.GetQuestsByDungeonMasterAsync` — added after `CreateFollowUpQuestAsync`
- `QuestBoardContext` — added two DbSets, fluent config: `ValueGeneratedNever()`, 1:1 User→Profile with Cascade, 1:1 Profile→Image with Cascade
- `EntityProfile` — added `DungeonMasterProfileEntity↔DungeonMasterProfile` mappings (ProfilePicture flattened from/to nested image entity)

**Commit:** `5e8dee3`

### Task 2: Repository, Service, DI Registrations, EF Migration

- `DungeonMasterProfileRepository` — `internal class`, extends `BaseRepository<DungeonMasterProfile, DungeonMasterProfileEntity>`, implements `IDungeonMasterProfileRepository`; `UpsertProfileImageAsync` follows `CharacterRepository.UpdateProfileImageAsync` lazy-create pattern verbatim
- `DungeonMasterProfileService` — `internal class`, extends `BaseService<DungeonMasterProfile>`, implements `IDungeonMasterProfileService`; `UpsertProfileAsync` creates profile entity on first save (D-03 lazy-create)
- DI: `services.AddScoped<IDungeonMasterProfileRepository, DungeonMasterProfileRepository>()` in Repository ServiceExtensions
- DI: `services.AddScoped<IDungeonMasterProfileService, DungeonMasterProfileService>()` in Domain ServiceExtensions
- Migration `20260617191315_AddDMProfileSystem` — `CreateTable("DungeonMasterProfiles")` + `CreateTable("DungeonMasterProfileImages")` with correct FK and cascade delete

**Commit:** `7f17748`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] GetQuestsByDungeonMasterAsync pulled forward from Task 2 into Task 1**

- **Found during:** Task 1 verification build
- **Issue:** Task 1 added `GetQuestsByDungeonMasterAsync` to `IQuestService`, which caused `QuestService` (implements `IQuestService`) to fail with CS0535. Task 1 verification target was `EuphoriaInn.Repository --no-incremental` which builds Repository + Domain, so QuestService failure blocked the build.
- **Fix:** Added `GetQuestsByDungeonMasterAsync` to `IQuestRepository`, `QuestRepository`, and `QuestService` in Task 1 commit (they were planned for Task 2). Task 2 commit then correctly omits these as they already existed.
- **Files modified:** `IQuestRepository.cs`, `QuestRepository.cs`, `QuestService.cs`
- **Commit:** `5e8dee3` (Task 1)

**2. [Rule 2 - Deferred] ViewModelProfile.cs ViewModel mappings omitted from Task 1**

- **Found during:** Task 1 planning
- **Issue:** The plan included adding `DMProfileViewModel` and `QuestSummaryViewModel` mappings to `ViewModelProfile.cs` in Task 1 step 9. These ViewModels do not exist in the codebase — they are created by Plan 02 (Wave 2, web layer). Adding references to non-existent types would break the `EuphoriaInn.Service` build.
- **Fix:** ViewModelProfile.cs changes omitted from Plan 01. They will be added in Plan 02 when the ViewModels are created. The plan note "forward-safe as long as both projects build together" applies to Wave 2 execution context, not Wave 1.
- **Files modified:** None (omission)
- **Impact:** Plan 02 must add the ViewModelProfile.cs mappings alongside the ViewModel creation.

## Known Stubs

None. All data and service layer code is fully functional. The only deferred work is ViewModelProfile.cs ViewModel mappings which belong in Plan 02.

## Threat Flags

No new threat surface was introduced in this plan. All new files are internal data/service layer classes with no network endpoints, auth paths, or trust boundaries. The threat mitigations defined in the plan's `<threat_model>` (T-07-01 through T-07-04) are scoped to Plan 02 controller/ViewModel layer and will be verified there.

## Self-Check: PASSED

All key files exist and both task commits are present in git history.

| Check | Result |
|-------|--------|
| DungeonMasterProfileEntity.cs | FOUND |
| DungeonMasterProfileImageEntity.cs | FOUND |
| DungeonMasterProfile.cs (domain model) | FOUND |
| IDungeonMasterProfileService.cs | FOUND |
| IDungeonMasterProfileRepository.cs | FOUND |
| DungeonMasterProfileRepository.cs | FOUND |
| DungeonMasterProfileService.cs | FOUND |
| Migration AddDMProfileSystem.cs | FOUND |
| Commit 5e8dee3 (Task 1) | FOUND |
| Commit 7f17748 (Task 2) | FOUND |
