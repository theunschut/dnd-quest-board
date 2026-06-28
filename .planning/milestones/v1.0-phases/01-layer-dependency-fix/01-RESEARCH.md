# Phase 1: Layer Dependency Fix - Research

**Researched:** 2026-04-15
**Domain:** ASP.NET Core 8 Clean Architecture — compile-time dependency inversion
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01: Repository Interfaces — New Home**
Move all repository interfaces from `EuphoriaInn.Repository/Interfaces/` to `EuphoriaInn.Domain/Interfaces/`. Files: `IBaseRepository.cs`, `IQuestRepository.cs`, `ICharacterRepository.cs`, `IPlayerSignupRepository.cs`, `IShopRepository.cs`, `ITradeItemRepository.cs`, `IUserRepository.cs`, `IUserTransactionRepository.cs`. Namespaces update from `EuphoriaInn.Repository.Interfaces` → `EuphoriaInn.Domain.Interfaces`. Repository implementations and Domain services update their `using` directives accordingly.

**D-02: Entity Construction in Domain Services**
`QuestService.UpdateProposedDatesIntelligentlyAsync` and `UpdateProposedDatesWithNotificationTrackingAsync` → extract to `IQuestRepository.UpdateProposedDatesAsync(int questId, IList<DateTime> newDates, CancellationToken)`. `CharacterService` profile image assignment → extract to `ICharacterRepository.UpdateProfileImageAsync(int characterId, byte[] imageData, CancellationToken)`. Signatures on these new interface methods use only primitive or domain model types.

**D-03: EntityProfile Move**
Move `EuphoriaInn.Domain/Automapper/EntityProfile.cs` → `EuphoriaInn.Repository/Automapper/EntityProfile.cs`. Update namespace. Delete old file and `Automapper/` subdirectory from Domain. Update `Program.cs` `using` to `EuphoriaInn.Repository.Automapper`.

**D-04: Removal Scope — Minimum Viable**
Only remove what blocks the build. Once the three changes above are complete, drop the `<ProjectReference>` from `EuphoriaInn.Domain.csproj` and verify `dotnet build` passes.

### Claude's Discretion

- Exact signature of `UpdateProposedDatesAsync` (return type, whether it returns the updated entity or just mutates in place) — decide based on how `QuestService` currently uses the result.
- Ordering of the four changes within Phase 1 plans (EntityProfile first vs interfaces first) — left to planner.

### Deferred Ideas (OUT OF SCOPE)

- Full sweep of EF-adjacent patterns remaining in Domain services.
- MailKit migration.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ARCH-01 | `EntityProfile.cs` (AutoMapper Entity↔DomainModel) lives in `EuphoriaInn.Repository`, not `EuphoriaInn.Domain` | D-03: move file, add AutoMapper NuGet + ProjectReference to Repository |
| ARCH-02 | `EuphoriaInn.Domain.csproj` has no `<ProjectReference>` to `EuphoriaInn.Repository` | D-04: requires D-01, D-02, D-03 all complete first |
| ARCH-03 | Dependency direction is `Service → Domain ← Repository`; Domain compiles without Repository | Requires domain services to be free of all `*Entity` type references |
| ARCH-04 | AutoMapper registration in `Program.cs` uses explicit type references — no `AppDomain` scanning | Already satisfied; just update the `using` directive after D-03 |
</phase_requirements>

---

## Summary

Phase 1 removes the compile-time `<ProjectReference>` from `EuphoriaInn.Domain` to `EuphoriaInn.Repository`. The current violation has two root causes: `EntityProfile.cs` in Domain references entity types (D-03 moves the file to Repository), and Domain services inject repository-typed interfaces that reference `*Entity` types in their signatures (D-01 moves interfaces, D-02 extracts entity construction logic).

**Critical finding for the planner:** The domain-specific interfaces (`IQuestRepository`, `ICharacterRepository`, etc.) currently declare their methods using `*Entity` return types (e.g., `Task<QuestEntity?> GetQuestWithDetailsAsync(...)`). Moving these interfaces to Domain as-is would still require Domain to import entity types — defeating the purpose. Additionally, every domain service class declaration names a specific entity type via `BaseService<TModel, TEntity>` (e.g., `BaseService<Quest, QuestEntity>`). Removing the project reference requires ALL entity type references to be eliminated from Domain — not just entity construction. This is a larger scope than D-01/D-02 alone describes.

**Primary recommendation:** Refactor domain-specific interface signatures to use domain model return types when moving to Domain; refactor `BaseService<TModel, TEntity>` to remove the `TEntity` type parameter; push all entity-type knowledge down into Repository implementations. The `IBaseRepository<T>` generic interface can move to Domain as-is (no entity constraint at the interface level).

---

## Project Constraints (from CLAUDE.md)

| Directive | Impact on This Phase |
|-----------|---------------------|
| EF packages ONLY in Repository project | AutoMapper must be added to `EuphoriaInn.Repository.csproj` when EntityProfile moves there |
| `dotnet ef migrations add` run from Service project with `--project ../EuphoriaInn.Repository` | No migrations in this phase — no DB schema changes |
| Auto-applied migrations on startup | Unaffected |
| `modern-card` CSS pattern for new views | No views in this phase |
| GSD workflow for all edits | Use `/gsd:execute-phase` |
| Stay on ASP.NET Core 8 MVC + SQL Server + EF Core | Confirmed — no stack changes |
| Docker `docker-compose up` must remain deployable | Unaffected by compile-time refactor |

---

## Standard Stack

### Core (no changes to versions)
| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| AutoMapper | 14.0.0 | Entity↔Model mapping | Must be added to `EuphoriaInn.Repository.csproj`; already in Domain and Service |
| EF Core | 9.0.6 | ORM | Already in Repository; no change |
| ASP.NET Core Identity | 8.0.11 | Auth entities | Already in Repository; no change |

**Repository csproj change required:**
```xml
<PackageReference Include="AutoMapper" Version="14.0.0" />
```

**Repository csproj ProjectReference to Domain required:**
The Repository project currently has NO `<ProjectReference>` to Domain. When `EntityProfile.cs` moves to Repository, it references `EuphoriaInn.Domain.Models.*` and `EuphoriaInn.Domain.Enums.*`. Repository therefore needs:
```xml
<ProjectReference Include="..\EuphoriaInn.Domain\EuphoriaInn.Domain.csproj" />
```
This is the CORRECT dependency direction (Repository → Domain). It does not create a circular reference.

---

## Architecture Patterns

### Current State (before phase)

```
EuphoriaInn.Domain.csproj
  └── <ProjectReference> EuphoriaInn.Repository  ← VIOLATION to remove

EuphoriaInn.Repository.csproj
  └── (no ProjectReference to Domain)             ← needs ProjectReference added

Domain/Automapper/EntityProfile.cs               ← references Repository.Entities
Domain/Services/BaseService.cs                   ← IBaseRepository<TEntity> from Repository.Interfaces
Domain/Services/QuestService.cs                  ← QuestEntity, ProposedDateEntity from Repository.Entities
Domain/Services/CharacterService.cs              ← CharacterImageEntity from Repository.Entities
Domain/Services/ShopService.cs                   ← UserTransactionEntity from Repository.Entities
Domain/Services/UserService.cs                   ← UserEntity from Repository.Entities
Domain/Services/PlayerSignupService.cs           ← PlayerSignupEntity from Repository.Entities
Repository/Interfaces/IQuestRepository.cs        ← IBaseRepository<QuestEntity> — entity-typed
(all 7 specific interfaces same pattern)
```

### Target State (after phase)

```
EuphoriaInn.Domain.csproj
  └── (no ProjectReference to Repository)         ← ARCH-02 achieved

EuphoriaInn.Repository.csproj
  └── <ProjectReference> EuphoriaInn.Domain       ← correct direction

Repository/Automapper/EntityProfile.cs           ← EntityProfile in correct layer
Domain/Interfaces/IBaseRepository.cs             ← generic, no entity constraint
Domain/Interfaces/IQuestRepository.cs            ← returns Quest (domain model), not QuestEntity
(all specific interfaces same — domain model return types)
Domain/Services/BaseService.cs                   ← no TEntity parameter
Domain/Services/QuestService.cs                  ← no entity type references
(all services same)
```

### Pattern 1: Interface Signature Migration

The specific repository interfaces must change return types when moving to Domain. Example:

**Current (in Repository):**
```csharp
// Source: EuphoriaInn.Repository/Interfaces/IQuestRepository.cs
public interface IQuestRepository : IBaseRepository<QuestEntity>
{
    Task<QuestEntity?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);
}
```

**Target (in Domain):**
```csharp
// EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
public interface IQuestRepository : IBaseRepository<Quest>
{
    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);
}
```

The Repository implementation (`QuestRepository`) maps internally using AutoMapper or manual construction before returning domain models. The Repository concrete class still works with EF entities internally — only the public interface contract changes.

### Pattern 2: BaseService TEntity Parameter Removal

`BaseService<TModel, TEntity>` uses `TEntity` only to parameterize `IBaseRepository<TEntity>`. Once interfaces are domain-model-typed, `IBaseRepository<T>` is bound to domain model types, and the `TEntity` parameter in `BaseService` is no longer needed.

**Current:**
```csharp
// EuphoriaInn.Domain/Services/BaseService.cs
internal abstract class BaseService<TModel, TEntity>(IBaseRepository<TEntity> repository, IMapper mapper)
    : IBaseService<TModel>
    where TModel : class, IModel
```

**Target:**
```csharp
// EuphoriaInn.Domain/Services/BaseService.cs
internal abstract class BaseService<TModel>(IBaseRepository<TModel> repository, IMapper mapper)
    : IBaseService<TModel>
    where TModel : class, IModel
```

Service declarations then become:
```csharp
// Before:
internal class QuestService(...) : BaseService<Quest, QuestEntity>(repository, mapper), IQuestService

// After:
internal class QuestService(...) : BaseService<Quest>(repository, mapper), IQuestService
```

### Pattern 3: Repository Implementation Handles Mapping

With interfaces returning domain models, the concrete Repository classes must map internally. For `GetQuestWithDetailsAsync`:

```csharp
// EuphoriaInn.Repository/QuestRepository.cs
internal class QuestRepository(QuestBoardContext context, IMapper mapper) : BaseRepository<Quest, QuestEntity>(context, mapper), IQuestRepository
{
    public async Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await _context.Quests
            .Include(q => q.PlayerSignups)
            // ...includes...
            .FirstOrDefaultAsync(q => q.Id == id, token);
        return entity == null ? null : _mapper.Map<Quest>(entity);
    }
}
```

This means `BaseRepository<T>` must also accept an `IMapper` and its generic parameter becomes the domain model type. Alternatively, mapping can be done in each concrete repository without a shared base.

### Pattern 4: UpdateProposedDatesAsync New Method

Per D-02, `QuestService.UpdateProposedDatesIntelligentlyAsync` and `UpdateProposedDatesWithNotificationTrackingAsync` move to the Repository layer. The interface gains:

```csharp
// EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
Task UpdateProposedDatesAsync(int questId, IList<DateTime> newDates, CancellationToken token = default);
Task<IList<User>> UpdateProposedDatesWithNotificationTrackingAsync(int questId, IList<DateTime> newDates, CancellationToken token = default);
```

The return type of the notification-tracking variant should be `IList<User>` (domain model) since `QuestService.UpdateQuestPropertiesWithNotificationsAsync` currently returns `IList<User>`. No entity type crosses the boundary.

### Pattern 5: UpdateProfileImageAsync New Method

Per D-02, `CharacterService.UpdateAsync` constructs `CharacterImageEntity` directly. This moves to:

```csharp
// EuphoriaInn.Domain/Interfaces/ICharacterRepository.cs
Task UpdateProfileImageAsync(int characterId, byte[] imageData, CancellationToken token = default);
```

`CharacterService.UpdateAsync` calls `_repository.UpdateProfileImageAsync(model.Id, model.ProfilePicture, token)` instead of constructing the entity.

### Pattern 6: ShopService and UserService Entity Construction

**ShopService** constructs `UserTransactionEntity` directly in `PurchaseItemAsync`, `ReturnOrSellItemAsync`, and `SellItemToShopAsync`. Per D-04 ("minimum viable"), these are NOT addressed in Phase 1 unless they block the build. They WILL block the build once `using EuphoriaInn.Repository.Entities` is removed.

**Resolution:** Either extract to new repository methods (like D-02 pattern), or — since D-04 says "only remove what blocks the build" — ensure ALL entity type usages are resolved, including these, before removing the project reference.

**UserService** constructs `UserEntity` in `CreateAsync`. This is an ASP.NET Identity pattern (`UserManager<UserEntity>`) which requires the entity type directly. This is a special case — `UserService` wraps `UserManager<UserEntity>` and `SignInManager<UserEntity>` from ASP.NET Identity, both of which are injected from the DI container. The entity type appears in the generic parameter of `UserManager<UserEntity>`.

**Key planning decision:** Either `UserService` stays in Domain and Domain is allowed to reference `UserEntity` via a special exception for Identity infrastructure, OR `UserService` is moved to the Repository layer (since it deeply couples to Identity's entity-based API). The CONTEXT.md does not address this.

### Anti-Patterns to Avoid

- **Assembly scanning AutoMapper:** Never replace `AddProfile<EntityProfile>()` with `services.AddAutoMapper(typeof(SomeAssemblyAnchor))` or `AppDomain.CurrentDomain.GetAssemblies()`. Causes `DuplicateTypeMapConfigurationException` if any assembly is scanned twice. Keep explicit `AddProfile<T>()` calls (ARCH-04).
- **Removing project reference before all entity usages are gone:** Build will fail with hundreds of compile errors. Always verify `dotnet build` passes after each sub-step.
- **Moving EntityProfile before Repository has AutoMapper NuGet:** `EntityProfile : Profile` won't compile. Add the NuGet reference first.
- **Moving EntityProfile before Repository has ProjectReference to Domain:** EntityProfile references domain models — won't compile without the reference.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| AutoMapper profile registration | Custom reflection scanner | Explicit `AddProfile<T>()` | Assembly scanning causes duplicate profile errors at startup |
| Entity↔Model mapping | Manual property assignment | AutoMapper `Mapper.Map<T>()` | Already established pattern; EntityProfile has all enum conversions handled |
| Namespace update across many files | Manual find/replace | IDE rename refactor or targeted file edits | Less error-prone; keeps build green incrementally |

---

## Common Pitfalls

### Pitfall 1: Domain-Specific Interfaces Still Reference Entity Types After Move
**What goes wrong:** `IQuestRepository` is moved to Domain but still declares `Task<QuestEntity?> GetQuestWithDetailsAsync(...)`. Domain project still won't compile without `EuphoriaInn.Repository` reference.
**Why it happens:** D-01 says "move interfaces" but doesn't explicitly say "change signatures." The interface signatures must change to domain model return types.
**How to avoid:** When moving each specific interface to Domain, simultaneously change its return type from `*Entity` to the corresponding domain model. Update the Repository implementation to map before returning.
**Warning signs:** Any `*Entity` type appearing in a file under `EuphoriaInn.Domain/`.

### Pitfall 2: BaseService<TModel, TEntity> Still Names Entity Types
**What goes wrong:** Even after interface move, every service class declaration says `BaseService<Quest, QuestEntity>`. This imports `QuestEntity` from Repository.
**Why it happens:** D-01/D-02 don't mention `BaseService` refactoring. The `TEntity` parameter must be removed from `BaseService`.
**How to avoid:** Refactor `BaseService<TModel, TEntity>` → `BaseService<TModel>` as part of the interface signature migration (both work together).
**Warning signs:** Any `BaseService<X, YEntity>` pattern remaining in Domain services.

### Pitfall 3: UserService Has Deep ASP.NET Identity Entity Coupling
**What goes wrong:** `UserService` injects `UserManager<UserEntity>` and `SignInManager<UserEntity>` — both require the entity type as generic parameter. Moving interfaces to Domain doesn't remove this reference.
**Why it happens:** ASP.NET Identity's manager classes are parameterized by the Identity user entity type. Domain can't use them without knowing the entity type.
**How to avoid:** Either move `UserService` to Repository layer (where entity types are valid), or accept that `IUserRepository` and the managers use `UserEntity` and keep `UserService` in Domain with a special Identity-exemption (incompatible with ARCH-02), or create a Domain-level `IUserIdentityService` abstraction that `UserService` in Repository implements.
**Warning signs:** `UserManager<UserEntity>` or `SignInManager<UserEntity>` appearing in Domain services.

### Pitfall 4: ShopService Constructs UserTransactionEntity Directly
**What goes wrong:** `ShopService.PurchaseItemAsync`, `ReturnOrSellItemAsync`, and `SellItemToShopAsync` all construct `new UserTransactionEntity { ... }`. These are NOT covered by D-02.
**Why it happens:** D-02 only addresses `QuestService` and `CharacterService`. D-04 says "minimum viable" but removing the project reference will break these too.
**How to avoid:** Add `IUserTransactionRepository.CreateTransactionAsync(...)` (or similar) methods that accept domain model or primitive parameters. Move entity construction to Repository.
**Warning signs:** `new UserTransactionEntity` anywhere in Domain services.

### Pitfall 5: Repository Needs AutoMapper to Map in Implementations
**What goes wrong:** Once repository implementations must return domain models (not entities), they need `IMapper`. But `EuphoriaInn.Repository.csproj` currently has no AutoMapper NuGet reference.
**Why it happens:** AutoMapper is currently only in Domain and Service projects. The mapping responsibility is moving to Repository.
**How to avoid:** Add `AutoMapper 14.0.0` NuGet to `EuphoriaInn.Repository.csproj`. Pass `IMapper` to Repository constructors (either `BaseRepository` or each concrete class).
**Warning signs:** `CS0246 The type or namespace name 'IMapper' could not be found` in Repository build.

### Pitfall 6: Wrong Move Order Causes Cascading Build Failures
**What goes wrong:** Removing the project reference before all entity usages are eliminated causes 50+ compile errors simultaneously.
**Why it happens:** Trying to validate the final state before all prerequisite steps are done.
**How to avoid:** Keep the `<ProjectReference>` in `EuphoriaInn.Domain.csproj` until `dotnet build` is verified green with zero `EuphoriaInn.Repository` using directives remaining in Domain. Remove the reference as the LAST step.
**Warning signs:** Any `using EuphoriaInn.Repository.*` directive remaining in any Domain file.

### Pitfall 7: BaseRepository TEntity Constraint Breaks When Signatures Change
**What goes wrong:** `BaseRepository<T>` has `where T : class, IEntity`. If `T` becomes a domain model type, it won't satisfy the `IEntity` constraint — domain models don't implement `IEntity`.
**Why it happens:** `IEntity` is the marker interface for EF entities only.
**How to avoid:** `BaseRepository` must be parameterized on BOTH the domain model and entity type, or the mapping is done manually in each concrete repository without using a fully generic base. Pattern: `BaseRepository<TModel, TEntity>` where `TEntity : class, IEntity` and the base class handles EF operations on `TEntity`, but CRUD methods accept/return `TModel`.
**Warning signs:** Compile error `TModel does not implement IEntity` when trying to use domain models with `BaseRepository`.

---

## Code Examples

### EntityProfile After Move
```csharp
// Source: EuphoriaInn.Repository/Automapper/EntityProfile.cs (new location)
using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Automapper;  // namespace updated

public class EntityProfile : Profile
{
    // All mappings unchanged — only namespace and file location change
}
```

### Program.cs AutoMapper Registration After Move
```csharp
// Source: EuphoriaInn.Service/Program.cs (line ~73)
using EuphoriaInn.Repository.Automapper;  // updated from EuphoriaInn.Domain.Automapper
using EuphoriaInn.Service.Automapper;

builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<ViewModelProfile>();   // unchanged
    config.AddProfile<EntityProfile>();      // unchanged call; using directive updated
});
```

### IBaseRepository After Move (safe — no entity constraint at interface level)
```csharp
// Source: EuphoriaInn.Domain/Interfaces/IBaseRepository.cs (new location)
namespace EuphoriaInn.Domain.Interfaces;  // namespace updated

public interface IBaseRepository<T>
{
    Task AddAsync(T entity, CancellationToken token = default);
    Task<bool> ExistsAsync(int id, CancellationToken token = default);
    Task<IList<T>> GetAllAsync(CancellationToken token = default);
    Task<T?> GetByIdAsync(int id, CancellationToken token = default);
    Task RemoveAsync(T entity, CancellationToken token = default);
    Task SaveChangesAsync(CancellationToken token = default);
    Task UpdateAsync(T entity, CancellationToken token = default);
}
// No entity types referenced — safe to live in Domain
```

### IQuestRepository After Signature Change (domain model return types)
```csharp
// EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IQuestRepository : IBaseRepository<Quest>  // Quest, not QuestEntity
{
    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);
    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);
    Task<IList<Quest>> GetQuestsForCalendarAsync(CancellationToken token = default);
    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);
    Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);
    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);
    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);
    Task<Quest?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default);
    // New method per D-02:
    Task UpdateProposedDatesAsync(int questId, IList<DateTime> newDates, CancellationToken token = default);
    Task<IList<User>> UpdateProposedDatesWithNotificationTrackingAsync(int questId, IList<DateTime> newDates, CancellationToken token = default);
}
```

### BaseRepository After Refactor (dual generic — handles mapping)
```csharp
// EuphoriaInn.Repository/BaseRepository.cs
using AutoMapper;

internal abstract class BaseRepository<TModel, TEntity>(QuestBoardContext dbContext, IMapper mapper)
    : IBaseRepository<TModel>
    where TModel : class, IModel
    where TEntity : class, IEntity
{
    protected QuestBoardContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();
    protected IMapper Mapper { get; } = mapper;

    public virtual async Task AddAsync(TModel model, CancellationToken token = default)
    {
        var entity = Mapper.Map<TEntity>(model);
        await DbSet.AddAsync(entity, token);
        await DbContext.SaveChangesAsync(token);
    }

    public virtual async Task<TModel?> GetByIdAsync(int id, CancellationToken token)
    {
        var entity = await DbSet.FindAsync([id], cancellationToken: token);
        return entity == null ? null : Mapper.Map<TModel>(entity);
    }

    // etc.
}
```

### BaseService After TEntity Removal
```csharp
// EuphoriaInn.Domain/Services/BaseService.cs
internal abstract class BaseService<TModel>(IBaseRepository<TModel> repository, IMapper mapper)
    : IBaseService<TModel>
    where TModel : class, IModel
{
    protected IMapper Mapper => mapper;

    public virtual async Task AddAsync(TModel model, CancellationToken token = default)
        => await repository.AddAsync(model, token);

    public virtual async Task<TModel?> GetByIdAsync(int id, CancellationToken token = default)
        => await repository.GetByIdAsync(id, token);

    // etc.
    // No mapper.Map<TEntity>() calls — repository handles entity conversion internally
}
```

---

## Scope Clarification: Full Entity Elimination Required

After reading all domain service files, the complete set of entity type usages that must be eliminated from Domain to achieve ARCH-02/ARCH-03:

| File | Entity References | Resolution |
|------|------------------|------------|
| `Domain/Automapper/EntityProfile.cs` | All entity types | D-03: move entire file to Repository |
| `Domain/Services/BaseService.cs` | `IBaseRepository<TEntity>`, `TEntity` param | Remove `TEntity` generic param; `IBaseRepository<TModel>` |
| `Domain/Services/QuestService.cs` | `QuestEntity`, `ProposedDateEntity` | D-02 + interface signature change |
| `Domain/Services/CharacterService.cs` | `CharacterEntity`, `CharacterImageEntity` | D-02 + interface signature change |
| `Domain/Services/ShopService.cs` | `ShopItemEntity`, `UserTransactionEntity` | New repository method (like D-02) |
| `Domain/Services/UserService.cs` | `UserEntity` (via Identity managers) | Move UserService to Repository OR wrap Identity behind a Domain abstraction |
| `Domain/Services/PlayerSignupService.cs` | `PlayerSignupEntity` (implicit via BaseService) | Interface signature change |

**UserService is the special case.** It directly injects `UserManager<UserEntity>` and `SignInManager<UserEntity>` — ASP.NET Identity APIs that require the entity type as a generic parameter. This cannot be resolved by adding new interface methods. Options for the planner:
1. Move `UserService` to Repository layer (it belongs there given Identity coupling).
2. Create a `IUserIdentityService` abstraction in Domain, implemented in Repository.
3. Accept that Domain depends on Identity's entity via a deliberate exception (weakens ARCH-03).

The CONTEXT.md does not address this. The planner must choose.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests/` |
| Full suite command | `dotnet test` (solution-level) |

### Current Test State
- **Unit tests:** 16 passing (all in `EuphoriaInn.UnitTests/Models/QuestModelTests.cs` — pure domain model tests, no Repository references)
- **Integration tests:** Test infrastructure exists (`EuphoriaInn.IntegrationTests/`) but 0 test classes found with Repository coupling
- **Build:** Clean `dotnet build` with 0 errors, 0 warnings before phase starts

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | Notes |
|--------|----------|-----------|-------------------|-------|
| ARCH-02 | Domain.csproj has no ProjectReference to Repository | Build verification | `dotnet build` | Not a unit test — verify via csproj inspection |
| ARCH-03 | Domain compiles without Repository | Build verification | `dotnet build EuphoriaInn.Domain/` | Isolated build of Domain project |
| ARCH-01 | EntityProfile.cs in Repository | File location check | `dotnet build` | Compile confirms location |
| ARCH-04 | No assembly scanning in Program.cs | Code review | — | Manual verification |

### Sampling Rate
- **Per task commit:** `dotnet build` — verify 0 errors before each commit
- **Per wave merge:** `dotnet test` — all 16 unit tests must pass
- **Phase gate:** `dotnet build` green + `dotnet test` 16/16 passing before `/gsd:verify-work`

### Wave 0 Gaps
- No new test files needed — this phase is a structural refactor with no new business behavior
- Existing 16 tests cover pure domain model behavior and will continue passing
- The primary validation is `dotnet build` green on Domain project in isolation

---

## Open Questions

1. **UserService / ASP.NET Identity Entity Coupling**
   - What we know: `UserService` injects `UserManager<UserEntity>` and `SignInManager<UserEntity>` — these are Identity APIs parameterized on the EF entity type. Cannot be hidden behind an interface without significant wrapper work.
   - What's unclear: CONTEXT.md does not address this. Is UserService intentionally left as a "special case" (and Domain keeps a conditional identity dependency), or should UserService move to Repository?
   - Recommendation: Move `UserService` to Repository layer. It wraps Identity infrastructure (entity-coupled by design) and has no pure business logic. Define `IUserService` in Domain (already exists), implement in Repository. This fully satisfies ARCH-03 without hacks.

2. **IUserRepository.ExistsAsync(string name) Overload**
   - What we know: `IUserRepository` has `Task<bool> ExistsAsync(string name)` — a string overload not on `IBaseRepository`. This is fine to keep in Domain.
   - What's unclear: After the signature migration, `IBaseRepository<User>` already has `ExistsAsync(int id)`. The string overload is an addition.
   - Recommendation: No change needed — it's an additional method, not a conflict.

3. **ShopService Transaction Entity Construction**
   - What we know: Three methods in `ShopService` construct `new UserTransactionEntity { ... }` with many fields. D-02 only calls out `QuestService` and `CharacterService`.
   - What's unclear: Will the planner extend D-02 pattern to ShopService, or create a different abstraction?
   - Recommendation: Add `IUserTransactionRepository.CreateTransactionAsync(int shopItemId, int userId, int quantity, decimal price, TransactionType type, string notes, int? originalTransactionId = null, CancellationToken token = default)` method. ShopService calls this instead of constructing the entity.

---

## Environment Availability

Step 2.6: SKIPPED — this phase is purely code/structural changes with no external dependencies beyond .NET SDK (already verified, build passes).

---

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection — all source files read and verified
- `dotnet build` output — confirmed current build is green (0 errors, 0 warnings)
- `dotnet test EuphoriaInn.UnitTests/` — confirmed 16/16 passing
- Microsoft Clean Architecture guidance — Domain defines interfaces, Repository implements them

### Secondary (MEDIUM confidence)
- CONTEXT.md D-01 through D-04 — locked decisions from `/gsd:discuss-phase`
- `.planning/research/SUMMARY.md` — project-level research context
- `.planning/codebase/ARCHITECTURE.md` — layer overview

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries; AutoMapper already in project at exact version needed
- Architecture: HIGH — all source files read; exact entity usages enumerated; patterns confirmed
- Pitfalls: HIGH — each pitfall grounded in actual code inspection (specific files, line numbers)
- Open questions: MEDIUM — UserService/Identity coupling is a real gap in the CONTEXT.md decisions

**Research date:** 2026-04-15
**Valid until:** 2026-06-15 (stable .NET/AutoMapper APIs)
