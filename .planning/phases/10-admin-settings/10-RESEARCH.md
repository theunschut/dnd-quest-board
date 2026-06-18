# Phase 10: Admin Settings - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC — key-value settings persistence, EF Core migrations, admin page patterns
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Use key-value store entity — `AdminSettingEntity` with columns `Key nvarchar(200) PK`, `Value nvarchar(max)`, `UpdatedAt datetime2`. Overrides SETT-06 (typed single-row entity).
- **D-02:** Keys in use for Phase 10: `OmphalosUrl`, `OmphalosSharedSecret`, `IsEnabled`.
- **D-03:** EF migration creates `AdminSettings` table (or `IntegrationSettings` — planner to decide; must be consistent with entity name).
- **D-04:** Service exposes single method: `GetSettingsAsync()` returning typed `IntegrationSettings` record with `OmphalosUrl (string?)`, `OmphalosSharedSecret (string?)`, `IsEnabled (bool)`.
- **D-05:** When no settings exist in DB, `GetSettingsAsync()` returns default record with `IsEnabled = false` and null URL/secret — never returns null.
- **D-06:** `IAdminSettingService` registered as Scoped — reads from DB per-request; settings take effect without restart.
- **D-07:** Service name is `IAdminSettingService` (not `IIntegrationSettingService`) — generic for future extensibility.
- **D-08:** SETT-04: saving the form with secret field blank MUST preserve existing secret. Service checks: if incoming secret value is null/empty, skip `OmphalosSharedSecret` key upsert entirely.
- **D-09:** Secret field renders as `type="password"`. View must display hint: "Leave blank to keep the existing value."
- **D-10:** Integration tests for service + repository layer. SQLite in-memory via `TestDatabase` helper. Cover: GetSettingsAsync() returns default when empty; returns stored values after save; blank secret preserves existing; upsert overwrites on second save.

### Claude's Discretion

- Table name for `AdminSettingEntity` (e.g., `AdminSettings` vs `IntegrationSettings`) — pick one consistent with entity name
- View layout follows `modern-card` pattern per CLAUDE.md conventions
- Settings link placement in Admin navbar dropdown (after Quest Management with a divider, or at the end)
- `IntegrationSettings` record lives in `EuphoriaInn.Domain/Models/` alongside other domain models

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SETT-01 | Admin can navigate to a Settings page from the Admin navbar dropdown | Existing `_Layout.cshtml` admin dropdown at lines 37–48; add divider + link after Quest Management |
| SETT-02 | Settings page has input fields for Omphalos URL and shared secret | UI-SPEC defines exact HTML; ViewModel with `OmphalosUrl`, `OmphalosSharedSecret`, `IsEnabled` |
| SETT-03 | Shared secret field renders as `type="password"` (masked) | UI-SPEC component contract §4; standard ASP.NET Core tag helpers |
| SETT-04 | Blank secret on form submit preserves existing value | Service upsert: skip `OmphalosSharedSecret` row when value is null/empty (D-08) |
| SETT-05 | "Integration Enabled" checkbox controls all Omphalos UI visibility | `IsEnabled` key stored as string "True"/"False"; parsed in `GetSettingsAsync()` |
| SETT-06 | Settings persisted in DB — OVERRIDDEN by D-01 (key-value store, not typed single-row) | `AdminSettingEntity` with string PK; EF upsert via FindAsync + Add/Update pattern |
| SETT-07 | Settings page protected by `AdminOnly` authorization policy | `AdminController` already has `[Authorize(Policy = "AdminOnly")]` at class level; `Settings` action inherits it |
| SETT-08 | EF Core migration creates the settings table | Standard EF migration; pattern confirmed from `AddDMProfileSystem` migration |

</phase_requirements>

---

## Summary

Phase 10 delivers `IAdminSettingService`, the compile-time dependency that Phases 11 and 12 build on. The phase is a self-contained vertical slice: new entity (`AdminSettingEntity`) → new repository (`IAdminSettingRepository` / `AdminSettingRepository`) → new domain service (`IAdminSettingService` / `AdminSettingService`) → new controller actions on the existing `AdminController` → new Razor view (`Settings.cshtml`) → new EF migration.

The key architectural constraint is that `AdminSettingEntity` uses a `string` PK (`Key`), not `int`. This means it **cannot** implement `IEntity` (which requires `int Id`) and `AdminSettingService` **cannot** extend `BaseService<TModel>`. Both the entity and service are standalone — they do not participate in the generic CRUD hierarchy. The repository also cannot extend `BaseRepository<TModel, TEntity>` for the same reason. This is expected and analogous to how `IdentityService` is standalone (`IdentityService` does not extend `BaseRepository`).

The upsert pattern (save → check if row exists → add if absent, update if present) is the correct approach for a string-keyed key-value store in EF Core. The existing codebase has the `DungeonMasterProfileRepository` as the closest analog for "upsert without BaseRepository" patterns.

**Primary recommendation:** Implement as a clean vertical slice with standalone entity/repository/service classes that do not touch the generic base hierarchy. The service exposes a single `SaveSettingsAsync(string? url, string? secret, bool isEnabled)` method for the controller POST action, plus `GetSettingsAsync()` for GET and downstream consumers.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Settings persistence | Database / EF Core | — | Key-value rows in `AdminSettings` table |
| Settings read (per-request) | API / Domain | — | `AdminSettingService.GetSettingsAsync()` queries DB each request (Scoped) |
| Settings write (form POST) | API / Domain | — | `AdminSettingService.SaveSettingsAsync()` upserts rows via repository |
| Admin settings UI | Frontend (Razor MVC) | — | `Settings.cshtml` view rendered by `AdminController.Settings` |
| Authorization enforcement | Frontend Server (ASP.NET Core middleware) | — | `[Authorize(Policy = "AdminOnly")]` on `AdminController` class |
| Navbar settings link | Frontend (Razor layout) | — | `_Layout.cshtml` admin dropdown — rendered for authenticated admins only |
| Integration test coverage | Test layer | — | SQLite in-memory via `TestDatabase`; service + repository wired directly |

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore | 9.0.6 [VERIFIED: csproj] | ORM for `AdminSettingEntity` + migration | Project standard; already in `EuphoriaInn.Repository` |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.6 [VERIFIED: csproj] | SQL Server provider | Project database is SQL Server |
| AutoMapper | 14.0.0 [VERIFIED: csproj] | Entity ↔ model mapping | Used at Entity↔DomainModel boundary (EntityProfile) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.6 [VERIFIED: csproj] | SQLite for integration tests | Integration test DB via `TestDatabase` helper |
| xUnit | 2.5.3 [VERIFIED: csproj] | Test runner | All tests in this project |
| FluentAssertions | 8.8.0 [VERIFIED: csproj] | Assertions | All test assertions |

### No New Packages Required
Phase 10 introduces no new NuGet dependencies. All required packages are already present in the relevant projects.

---

## Architecture Patterns

### System Architecture Diagram

```
HTTP GET /Admin/Settings
        |
        v
[AdminController.Settings GET]  -- [Authorize(Policy="AdminOnly")] (class-level)
        |
        v
[IAdminSettingService.GetSettingsAsync()]
        |
        v
[AdminSettingRepository.GetAllSettingsAsync()]
        |
        v
[QuestBoardContext.AdminSettings DbSet] --> SQL: SELECT * FROM AdminSettings
        |
        v
[Map rows to IntegrationSettings record]  -- Key lookup: "OmphalosUrl", "OmphalosSharedSecret", "IsEnabled"
        |
        v
[SettingsViewModel populated]  -- OmphalosUrl pre-filled; secret field EMPTY (type=password)
        |
        v
[Settings.cshtml rendered]


HTTP POST /Admin/Settings
        |
        v
[AdminController.Settings POST]  -- [ValidateAntiForgeryToken]
        |
        v (model valid?)
[IAdminSettingService.SaveSettingsAsync(url, secret, isEnabled)]
        |
        v
[AdminSettingRepository.UpsertAsync("OmphalosUrl", url)]
[AdminSettingRepository.UpsertAsync("IsEnabled", isEnabled.ToString())]
[AdminSettingRepository.UpsertAsync("OmphalosSharedSecret", secret)]  -- SKIPPED if secret null/empty
        |
        v
TempData["SuccessMessage"] = "Integration settings saved successfully."
RedirectToAction("Settings")  -- PRG pattern
```

### Recommended Project Structure (new files only)

```
EuphoriaInn.Domain/
├── Models/
│   └── IntegrationSettings.cs          # record with OmphalosUrl, OmphalosSharedSecret, IsEnabled, IsConfigured
├── Interfaces/
│   ├── IAdminSettingService.cs          # GetSettingsAsync() + SaveSettingsAsync()
│   └── IAdminSettingRepository.cs       # GetByKeyAsync(), UpsertAsync(), GetAllAsync()
└── Services/
    └── AdminSettingService.cs           # internal class; standalone (NOT extending BaseService)

EuphoriaInn.Repository/
├── Entities/
│   └── AdminSettingEntity.cs            # Key (string PK), Value (string?), UpdatedAt (datetime2)
├── AdminSettingRepository.cs            # internal class; standalone (NOT extending BaseRepository)
└── Migrations/
    └── [timestamp]_AddAdminSettings.cs  # CreateTable "AdminSettings"

EuphoriaInn.Service/
├── ViewModels/AdminViewModels/
│   └── SettingsViewModel.cs             # OmphalosUrl, OmphalosSharedSecret, IsEnabled
└── Views/Admin/
    └── Settings.cshtml                  # modern-card pattern; PRG; TempData alerts

EuphoriaInn.IntegrationTests/
└── Services/
    └── AdminSettingServiceTests.cs      # D-10 integration tests
```

### Pattern 1: Standalone Repository with String PK (Key-Value Upsert)

**What:** `AdminSettingRepository` does not extend `BaseRepository<TModel, TEntity>` because the entity has a string PK. It directly injects `QuestBoardContext` and performs find-or-add/update.

**When to use:** Whenever the entity has a non-int primary key or the generic CRUD contract does not fit the domain.

**Example:**
```csharp
// Source: Verified from DungeonMasterProfileRepository.cs pattern + EF Core docs
internal class AdminSettingRepository(QuestBoardContext dbContext) : IAdminSettingRepository
{
    public async Task<AdminSettingEntity?> GetByKeyAsync(string key, CancellationToken token = default)
    {
        return await dbContext.AdminSettings.FindAsync([key], cancellationToken: token);
    }

    public async Task UpsertAsync(string key, string? value, CancellationToken token = default)
    {
        var existing = await dbContext.AdminSettings.FindAsync([key], cancellationToken: token);
        if (existing == null)
        {
            await dbContext.AdminSettings.AddAsync(new AdminSettingEntity
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            }, token);
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(token);
    }

    public async Task<IList<AdminSettingEntity>> GetAllAsync(CancellationToken token = default)
    {
        return await dbContext.AdminSettings.ToListAsync(token);
    }
}
```

### Pattern 2: Standalone Domain Service (No BaseService)

**What:** `AdminSettingService` does not extend `BaseService<TModel>` because there is no corresponding `IModel` domain model for settings rows — the service works directly with the `IntegrationSettings` record DTO.

**When to use:** Whenever a service's contract does not map cleanly to `IBaseService<T>` CRUD operations.

**Example:**
```csharp
// Source: Verified from existing service patterns; IAdminSettingService is standalone
internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
{
    public async Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default)
    {
        var rows = await repository.GetAllAsync(token);
        var dict = rows.ToDictionary(r => r.Key, r => r.Value);

        return new IntegrationSettings
        {
            OmphalosUrl = dict.GetValueOrDefault("OmphalosUrl"),
            OmphalosSharedSecret = dict.GetValueOrDefault("OmphalosSharedSecret"),
            IsEnabled = bool.TryParse(dict.GetValueOrDefault("IsEnabled"), out var enabled) && enabled
        };
    }

    public async Task SaveSettingsAsync(
        string? url,
        string? secret,
        bool isEnabled,
        CancellationToken token = default)
    {
        await repository.UpsertAsync("OmphalosUrl", url, token);
        await repository.UpsertAsync("IsEnabled", isEnabled.ToString(), token);

        // D-08: blank secret = preserve existing — skip upsert entirely
        if (!string.IsNullOrWhiteSpace(secret))
        {
            await repository.UpsertAsync("OmphalosSharedSecret", secret, token);
        }
    }
}
```

### Pattern 3: IntegrationSettings Record with IsConfigured Convenience Property

**What:** The return type of `GetSettingsAsync()` is a C# record with a computed convenience property. Phase 11 uses `settings.IsConfigured` to decide whether to show Omphalos UI elements.

**Example:**
```csharp
// Source: CONTEXT.md §Specific Ideas; C# record syntax (ASSUMED for exact record vs class syntax)
namespace EuphoriaInn.Domain.Models;

public record IntegrationSettings
{
    public string? OmphalosUrl { get; init; }
    public string? OmphalosSharedSecret { get; init; }
    public bool IsEnabled { get; init; }

    // Phase 11 needs both conditions satisfied to show any Omphalos UI
    public bool IsConfigured => IsEnabled && !string.IsNullOrWhiteSpace(OmphalosUrl);
}
```

### Pattern 4: AdminController Settings Actions (PRG Pattern)

**What:** GET loads from service and maps to ViewModel; POST validates, calls service, sets TempData, redirects (Post-Redirect-Get). Matches the pattern already used in `ResetPassword`.

**Example:**
```csharp
// Source: Verified from AdminController.cs and ResetPassword pattern
[HttpGet]
public async Task<IActionResult> Settings()
{
    var settings = await adminSettingService.GetSettingsAsync();
    var model = new SettingsViewModel
    {
        OmphalosUrl = settings.OmphalosUrl,
        // DO NOT populate OmphalosSharedSecret — password fields load empty
        IsEnabled = settings.IsEnabled
    };
    return View(model);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Settings(SettingsViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    await adminSettingService.SaveSettingsAsync(
        model.OmphalosUrl,
        model.OmphalosSharedSecret,  // null/empty = preserve existing (D-08)
        model.IsEnabled);

    TempData["SuccessMessage"] = "Integration settings saved successfully.";
    return RedirectToAction(nameof(Settings));
}
```

### Pattern 5: AdminSettingEntity — String PK with [Table] Attribute

**What:** Entity uses `Key nvarchar(200)` as primary key. Must use `[DatabaseGenerated(DatabaseGeneratedOption.None)]` since the key is user-assigned (not DB-generated). Does NOT implement `IEntity` (which requires `int Id`).

**Example:**
```csharp
// Source: Verified from DungeonMasterProfileEntity.cs pattern + EF Core data annotations
[Table("AdminSettings")]
public class AdminSettingEntity  // intentionally NOT : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; }
}
```

### Pattern 6: Integration Tests for Standalone Service

**What:** D-10 integration tests wire up `TestDatabase` directly (not via `WebApplicationFactoryBase`) since this is a service/repository test, not a controller/HTTP test.

**Example:**
```csharp
// Source: Verified from TestDatabase.cs and existing integration test structure
public class AdminSettingServiceTests : IDisposable
{
    private readonly TestDatabase _db;
    private readonly IAdminSettingService _sut;

    public AdminSettingServiceTests()
    {
        _db = new TestDatabase($"AdminSettingTest_{Guid.NewGuid():N}");
        var context = _db.CreateContext();
        var repo = new AdminSettingRepository(context);
        _sut = new AdminSettingService(repo);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenDbEmpty_ReturnsDefault()
    {
        var result = await _sut.GetSettingsAsync();

        result.Should().NotBeNull();
        result.IsEnabled.Should().BeFalse();
        result.OmphalosUrl.Should().BeNull();
        result.OmphalosSharedSecret.Should().BeNull();
    }

    // ...additional test cases per D-10

    public void Dispose() => _db.Dispose();
}
```

### Anti-Patterns to Avoid

- **Extending BaseService/BaseRepository for string-keyed entities:** `IModel` and `IEntity` both require `int Id`. Forcing a string-keyed entity into the generic hierarchy would require an awkward adapter layer. Use standalone classes instead.
- **Populating the secret field on GET:** The `OmphalosSharedSecret` property on the ViewModel returned by the GET action must remain null/empty. Populating it would cause an empty string to be submitted if the user saves without editing, which would wipe the stored secret despite D-08.
- **Missing `[ValidateAntiForgeryToken]` on POST:** All POST actions in `AdminController` use this attribute. The new Settings POST action must include it.
- **Registering `AdminSettingEntity` in `QuestBoardContext` without `OnModelCreating` config:** For SQL Server, EF Core will infer the column type from `[StringLength(200)]` on the PK. However, `nvarchar(max)` on `Value` requires no special annotation (it is the EF Core default for `string?` with no `[StringLength]`). The `[Table("AdminSettings")]` attribute controls the generated table name — no `OnModelCreating` call is strictly required, but verifying column types against SQL Server defaults is advisable.
- **AutoMapper for IntegrationSettings:** Do not add a mapping between `AdminSettingEntity` and `IntegrationSettings` in `EntityProfile`. The service builds `IntegrationSettings` from a dictionary lookup — no AutoMapper involvement. This avoids a mapping that would have no use after Phase 10.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| CSRF protection on POST | Custom token validation | `[ValidateAntiForgeryToken]` attribute | Already used on every POST in AdminController |
| Admin authorization check | Role check in action body | `[Authorize(Policy = "AdminOnly")]` (class-level, already present) | Handler already registered; Settings action inherits it |
| Password field masking | JavaScript masking | `type="password"` HTML attribute | Native browser behavior; no JS needed |
| Model validation summary | Custom error display | `asp-validation-summary="ModelOnly"` | Pattern from EditUser.cshtml; works with DataAnnotations |
| TempData feedback | Toast library | Bootstrap dismissible alert with `TempData` | Matches existing admin page pattern (ResetPassword) |

---

## Common Pitfalls

### Pitfall 1: AdminSettingEntity Implements IEntity
**What goes wrong:** Compiler error — `IEntity` requires `public int Id { get; }` but the entity's PK is `public string Key { get; set; }`. If the planner specifies `AdminSettingEntity : IEntity`, the build fails immediately.
**Why it happens:** All other entities in the project implement `IEntity`. It is easy to add it by habit.
**How to avoid:** `AdminSettingEntity` must NOT implement `IEntity`. Explicitly comment this in the entity class: `// String PK — does not implement IEntity`.
**Warning signs:** Build error "does not implement interface member 'IEntity.Id'".

### Pitfall 2: BaseRepository Constraint Mismatch
**What goes wrong:** `BaseRepository<TModel, TEntity>` constrains `TEntity : class, IEntity`. Since `AdminSettingEntity` does not implement `IEntity`, `AdminSettingRepository` cannot extend `BaseRepository`.
**Why it happens:** Reflex to follow the established pattern for all repositories.
**How to avoid:** `AdminSettingRepository` is standalone. Inject `QuestBoardContext` directly.
**Warning signs:** Compiler error "The type 'AdminSettingEntity' cannot be used as type parameter 'TEntity'".

### Pitfall 3: Blank Secret Overwrites Existing Value
**What goes wrong:** SETT-04 broken — admin loads the settings page, does not touch the secret field (which loads blank as a password input), saves → blank string replaces the stored secret in the DB.
**Why it happens:** The upsert logic unconditionally calls `UpsertAsync("OmphalosSharedSecret", model.OmphalosSharedSecret)`.
**How to avoid:** In `AdminSettingService.SaveSettingsAsync()`: `if (!string.IsNullOrWhiteSpace(secret)) { await repository.UpsertAsync("OmphalosSharedSecret", secret, token); }` — skip entirely when blank.
**Warning signs:** Integration test "BlankSecret_Preserves_ExistingSecret" fails.

### Pitfall 4: IsEnabled Stored as Boolean vs String
**What goes wrong:** EF Core maps `bool` to `bit` in SQL Server. The key-value design stores `Value` as `nvarchar(max)`. If the code stores `true`/`false` as a C# bool in the `Value` column, EF Core will throw a type mismatch.
**Why it happens:** Developers sometimes store the bool directly and expect EF to handle conversion.
**How to avoid:** Store `IsEnabled` as the string `"True"` or `"False"` (via `isEnabled.ToString()`). Parse on read: `bool.TryParse(dict.GetValueOrDefault("IsEnabled"), out var enabled) && enabled`.
**Warning signs:** `InvalidCastException` or `InvalidOperationException` at runtime when reading/writing `IsEnabled`.

### Pitfall 5: Missing DbSet Registration in QuestBoardContext
**What goes wrong:** EF migration tooling or runtime throws "EntityType 'AdminSettingEntity' was not found in the model" because the `DbSet<AdminSettingEntity>` was not added to `QuestBoardContext`.
**Why it happens:** The entity is created in `EuphoriaInn.Repository/Entities/` but the `DbSet` property was not added to `QuestBoardContext.cs`.
**How to avoid:** Add `public DbSet<AdminSettingEntity> AdminSettings { get; set; }` to `QuestBoardContext`.
**Warning signs:** `dotnet ef migrations add` outputs "No entity types were found" or migration is empty; runtime `NullReferenceException` on `dbContext.AdminSettings`.

### Pitfall 6: EF Migration Run from Wrong Directory
**What goes wrong:** `dotnet ef migrations add AddAdminSettings` run from the root or `EuphoriaInn.Service` directory without `--project ../EuphoriaInn.Repository` fails.
**Why it happens:** EF tools look for `DbContext` in the startup project.
**How to avoid:** Per CLAUDE.md: `cd EuphoriaInn.Service && dotnet ef migrations add AddAdminSettings --project ../EuphoriaInn.Repository`
**Warning signs:** "No DbContext was found in assembly 'EuphoriaInn.Service'."

### Pitfall 7: `IAdminSettingService` Not Registered in DI
**What goes wrong:** `AdminController` constructor fails with `InvalidOperationException: Unable to resolve service for type 'IAdminSettingService'` at startup or first request.
**Why it happens:** New services must be registered in both `AddDomainServices()` (the service) and `AddRepositoryServices()` (the repository). Missing one registration causes a silent failure at DI resolution.
**How to avoid:**
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` → `AddDomainServices()`: `services.AddScoped<IAdminSettingService, AdminSettingService>();`
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` → `AddRepositoryServices()`: `services.AddScoped<IAdminSettingRepository, AdminSettingRepository>();`
**Warning signs:** `InvalidOperationException` at startup; integration test factory fails to build.

---

## Code Examples

### AdminSettingEntity (full)
```csharp
// Source: VERIFIED — DungeonMasterProfileEntity.cs and QuestBoardContext.cs patterns
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;

[Table("AdminSettings")]
public class AdminSettingEntity  // String PK — does NOT implement IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; }
}
```

### IAdminSettingRepository (full)
```csharp
// Source: VERIFIED — IDungeonMasterProfileRepository.cs pattern
namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingRepository
{
    Task<AdminSettingEntity?> GetByKeyAsync(string key, CancellationToken token = default);
    Task UpsertAsync(string key, string? value, CancellationToken token = default);
    Task<IList<AdminSettingEntity>> GetAllAsync(CancellationToken token = default);
}
```

Note: `IAdminSettingRepository` lives in `EuphoriaInn.Domain/Interfaces/` — the Domain layer defines repository interfaces. The concrete `AdminSettingRepository` lives in `EuphoriaInn.Repository/`. This is the same cross-layer pattern used for all other repositories. The Domain layer references the `AdminSettingEntity` type for the repository interface signature — which is acceptable since Domain already depends on Repository entities (same as `EntityProfile.cs`).

**Alternative to avoid the cross-layer entity reference:** The repository interface could return a plain `(string Key, string? Value)` tuple or a `Dictionary<string, string?>`. However, the existing pattern (e.g., `IDungeonMasterProfileRepository`) directly returns entity types. Stay consistent.

### SettingsViewModel
```csharp
// Source: VERIFIED — EditUserViewModel.cs pattern
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class SettingsViewModel
{
    [Url]
    [StringLength(2000)]
    [Display(Name = "Omphalos URL")]
    public string? OmphalosUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Shared Secret")]
    public string? OmphalosSharedSecret { get; set; }

    [Display(Name = "Enable Omphalos integration")]
    public bool IsEnabled { get; set; }
}
```

### _Layout.cshtml Admin Dropdown Addition
```html
<!-- Source: VERIFIED — _Layout.cshtml lines 37-48; UI-SPEC §9 -->
<li><hr class="dropdown-divider"></li>
<li>
    <a class="dropdown-item" asp-controller="Admin" asp-action="Settings">
        <i class="fas fa-plug me-2"></i>Integration Settings
    </a>
</li>
```
Insert after the existing "Quest Management" `<li>` block (currently line 43-47 in `_Layout.cshtml`).

---

## Runtime State Inventory

> Not applicable — this is a greenfield phase adding new tables and UI. No existing data, OS state, or build artifacts reference the new `AdminSettings` table or `IAdminSettingService`. No rename/refactor operations.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `appsettings.json` for runtime config | DB-persisted key-value settings | Phase 10 (new) | Settings editable at runtime; no restart required |
| Typed single-row entity (SETT-06) | Key-value store (D-01) | Phase 10 context discussion | Extensible for future settings without new migrations |

**Not applicable in this codebase:** No prior settings storage exists for Omphalos integration. This is net-new.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `IntegrationSettings` is implemented as a C# `record` (not a class) | Architecture Patterns §3 | If the team prefers a class, the `init` accessors become `set`; functional difference is minimal |
| A2 | `IAdminSettingRepository` interface is in `EuphoriaInn.Domain/Interfaces/` and returns `AdminSettingEntity` directly (following existing pattern) | Code Examples | If team prefers a DTO-only interface boundary, repository returns `Dictionary<string, string?>` instead; service code changes slightly |
| A3 | `SaveSettingsAsync` signature accepts `(string? url, string? secret, bool isEnabled)` parameters individually, not a single ViewModel parameter | Architecture Patterns §2 | If team prefers to pass the record, interface signature changes; no functional risk |

**If this table is empty:** Not applicable — three low-risk assumptions are documented above.

---

## Open Questions

1. **Table name: `AdminSettings` vs `IntegrationSettings`**
   - What we know: D-03 defers to planner; entity is `AdminSettingEntity`
   - What's unclear: Neither name is locked
   - Recommendation: Use `AdminSettings` — matches the entity name prefix (`AdminSetting`-`Entity`) and the generic intent of the service (`IAdminSetting`-`Service`). `IntegrationSettings` is the name of the _return type_ record, not the table.

2. **Where does `IAdminSettingRepository` declare its return type for `GetByKeyAsync`?**
   - What we know: Domain defines repository interfaces; repository interfaces currently return domain model types (e.g., `DungeonMasterProfile`) not entity types
   - What's unclear: `AdminSettingEntity` has no corresponding domain model — there is no `AdminSetting` domain model (the only "model" is `IntegrationSettings`, a read DTO)
   - Recommendation: `IAdminSettingRepository` returns `string?` values directly (not `AdminSettingEntity`), keeping entity types out of the Domain interface. The repository implementation works with `AdminSettingEntity` internally. See revised interface below.

**Revised IAdminSettingRepository (cleaner approach):**
```csharp
// This avoids exposing AdminSettingEntity to the Domain layer
namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken token = default);
    Task UpsertAsync(string key, string? value, CancellationToken token = default);
}
```
The service calls `GetValueAsync("OmphalosUrl")`, `GetValueAsync("OmphalosSharedSecret")`, `GetValueAsync("IsEnabled")` individually. This is three DB queries per `GetSettingsAsync()` call instead of one `GetAllAsync()`. For a settings page loaded by admins only, this is acceptable and keeps the layering cleaner. Planner may choose either approach.

---

## Environment Availability

> Step 2.6: SKIPPED — Phase 10 is code/config/migration only. No external services, CLI tools beyond standard `dotnet`/`dotnet-ef`, or runtime dependencies outside the existing project stack.

Standard project tools verified:
- `dotnet` SDK — available (project builds, confirmed by existing migration history)
- `dotnet-ef` — required for migration; CLAUDE.md confirms it as a project tool

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "AdminSetting" --no-build` |
| Full suite command | `dotnet test EuphoriaInn.IntegrationTests` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SETT-04 | Blank secret on save preserves existing secret | Integration (service) | `dotnet test --filter "AdminSetting"` | ❌ Wave 0 |
| SETT-05 | `IsEnabled = false` stored correctly; `GetSettingsAsync()` returns false | Integration (service) | `dotnet test --filter "AdminSetting"` | ❌ Wave 0 |
| SETT-07 | Non-admin cannot access `/Admin/Settings` | Integration (HTTP) | `dotnet test --filter "AdminController"` | `AdminControllerIntegrationTests.cs` — needs new test |
| D-05 | Empty DB returns default `IntegrationSettings` record | Integration (service) | `dotnet test --filter "AdminSetting"` | ❌ Wave 0 |
| D-08 | Upsert-twice overwrites first save | Integration (service) | `dotnet test --filter "AdminSetting"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test EuphoriaInn.IntegrationTests --filter "AdminSetting" --no-build`
- **Per wave merge:** `dotnet test EuphoriaInn.IntegrationTests --no-build`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` — covers SETT-04, SETT-05, D-05, D-08
- [ ] New test method in `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — covers SETT-07 (`/Admin/Settings` returns 302/403 for non-admin)

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (indirect) | Admin-only page; ASP.NET Core Identity handles auth |
| V3 Session Management | no | No session changes in this phase |
| V4 Access Control | yes | `[Authorize(Policy = "AdminOnly")]` on `AdminController` class; `AdminOnly` policy enforced by `AdminHandler.cs` |
| V5 Input Validation | yes | DataAnnotations on `SettingsViewModel` (`[Url]`, `[StringLength]`); `ModelState.IsValid` check in POST action |
| V6 Cryptography | no | Phase 10 stores the secret in DB (plain text as a configuration value); encryption at rest is out of scope for this phase |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unauthorized settings access | Elevation of Privilege | `[Authorize(Policy = "AdminOnly")]` class-level attribute (already present on `AdminController`) |
| CSRF on settings POST | Tampering | `[ValidateAntiForgeryToken]` on POST action |
| URL injection via Omphalos URL field | Tampering | `[Url]` DataAnnotation validates URL format; browser `type="url"` provides client-side check |
| Secret leakage via page source | Information Disclosure | `type="password"` on secret field; field loads empty on GET; `autocomplete="off"` |
| Overly long input in URL/secret fields | Denial of Service (minor) | `[StringLength]` attributes on ViewModel properties |

**Note on secret storage:** The `OmphalosSharedSecret` is stored as plain text in the `AdminSettings` table. This is intentional for the self-hosted group app context (consistent with REQUIREMENTS.md "OAuth/OIDC overkill" rationale). Column-level encryption or DPAPI protection is out of scope for Phase 10.

---

## Sources

### Primary (HIGH confidence)
- `EuphoriaInn.Repository/Entities/AdminSettingEntity.cs` (does not exist yet — pattern VERIFIED from `DungeonMasterProfileEntity.cs`)
- `EuphoriaInn.Repository/BaseRepository.cs` — confirmed generic constraints (`IModel`, `IEntity`) that exclude string-PK entities
- `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs` — confirmed standalone upsert pattern
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — confirmed `[Authorize(Policy = "AdminOnly")]` class-level, primary constructor injection pattern
- `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` — confirmed modern-card form pattern, checkbox pattern, button row pattern
- `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs` — confirmed SQLite in-memory test helper
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` — confirmed integration test factory pattern
- `EuphoriaInn.Repository/Migrations/20260617191315_AddDMProfileSystem.cs` — confirmed migration structure for new table
- `EuphoriaInn.Domain/Interfaces/IBaseRepository.cs` — confirmed `int id` methods that make BaseRepository unsuitable for string-PK entities
- `.planning/phases/10-admin-settings/10-UI-SPEC.md` — UI contracts for all 9 components, copywriting, interaction model

### Secondary (MEDIUM confidence)
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` lines 37–48 — admin dropdown structure for Settings link insertion point

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages already in project; no new dependencies
- Architecture: HIGH — all patterns verified from existing codebase code; standalone entity/service/repository necessity confirmed from base class constraints
- Pitfalls: HIGH — pitfalls 1–4 are compile-time or runtime failures verified by reading the code; pitfalls 5–7 follow from DI/EF registration requirements
- Test coverage: HIGH — TestDatabase helper and integration test patterns verified in detail

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stable stack — no fast-moving dependencies)
