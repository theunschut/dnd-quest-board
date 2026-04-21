# Architecture

**Analysis Date:** 2026-04-15

## Pattern Overview

**Overall:** Three-layer Clean Architecture (Domain / Repository / Service) within a single deployable monolith

**Key Characteristics:**
- Strict dependency direction: Service → Domain → Repository (Domain does NOT depend on Service; Repository does NOT depend on Domain directly — they share interfaces defined in Domain)
- All cross-layer communication goes through interfaces; concrete implementations are hidden (`internal class QuestService`, etc.)
- ASP.NET Core Identity is integrated at the Repository layer via `IdentityDbContext`; the Domain `UserService` wraps `UserManager<UserEntity>` and `SignInManager<UserEntity>`
- AutoMapper is used at two distinct boundaries: Entity↔DomainModel (in `EuphoriaInn.Domain/Automapper/EntityProfile.cs`) and DomainModel↔ViewModel (in `EuphoriaInn.Service/Automapper/ViewModelProfile.cs`)
- Every service and repository is registered as `Scoped` via extension methods; the DI container is the only wiring point

## Layers

**Presentation (EuphoriaInn.Service):**
- Purpose: Handle HTTP requests, render Razor views, enforce authorization, coordinate domain services
- Location: `EuphoriaInn.Service/`
- Contains: MVC Controllers, Razor Views, ViewModels, AutoMapper `ViewModelProfile`, Authorization handlers/requirements, `Program.cs`
- Depends on: `EuphoriaInn.Domain` (interfaces only)
- Used by: End users via browser

**Domain (EuphoriaInn.Domain):**
- Purpose: Business logic, domain models, service interfaces, AutoMapper entity profiles
- Location: `EuphoriaInn.Domain/`
- Contains: `Models/`, `Services/` (internal implementations), `Interfaces/` (public contracts), `Automapper/EntityProfile.cs`, `Enums/`, `Extensions/ServiceExtensions.cs`
- Depends on: `EuphoriaInn.Repository` (interfaces via `IBaseRepository<T>`, concrete entities for AutoMapper)
- Used by: `EuphoriaInn.Service`

**Repository (EuphoriaInn.Repository):**
- Purpose: Data persistence, Entity Framework Core, ASP.NET Core Identity store
- Location: `EuphoriaInn.Repository/`
- Contains: `Entities/` (EF entity classes + `QuestBoardContext`), `Interfaces/` (repository contracts), concrete repository implementations, `Migrations/`, `Extensions/ServiceExtensions.cs`
- Depends on: SQL Server via EF Core
- Used by: `EuphoriaInn.Domain`

## Data Flow

**Typical Read Request (e.g., quest details page):**

1. Browser sends `GET /Quest/Details/{id}`
2. `QuestController.Details()` in `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` is invoked
3. Controller calls `IQuestService.GetQuestWithDetailsAsync(id)` (Domain interface)
4. `QuestService` (internal) calls `IQuestRepository.GetQuestWithDetailsAsync(id)` (Repository interface)
5. `QuestRepository` executes EF Core query against `QuestBoardContext`, returns `QuestEntity`
6. `QuestService` calls `AutoMapper.Map<Quest>(questEntity)` using `EntityProfile` — Entity → DomainModel
7. Controller calls `AutoMapper.Map<QuestDetailsViewModel>(quest)` using `ViewModelProfile` — DomainModel → ViewModel
8. Razor view renders the ViewModel

**Typical Write Request (e.g., finalize quest):**

1. Browser sends `POST /Quest/Finalize/{id}`
2. `QuestController.Finalize()` validates anti-forgery token and `[Authorize(Policy = "DungeonMasterOnly")]`
3. `DungeonMasterHandler` checks ASP.NET Identity roles via `IUserService.IsInRoleAsync()`
4. Controller calls `IQuestService.FinalizeQuestAsync(questId, date, selectedPlayerIds)`
5. `QuestService` fetches `QuestEntity`, mutates it directly, calls `repository.SaveChangesAsync()`
6. Controller calls `IEmailService.SendQuestFinalizedEmailAsync()` for each selected player
7. Controller redirects to management view

**State Management:**
- Authentication state: ASP.NET Core Identity cookies; session (`IdleTimeout = 24h`) for supplemental state
- Database state: Managed entirely through EF Core change tracking; `SaveChangesAsync()` is called explicitly in services
- UI state: `ViewBag` for simple controller-to-view data; strongly-typed ViewModels for form binding

## Key Abstractions

**IBaseService<TModel> / BaseService<TModel, TEntity>:**
- Purpose: Generic CRUD operations (Add, GetById, GetAll, Update, Remove, SaveChanges) common to all domain services
- Examples: `EuphoriaInn.Domain/Interfaces/IBaseService.cs`, `EuphoriaInn.Domain/Services/BaseService.cs`
- Pattern: Template Method — subclasses override specific methods (e.g., `QuestService.RemoveAsync` performs manual cascade cleanup before delegating)

**IBaseRepository<T> / BaseRepository<T>:**
- Purpose: Generic repository CRUD over EF Core entities
- Examples: `EuphoriaInn.Repository/Interfaces/IBaseRepository.cs`
- Pattern: Repository — concrete repos (`QuestRepository`, `UserRepository`) extend the base and add domain-specific queries (e.g., `GetQuestsWithDetailsAsync` with eager-loaded navigation properties)

**IModel:**
- Purpose: Marker interface for all domain models, ensures `int Id` is present
- Location: `EuphoriaInn.Domain/Models/IModel.cs`

**IEntity:**
- Purpose: Marker interface for all EF Core entities
- Location: `EuphoriaInn.Repository/Entities/IEntity.cs`

**AutoMapper Profiles:**
- `EntityProfile` (`EuphoriaInn.Domain/Automapper/EntityProfile.cs`): Maps between `*Entity` ↔ domain `Model` classes; handles enum int↔enum conversions, password/security fields exclusions
- `ViewModelProfile` (`EuphoriaInn.Service/Automapper/ViewModelProfile.cs`): Maps between domain `Model` ↔ `*ViewModel`; registered in `Program.cs`

## Entry Points

**Web Application Bootstrap:**
- Location: `EuphoriaInn.Service/Program.cs`
- Triggers: ASP.NET Core host startup
- Responsibilities: Configure Identity, Authorization policies, Session, DI registrations (via `AddRepositoryServices()` + `AddDomainServices()`), AutoMapper, Kestrel limits; run migrations (`ConfigureDatabase()`); seed shop data; mount default MVC route

**Default Route:**
- Pattern: `{controller=Home}/{action=Index}/{id?}`
- All controllers are in subdirectories under `EuphoriaInn.Service/Controllers/` and use standard MVC conventions

## Error Handling

**Strategy:** Minimal explicit error handling; relies on ASP.NET Core built-in pipeline

**Patterns:**
- Non-Development environments use the `/Error` exception handler page (`app.UseExceptionHandler("/Error")`)
- Controller actions return `NotFound()`, `BadRequest()`, `Challenge()`, or `Forbid()` where appropriate
- Services silently return `null` or early-exit when an entity is not found (e.g., `if (entity == null) return;`)
- `EmailService` catches all exceptions internally and logs a warning rather than propagating — email failure does not block the request
- Shop seed failures on startup are caught and logged without stopping the app

## Authorization

**Roles:** `Admin`, `DungeonMaster`, `Player` (stored in ASP.NET Identity `IdentityRole<int>`)

**Policies:**
- `"DungeonMasterOnly"` — satisfied if user is in `DungeonMaster` OR `Admin` role; implemented in `EuphoriaInn.Service/Authorization/DungeonMasterHandler.cs`
- `"AdminOnly"` — satisfied if user is in `Admin` role; implemented in `EuphoriaInn.Service/Authorization/AdminHandler.cs`

**Usage pattern in controllers:**
```csharp
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Create(...) { ... }
```

## Cross-Cutting Concerns

**Logging:** `ILogger<T>` injected into services that need it (e.g., `EmailService`); otherwise relies on ASP.NET Core default console logging
**Validation:** Data Annotations on domain `Model` classes and ViewModels; `ModelState.IsValid` checks in controllers; range validation in views via client-side Bootstrap
**Authentication:** ASP.NET Core Identity with `UserEntity : IdentityUser<int>`; `QuestBoardContext : IdentityDbContext<UserEntity, IdentityRole<int>, int>`
**Async:** All service and repository methods are fully async using `Task`/`async`/`await`; `CancellationToken` is threaded through the entire call chain

---

*Architecture analysis: 2026-04-15*
