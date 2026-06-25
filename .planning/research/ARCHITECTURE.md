# Architecture Research — Omphalos Integration

**Researched:** 2026-06-18
**Confidence:** HIGH — direct codebase inspection of both repos

---

## Quest Board New Components

### Phase 10: Admin Settings

**Goal:** Admin can save and load Omphalos URL and shared HMAC secret via a new /Admin/Settings page.

#### Layer Placement

AdminSetting is a simple key-value configuration store. It follows the same full-stack pattern as every other entity in the codebase: entity in Repository, domain model + interface + service in Domain, controller action in Service layer. There is no shortcut here — putting settings logic in the controller would violate the "thin controller" principle that the Milestone 2 refactor established.

#### New Files

**Repository layer — `EuphoriaInn.Repository/`**

- `Entities/AdminSettingEntity.cs` — EF entity implementing `IEntity`. Properties: `int Id` (identity PK, satisfies `IEntity` contract), `string Key` (unique), `string Value`, `DateTime UpdatedAt`. Uses data annotation `[Table("AdminSettings")]`, `[Key]`, `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, `[Required]`, `[StringLength(200)]` on Key, consistent with `ShopItemEntity` style.
- `Interfaces/IAdminSettingRepository.cs` — placed in `EuphoriaInn.Repository/Interfaces/` (not Domain, because this is a concrete infrastructure interface); note: repository interfaces go in `EuphoriaInn.Domain/Interfaces/` per the existing pattern — see `IQuestRepository`, `IShopRepository` etc. Therefore this file goes to `EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs`. Extends `IBaseRepository<AdminSetting>` and adds `Task<AdminSetting?> GetByKeyAsync(string key, CancellationToken ct)` and `Task UpsertByKeyAsync(string key, string value, CancellationToken ct)`.
- `AdminSettingRepository.cs` — placed at root of `EuphoriaInn.Repository/` (same level as `ShopRepository.cs`, `QuestRepository.cs`). Extends `BaseRepository<AdminSetting, AdminSettingEntity>`, implements `GetByKeyAsync` via `DbSet.FirstOrDefaultAsync(e => e.Key == key)` and `UpsertByKeyAsync` via find-then-add-or-update pattern, calls `DbContext.SaveChangesAsync`.

**QuestBoardContext modification** — `EuphoriaInn.Repository/Entities/QuestBoardContext.cs`

- Add `public DbSet<AdminSettingEntity> AdminSettings { get; set; }` — one line, follows the pattern of every other `DbSet` already present.
- Add a unique index on `Key` in `OnModelCreating`: `modelBuilder.Entity<AdminSettingEntity>().HasIndex(a => a.Key).IsUnique()` — prevents duplicate keys at the DB level.

**Domain layer — `EuphoriaInn.Domain/`**

- `Models/AdminSetting.cs` — domain model with `int Id`, `string Key`, `string Value`, `DateTime UpdatedAt`; implements `IModel`. Placed in `EuphoriaInn.Domain/Models/` alongside `Quest.cs`, `User.cs` etc.
- `Interfaces/IAdminSettingRepository.cs` — (see above — repository interfaces live in Domain per existing pattern)
- `Interfaces/IAdminSettingService.cs` — extends `IBaseService<AdminSetting>` with two extra methods: `Task<string?> GetValueAsync(string key, CancellationToken ct = default)` and `Task SetValueAsync(string key, string value, CancellationToken ct = default)`.
- `Services/AdminSettingService.cs` — `internal class AdminSettingService` extends `BaseService<AdminSetting>` (note: `BaseService<TModel>` does not have a TEntity parameter visible to callers — the concrete class signature is `internal class AdminSettingService(IAdminSettingRepository repository, IMapper mapper) : BaseService<AdminSetting>(repository, mapper), IAdminSettingService`). Implements `GetValueAsync` by calling `repository.GetByKeyAsync(key, ct)` and returning `result?.Value`. Implements `SetValueAsync` by calling `repository.UpsertByKeyAsync(key, value, ct)`.

**AutoMapper** — `EuphoriaInn.Repository/Automapper/EntityProfile.cs` (modified)

- Add `CreateMap<AdminSetting, AdminSettingEntity>().ReverseMap()` — both sides are flat primitives, no enum conversions needed. One line.

**Service layer — `EuphoriaInn.Service/`**

- `Controllers/Admin/AdminController.cs` (modified) — inject `IAdminSettingService` into the existing primary constructor alongside `IUserService` and `IQuestService`. Add two actions: `[HttpGet] public async Task<IActionResult> Settings()` that loads OmphalosUrl and OmphalosSecret keys and maps to `AdminSettingsViewModel`; `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> Settings(AdminSettingsViewModel model)` that calls `SetValueAsync` for each key.
- `ViewModels/AdminViewModels/AdminSettingsViewModel.cs` (new) — two string properties: `OmphalosUrl` (`[DataType(DataType.Url)]`, `[StringLength(500)]`) and `OmphalosSharedSecret` (`[StringLength(500)]`); neither is `[Required]` since the admin may want to clear them.
- `Views/Admin/Settings.cshtml` (new) — follows `modern-card` pattern mandated in CLAUDE.md. Two inputs: URL as text, secret as password (type="password" to prevent shoulder-surfing). Save button with `fa-save` icon. Back link to `/Admin/Users`.

#### Modified Files (Phase 10)

| File | Change |
|------|--------|
| `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` | Add `DbSet<AdminSettingEntity>` + unique index on Key |
| `EuphoriaInn.Repository/Automapper/EntityProfile.cs` | Add AdminSetting↔AdminSettingEntity map |
| `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` | Add `services.AddScoped<IAdminSettingRepository, AdminSettingRepository>()` |
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | Add `services.AddScoped<IAdminSettingService, AdminSettingService>()` |
| `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` | Inject `IAdminSettingService`; add Settings GET/POST actions |

#### EF Migration

One migration: `AddAdminSettings`. Adds `AdminSettings` table with `Id int identity PK`, `Key nvarchar(200) not null unique`, `Value nvarchar(max) not null`, `UpdatedAt datetime2 not null`. Created by running `dotnet ef migrations add AddAdminSettings --project ../EuphoriaInn.Repository` from the Service project directory. Auto-applied on startup.

---

### Phase 11: Navigation + Token Generation

**Goal:** DM navbar shows "Open DM Tool" when Omphalos URL is configured; Quest Detail and Manage pages show "Open Session Notes" button that generates a short-lived HMAC-signed redirect URL.

#### Where HMAC Token Generation Lives

**Decision: a dedicated `IIntegrationTokenService` in the Domain layer, not inline in the controller.**

Rationale:
- Token generation reads the shared secret from `IAdminSettingService` — that is a service call, which is business logic, not presentation logic.
- Controllers in this codebase do not contain business logic; they coordinate services and produce HTTP responses. Adding `HMACSHA256` calls to a controller action violates the pattern established in Milestone 2.
- The service will be reusable: future Omphalos → Quest Board API calls will need the same HMAC validation logic, and a service behind an interface can be tested in isolation.
- All Domain services are already `internal class` implementations behind `I*Service` interfaces — this is the established pattern.

#### New Files (Phase 11)

**Domain layer — `EuphoriaInn.Domain/`**

- `Interfaces/IIntegrationTokenService.cs` — public interface:

  ```csharp
  public interface IIntegrationTokenService
  {
      Task<string?> GenerateQuestDeepLinkAsync(int questId, string username, CancellationToken ct = default);
  }
  ```

  Returns `null` when Omphalos URL or secret is not configured; callers must handle gracefully (do not show button, do not redirect).

- `Services/IntegrationTokenService.cs` — `internal class IntegrationTokenService(IAdminSettingService settings) : IIntegrationTokenService`. Reads `OmphalosUrl` and `OmphalosSharedSecret` keys via `settings.GetValueAsync`. Constructs payload string `"{questId}|{username}|{unixTimestampUtc}"`. Computes `HMACSHA256(payload, sharedSecret)` using `System.Security.Cryptography.HMACSHA256` — this namespace is already available in the Domain project via `System.Security.Cryptography.Xml` 8.0.3. Sets expiry to `DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()`. Returns full URL: `{omphalosUrl}/api/sso/quest-board?questId={questId}&user={username}&expires={unix}&sig={hmacHex}`.

**Service layer — `EuphoriaInn.Service/`**

- `ViewComponents/OmphalosNavItemViewComponent.cs` (new) — `public class OmphalosNavItemViewComponent(IAdminSettingService settings) : ViewComponent`. Reads `OmphalosUrl` in `InvokeAsync`; passes a bool `IsConfigured` to the Default view.
- `Views/Shared/Components/OmphalosNavItem/Default.cshtml` (new) — renders a single `<li>` dropdown item `<a href="/Quest/LaunchOmphalos">Open DM Tool</a>` when `IsConfigured` is true.

No new controller files for Phase 11. Token generation and redirect are a new action on the existing `QuestController`.

#### Modified Files (Phase 11)

| File | Change |
|------|--------|
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | Add `services.AddScoped<IIntegrationTokenService, IntegrationTokenService>()` |
| `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` | Inject `IAdminSettingService` and `IIntegrationTokenService`; add `LaunchOmphalos(int id)` GET action; set `ViewBag.OmphalosConfigured` in Details and Manage actions |
| `EuphoriaInn.Service/Views/Quest/Details.cshtml` | Add conditional "Open Session Notes" button (shown if `ViewBag.OmphalosConfigured && ViewBag.CanManage`) |
| `EuphoriaInn.Service/Views/Quest/Manage.cshtml` | Add conditional "Open Session Notes" button (shown if `ViewBag.OmphalosConfigured`) |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | Add `@await Component.InvokeAsync("OmphalosNavItem")` in DM dropdown section |

#### ViewModel vs ViewBag decision for quest pages

The Details and Manage actions already use `ViewBag` heavily: `ViewBag.CanManage`, `ViewBag.IsAuthorized`, `ViewBag.IsAdmin`, `ViewBag.CalendarMonths`, `ViewBag.CurrentQuestId`. Adding `ViewBag.OmphalosConfigured = true/false` is one more boolean in an existing `ViewBag` pattern — no ViewModel wrapper needed. Introducing a strongly-typed ViewModel for the Manage action (which currently passes a raw `Quest` domain model via `return View(quest)`) would require wrapping the domain model, which is a larger refactor outside this milestone's scope.

#### Navbar View Component decision

The shared `_Layout.cshtml` cannot receive controller-specific `ViewBag` values injected from one specific controller — every page shares the layout. Options:

1. **View Component** — `OmphalosNavItemViewComponent` injects `IAdminSettingService` via DI, reads the URL, renders conditionally. This is the standard ASP.NET Core pattern for layout-level data requiring a service call. No controller modification needed for the navbar.
2. **Base controller with `OnActionExecutionAsync`** — sets `ViewBag.OmphalosUrl` for every action across all controllers. Introduces an inheritance coupling that does not exist in this codebase today (all controllers inherit directly from `Controller`). Avoid.

**Decision: View Component.** It is self-contained, follows the ASP.NET Core recommended pattern, and requires no changes to existing controllers.

---

## Omphalos New Components

### Phase 20: SSO Endpoint + Session Linking

**Goal:** Omphalos validates a Quest Board HMAC token, auto-provisions the DM account on first use, finds or creates the quest's `GameSession`, issues a JWT cookie, and redirects the user into the correct session.

#### Omphalos Architecture Recap (from reading)

Omphalos is a .NET 10 Minimal API + PostgreSQL app with a flat domain structure: entities in `Omphalos.Domain/Entities/`, interfaces in `Omphalos.Domain/Interfaces/`, DTOs in `Omphalos.Domain/DTOs/`, service implementations in `Omphalos.Services/Implementations/`, repositories in `Omphalos.Repository/Repositories/`, entity configurations via `IEntityTypeConfiguration<T>` in `Omphalos.Repository/Configurations/`, and endpoint groups in `Omphalos.Web/Endpoints/`. `Program.cs` registers everything directly — no extension method wrappers.

`GameSession.Id` is a client-provided `string` (not auto-generated). `User.Id` is a `Guid`. There are no base repository or base service classes — each service and repository is standalone.

#### ExternalQuestId on GameSession

`GameSession` needs a nullable `int? ExternalQuestId` to record which Quest Board quest it corresponds to. This allows the SSO endpoint to find-or-create the session for a given quest.

**Domain entity** — `Omphalos.Domain/Entities/GameSession.cs` (modified)
- Add `public int? ExternalQuestId { get; set; }` — nullable, so existing sessions without a Quest Board link continue to work unchanged.

**Repository configuration** — `Omphalos.Repository/Configurations/GameSessionConfiguration.cs` (modified)
- Add `builder.Property(s => s.ExternalQuestId).IsRequired(false)` — nullable column.
- Add `builder.HasIndex(s => s.ExternalQuestId)` — non-unique index for fast lookup by external ID (one DM can theoretically have multiple sessions for the same quest over time, so unique is wrong here).

**Repository interface** — `Omphalos.Domain/Interfaces/ISessionRepository.cs` (modified)
- Add `Task<GameSession?> GetByExternalQuestIdAsync(Guid userId, int questId, CancellationToken ct = default)`.

**Repository implementation** — `Omphalos.Repository/Repositories/SessionRepository.cs` (modified)
- Implement `GetByExternalQuestIdAsync`: `.Where(s => s.UserId == userId && s.ExternalQuestId == questId).OrderByDescending(s => s.DateModified).FirstOrDefaultAsync(ct)` — returns the most recently modified session for this quest/user combination.

**EF Migration** — `AddExternalQuestIdToGameSession`
- Adds nullable `external_quest_id int` column to `game_sessions` table (Npgsql snake_cases the property name automatically unless overridden in config).
- Adds the index.
- Run `dotnet ef migrations add AddExternalQuestIdToGameSession` from the Omphalos.Web or appropriate project; auto-applied on startup via existing `db.Database.MigrateAsync()` in Program.cs.

#### SsoEndpoints.cs

**New file** — `Omphalos.Web/Endpoints/SsoEndpoints.cs`

Pattern matches `AuthEndpoints.cs` exactly: a static class with a `MapSsoEndpoints(this IEndpointRouteBuilder app)` extension method.

Endpoint: `POST /api/sso/quest-board`, `AllowAnonymous`.

Request body (JSON): `{ questId: int, user: string, expires: long, sig: string }` — maps to a new `SsoRequest` DTO.

The endpoint:
1. Passes the request to `ISsoService.ValidateAndProvisionAsync` — validates HMAC signature and expiry, provisions user if not found, finds or creates session.
2. On failure (invalid sig, expired token, missing secret config), returns `Results.Unauthorized()` or `Results.BadRequest("message")`.
3. On success, calls `IAuthService` to generate a JWT for the provisioned user.
4. Sets the `omphalos_token` cookie using the same `TokenCookieOptions` already defined in `AuthEndpoints.cs` — these options should be extracted to a shared constant in the `Endpoints` namespace or a shared `CookieConfig` static class to avoid duplication.
5. Returns `Results.Ok(new { sessionId, redirectUrl })` — the React SPA uses the `sessionId` to navigate to the correct session.

#### ISsoService (new interface and implementation)

HMAC validation and find-or-create session logic belongs in a service (Domain layer), not in the endpoint (Web layer). This keeps business logic testable and keeps the endpoint thin.

**New file** — `Omphalos.Domain/Interfaces/ISsoService.cs`

```csharp
public interface ISsoService
{
    Task<SsoResult> ValidateAndProvisionAsync(SsoRequest request, CancellationToken ct = default);
}
```

**New file** — `Omphalos.Services/Implementations/SsoService.cs`

`SsoService(IUserRepository users, ISessionRepository sessions, IConfiguration config) : ISsoService`

`ValidateAndProvisionAsync` logic:
1. Read `QuestBoard:Secret` from config — if absent, return failure result (Omphalos is misconfigured).
2. Validate `request.Expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds()` — token not expired.
3. Recompute HMAC: `HMACSHA256("{questId}|{user}|{expires}", secret)` — compare constant-time with `CryptographicOperations.FixedTimeEquals`.
4. Look up user by username via `users.GetByUsernameAsync(request.User)` — if not found, auto-provision with `BCrypt`-hashed random password (same as `AuthService.SeedAdminAsync` pattern; role = `UserRole.Player` initially).
5. Call `sessions.GetByExternalQuestIdAsync(user.Id, request.QuestId)` — if null, create a stub `GameSession` with `Id = Guid.NewGuid().ToString()`, `Title = $"Quest #{request.QuestId}"`, `ExternalQuestId = request.QuestId`, `UserId = user.Id`, `DateCreated = DateModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()`.
6. Return `SsoResult(User: user, SessionId: session.Id, IsNewSession: wasCreated)`.

**IAuthService modification** — `Omphalos.Domain/Interfaces/IAuthService.cs` (modified)

`GenerateToken(User user)` is currently `private` in `AuthService`. The SSO endpoint needs to issue a JWT after provisioning. Add `string GenerateToken(User user)` to `IAuthService` (make it `internal` in the interface if desired, but `public` is simpler and consistent). `AuthService` makes its existing private `GenerateToken` method the public implementation.

Alternatively, add `Task<LoginResponse> ExchangeForTokenAsync(User user, CancellationToken ct)` to keep the interface at the LoginResponse abstraction level. Either works — the simpler change is exposing `GenerateToken` directly, since `AuthService` already has the full implementation.

#### New DTOs

- `Omphalos.Domain/DTOs/SsoRequest.cs` — `public record SsoRequest(int QuestId, string User, long Expires, string Sig)`
- `Omphalos.Domain/DTOs/SsoResult.cs` — `public record SsoResult(bool Success, Domain.Entities.User? User, string? SessionId, bool IsNewSession, string? ErrorMessage = null)`

#### Program.cs modifications

Two additions in `Program.cs`:

In the services registration section (after `IGlobalCharacterService`):
```csharp
builder.Services.AddScoped<ISsoService, SsoService>();
```

In the endpoint registration section (after `app.MapGlobalCharacterEndpoints()`):
```csharp
app.MapSsoEndpoints();
```

#### HMAC secret configuration

Omphalos reads the shared secret from config key `QuestBoard:Secret`. In Docker Compose, this maps to env var `QuestBoard__Secret` (double-underscore convention, consistent with how Omphalos already handles `Jwt__Secret`, `Admin__Username` etc.). The value must match the `OmphalosSharedSecret` stored in Quest Board's `AdminSettings` table. This is a deployment concern, not an architectural one.

#### Modified Files (Phase 20 — Omphalos)

| File | Change |
|------|--------|
| `Omphalos.Domain/Entities/GameSession.cs` | Add `int? ExternalQuestId` |
| `Omphalos.Domain/Interfaces/ISessionRepository.cs` | Add `GetByExternalQuestIdAsync` |
| `Omphalos.Domain/Interfaces/IAuthService.cs` | Expose `GenerateToken(User user)` |
| `Omphalos.Repository/Configurations/GameSessionConfiguration.cs` | Add nullable column config + index |
| `Omphalos.Repository/Repositories/SessionRepository.cs` | Implement `GetByExternalQuestIdAsync` |
| `Omphalos.Services/Implementations/AuthService.cs` | Make `GenerateToken` public to satisfy interface |
| `Omphalos.Web/Program.cs` | Register `ISsoService`; call `app.MapSsoEndpoints()` |

#### New Files (Phase 20 — Omphalos)

| File | Purpose |
|------|---------|
| `Omphalos.Domain/Interfaces/ISsoService.cs` | Interface for SSO validation and provisioning |
| `Omphalos.Domain/DTOs/SsoRequest.cs` | Inbound token payload DTO |
| `Omphalos.Domain/DTOs/SsoResult.cs` | Result of validation/provisioning |
| `Omphalos.Services/Implementations/SsoService.cs` | HMAC validation, user provisioning, session find-or-create |
| `Omphalos.Web/Endpoints/SsoEndpoints.cs` | `POST /api/sso/quest-board` HTTP endpoint |

---

## Integration Points

### Phase 10 is the sole blocker for Phase 11

Phase 11 has a hard compile-time dependency on Phase 10. `IIntegrationTokenService` calls `IAdminSettingService.GetValueAsync` — the service interface and entity must exist before Phase 11 compiles. The View Component for the navbar also calls `IAdminSettingService`. Phase 11 cannot start until the `AdminSetting` entity, migration, and service registration from Phase 10 are complete and merged.

### Phase 20 is independent of Phases 10 and 11

Phase 20 (Omphalos SSO endpoint) has no compile-time dependency on any Quest Board code. Both repos are independent deployments. Phase 20 can be developed and deployed to the Omphalos repo entirely in parallel with Quest Board Phase 10 work, provided the token format contract is agreed before either side begins implementation.

**Token format contract — must be agreed before any implementation begins:**

```
Payload:    "{questId}|{username}|{unixTimestampUtc}"
Algorithm:  HMAC-SHA256
Key:        the shared secret string, UTF-8 encoded bytes
Signature:  lowercase hex string of the HMAC bytes
URL params: questId (int), user (string), expires (unix timestamp seconds), sig (hex string)
Expiry:     5 minutes from generation time
```

This contract is the sole coupling point between Phase 11 and Phase 20. Write it as a comment at the top of both `IntegrationTokenService.cs` and `SsoService.cs`.

### End-to-end test dependency

The first end-to-end test requires both apps running and the shared secret configured in both. This is the only point where the two development streams converge. It cannot be tested until Phase 11 generates valid tokens and Phase 20 validates them in a shared environment.

---

## Build Order

```
Phase 10: Admin Settings (Quest Board only)
    |
    | Phase 10 merged + migration deployed
    |
    +--> Phase 11: Navigation + Token Generation (Quest Board)
    |       Depends on: IAdminSettingService, AdminSettingEntity migration
    |       Can start only after Phase 10 is merged
    |
    +--> Phase 20: SSO Endpoint + Session Linking (Omphalos)
            Depends on: agreed token format contract only
            Can start in PARALLEL with Phase 11 (independent repo)
                |
                | Both Phase 11 AND Phase 20 complete
                |
            End-to-end integration test (both containers running)
```

**Practical parallel work:** A developer working on Omphalos (Phase 20) has zero blocked time once the token format contract is written down. Phase 20 work on Omphalos can begin the moment the format is agreed during or immediately after Phase 10 planning. The Quest Board phases (10 then 11) are sequential in the same repo.

---

## Key Architectural Decisions

### IIntegrationTokenService vs inline controller logic

**Decision: dedicated `IIntegrationTokenService` in the Domain layer.**

Inline approach rejected because: (1) reading the shared secret requires calling `IAdminSettingService`, which is a service-layer dependency not appropriate for a controller; (2) putting `HMACSHA256` in a controller action contradicts the thin-controller principle enforced in Milestone 2; (3) the same signing logic will be needed for future bidirectional API authentication. A service behind a public interface is testable, injectable, and reusable.

### ViewModel vs ViewBag for Omphalos URL availability on quest pages

**Decision: ViewBag boolean flag, consistent with existing pattern.**

The Details and Manage actions already use `ViewBag` for five different flags. Adding `ViewBag.OmphalosConfigured = true/false` adds no new pattern. Introducing a strongly-typed ViewModel wrapper for the Manage action (which currently passes a raw `Quest` domain model) would require a wider refactor outside this milestone's scope.

### View Component for navbar vs base controller inheritance

**Decision: View Component.**

A `BaseController` that sets `ViewBag.OmphalosUrl` in `OnActionExecutionAsync` for every request would introduce inheritance coupling that does not exist in this codebase and would fire a DB read on every request from every controller. A View Component fires once per layout render, is self-contained, follows the ASP.NET Core documented pattern for layout-level service calls, and requires no changes to any existing controller.

### ExternalQuestId as nullable column on GameSession vs join table

**Decision: nullable column on `GameSession`.**

A join table is warranted only if a `GameSession` could link to multiple Quest Board quests, or if multiple external systems could each add their own quest ID. For this milestone the relationship is 1-per-DM-per-quest. A nullable column is simpler, requires no join on session reads, and is directly indexable. If the relationship becomes many-to-many in a future milestone, a join table migration is a forward step, not a rewrite.

### HMAC secret storage: DB in Quest Board, env var in Omphalos

**Decision: asymmetric storage.**

Quest Board stores the secret in `AdminSettings` (editable via Admin UI — the same Admin UI that manages users and quests). Omphalos reads it from env var `QuestBoard__Secret` in docker-compose (consistent with how Omphalos already handles `Jwt__Secret` and `Admin__Password`). Omphalos has no admin UI for secret management in this milestone. The operational requirement — that both values must match — is a deployment concern, not an architectural one.

### Token expiry: 5 minutes, no revocation list

**Decision: 5-minute expiry, no revocation.**

Short-lived prevents replay attacks via shared or bookmarked redirect links. The user experience is: click button → fresh token generated → redirected immediately → token consumed. Five minutes is long enough to survive a brief network delay or a browser redirect chain, but short enough that a leaked token is practically useless. No revocation list is needed because the attack window is too narrow to be exploitable in practice for a self-hosted group app.

### Auto-provisioning: random password, Player role

**Decision: provision with random BCrypt-hashed password, initial role Player.**

On first SSO from Quest Board, Omphalos creates a new `User` with a cryptographically random password that the user can never log in with directly (they always use the Quest Board SSO link). Role is `Player` — not `Admin`. If the user needs elevated Omphalos permissions, an Omphalos admin promotes them separately. This keeps the provisioning path minimal and avoids accidental privilege escalation.
