# Coding Conventions

**Analysis Date:** 2026-04-15

## Naming Patterns

**Files:**
- Domain model files: PascalCase, no suffix — `Quest.cs`, `User.cs`, `PlayerSignup.cs`
- Entity files: PascalCase with `Entity` suffix — `QuestEntity.cs`, `UserEntity.cs`, `CharacterEntity.cs`
- Repository files: PascalCase with `Repository` suffix — `QuestRepository.cs`, `BaseRepository.cs`
- Interface files: PascalCase with `I` prefix — `IQuestService.cs`, `IBaseRepository.cs`
- Service files: PascalCase with `Service` suffix — `QuestService.cs`, `BaseService.cs`
- ViewModel files: PascalCase with `ViewModel` suffix — `EditQuestViewModel.cs`, `RegisterViewModel.cs`
- Controller files: PascalCase with `Controller` suffix — `QuestController.cs`
- AutoMapper profile files: PascalCase with `Profile` suffix — `ViewModelProfile.cs`, `EntityProfile.cs`
- Authorization files: PascalCase with `Handler` or `Requirement` suffix — `DungeonMasterHandler.cs`, `AdminRequirement.cs`

**Directories:**
- Layer directories: PascalCase project names — `EuphoriaInn.Domain`, `EuphoriaInn.Repository`, `EuphoriaInn.Service`
- Feature subdirectories within controllers: PascalCase by domain area — `Controllers/QuestBoard/`, `Controllers/Admin/`, `Controllers/Shop/`
- ViewModel directories: PascalCase with `ViewModels` suffix — `QuestViewModels/`, `AccountViewModels/`

**Classes:**
- All classes: PascalCase
- Internal service implementations: `internal class QuestService`
- Abstract base classes: `abstract class BaseService<TModel, TEntity>`, `abstract class BaseRepository<T>`
- Interfaces: `IQuestService`, `IBaseRepository<T>`

**Methods:**
- All async methods: PascalCase with `Async` suffix — `GetQuestWithDetailsAsync`, `FinalizeQuestAsync`, `AddAsync`
- Private helper methods: PascalCase — `IsSameDateTime`, `UpdateProposedDatesIntelligentlyAsync`

**Variables and Parameters:**
- Local variables and parameters: camelCase — `questId`, `currentUser`, `dmName`
- Private fields (from constructor injection): camelCase — not stored as fields; primary constructor parameters used directly
- Public properties: PascalCase — `IsFinalized`, `FinalizedDate`, `TotalPlayerCount`

**Generics:**
- Model type parameter: `TModel`
- Entity type parameter: `TEntity`
- Generic type: `T` — e.g., `BaseRepository<T>`, `IBaseService<T>`

## Code Style

**Primary constructor syntax:**
All injectable classes use C# 12 primary constructors instead of constructor body injection:
```csharp
internal class QuestService(IQuestRepository repository, IPlayerSignupRepository playerSignupRepository, IMapper mapper)
    : BaseService<Quest, QuestEntity>(repository, mapper), IQuestService
```

**Nullable reference types:** Enabled across all projects (`<Nullable>enable</Nullable>`). Nullable returns use `?` — `Task<Quest?>`, `Task<T?>`.

**Collection initialization:** Use collection expression syntax `[]` for empty default values:
```csharp
public IList<ProposedDate> ProposedDates { get; set; } = [];
public IList<PlayerSignup> PlayerSignups { get; set; } = [];
```

**String defaults:** Properties default to `string.Empty`, not `null`:
```csharp
public string Title { get; set; } = string.Empty;
```

**Early return on null:** Services and controllers return early when entity is null:
```csharp
var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
if (entity == null) return;
```

**Pattern matching for null checks:**
```csharp
if (await repository.GetQuestWithDetailsAsync(id, token) is not QuestEntity entity)
{
    return null;
}
```

**Async/await:** All data access is fully async throughout all layers. Every service method accepts a `CancellationToken token = default` parameter.

**`virtual` on navigation properties:** All navigation properties on EF entities use `virtual`:
```csharp
public virtual UserEntity Owner { get; set; } = null!;
public virtual ICollection<CharacterClassEntity> Classes { get; set; } = [];
```

**`null!` for required navigation properties:** Non-nullable required navigation properties use `null!` to satisfy nullable analysis:
```csharp
public virtual UserEntity Owner { get; set; } = null!;
```

## Import Organization

**Order:**
1. System and framework namespaces (`using AutoMapper;`, `using Microsoft.EntityFrameworkCore;`)
2. External library namespaces
3. Project-internal namespaces (`using EuphoriaInn.Domain.Models;`)

**Namespace style:** File-scoped namespace declarations (C# 10+):
```csharp
namespace EuphoriaInn.Domain.Services;
```

**Global usings:** Test projects use `GlobalUsings.cs` to declare common imports:
- `EuphoriaInn.IntegrationTests/GlobalUsings.cs` — `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore`, etc.
- Unit test projects declare `<Using Include="Xunit" />` in the `.csproj`

## Error Handling

**Controllers:** Return typed `IActionResult` results for HTTP error cases:
- `return NotFound();` — resource does not exist
- `return Forbid();` — user lacks permission
- `return BadRequest("message");` — invalid request state
- `return Challenge();` — unauthenticated user

**Services:** Return early without throwing for missing entities:
```csharp
if (entity == null) return;
// or
if (entity == null) return [];
```

**Identity operations:** Return `IdentityResult` from methods that modify users; callers check `result.Succeeded`.

## Comments

**Inline comments:** Used frequently to explain non-obvious business logic, especially in service methods. Comments are placed immediately before the relevant code block:
```csharp
// Manual cleanup required since Quest->PlayerSignup is NoAction to avoid cascade cycles
// Remove PlayerSignups first (DateVotes will cascade delete from PlayerSignups)
```

**Section comments:** Group related code blocks with a short header comment:
```csharp
// Update quest finalization properties
entity.IsFinalized = true;
// Update player selections
foreach (var playerSignup in entity.PlayerSignups)
```

**No XML doc comments (///``):** Not used in the codebase; plain inline `//` comments only.

## Patterns Used in Controllers

**Constructor injection via primary constructor:**
```csharp
public class QuestController(
    IUserService userService,
    IEmailService emailService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService
    ) : Controller
```

**Action method HTTP verb attributes:** Always annotated explicitly:
```csharp
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Create(CancellationToken token = default)
```

**POST actions:** Always include `[ValidateAntiForgeryToken]`:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Create(QuestViewModel viewModel, CancellationToken token = default)
```

**Authorization policies:** Named string policies used (not `[Authorize(Roles = "...")]`):
- `"DungeonMasterOnly"` — requires DungeonMaster or Admin role
- `"AdminOnly"` — requires Admin role

**Redirect after POST:** `return RedirectToAction("Index", "Home");`

**Model state validation:** Check `ModelState.IsValid` at the top of POST actions, return the view with model on failure.

## Patterns Used in Services

**Internal visibility:** Domain service implementations are `internal`, exposed only via their interfaces.

**Base class via generic inheritance:**
`BaseService<TModel, TEntity>` → concrete services like `QuestService : BaseService<Quest, QuestEntity>, IQuestService`

**AutoMapper usage:**
- Map entity → model: `Mapper.Map<Quest>(entity)` or `Mapper.Map<IList<Quest>>(entities)`
- Map model → entity: `mapper.Map<TEntity>(model)`
- Update existing entity from model: `Mapper.Map(model, entity)`

**SaveChanges pattern:** Services call `repository.SaveChangesAsync(token)` after mutating entities directly (EF change tracking), not `UpdateAsync` for every property change.

## Patterns Used in Repositories

**Base class via generic inheritance:**
`BaseRepository<T>` → concrete repos like `QuestRepository : BaseRepository<QuestEntity>, IQuestRepository`

**Internal visibility:** Concrete repository implementations are `internal`.

**DbSet access:** Repositories use `DbSet<T> DbSet` and `QuestBoardContext DbContext` protected properties from the base.

**Eager loading in specialized queries:** Repository methods with `WithDetails` suffix use `.Include()` chains. Raw `GetAllAsync` returns flat entities.

## View Model and DTO Conventions

**View model namespace:** `EuphoriaInn.Service.ViewModels.{Area}ViewModels`

**Initialization defaults:** View model collections initialize to empty collection `[]`; nested view model properties initialize to `new()`:
```csharp
public QuestViewModel Quest { get; set; } = new();
public IList<User> DungeonMasters { get; set; } = [];
```

**Data annotations on view models:** `[Required]`, `[StringLength]`, `[EmailAddress]`, `[DataType]`, `[Compare]`, `[Display]` are placed on view model properties, not on domain models (domain models have minimal annotations for EF constraints only).

**AutoMapper profiles:**
- `EntityProfile` (`EuphoriaInn.Domain/Automapper/EntityProfile.cs`) — maps between domain models and repository entities
- `ViewModelProfile` (`EuphoriaInn.Service/Automapper/ViewModelProfile.cs`) — maps between domain models and view models
- Enums stored as `int` in entities; cast explicitly in AutoMapper mappings: `opt => opt.MapFrom(src => (int)src.Type)`

## Common Abstractions and Base Classes

| Abstraction | Location | Purpose |
|---|---|---|
| `IModel` | `EuphoriaInn.Domain/Models/IModel.cs` | Marker interface requiring `int Id` on all domain models |
| `IEntity` | `EuphoriaInn.Repository/Entities/IEntity.cs` | Marker interface requiring `int Id` on all EF entities |
| `IBaseService<T>` | `EuphoriaInn.Domain/Interfaces/IBaseService.cs` | CRUD interface contract for all services |
| `BaseService<TModel, TEntity>` | `EuphoriaInn.Domain/Services/BaseService.cs` | Generic CRUD implementation delegating to repository |
| `IBaseRepository<T>` | `EuphoriaInn.Repository/Interfaces/IBaseRepository.cs` | CRUD interface contract for all repositories |
| `BaseRepository<T>` | `EuphoriaInn.Repository/BaseRepository.cs` | Generic CRUD implementation using EF `DbSet<T>` |

**Dependency injection registration:** Each layer has a `ServiceExtensions.cs` with an extension method:
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` → `AddDomainServices()`
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` → `AddRepositoryServices()`

---

*Convention analysis: 2026-04-15*
