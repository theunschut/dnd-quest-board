# Phase 10: Admin Settings - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 13 new/modified files
**Analogs found:** 11 / 13

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Domain/Models/IntegrationSettings.cs` | model | transform | `EuphoriaInn.Domain/Models/DungeonMasterProfile.cs` | role-match |
| `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` | interface | request-response | `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs` (inferred) | role-match |
| `EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs` | interface | CRUD | any existing `IXxxRepository.cs` | role-match |
| `EuphoriaInn.Domain/Services/AdminSettingService.cs` | service | request-response | `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs` | role-match |
| `EuphoriaInn.Repository/Entities/AdminSettingEntity.cs` | model (entity) | CRUD | `EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs` | exact |
| `EuphoriaInn.Repository/AdminSettingRepository.cs` | repository | CRUD | `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs` | role-match |
| `EuphoriaInn.Repository/Migrations/[ts]_AddAdminSettings.cs` | migration | batch | `EuphoriaInn.Repository/Migrations/20260617191315_AddDMProfileSystem.cs` | exact |
| `EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs` | view model | request-response | `EuphoriaInn.Service/ViewModels/AdminViewModels/EditUserViewModel.cs` | exact |
| `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` (modified) | controller | request-response | self (same file) | exact |
| `EuphoriaInn.Service/Views/Admin/Settings.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` | exact |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (modified) | view | request-response | self (same file) | exact |
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (modified) | config | CRUD | self (same file) | exact |
| `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` (modified) | config | CRUD | self (same file) | exact |
| `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` | test | request-response | `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` | partial — no service-only test exists; see note |

---

## Pattern Assignments

### `EuphoriaInn.Domain/Models/IntegrationSettings.cs` (model, transform)

**Analog:** `EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs` (entity shape reference only — record vs class differs)

**Key decision:** Implemented as a C# `record` (not a class). It is a read-only DTO, not an `IModel`-implementing domain model. It must NOT implement `IModel` (which requires `int Id`).

**Imports pattern:**
```csharp
namespace EuphoriaInn.Domain.Models;
```

**Core pattern:**
```csharp
public record IntegrationSettings
{
    public string? OmphalosUrl { get; init; }
    public string? OmphalosSharedSecret { get; init; }
    public bool IsEnabled { get; init; }

    // Phase 11 checks this before showing any Omphalos UI
    public bool IsConfigured => IsEnabled && !string.IsNullOrWhiteSpace(OmphalosUrl);
}
```

No AutoMapper mapping for this type — it is assembled by the service from a dictionary, not mapped from an entity.

---

### `EuphoriaInn.Repository/Entities/AdminSettingEntity.cs` (model/entity, CRUD)

**Analog:** `EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs` (lines 1–17)

**Imports pattern** (lines 1–3 of DungeonMasterProfileEntity):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;
```

**Core pattern** — `[DatabaseGenerated(DatabaseGeneratedOption.None)]` on the PK (same as DungeonMasterProfileEntity line 10):
```csharp
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

Critical distinction from analog: `DungeonMasterProfileEntity` uses `int Id : IEntity`; `AdminSettingEntity` uses `string Key` and must NOT implement `IEntity`.

---

### `EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs` (interface, CRUD)

**Analog:** Any existing `IXxxRepository` in `EuphoriaInn.Domain/Interfaces/` — e.g., `IQuestRepository`, `IDungeonMasterProfileRepository`

**Namespace pattern:**
```csharp
namespace EuphoriaInn.Domain.Interfaces;
```

**Core pattern** — cleaner approach (avoids entity type leaking into Domain interface; returns plain values):
```csharp
public interface IAdminSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken token = default);
    Task UpsertAsync(string key, string? value, CancellationToken token = default);
}
```

Note: The RESEARCH.md documents two options (returning `AdminSettingEntity` vs plain `string?`). The cleaner approach (plain `string?`) keeps the entity type out of the Domain layer. Pick one and be consistent throughout service and repository implementation.

---

### `EuphoriaInn.Repository/AdminSettingRepository.cs` (repository, CRUD)

**Analog:** `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs` (lines 1–71)

**Imports pattern** (lines 1–6 of DungeonMasterProfileRepository):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;
```

**Core pattern** — standalone class, primary constructor, `QuestBoardContext` injected directly (does NOT extend `BaseRepository`). The upsert pattern from `UpsertProfileImageAsync` (lines 45–69) is the structural reference:
```csharp
internal class AdminSettingRepository(QuestBoardContext dbContext) : IAdminSettingRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken token = default)
    {
        var entity = await dbContext.AdminSettings.FindAsync([key], cancellationToken: token);
        return entity?.Value;
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
}
```

Key difference from `DungeonMasterProfileRepository`: no `IMapper mapper` constructor parameter (no AutoMapper involved), no `BaseRepository` base class.

---

### `EuphoriaInn.Domain/Services/AdminSettingService.cs` (service, request-response)

**Analog:** `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs` (structural pattern; internal class with primary constructor injection)

**Imports pattern:**
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;
```

**Core pattern** — standalone `internal` class, does NOT extend `BaseService`:
```csharp
internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
{
    public async Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default)
    {
        return new IntegrationSettings
        {
            OmphalosUrl = await repository.GetValueAsync("OmphalosUrl", token),
            OmphalosSharedSecret = await repository.GetValueAsync("OmphalosSharedSecret", token),
            IsEnabled = bool.TryParse(
                await repository.GetValueAsync("IsEnabled", token), out var enabled) && enabled
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

`IsEnabled` is stored as the string `"True"` / `"False"` — never as a `bool` — because `Value` is `nvarchar(max)`.

---

### `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` (interface, request-response)

**Analog:** any `IXxxService.cs` in `EuphoriaInn.Domain/Interfaces/`

**Core pattern:**
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingService
{
    Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default);
    Task SaveSettingsAsync(string? url, string? secret, bool isEnabled, CancellationToken token = default);
}
```

Does NOT extend `IBaseService<T>` — the generic CRUD contract does not apply.

---

### `EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs` (view model, request-response)

**Analog:** `EuphoriaInn.Service/ViewModels/AdminViewModels/EditUserViewModel.cs` (lines 1–22)

**Imports + namespace pattern** (lines 1–3 of EditUserViewModel):
```csharp
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;
```

**Core pattern** — DataAnnotations on each property, `Display` name, optional properties with `string?`:
```csharp
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

`OmphalosSharedSecret` has no `[Required]` — leaving it blank is valid (D-08 blank-preserves behavior).

---

### `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — Settings actions (controller, request-response)

**Analog:** `AdminController.cs` itself — `ResetPassword` GET/POST (lines 153–197) is the closest PRG pattern with `TempData["SuccessMessage"]`

**Imports addition:** Add `IAdminSettingService` to constructor parameter list and namespace import:
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
public class AdminController(
    IUserService userService,
    IQuestService questService,
    IAdminSettingService adminSettingService) : Controller
```

**GET action pattern** (modelled on ResetPassword GET, lines 153–167):
```csharp
[HttpGet]
public async Task<IActionResult> Settings()
{
    var settings = await adminSettingService.GetSettingsAsync();
    var model = new SettingsViewModel
    {
        OmphalosUrl = settings.OmphalosUrl,
        // DO NOT populate OmphalosSharedSecret — password fields must load empty
        IsEnabled = settings.IsEnabled
    };
    return View(model);
}
```

**POST action pattern** (modelled on ResetPassword POST, lines 172–197, with TempData + PRG):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Settings(SettingsViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    await adminSettingService.SaveSettingsAsync(
        model.OmphalosUrl,
        model.OmphalosSharedSecret,   // null/empty = preserve existing (D-08)
        model.IsEnabled);

    TempData["SuccessMessage"] = "Integration settings saved successfully.";
    return RedirectToAction(nameof(Settings));
}
```

Auth: `[Authorize(Policy = "AdminOnly")]` is already at class level — the new actions inherit it automatically.

---

### `EuphoriaInn.Service/Views/Admin/Settings.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` (lines 1–79) — exact structural match for `modern-card` + form + validation summary + button row

**Model directive + ViewData:**
```cshtml
@model SettingsViewModel
@{
    ViewData["Title"] = "Integration Settings";
}
```

**Card structure** (lines 14–21 of EditUser.cshtml):
```cshtml
<div class="card modern-card">
    <div class="card-header modern-card-header">
        <h2 class="mb-0">
            <i class="fas fa-plug text-warning me-2"></i>
            Integration Settings
        </h2>
    </div>
    <div class="card-body modern-card-body">
```

**Validation summary** (line 23 of EditUser.cshtml):
```cshtml
<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
```

**Password field pattern** — secret field must use `type="password"` with hint text (D-09):
```cshtml
<div class="mb-3">
    <label asp-for="OmphalosSharedSecret" class="form-label"></label>
    <input asp-for="OmphalosSharedSecret" class="form-control" type="password" autocomplete="off" />
    <div class="form-text text-muted">Leave blank to keep the existing value.</div>
    <span asp-validation-for="OmphalosSharedSecret" class="text-danger"></span>
</div>
```

**Checkbox pattern** (lines 41–49 of EditUser.cshtml):
```cshtml
<div class="mb-3">
    <div class="form-check">
        <input asp-for="IsEnabled" class="form-check-input" type="checkbox" />
        <label asp-for="IsEnabled" class="form-check-label">
            <i class="fas fa-toggle-on text-success me-2"></i>
            Enable Omphalos integration
        </label>
    </div>
</div>
```

**TempData success alert** — pattern from ResetPassword (no Razor view exists for that, but TempData is set in controller); wire it up in view before the form:
```cshtml
@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="fas fa-check-circle me-2"></i>@TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

**Button row** (lines 64–73 of EditUser.cshtml):
```cshtml
<hr>
<div class="d-flex justify-content-between">
    <a asp-action="Users" class="btn btn-secondary">
        <i class="fas fa-arrow-left me-2"></i>
        Back
    </a>
    <button type="submit" class="btn btn-primary">
        <i class="fas fa-save me-2"></i>
        Save Settings
    </button>
</div>
```

---

### `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — admin dropdown addition (view, request-response)

**Analog:** self — lines 37–48 (existing admin dropdown block)

**Insertion point** — after the Quest Management `<li>` block (lines 43–47), before the closing `</ul>` of the dropdown (line 48):
```cshtml
<li><hr class="dropdown-divider"></li>
<li>
    <a class="dropdown-item" asp-controller="Admin" asp-action="Settings">
        <i class="fas fa-plug me-2"></i>Integration Settings
    </a>
</li>
```

---

### `EuphoriaInn.Repository/Migrations/[ts]_AddAdminSettings.cs` (migration, batch)

**Analog:** `EuphoriaInn.Repository/Migrations/20260617191315_AddDMProfileSystem.cs` (lines 1–60)

**Class header pattern** (lines 1–9):
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuphoriaInn.Repository.Migrations
{
    public partial class AddAdminSettings : Migration
    {
```

**Up() pattern** — `CreateTable` with string PK (no FK, no identity):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "AdminSettings",
        columns: table => new
        {
            Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_AdminSettings", x => x.Key);
        });
}
```

**Down() pattern** (lines 51–58):
```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "AdminSettings");
}
```

**Migration generation command** (from CLAUDE.md):
```
cd EuphoriaInn.Service
dotnet ef migrations add AddAdminSettings --project ../EuphoriaInn.Repository
```

---

### `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — DbSet addition (modified)

**Analog:** self — existing `DbSet` properties (lines 9–29)

**Addition pattern** — insert after `DungeonMasterProfileImages` DbSet (line 29):
```csharp
public DbSet<AdminSettingEntity> AdminSettings { get; set; }
```

No `OnModelCreating` entry needed: `[Table("AdminSettings")]` and `[Key]` + `[DatabaseGenerated(DatabaseGeneratedOption.None)]` on the entity are sufficient. EF Core infers `nvarchar(200)` from `[StringLength(200)]` and `nvarchar(max)` from `string?` with no length annotation.

---

### `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — DI registration (modified)

**Analog:** self — lines 14–23 (existing `services.AddScoped<IXxx, Xxx>()` block)

**Addition pattern:**
```csharp
services.AddScoped<IAdminSettingService, AdminSettingService>();
```

Insert alongside the existing service registrations. No `IConfiguration` usage needed.

---

### `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — DI registration (modified)

**Analog:** self — lines 18–29 (existing repository scoped registrations)

**Addition pattern:**
```csharp
services.AddScoped<IAdminSettingRepository, AdminSettingRepository>();
```

---

### `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` (test, request-response)

**Analog:** `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` (partial match — HTTP-layer tests; no service-layer-only test file exists in the codebase)

**Key distinction:** This is a service/repository integration test, not an HTTP test. It wires `TestDatabase` → `QuestBoardContext` → `AdminSettingRepository` → `AdminSettingService` directly, without `WebApplicationFactory`. The constructor/Dispose pattern comes from `TestDatabase.cs` (lines 15–61).

**Imports pattern** — mirrors `AdminControllerIntegrationTests.cs` global usings plus direct repository references:
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.IntegrationTests.Helpers;
using EuphoriaInn.Repository;

namespace EuphoriaInn.IntegrationTests.Services;
```

**Constructor + Dispose pattern** (from TestDatabase lines 35–61):
```csharp
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

    public void Dispose() => _db.Dispose();
}
```

**Test method pattern** (from AdminControllerIntegrationTests.cs lines 19–26 — arrange/act/assert):
```csharp
[Fact]
public async Task GetSettingsAsync_WhenDbEmpty_ReturnsDefault()
{
    var result = await _sut.GetSettingsAsync();

    result.Should().NotBeNull();
    result.IsEnabled.Should().BeFalse();
    result.OmphalosUrl.Should().BeNull();
    result.OmphalosSharedSecret.Should().BeNull();
}
```

**D-10 test cases to implement:**
1. `GetSettingsAsync_WhenDbEmpty_ReturnsDefault` — IsEnabled=false, nulls
2. `GetSettingsAsync_AfterSave_ReturnsStoredValues` — save then get
3. `SaveSettingsAsync_WithBlankSecret_PreservesExistingSecret` — SETT-04
4. `SaveSettingsAsync_CalledTwice_SecondOverwritesFirst` — upsert behavior

**SETT-07 test** (non-admin access) goes into the existing `AdminControllerIntegrationTests.cs` as a new `[Theory]` `[InlineData("/Admin/Settings")]` entry in the existing `AdminActions_WhenNotAuthenticated_ShouldRedirectToLogin` theory (lines 52–64).

---

## Shared Patterns

### Authorization — AdminOnly
**Source:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` line 8
**Apply to:** `AdminController` Settings actions (inherited from class-level attribute — no per-action attribute needed)
```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminController(...) : Controller
```

### Post-Redirect-Get with TempData
**Source:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` lines 172–197 (ResetPassword)
**Apply to:** `AdminController.Settings` POST action
```csharp
TempData["SuccessMessage"] = "...";
return RedirectToAction(nameof(Settings));
```

### CSRF Protection on POST
**Source:** `AdminController.cs` — every POST action (lines 39, 57, 74, 91, 128, 172, 199, 228)
**Apply to:** `AdminController.Settings` POST action
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
```

### ModelState.IsValid guard
**Source:** `AdminController.cs` lines 130, 175
**Apply to:** `Settings` POST action
```csharp
if (!ModelState.IsValid)
    return View(model);
```

### Primary constructor injection
**Source:** `AdminController.cs` line 9; `DungeonMasterProfileRepository.cs` line 9
**Apply to:** `AdminSettingRepository`, `AdminSettingService`, `AdminController` (expanded constructor)
```csharp
internal class AdminSettingRepository(QuestBoardContext dbContext) : IAdminSettingRepository
internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
```

### modern-card view structure
**Source:** `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` lines 14–77
**Apply to:** `Settings.cshtml`
```cshtml
<div class="card modern-card">
    <div class="card-header modern-card-header">...</div>
    <div class="card-body modern-card-body">...</div>
</div>
```

### Scoped DI registration
**Source:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` lines 15–22; `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` lines 18–28
**Apply to:** Both `ServiceExtensions.cs` files for the new service and repository
```csharp
services.AddScoped<IAdminSettingService, AdminSettingService>();
services.AddScoped<IAdminSettingRepository, AdminSettingRepository>();
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` | test | request-response | No direct-wiring service-layer integration test exists — all existing tests go through `WebApplicationFactory`. Patterns assembled from `TestDatabase.cs` constructor + `AdminControllerIntegrationTests.cs` assertion style. |

---

## Critical Anti-Patterns (do not copy)

| Anti-Pattern | Source to Avoid | Correct Approach |
|---|---|---|
| `AdminSettingEntity : IEntity` | All other entities extend `IEntity` by habit | `AdminSettingEntity` has string PK — must NOT implement `IEntity` |
| `AdminSettingRepository : BaseRepository<...>` | All other repos extend `BaseRepository` | `BaseRepository` constrains `TEntity : IEntity`; use standalone class |
| `AdminSettingService : BaseService<...>` | All other services extend `BaseService` | `IntegrationSettings` is not an `IModel`; use standalone class |
| Populating `OmphalosSharedSecret` on GET | Any pre-fill pattern | Secret field must load empty; populating it would wipe the value on unedited saves |
| `UpsertAsync("OmphalosSharedSecret", secret)` unconditionally | Straightforward upsert | Skip the call when `string.IsNullOrWhiteSpace(secret)` (D-08) |
| Storing `IsEnabled` as `bool` in `Value` column | EF type mapping | Store as `"True"`/`"False"` string; parse with `bool.TryParse` on read |

---

## Metadata

**Analog search scope:** `EuphoriaInn.Domain/`, `EuphoriaInn.Repository/`, `EuphoriaInn.Service/Controllers/Admin/`, `EuphoriaInn.Service/Views/Admin/`, `EuphoriaInn.Service/Views/Shared/`, `EuphoriaInn.IntegrationTests/`
**Files scanned:** 14
**Pattern extraction date:** 2026-06-18
