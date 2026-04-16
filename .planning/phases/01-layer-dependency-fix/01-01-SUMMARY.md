---
phase: 01-layer-dependency-fix
plan: 01
subsystem: architecture
tags: [automapper, entity-framework, clean-architecture, repository-pattern, dependency-inversion]

requires: []
provides:
  - "EntityProfile.cs in EuphoriaInn.Repository.Automapper with correct namespace"
  - "BaseRepository<TModel, TEntity> dual-generic with IMapper injection"
  - "All repository interfaces in EuphoriaInn.Domain.Interfaces with domain-model signatures"
  - "IIdentityService in Domain.Interfaces abstracting ASP.NET Core Identity operations"
  - "IdentityService in Repository implementing IIdentityService using UserEntity"
  - "Domain.csproj has zero ProjectReferences to Repository"
  - "Repository.csproj references Domain and AutoMapper"
  - "All concrete repositories implement Domain interfaces and return domain models"
affects: [01-02-layer-dependency-fix, 02-controller-slim, future-phases]

tech-stack:
  added: [AutoMapper 14.0.0 in Repository project]
  patterns:
    - "Dual-generic BaseRepository<TModel, TEntity> with IMapper maps entities to models before returning"
    - "Repository interfaces defined in Domain.Interfaces with domain-model signatures (not entity types)"
    - "IIdentityService pattern: Domain defines interface, Repository implements with UserEntity"
    - "Entity mutation logic (finalize, open quest, update profile image, proposed dates) lives in concrete repositories"

key-files:
  created:
    - "EuphoriaInn.Repository/Automapper/EntityProfile.cs"
    - "EuphoriaInn.Domain/Interfaces/IBaseRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IQuestRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/ICharacterRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IPlayerSignupRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IShopRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/ITradeItemRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IUserRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IUserTransactionRepository.cs"
    - "EuphoriaInn.Domain/Interfaces/IIdentityService.cs"
    - "EuphoriaInn.Repository/IdentityService.cs"
  modified:
    - "EuphoriaInn.Repository/EuphoriaInn.Repository.csproj"
    - "EuphoriaInn.Domain/EuphoriaInn.Domain.csproj"
    - "EuphoriaInn.Service/EuphoriaInn.Service.csproj"
    - "EuphoriaInn.Service/Program.cs"
    - "EuphoriaInn.Repository/BaseRepository.cs"
    - "EuphoriaInn.Repository/QuestRepository.cs"
    - "EuphoriaInn.Repository/CharacterRepository.cs"
    - "EuphoriaInn.Repository/PlayerSignupRepository.cs"
    - "EuphoriaInn.Repository/ShopRepository.cs"
    - "EuphoriaInn.Repository/TradeItemRepository.cs"
    - "EuphoriaInn.Repository/UserRepository.cs"
    - "EuphoriaInn.Repository/UserTransactionRepository.cs"
    - "EuphoriaInn.Repository/Extensions/ServiceExtensions.cs"
    - "EuphoriaInn.Domain/Services/BaseService.cs"
    - "EuphoriaInn.Domain/Services/QuestService.cs"
    - "EuphoriaInn.Domain/Services/CharacterService.cs"
    - "EuphoriaInn.Domain/Services/PlayerSignupService.cs"
    - "EuphoriaInn.Domain/Services/UserService.cs"
    - "EuphoriaInn.Domain/Services/ShopService.cs"
    - "EuphoriaInn.Domain/Extensions/ServiceExtensions.cs"

key-decisions:
  - "Plan 01-01 and 01-02 merged: circular project reference (Domain->Repository + Repository->Domain) made sequential execution impossible"
  - "IIdentityService pattern introduced: Domain defines interface, Repository implements with UserEntity — keeps Identity coupling out of Domain"
  - "BaseRepository<TModel, TEntity> implements IBaseRepository<TModel> (domain-model-typed) not IBaseRepository<TEntity>"
  - "Complex entity mutation logic (FinalizeQuestAsync, OpenQuestAsync, UpdateProfileImageAsync, UpdateProposedDatesAsync) pushed to concrete repositories where entity types are accessible"
  - "Service project now directly references Repository project (in addition to Domain) to access QuestBoardContext and UserEntity for Program.cs"

patterns-established:
  - "Repository layer owns all entity-to-model mapping via AutoMapper in concrete repository methods"
  - "Domain services delegate to domain interfaces without any entity type knowledge"
  - "Dependency direction enforced at compile time: Service -> Repository -> Domain (no reverse deps)"

requirements-completed: [ARCH-01]

duration: 45min
completed: 2026-04-16
---

# Phase 01 Plan 01: Layer Dependency Fix Summary

**EntityProfile moved to Repository, all repository interfaces promoted to Domain with domain-model signatures, BaseRepository<TModel, TEntity> with IMapper established, Domain decoupled from Repository at compile time**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-04-16T07:00:00Z
- **Completed:** 2026-04-16T07:31:47Z
- **Tasks:** 2 (combined with Plan 02 scope due to circular dependency constraint)
- **Files modified:** 31

## Accomplishments
- EntityProfile.cs relocated from EuphoriaInn.Domain/Automapper to EuphoriaInn.Repository/Automapper with updated namespace
- Domain.csproj ProjectReference to Repository removed — Domain compiles in isolation
- All 8 repository interfaces moved to EuphoriaInn.Domain/Interfaces with domain-model return types
- BaseRepository<TModel, TEntity> with IMapper implements IBaseRepository<TModel> — all CRUD returns mapped domain models
- All 7 concrete repositories implement Domain interfaces and map entities to models internally
- IIdentityService + IdentityService pattern cleanly abstracts ASP.NET Core Identity from Domain
- Solution builds with zero errors, all 16 unit tests pass

## Task Commits

1. **Task 1: Move EntityProfile to Repository + remove Domain->Repository dependency** - `de3bd9e` (feat)
2. **Task 2: Refactor BaseRepository to dual-generic and update all concrete repositories** - `6234e93` (feat)

## Files Created/Modified

**Created:**
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` - AutoMapper entity-to-model profiles, namespace EuphoriaInn.Repository.Automapper
- `EuphoriaInn.Domain/Interfaces/IBaseRepository.cs` - Generic domain-model-typed CRUD interface
- `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs` - Quest repository contract with Quest return types
- `EuphoriaInn.Domain/Interfaces/ICharacterRepository.cs` - Character repository contract with UpdateProfileImageAsync
- `EuphoriaInn.Domain/Interfaces/IPlayerSignupRepository.cs` - PlayerSignup repository contract
- `EuphoriaInn.Domain/Interfaces/IShopRepository.cs` - ShopItem repository contract
- `EuphoriaInn.Domain/Interfaces/ITradeItemRepository.cs` - TradeItem repository contract
- `EuphoriaInn.Domain/Interfaces/IUserRepository.cs` - User repository contract
- `EuphoriaInn.Domain/Interfaces/IUserTransactionRepository.cs` - UserTransaction repository contract
- `EuphoriaInn.Domain/Interfaces/IIdentityService.cs` - ASP.NET Core Identity abstraction for Domain
- `EuphoriaInn.Repository/IdentityService.cs` - IIdentityService implementation using UserManager/SignInManager

**Modified:**
- `EuphoriaInn.Repository/BaseRepository.cs` - Dual-generic BaseRepository<TModel, TEntity> with IMapper
- `EuphoriaInn.Repository/[all 7 concrete repositories]` - Implement Domain interfaces, map entities to models
- `EuphoriaInn.Domain/Services/[all 6 services]` - Use Domain.Interfaces only, no Repository entity references
- `EuphoriaInn.Domain/EuphoriaInn.Domain.csproj` - Repository ProjectReference removed
- `EuphoriaInn.Repository/EuphoriaInn.Repository.csproj` - Added AutoMapper and Domain ProjectReference
- `EuphoriaInn.Service/EuphoriaInn.Service.csproj` - Added Repository ProjectReference
- `EuphoriaInn.Service/Program.cs` - Updated using directive to EuphoriaInn.Repository.Automapper

## Decisions Made
- Plan 01-01 and 01-02 merged into single execution because circular project references (both directions simultaneously) crash .NET restore — they cannot be applied sequentially
- IIdentityService interface introduced to avoid Domain services needing UserManager<UserEntity> (entity type from Repository)
- Complex entity navigation mutations (finalize, open, update dates) moved from Domain QuestService into QuestRepository where ProposedDateEntity/PlayerSignupEntity are accessible
- Service project directly references both Domain and Repository (Service is composition root and may reference all layers)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Plans 01-01 and 01-02 merged due to circular project reference**
- **Found during:** Task 1 (EntityProfile move)
- **Issue:** Adding Repository -> Domain ProjectReference while Domain -> Repository still existed created a circular dependency that fails .NET build immediately (`MSB4006: circular dependency`). The plans were sequenced incorrectly — they cannot be applied one at a time.
- **Fix:** Combined both plans' work: removed Domain -> Repository reference, moved all repository interfaces to Domain.Interfaces with domain-model signatures, updated all Domain services to use Domain interfaces, updated all Repository implementations to implement Domain interfaces, added IdentityService to abstract Identity operations
- **Files modified:** All files listed in both 01-01 and 01-02 PLAN.md
- **Verification:** `dotnet build EuphoriaInn.slnx` passes with 0 errors; 16 unit tests pass
- **Committed in:** de3bd9e (Task 1), 6234e93 (Task 2)

**2. [Rule 3 - Blocking] Added IIdentityService + IdentityService to handle UserManager<UserEntity> coupling**
- **Found during:** Task 1 (Domain service refactoring)
- **Issue:** UserService needed `UserManager<UserEntity>` and `SignInManager<UserEntity>` — concrete Identity types from Repository. Domain cannot reference Repository entities, so a direct dependency was impossible.
- **Fix:** Defined `IIdentityService` interface in Domain.Interfaces with int-based user ID parameters; implemented `IdentityService` in Repository using `UserManager<UserEntity>`. UserService now injects IIdentityService.
- **Files modified:** EuphoriaInn.Domain/Interfaces/IIdentityService.cs (new), EuphoriaInn.Repository/IdentityService.cs (new), EuphoriaInn.Domain/Services/UserService.cs
- **Verification:** Domain compiles in isolation; build passes
- **Committed in:** de3bd9e

**3. [Rule 3 - Blocking] Service.csproj added Repository ProjectReference**
- **Found during:** Task 1 (build verification)
- **Issue:** Program.cs references QuestBoardContext and UserEntity from Repository; Service had only Domain ProjectReference which is insufficient
- **Fix:** Added `<ProjectReference Include="..\EuphoriaInn.Repository\EuphoriaInn.Repository.csproj" />` to Service.csproj
- **Files modified:** EuphoriaInn.Service/EuphoriaInn.Service.csproj
- **Verification:** Build passes
- **Committed in:** de3bd9e

---

**Total deviations:** 3 auto-fixed (all Rule 3 - blocking)
**Impact on plan:** Plans 01-01 and 01-02 were merged into one execution. All work is correct and complete. Plan 02 is now effectively done as part of Plan 01. The REQUIREMENTS.md entry for ARCH-02/ARCH-03/ARCH-04 should also be marked complete.

## Known Stubs

None — all domain services delegate to repositories, all repositories return populated domain models.

## Next Phase Readiness
- Domain layer is fully decoupled from Repository at compile time
- Repository layer owns all entity-to-model mapping via AutoMapper
- All repository interfaces are in Domain.Interfaces with domain-model signatures
- Plan 01-02 scope is fully complete — no separate execution needed for 01-02
- Phase 02 (controller slimming) can proceed immediately

---
*Phase: 01-layer-dependency-fix*
*Completed: 2026-04-16*
