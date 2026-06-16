---
phase: 06-follow-up-quest
plan: 01
subsystem: database
tags: [ef-core, migrations, sql-server, self-referential-fk, quest, player-signup]

# Dependency graph
requires:
  - phase: 03-code-quality-dead-code
    provides: SignupRole enum fix required for importing players as Player role
provides:
  - Nullable OriginalQuestId self-referential FK on QuestEntity and Quest domain model
  - EF Core migration AddFollowUpQuestLink adding the FK column to SQL Server
  - IQuestService.CreateFollowUpQuestAsync service contract
  - QuestService.CreateFollowUpQuestAsync implementation (D-01 through D-07, D-11, D-14)
  - IQuestRepository.HasFollowUpQuestAsync for D-11 enforcement
affects: [06-follow-up-quest plan 02 (controller and views)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Self-referential FK on QuestEntity using EF Core one-to-one HasOne/WithOne with ClientSetNull"
    - "Navigation properties ignored in AutoMapper to prevent circular mapping cycles"
    - "Follow-up existence checked via dedicated repository query (HasFollowUpQuestAsync) rather than nav property"

key-files:
  created:
    - EuphoriaInn.Repository/Migrations/20260616205501_AddFollowUpQuestLink.cs
    - EuphoriaInn.Repository/Migrations/20260616205501_AddFollowUpQuestLink.Designer.cs
  modified:
    - EuphoriaInn.Repository/Entities/QuestEntity.cs
    - EuphoriaInn.Domain/Models/QuestBoard/Quest.cs
    - EuphoriaInn.Repository/Automapper/EntityProfile.cs
    - EuphoriaInn.Repository/Entities/QuestBoardContext.cs
    - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
    - EuphoriaInn.Domain/Interfaces/IQuestService.cs
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Repository/QuestRepository.cs
    - EuphoriaInn.Repository/EuphoriaInn.Repository.csproj
    - EuphoriaInn.Service/EuphoriaInn.Service.csproj

key-decisions:
  - "EF Core packages updated 10.0.7 -> 10.0.9 to match upgraded dotnet-ef global tool (was 9.0.6 before update)"
  - "Navigation properties (OriginalQuest, FollowUpQuest) ignored in AutoMapper to prevent self-referential circular mapping"
  - "D-11 enforcement uses HasFollowUpQuestAsync(questId) database query rather than nav property check - nav properties are not included in GetQuestWithDetailsAsync query projection"
  - "ClientSetNull delete behaviour: EF Core handles null-setting client-side before save; SQL Server FK constraint uses NO ACTION (prevents cascade cycles)"
  - "Migration 20260616205501_AddFollowUpQuestLink adds nullable OriginalQuestId column with unique index"

patterns-established:
  - "Self-referential FK pattern: QuestEntity.HasOne(OriginalQuest).WithOne(FollowUpQuest).ClientSetNull"
  - "Scalar FK (OriginalQuestId) round-trips via AutoMapper; navigation objects explicitly ignored to prevent recursion"

requirements-completed: [FOLLOW-04, FOLLOW-05]

# Metrics
duration: 28min
completed: 2026-06-16
---

# Phase 06 Plan 01: Follow-Up Quest Data Layer Summary

**Self-referential OriginalQuestId FK on QuestEntity with EF Core migration, AutoMapper cycle guard, and CreateFollowUpQuestAsync service method copying D-01 through D-07 decisions**

## Performance

- **Duration:** 28 min
- **Started:** 2026-06-16T20:30:00Z
- **Completed:** 2026-06-16T20:58:00Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- Added nullable `OriginalQuestId` self-referential FK to `QuestEntity` and `Quest` domain model with matching `OriginalQuest`/`FollowUpQuest` navigation properties
- Created EF Core migration `AddFollowUpQuestLink` (nullable int column + unique index + self-referential FK constraint)
- Implemented `CreateFollowUpQuestAsync` in QuestService: copies title/description/CR/playerCount/DM, appends " - Part 2", clears dates, resets DM session, bulk-imports IsSelected=true players as SignupRole.Player
- Added `HasFollowUpQuestAsync` to IQuestRepository + QuestRepository for reliable D-11 enforcement via database query

## Task Commits

Each task was committed atomically:

1. **Task 1: Add OriginalQuestId FK and navigation properties** - `151bdad` (feat)
2. **Task 2: EF migration, service interface + implementation** - `93c7ff1` (feat)

## Files Created/Modified
- `EuphoriaInn.Repository/Entities/QuestEntity.cs` - Added OriginalQuestId, OriginalQuest, FollowUpQuest
- `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` - Added OriginalQuestId, OriginalQuest, FollowUpQuest
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` - Split ReverseMap into two explicit maps with Ignore() for nav properties
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` - Added self-referential HasOne/WithOne FK config with ClientSetNull
- `EuphoriaInn.Repository/Migrations/20260616205501_AddFollowUpQuestLink.cs` - New migration for OriginalQuestId column
- `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs` - Added HasFollowUpQuestAsync
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs` - Added CreateFollowUpQuestAsync
- `EuphoriaInn.Domain/Services/QuestService.cs` - Implemented CreateFollowUpQuestAsync
- `EuphoriaInn.Repository/QuestRepository.cs` - Implemented HasFollowUpQuestAsync
- `EuphoriaInn.Repository/EuphoriaInn.Repository.csproj` + `EuphoriaInn.Service/EuphoriaInn.Service.csproj` - Updated EF Core packages 10.0.7 -> 10.0.9

## Decisions Made
- **EF Core package upgrade 10.0.7 -> 10.0.9**: The globally installed `dotnet-ef` tool was version 9.0.6, which was incompatible with .NET 10 runtime. After upgrading the tool to 10.0.9, the project packages needed to match to resolve a `MissingMethodException`. Packages updated accordingly.
- **AutoMapper navigation property ignoring**: `OriginalQuest` and `FollowUpQuest` are ignored in both map directions to prevent EF circular reference recursion. Only the scalar `OriginalQuestId` round-trips automatically.
- **HasFollowUpQuestAsync for D-11**: The `GetQuestWithDetailsAsync` method uses `ProjectWithoutCharacterImages` which does not include `FollowUpQuest` navigation. Checking `original.FollowUpQuest != null` would always be false. Added a dedicated `HasFollowUpQuestAsync(questId)` query using `AnyAsync` to enforce D-11 reliably.
- **ClientSetNull delete behavior**: Produces no SQL `ON DELETE` clause (default `NO ACTION`) in SQL Server, which is consistent with this project's pattern of avoiding cascade cycles. EF Core handles null-setting client-side before save.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] EF Core tool/runtime version mismatch**
- **Found during:** Task 2 (EF Core migration generation)
- **Issue:** `dotnet-ef` global tool was version 9.0.6, runtime was .NET 10.0.7; `MissingMethodException` when running migration add
- **Fix:** Updated `dotnet-ef` global tool to 10.0.9 and EF Core packages in Repository/Service projects from 10.0.7 to 10.0.9
- **Files modified:** EuphoriaInn.Repository/EuphoriaInn.Repository.csproj, EuphoriaInn.Service/EuphoriaInn.Service.csproj
- **Verification:** Migration generated successfully; `dotnet build` exits 0
- **Committed in:** `93c7ff1` (Task 2 commit)

**2. [Rule 2 - Missing Critical] Added HasFollowUpQuestAsync for reliable D-11 enforcement**
- **Found during:** Task 2 (QuestService.CreateFollowUpQuestAsync implementation)
- **Issue:** Plan specified `original.FollowUpQuest != null` check but `FollowUpQuest` nav property is ignored in AutoMapper and not included in `GetQuestWithDetailsAsync` projection — check would always be false, allowing duplicate follow-ups
- **Fix:** Added `HasFollowUpQuestAsync(int questId)` to IQuestRepository and QuestRepository using `AnyAsync(q => q.OriginalQuestId == questId)`; updated QuestService to call it
- **Files modified:** EuphoriaInn.Domain/Interfaces/IQuestRepository.cs, EuphoriaInn.Repository/QuestRepository.cs, EuphoriaInn.Domain/Services/QuestService.cs
- **Verification:** Build succeeds; D-11 enforcement uses database query
- **Committed in:** `93c7ff1` (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 missing critical)
**Impact on plan:** Both fixes necessary for correct functioning. Package upgrade was a prerequisite for tooling. HasFollowUpQuestAsync ensures D-11 is enforced as intended.

## Issues Encountered
- EF Tools version mismatch required package upgrades before migration could be generated (see deviations)

## User Setup Required
None - no external service configuration required. The migration will auto-apply on next application startup.

## Next Phase Readiness
- Data layer complete: OriginalQuestId FK, migration, domain navigation, service contract all stable
- Plan 02 can now build the controller actions (`CreateFollowUp` GET/POST) and views (Manage.cshtml button, Details/Manage sidebar "Continues in/from" links)
- `IQuestService.CreateFollowUpQuestAsync(int originalQuestId)` is the stable contract for the controller to call

---
*Phase: 06-follow-up-quest*
*Completed: 2026-06-16*
