# Phase 1: Layer Dependency Fix - Context

**Gathered:** 2026-04-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove the compile-time `<ProjectReference>` from `EuphoriaInn.Domain` → `EuphoriaInn.Repository`.
After this phase, the dependency direction `Service → Domain ← Repository` is enforced at build
time: `dotnet build` succeeds with Domain having zero references to the Repository project.

This phase does NOT include controller slimming, email refactor, or any other changes — those
belong to Phase 2. Scope is strictly the compile-time dependency inversion.

</domain>

<decisions>
## Implementation Decisions

### Repository Interfaces — New Home

- **D-01:** Move all repository interfaces from `EuphoriaInn.Repository/Interfaces/` to
  `EuphoriaInn.Domain/Interfaces/`. This is the standard clean architecture placement:
  Domain defines the repository contracts it needs; Repository implements them.
  - Files to move: `IBaseRepository.cs`, `IQuestRepository.cs`, `ICharacterRepository.cs`,
    `IPlayerSignupRepository.cs`, `IShopRepository.cs`, `ITradeItemRepository.cs`,
    `IUserRepository.cs`, `IUserTransactionRepository.cs`
  - Namespaces update from `EuphoriaInn.Repository.Interfaces` → `EuphoriaInn.Domain.Interfaces`
  - Repository implementations update their `using` directives accordingly
  - Domain service `using EuphoriaInn.Repository.Interfaces` directives are replaced with
    `using EuphoriaInn.Domain.Interfaces`

### Entity Construction in Domain Services

- **D-02:** Domain services (`QuestService`, `CharacterService`) that currently instantiate
  concrete `*Entity` types must have that logic pushed down into new Repository methods.
  Domain services call the interface; Repository implementations own entity construction.
  - `QuestService.UpdateProposedDatesIntelligentlyAsync` and
    `UpdateProposedDatesWithNotificationTrackingAsync` → extract to
    `IQuestRepository.UpdateProposedDatesAsync(int questId, IList<DateTime> newDates, CancellationToken)`.
    The domain service calls this method; `QuestRepository` implements it using `ProposedDateEntity`.
  - `CharacterService` profile image assignment (`entity.ProfileImage = new CharacterImageEntity { ... }`)
    → extract to `ICharacterRepository.UpdateProfileImageAsync(int characterId, byte[] imageData, CancellationToken)`.
    `CharacterRepository` implements it using `CharacterImageEntity`.
  - Signatures on these new interface methods use only primitive or domain model types — no entity types cross the boundary.

### EntityProfile Move

- **D-03:** Move `EuphoriaInn.Domain/Automapper/EntityProfile.cs` →
  `EuphoriaInn.Repository/Automapper/EntityProfile.cs`.
  - Update namespace: `EuphoriaInn.Domain.Automapper` → `EuphoriaInn.Repository.Automapper`
  - Delete the old file and the `Automapper/` subdirectory from Domain (if it becomes empty)
  - Update `Program.cs`: the `AddProfile<EntityProfile>()` call already uses an explicit type reference;
    update the `using` to `EuphoriaInn.Repository.Automapper` (ARCH-04 requires no assembly scanning — keep explicit type refs)

### Removal Scope — Minimum Viable

- **D-04:** Only remove what blocks the build. Once EntityProfile is moved, repository interfaces
  are in Domain, and the two targeted repository methods are added, drop the `<ProjectReference>`
  from `EuphoriaInn.Domain.csproj` and verify `dotnet build` passes.
  - No broader audit of EF-adjacent patterns in Domain services during this phase.
  - Remaining service-level improvements (business logic out of controllers, email refactor)
    are Phase 2 scope.

### Claude's Discretion

- The exact signature of `UpdateProposedDatesAsync` (return type, whether it returns
  the updated entity or just mutates in place) is left to the planner to decide based on
  how `QuestService` currently uses the result.
- Ordering of the four changes within Phase 1 plans (EntityProfile first vs interfaces first)
  is left to the planner.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Files Being Changed
- `EuphoriaInn.Domain/EuphoriaInn.Domain.csproj` — ProjectReference to remove
- `EuphoriaInn.Domain/Automapper/EntityProfile.cs` — file to move
- `EuphoriaInn.Repository/Interfaces/` — all interface files to relocate to Domain
- `EuphoriaInn.Domain/Services/QuestService.cs` — contains entity-instantiating private methods
- `EuphoriaInn.Domain/Services/CharacterService.cs` — contains `CharacterImageEntity` instantiation
- `EuphoriaInn.Domain/Services/BaseService.cs` — uses `IBaseRepository<TEntity>` (interface must move)
- `EuphoriaInn.Service/Program.cs` — AutoMapper profile registration (line ~73-76)

### Requirements
- `.planning/REQUIREMENTS.md` §Architecture Refactor — ARCH-01 through ARCH-04

### Codebase Map
- `.planning/codebase/ARCHITECTURE.md` — layer overview and data flow diagrams
- `.planning/research/SUMMARY.md` — recommended approach context

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EuphoriaInn.Domain/Interfaces/` — already contains service interfaces (IBaseService, IQuestService, etc.); repository interfaces will be added alongside these
- `EuphoriaInn.Repository/Automapper/` — does not yet exist; create this directory for EntityProfile
- `EuphoriaInn.Service/Program.cs:73-76` — AutoMapper registration using explicit `AddProfile<T>()` calls; already satisfies ARCH-04 once the using is updated

### Established Patterns
- All Domain service implementations are `internal` — this does not change
- Repository implementations (`QuestRepository`, `CharacterRepository`) already extend `BaseRepository<T>`; new methods (`UpdateProposedDatesAsync`, `UpdateProfileImageAsync`) are added as overrides or new methods on these concrete classes
- `IBaseRepository<T>` is generic with no entity constraints in the interface signature — safe to move to Domain without pulling in any entity types

### Integration Points
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — registers repositories; no changes expected
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — registers domain services; no changes expected
- The `<ProjectReference>` in `EuphoriaInn.Repository.csproj` pointing to Domain stays (Repository → Domain is correct)

</code_context>

<specifics>
## Specific Ideas

- Research confirmed: do NOT use AutoMapper assembly scanning. Keep `AddProfile<EntityProfile>()` and `AddProfile<ViewModelProfile>()` as explicit type references.
- The two new repository methods (`UpdateProposedDatesAsync`, `UpdateProfileImageAsync`) must use only primitive types or domain models in their signatures — no `*Entity` types may appear in the interface definition (that would re-introduce the dependency).

</specifics>

<deferred>
## Deferred Ideas

- Full sweep of EF-adjacent patterns remaining in Domain services — intentionally deferred to avoid scope creep; Phase 2 picks up controller/service improvements
- MailKit migration (replacing deprecated `SmtpClient`) — out of scope for Milestone 2 per PROJECT.md research findings

</deferred>

---

*Phase: 01-layer-dependency-fix*
*Context gathered: 2026-04-15*
