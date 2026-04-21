# Architecture Patterns

**Domain:** ASP.NET Core 8 MVC — Clean Architecture refactor of existing app
**Researched:** 2026-04-15
**Confidence:** HIGH — authoritative Microsoft docs + cross-validated with multiple sources

---

## The Core Problem, Stated Precisely

The codebase has a **compile-time dependency inversion violation**: `EuphoriaInn.Domain` holds a `<ProjectReference>` to `EuphoriaInn.Repository`. This means the Domain assembly cannot compile without the Repository assembly. The intended arrow is Domain ← Repository; the actual arrow is Domain → Repository. Everything else (AutoMapper boundary confusion, controller bloat) follows from this single root cause.

---

## Q1: Where Should EntityProfile Live?

**Answer: In `EuphoriaInn.Repository`, not `EuphoriaInn.Domain`.**

### Why this is definitively correct

EntityProfile maps between `*Entity` types (EF Core classes that live in Repository) and domain `Model` classes (that live in Domain). For EntityProfile to compile, it must reference both assembly types. The two possible placements are:

| Placement | Required references | Dependency arrow |
|-----------|--------------------|-----------------:|
| Domain (current) | Domain refs Repository to see `*Entity` types | Domain → Repository (WRONG) |
| Repository | Repository refs Domain to see domain `Model` types | Repository → Domain (CORRECT) |

The correct dependency direction is **Repository → Domain** — infrastructure knows about the domain, not the reverse. Microsoft's .NET Microservices Architecture Guide states this explicitly: "The IBuyerRepository interface comes from the domain model layer as a contract. However, the repository implementation is done at the persistence and infrastructure layer."

The article *EF Core: Effectively Decouple the Data and Domain Model* (thecodewrapper.com) states: "Data entities and any persistence-related code should be kept ONLY in the Infrastructure layer and never be allowed to leave." The mapping profile that translates EF entities into domain models is persistence-related code — it knows about `QuestEntity.PlayerSignups` navigation properties, `CharacterImageEntity` blob structure, int-to-enum coercions required by EF's column storage. That knowledge belongs in the infrastructure layer.

### What this looks like after the move

```
EuphoriaInn.Domain
  ├── Models/          (Quest, User, PlayerSignup, etc.)
  ├── Interfaces/      (IQuestService, IQuestRepository, IEmailService, etc.)
  ├── Enums/
  └── Extensions/ServiceExtensions.cs
  [NO reference to EuphoriaInn.Repository]

EuphoriaInn.Repository
  ├── Entities/        (*Entity classes, QuestBoardContext)
  ├── Automapper/EntityProfile.cs   ← MOVED HERE
  ├── Interfaces/      (IBaseRepository<T>, IQuestRepository, etc.)
  ├── Repositories/    (concrete implementations)
  ├── Migrations/
  └── Extensions/ServiceExtensions.cs
  [References EuphoriaInn.Domain for Model types]

EuphoriaInn.Service
  ├── Controllers/
  ├── Automapper/ViewModelProfile.cs   (stays here)
  ├── ViewModels/
  └── Program.cs
  [References EuphoriaInn.Domain only]
```

### Registering AutoMapper after the move

AutoMapper profile scanning must be told to include the Repository assembly. In `Program.cs`:

```csharp
// Before (scans only executing assembly / Domain):
builder.Services.AddAutoMapper(typeof(EntityProfile), typeof(ViewModelProfile));

// After (EntityProfile is now in Repository assembly):
builder.Services.AddAutoMapper(
    typeof(EuphoriaInn.Repository.Automapper.EntityProfile),
    typeof(EuphoriaInn.Service.Automapper.ViewModelProfile));
```

The Service project already references Domain; Domain no longer references Repository; Repository references Domain. The dependency graph becomes a strict DAG pointing inward toward Domain.

### What to do about `BaseService<TModel, TEntity>`

`BaseService` is the trickiest piece. It currently holds a generic `IMapper mapper` and calls `mapper.Map<TEntity>(model)` and `mapper.Map<TModel>(entity)`. After the move, Domain no longer knows `TEntity` is an EF entity type — but `BaseService` never imports any EF namespace directly; it only holds `IMapper` (which is an AutoMapper abstraction that lives in the `AutoMapper` NuGet package, independent of Repository). This means `BaseService` can stay in Domain as-is. The `IMapper` contract is resolved at runtime from DI; the actual profile registrations happen in Service's `Program.cs`. The generic constraint `TEntity` in `BaseService<TModel, TEntity>` is just a C# type parameter — it does not create a compile-time project reference.

**The only change needed in Domain**: remove `using EuphoriaInn.Repository.Entities;` from `EntityProfile.cs` (which moves to Repository) and from any other Domain file that imports Repository entity types.

---

## Q2: Moving Email Dispatch and Finalization Out of Controllers

### The concrete pattern: Service method returns an enriched result

The current controller `Finalize` action does three things:
1. Validates form input and authorization — belongs in controller
2. Calls `questService.FinalizeQuestAsync(...)` — already in service (good)
3. Builds email recipient list from pre-fetch `quest` object, calls `emailService` per player — belongs in service

The fix is to make `FinalizeQuestAsync` responsible for dispatching emails itself, by injecting `IEmailService` into `QuestService`. The controller becomes:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Finalize(int id, CancellationToken token)
{
    // 1. Validate input
    if (!int.TryParse(Request.Form["SelectedDateId"], out var selectedDateId))
    {
        TempData["Error"] = "Please select a date.";
        return RedirectToAction("Manage", new { id });
    }

    var selectedPlayerIds = /* parse from form */;

    // 2. Delegate entirely to service
    var result = await questService.FinalizeQuestAsync(id, selectedDateId, selectedPlayerIds, token);

    if (!result.Success)
    {
        TempData["Error"] = result.ErrorMessage;
        return RedirectToAction("Manage", new { id });
    }

    // 3. Redirect
    return RedirectToAction("Details", new { id });
}
```

The `FinalizeQuestAsync` service method:
- Loads the quest with all required player data
- Validates the selected date exists on this quest (moves the domain-level guard out of the controller)
- Validates player count against `TotalPlayerCount`
- Marks the entity finalized
- Saves changes
- Sends emails to eligible players (using injected `IEmailService`)
- Returns a result object indicating success/failure with optional message

### Service result object pattern

Rather than `void` or throwing exceptions for business-rule failures, use a lightweight result type:

```csharp
public record ServiceResult(bool Success, string? ErrorMessage = null)
{
    public static ServiceResult Ok() => new(true);
    public static ServiceResult Fail(string message) => new(false, message);
}
```

This keeps HTTP concern (what status to return, what TempData to set) in the controller while business logic (is the selected player count valid?) lives in the service.

### Injecting IEmailService into QuestService

`QuestService` constructor becomes:

```csharp
internal class QuestService(
    IQuestRepository repository,
    IPlayerSignupRepository playerSignupRepository,
    IEmailService emailService,
    IMapper mapper) : BaseService<Quest, QuestEntity>(repository, mapper), IQuestService
```

Both `IQuestService` and `IEmailService` are Domain interfaces. `QuestService` calling `IEmailService` is a Domain-to-Domain call — it does not introduce any new layer violation.

### Concern #28 (stale quest state in email loop) is resolved automatically

The current controller builds the email list from a `quest` object fetched before `FinalizeQuestAsync` runs, then sends to all spectators regardless. When email dispatch moves into `FinalizeQuestAsync`, the service has access to the entity it just mutated and can build the correct recipient list from fresh data before returning.

### UpdateQuestPropertiesWithNotificationsAsync already shows the pattern

`IQuestService.UpdateQuestPropertiesWithNotificationsAsync` returns `IList<User>` — the list of players who need notification emails. The controller then calls `emailService` once per user. This is the half-way-house pattern: the service decides who gets emails but the controller still dispatches them. For the refactor, push the dispatch into the service and return `ServiceResult` instead of the user list.

---

## Q3: Service Layer Structure — Preventing Controller Knowledge of Persistence

### Repository interfaces stay in Domain (already correct)

`IQuestRepository` is already in `EuphoriaInn.Domain/Interfaces/`. Controllers only see `IQuestService`. This part of the architecture is already correct — the service is the sole gate to persistence from the controller's perspective.

### What "thin controller" means in this codebase

A controller action in this app should be reducible to this skeleton:

```
1. Resolve the current user (GetUserAsync) — 2–4 lines
2. Check authorization beyond the policy attribute — 1–3 lines
3. Call one service method, passing validated form inputs — 1 line
4. Handle the result: set TempData, return View/Redirect — 2–6 lines
```

Any logic that answers "what business rules apply here?" (player count cap, spectator auto-approval, owned-item validation before sell) must move into the service. Any logic that answers "what HTTP response do I send?" stays in the controller.

### ShopController.Index remaining quantity calculation

The `ShopController.Index` action currently computes remaining quantities for purchase transactions in a nested loop (Concerns #26 concern about missing logger, but also a thin-controller issue). This should move to a `GetUserInventoryAsync` method on `IShopService` that returns `UserTransactionViewModel`-like data (or a domain model with `RemainingQuantity` already set). The controller maps and passes to view.

### ViewBag usage

`ViewBag.IsAuthorized` and `ViewBag.IsAdmin` in `QuestController.Manage` should be either a property on the ViewModel or eliminated — the Razor view should derive authorization display state from the ViewModel rather than weakly-typed ViewBag.

---

## Q4: ASP.NET Core 8 / .NET 8 Features That Help This Refactor

### IOptions<T> with ValidateOnStart (HIGH confidence — official Microsoft docs)

The current `EmailService` reads `IConfiguration` by string key inside every send method. .NET 6+ introduced `ValidateOnStart()` which surfaces missing config at app startup rather than at first email send. In .NET 8, compile-time source generation for options validation is available via `[OptionsValidator]`.

Recommended pattern for `EmailSettings`:

```csharp
// In EuphoriaInn.Domain (or a shared config namespace)
public record EmailSettings
{
    public string SmtpServer { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = "";
    public string SmtpPassword { get; init; } = "";
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "";
}

// In Program.cs
builder.Services
    .AddOptions<EmailSettings>()
    .BindConfiguration("EmailSettings")
    .ValidateOnStart();  // fails fast if config section is absent
```

`EmailService` constructor becomes `IOptions<EmailSettings> emailOptions` instead of `IConfiguration`. The duplicated SMTP setup block collapses to a single field.

### Primary constructors (C# 12, .NET 8)

The codebase already uses primary constructors throughout (e.g., `class QuestService(...) : BaseService<Quest, QuestEntity>(...)`). Continue using them. No change needed.

### Rate limiting middleware (Concerns #6)

ASP.NET Core 7+ includes `Microsoft.AspNetCore.RateLimiting`. Apply a fixed-window policy in `Program.cs` — no third-party library needed. This is a cross-cutting concern that belongs in the Service project's `Program.cs`.

### Result types with `record` structs

`record ServiceResult(bool Success, string? ErrorMessage = null)` as shown above works well with C# 12 primary record syntax. No additional framework is needed.

---

## Component Location After Refactor

| Component | Current location | Target location | Reason |
|-----------|-----------------|-----------------|--------|
| `EntityProfile.cs` | `EuphoriaInn.Domain/Automapper/` | `EuphoriaInn.Repository/Automapper/` | References EF entity types; belongs in infrastructure |
| `EmailSettings` typed options class | Does not exist | `EuphoriaInn.Domain/Configuration/` | Shared between Domain (EmailService) and Service (Program.cs registration) |
| Email dispatch on finalize | `QuestController.Finalize()` | `QuestService.FinalizeQuestAsync()` | Business operation; controller should not orchestrate |
| Email dispatch on date change | `QuestController` (UpdateQuestProperties flow) | `QuestService.UpdateQuestPropertiesWithNotificationsAsync()` | Completes the half-done move |
| Remaining-quantity calculation | `ShopController.Index()` loop | `ShopService.GetUserInventoryAsync()` | Business calculation; not presentation logic |
| `SecurityConfiguration.cs` | `EuphoriaInn.Domain/Configuration/` | Delete | Dead code (Concerns #15) |
| `IQuestRepository` interface | `EuphoriaInn.Domain/Interfaces/` | Stays | Correct location |
| Repository implementations | `EuphoriaInn.Repository/` | Stays | Correct location |
| `ViewModelProfile.cs` | `EuphoriaInn.Service/Automapper/` | Stays | Presentation layer mapping, correct location |

---

## Recommended Refactor Order

Order matters because changes to the dependency graph must not break the build at any intermediate step.

### Step 1: Move EntityProfile to Repository (unblocks everything)

**Why first:** This is the single change that fixes the dependency direction. Until it is done, Domain still holds a compile-time reference to Repository and no other architectural move changes that fundamental fact.

**How to do it without breaking the build:**
1. Add `AutoMapper` NuGet to `EuphoriaInn.Repository.csproj`
2. Create `EuphoriaInn.Repository/Automapper/EntityProfile.cs` with the same content
3. Update `Program.cs` AutoMapper registration to reference `typeof(EuphoriaInn.Repository.Automapper.EntityProfile)`
4. Remove the `using EuphoriaInn.Repository.Entities;` imports from Domain files
5. Remove `<ProjectReference Include="..\EuphoriaInn.Repository\EuphoriaInn.Repository.csproj" />` from `EuphoriaInn.Domain.csproj`
6. Build and verify Domain no longer references Repository

**Risk:** Low. AutoMapper profiles are pure configuration classes — moving the file is the entire change. The only failure mode is forgetting to update the `AddAutoMapper` registration in `Program.cs`.

### Step 2: Introduce `EmailSettings` options class and update `EmailService`

**Why second:** Independent of Step 1 but small and isolated. Resolves Concerns #19 and #30. No interface changes. No controller changes.

**How:**
1. Add `EmailSettings` record to `EuphoriaInn.Domain/Configuration/`
2. Replace `IConfiguration` constructor parameter in `EmailService` with `IOptions<EmailSettings>`
3. Extract SMTP client creation to a private helper method inside `EmailService`
4. Register with `AddOptions<EmailSettings>().BindConfiguration("EmailSettings")` in `ServiceExtensions` or `Program.cs`

### Step 3: Move finalize email dispatch into `QuestService`

**Why third:** Requires Step 1 to be done first (Domain must not reference Repository; `QuestService` already satisfies this after Step 1). Inject `IEmailService` into `QuestService` constructor. Move email loop from `QuestController.Finalize` into `QuestService.FinalizeQuestAsync`. Add `ServiceResult` return type.

**Controller change:** `Finalize` action drops from ~60 lines to ~20. `IEmailService` can be removed from `QuestController` constructor injection entirely once Step 4 is also done.

### Step 4: Complete the date-change email move

`UpdateQuestPropertiesWithNotificationsAsync` currently returns `IList<User>` to the controller, which calls `emailService` per user. After Step 2 (EmailService using IOptions), move the dispatch into the service method. Return `ServiceResult` instead of user list. Remove `IEmailService` from `QuestController` constructor.

### Step 5: Move ShopController remaining-quantity calculation into ShopService

Add `GetUserInventoryAsync(int userId)` to `IShopService` returning a domain model (or a list of transaction summaries with `RemainingQuantity`). Controller maps result and renders. This also enables adding the missing logger to `ShopController` (Concerns #26) without adding business logic.

### Step 6: Remove dead code

In any order after Steps 1–5 are green:
- Delete `SecurityConfiguration.cs` and the `Security` appsettings section (Concerns #15)
- Remove `UpdateQuestPropertiesAsync` (non-notification variant) from interface and service (Concerns #16)
- Replace `SignupRole == 1` with enum reference throughout (Concerns #18)
- Extract 30-minute constant (Concerns #17)
- Rename `CharacterViewModels/GuildMembersIndexViewModel.cs` (Concerns #20)
- Remove `Password` from `User` domain model (Concerns #8)

---

## Architecture Diagram (After Refactor)

```
EuphoriaInn.Service (Presentation)
  ├── Controllers/ [thin: validate → call service → respond]
  ├── ViewModels/
  ├── Automapper/ViewModelProfile.cs  [DomainModel ↔ ViewModel]
  └── Program.cs  [DI wiring, AutoMapper registration for both profiles]
        ↓ references
EuphoriaInn.Domain (Core)
  ├── Models/          [pure C# classes, no EF/HTTP dependencies]
  ├── Interfaces/      [IQuestService, IQuestRepository, IEmailService, ...]
  ├── Services/        [QuestService, EmailService, UserService, ...]
  └── Configuration/   [EmailSettings options record]
        ↑ references (Repository → Domain)
EuphoriaInn.Repository (Infrastructure)
  ├── Entities/        [EF entity classes, QuestBoardContext]
  ├── Automapper/EntityProfile.cs  [*Entity ↔ DomainModel] ← MOVED HERE
  ├── Repositories/    [concrete EF implementations of IXxxRepository]
  └── Migrations/
```

Dependency arrows: Service → Domain ← Repository. Domain knows nothing about EF or HTTP. Repository knows about Domain models (to map to/from them) but not about HTTP or ViewModels.

---

## Concerns From CONCERNS.md: Architecture vs Code Quality

### Architectural concerns (require layer boundary changes)

| # | Concern | Category |
|---|---------|----------|
| 15 | `SecurityConfiguration` dead code | Code quality (safe to delete in any step) |
| 16 | Dead `UpdateQuestPropertiesAsync` | Code quality (safe to delete after Step 4) |
| 18 | `SignupRole == 1` magic number | Code quality |
| 19 | SMTP client reconstructed per send | Architecture: EmailService design (fixed in Step 2) |
| 22 | `[Quest Board URL]` placeholder | Architecture: EmailService contract (fix during Step 2/4) |
| 23 | Sell-without-ownership validation | Architecture: business logic not in service (fix in Step 5 scope) |
| 26 | Exception swallowed in ShopController, no logger | Architecture: missing logger in controller (fix in Step 5) |
| 27 | Race condition in stock decrement | Architecture: needs optimistic concurrency on ShopItemEntity |
| 28 | Stale quest state in email loop | Architecture: resolved by Step 3 |
| 30 | IConfiguration access in business logic | Architecture: fix in Step 2 |

### Code quality concerns (no layer changes required)

| # | Concern | Category |
|---|---------|----------|
| 3 | Account lockout disabled | Security config in Program.cs |
| 4 | Password minimum length | Security config in Program.cs |
| 5 | HasKey user-editable | View + controller guard |
| 8 | `Password` property on User model | Remove property |
| 17 | 30-minute magic constant | Named constant |
| 20 | Filename mismatch | Rename file |

### Out of scope for this milestone

| # | Concern | Reason |
|---|---------|--------|
| 10 | N+1 on admin users | Performance; separate concern |
| 11 | In-memory filter on completed quests | Performance; separate concern |
| 12 | Images as SQL blobs | Infrastructure; large change |
| 13 | Quest detail loads all quests | Performance; separate concern |
| 14 | Deep eager loading | Performance; separate concern |
| 24 | No pagination | Explicitly out of scope in PROJECT.md |
| 25 | No caching | Performance; not in milestone scope |

---

## Pitfalls Specific to This Refactor

### AutoMapper assembly scanning

`builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly())` or `typeof(Program).Assembly` scans only the Service assembly. After moving `EntityProfile` to Repository, the scan will miss it unless the Repository assembly is explicitly included. Use `typeof(EntityProfile)` (the one in Repository) as the anchor type, not assembly scanning. Verify by running the app and checking that entity-to-model mapping works on the first request.

### BaseService generic constraint after removing Repository reference from Domain

`BaseService<TModel, TEntity>` has a `TEntity` type parameter that was previously implicitly assumed to be a Repository entity. After the refactor, `TEntity` is still an unconstrained type parameter — Domain code never `new`s or imports entity types directly; it only passes them through `IMapper`. This is safe. The C# compiler does not require a project reference to satisfy a generic type parameter that is only instantiated by the caller (Service project or Repository registrations). Build and confirm.

### IQuestRepository still in Domain

`IQuestRepository` and `IBaseRepository<T>` are Domain interfaces — this is correct and should not change. Repository implements Domain interfaces; Domain does not know about the concrete Repository classes. The project reference direction (Repository → Domain) satisfies this.

### Do not move `UserService` to fix identity coupling in this milestone

`UserService` wraps `UserManager<UserEntity>` and `SignInManager<UserEntity>` (ASP.NET Core Identity types). Properly fixing this would require introducing an abstraction over Identity in Domain. That is a larger refactor not in scope for this milestone. Leave `UserService` as-is; it depends on Identity infrastructure but the project dependency graph is already Service → Domain → (nothing) once `EntityProfile` moves and the Repository project reference is removed from Domain.csproj. Actually: check whether `UserService` in Domain imports any types from `EuphoriaInn.Repository` directly. If it only imports `Microsoft.AspNetCore.Identity` (a separate NuGet), the project reference removal from Domain.csproj is still safe.

---

## Sources

- Microsoft .NET Microservices Architecture Guide — Infrastructure Persistence Layer: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- The Code Wrapper — EF Core: Effectively Decouple the Data and Domain Model: https://dev.to/thecodewrapper/ef-core-effectively-decouple-the-data-and-domain-model-4h8j
- The Code Wrapper — Implementing Clean Architecture in ASP.NET Core: https://thecodewrapper.com/dev/implementing-clean-architecture-in-aspnetcore-6/
- Microsoft Learn — Options pattern in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options
- Milan Jovanovic — Adding Validation to the Options Pattern: https://www.milanjovanovic.tech/blog/adding-validation-to-the-options-pattern-in-asp-net-core
- Gunnar Peipman — Moving code from controller action to service layer: https://gunnarpeipman.com/asp-net-mvc-moving-code-from-controller-action-to-service-layer/

---

*Architecture research: 2026-04-15*
