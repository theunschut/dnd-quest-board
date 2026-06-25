# Phase 11: Navigation + Token Generation - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 9 new/modified files
**Analogs found:** 9 / 9

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs` | interface | request-response | `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` | exact |
| `EuphoriaInn.Domain/Services/IntegrationTokenService.cs` | service | request-response | `EuphoriaInn.Domain/Services/AdminSettingService.cs` | role-match (stateless vs repository-backed) |
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | config | CRUD | `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (modify) | exact |
| `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` | controller | request-response | same file (modify) | exact |
| `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` | component | request-response | no exact analog (first ViewComponent) | research-pattern |
| `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` lines 65–84 | role-match |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | view | request-response | same file (modify) | exact |
| `EuphoriaInn.Service/Views/Quest/Details.cshtml` | view | request-response | same file (modify) | exact |
| `EuphoriaInn.Service/Views/Quest/Manage.cshtml` | view | request-response | same file (modify) | exact |
| `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` | test | request-response | `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` | role-match |
| `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` | test | request-response | `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` | exact |

---

## Pattern Assignments

### `EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs` (interface, request-response)

**Analog:** `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs`

**Imports + namespace pattern** (lines 1–9):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingService
{
    Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default);
    Task SaveSettingsAsync(string? url, string? secret, bool isEnabled, CancellationToken token = default);
}
```

**Copy this pattern — adapted for IIntegrationTokenService:**
- `public interface`, namespace `EuphoriaInn.Domain.Interfaces`
- No `using` directives needed (BCL types only: `string`, `int`)
- Single synchronous method (no async — pure computation, no I/O):
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IIntegrationTokenService
{
    string GenerateSignedUrl(string omphalosBaseUrl, int questId, string questTitle, string username, string sharedSecret);
}
```

---

### `EuphoriaInn.Domain/Services/IntegrationTokenService.cs` (service, request-response)

**Analog:** `EuphoriaInn.Domain/Services/AdminSettingService.cs`

**File structure pattern** (lines 1–36):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;

internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
{
    public async Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default)
    {
        ...
    }
    ...
}
```

**Key differences for IntegrationTokenService:**
- `internal class IntegrationTokenService : IIntegrationTokenService` — NO constructor parameters (stateless pure computation)
- Uses BCL `System.Security.Cryptography.HMACSHA256` only
- `using` block: `using System.Security.Cryptography;` and `using System.Text;`
- Namespace: `EuphoriaInn.Domain.Services`

**Core implementation pattern (locked by D-03, D-16, D-17, Risk 6, Risk 7, Risk 8 in RESEARCH.md):**
```csharp
using System.Security.Cryptography;
using System.Text;
using EuphoriaInn.Domain.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal class IntegrationTokenService : IIntegrationTokenService
{
    public string GenerateSignedUrl(
        string omphalosBaseUrl, int questId, string questTitle,
        string username, string sharedSecret)
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds();
        var encodedTitle = Uri.EscapeDataString(questTitle);  // percent-encoding, not UrlEncode (Risk 7)
        var lowerUser = username.ToLower();

        // Canonical message: alphabetical key order (D-03)
        var message = $"expiry={expiry}&questId={questId}&questTitle={encodedTitle}&username={lowerUser}";

        var keyBytes = Encoding.UTF8.GetBytes(sharedSecret);
        var msgBytes = Encoding.UTF8.GetBytes(message);
        var hashBytes = HMACSHA256.HashData(keyBytes, msgBytes);
        var sig = Convert.ToHexString(hashBytes).ToLower();  // lowercase hex, .NET 5+ BCL (Risk 8)

        // TrimEnd('/') prevents double-slash (Risk 6)
        return $"{omphalosBaseUrl.TrimEnd('/')}/api/sso/open-quest" +
               $"?expiry={expiry}&questId={questId}&questTitle={encodedTitle}&username={lowerUser}&sig={sig}";
    }
}
```

---

### `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (config, CRUD — modify)

**Analog:** Same file, lines 1–27.

**Current tail of `AddDomainServices()`** (lines 20–26):
```csharp
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<IDungeonMasterProfileService, DungeonMasterProfileService>();
        services.AddScoped<IAdminSettingService, AdminSettingService>();

        return services;
    }
}
```

**Change:** Append one line after line 23 (after `IAdminSettingService`):
```csharp
        services.AddTransient<IIntegrationTokenService, IntegrationTokenService>();
```

`AddTransient` is correct — stateless pure computation (no repository, no request-scoped state).

**Required new `using` at top of file:**
```csharp
// Already present:
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Services;
// No new using needed — IIntegrationTokenService and IntegrationTokenService
// are in already-imported namespaces
```

---

### `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` (controller, request-response — modify)

**Analog:** Same file.

**1. Constructor pattern** (lines 13–19 — current):
```csharp
public class QuestController(
    IUserService userService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService
    ) : Controller
```

**Change:** Add two parameters after `ICharacterService characterService`:
```csharp
public class QuestController(
    IUserService userService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService,
    IAdminSettingService adminSettingService,
    IIntegrationTokenService integrationTokenService
    ) : Controller
```

**Required new `using` directives** — add to existing import block (lines 1–9):
```csharp
// Already present: using EuphoriaInn.Domain.Interfaces;
// No new usings needed — IAdminSettingService and IIntegrationTokenService
// are in EuphoriaInn.Domain.Interfaces, already imported
```

**2. ViewBag.ShowOmphalosButton in Details GET** (lines 241–244 — existing context):
```csharp
        // Check if current user can manage this quest (DM or admin)
        var isQuestDm = currentUser?.Name == quest.DungeonMaster?.Name;
        var isAdmin = currentUser != null && await userService.IsInRoleAsync(User, "Admin");
        ViewBag.CanManage = isQuestDm || isAdmin;
        // INSERT AFTER THIS LINE:
```

**Insert after line 244 (note: use OrdinalIgnoreCase per Risk 4 + Risk 5 guard):**
```csharp
        var settings = await adminSettingService.GetSettingsAsync(token);
        ViewBag.ShowOmphalosButton = settings.IsConfigured
            && currentUser != null
            && (currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase) || isAdmin);
```

**3. ViewBag.ShowOmphalosButton in Manage GET** (lines 638–641 — existing context):
```csharp
        var isQuestDm = currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase);
        var isAdmin = await userService.IsInRoleAsync(User, "Admin");
        ViewBag.IsAuthorized = isQuestDm || isAdmin;
        ViewBag.IsAdmin = isAdmin;
        // INSERT AFTER THIS LINE:
```

**Insert after line 641 (reuses locals already computed above):**
```csharp
        var omphalosSettings = await adminSettingService.GetSettingsAsync();
        ViewBag.ShowOmphalosButton = omphalosSettings.IsConfigured && (isQuestDm || isAdmin);
```

**4. LaunchOmphalos action — insert before closing `}` of class (after line 795):**

Existing authorize pattern from same file (lines 620–621):
```csharp
    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Manage(int id)
```

New action follows exact same attribute pattern:
```csharp
    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> LaunchOmphalos(int id, CancellationToken token = default)
    {
        var settings = await adminSettingService.GetSettingsAsync(token);
        if (!settings.IsConfigured)
            return NotFound();

        var quest = await questService.GetQuestWithDetailsAsync(id, token);
        if (quest == null)
            return NotFound();

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
            return Challenge();

        var signedUrl = integrationTokenService.GenerateSignedUrl(
            settings.OmphalosUrl!,
            quest.Id,
            quest.Title,
            currentUser.Name.ToLower(),
            settings.OmphalosSharedSecret!);

        return Redirect(signedUrl);
    }
```

Null guard pattern from existing Create action (lines 26–30):
```csharp
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }
```

---

### `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` (component, request-response — new)

**No prior ViewComponent in codebase.** ASP.NET Core MVC auto-discovers by convention — class name ends `ViewComponent`, placed anywhere in assembly. No registration in `Program.cs` needed.

**Class shape (from RESEARCH.md Section 3.4):**
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Components;

public class OmphalosNavItemViewComponent(IAdminSettingService adminSettingService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await adminSettingService.GetSettingsAsync();
        if (!settings.IsConfigured)
            return Content(string.Empty);   // renders nothing — no view file needed for empty path
        return View(settings);              // passes IntegrationSettings to Default.cshtml
    }
}
```

**D-07:** Constructor injects `IAdminSettingService` directly — one DB hit per layout render (Scoped).
**D-09:** No role checks in ViewComponent — link is placed inside DM dropdown in `_Layout.cshtml` which is already role-gated.

---

### `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` (view — new)

**Analog:** `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` lines 65–80 (DM dropdown `<li>` items).

**Existing dropdown item pattern** (lines 66–70 and 71–75):
```razor
<li>
    <a class="dropdown-item" asp-controller="Quest" asp-action="Create">
        <i class="fas fa-scroll me-2"></i>Create Quest
    </a>
</li>
```

**New Default.cshtml — plain navigation link, opens new tab (D-08, D-06 from CONTEXT.md):**
```razor
@model EuphoriaInn.Domain.Models.IntegrationSettings
<li>
    <a class="dropdown-item" href="@Model.OmphalosUrl" target="_blank" rel="noopener noreferrer">
        <i class="fas fa-external-link-alt me-2"></i>Open Omphalos
    </a>
</li>
```

No SSO token for navbar link (D-08). `target="_blank" rel="noopener noreferrer"` per CONTEXT.md Specific Ideas.

---

### `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (view — modify)

**Analog:** Same file, lines 65–84.

**Current DM dropdown** (lines 65–84):
```razor
<ul class="dropdown-menu">
    <li>
        <a class="dropdown-item" asp-controller="Quest" asp-action="Create">
            <i class="fas fa-scroll me-2"></i>Create Quest
        </a>
    </li>
    <li>
        <a class="dropdown-item" asp-controller="ShopManagement" asp-action="Index">
            <i class="fas fa-coins me-2"></i>Manage Shop
        </a>
    </li>
    <li>
        <a class="dropdown-item" asp-controller="DungeonMaster" asp-action="EditProfile">
            <i class="fas fa-user-edit me-2"></i>Edit My Profile
        </a>
    </li>
                                                            ← line 81 gap here
</ul>
```

**Insert after line 80 (closing `</li>` of "Edit My Profile"), before line 82 (`</ul>`):**
```razor
<li><hr class="dropdown-divider"></li>
@await Component.InvokeAsync("OmphalosNavItem")
```

`dropdown-divider` is Bootstrap 5 standard pattern for separating groups of nav items visually.

---

### `EuphoriaInn.Service/Views/Quest/Details.cshtml` (view — modify)

**Analog:** Same file, lines 572–586 (DM Controls card).

**Current card structure** (lines 572–586):
```razor
<div class="col-lg-2 col-md-3">
    @if ((bool)ViewBag.CanManage)
    {
        <div class="card modern-card mb-3">
            <div class="card-header modern-card-header">
                <h5>DM Controls</h5>
            </div>
            <div class="card-body modern-card-body">
                <a href="@Url.Action("Manage", "Quest", new { id = Model.Quest?.Id })" class="btn btn-primary w-100">
                    <i class="fas fa-cog me-2"></i>
                    Manage Quest
                </a>
            </div>
        </div>
    }
```

**Insert inside `modern-card-body`, after the "Manage Quest" `<a>` anchor (after line 583):**
```razor
                @if ((bool)(ViewBag.ShowOmphalosButton ?? false))
                {
                    <a href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Quest?.Id })"
                       class="btn btn-warning w-100 mt-2">
                        <i class="fas fa-book-open me-2"></i>
                        Open Session Notes
                    </a>
                }
```

`mt-2` adds top margin to separate from "Manage Quest" button. `(ViewBag.ShowOmphalosButton ?? false)` cast pattern is defensive (Risk 2 in RESEARCH.md). `w-100` matches existing button (D-06). `btn-warning` chosen for visual distinction from `btn-primary`.

---

### `EuphoriaInn.Service/Views/Quest/Manage.cshtml` (view — modify)

**Analog:** Same file, lines 475–486 ("View Public Page" card).

**Current "View Public Page" card** (lines 476–486):
```razor
<div class="card modern-card mb-3">
    <div class="card-header modern-card-header">
        <h5>View Public Page</h5>
    </div>
    <div class="card-body modern-card-body">
        <a href="@Url.Action("Details", "Quest", new { id = Model.Id })" class="btn btn-secondary w-100">
            <i class="fas fa-eye me-2"></i>
            View Public Page
        </a>
    </div>
</div>
```

**Insert after line 486 (closing `</div>` of "View Public Page" card), before line 488 ("Quest Summary" card):**
```razor
@if ((bool)(ViewBag.ShowOmphalosButton ?? false))
{
    <div class="card modern-card mb-3">
        <div class="card-header modern-card-header">
            <h5>Session Notes</h5>
        </div>
        <div class="card-body modern-card-body">
            <a href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Id })"
               class="btn btn-warning w-100">
                <i class="fas fa-book-open me-2"></i>
                Open Session Notes
            </a>
        </div>
    </div>
}
```

Note: Manage view model is `Quest` directly, so `Model.Id` not `Model.Quest?.Id`.
Card uses `modern-card`, `modern-card-header`, `modern-card-body` per CLAUDE.md UI guidelines.

---

### `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` (test — new)

**Analog:** `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs`

**File structure pattern** (lines 1–15):
```csharp
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EuphoriaInn.UnitTests.Services;

public class EmailServiceTests
{
    private static EmailService Create(EmailSettings settings)
    {
        ...
        return new EmailService(Options.Create(settings), logger);
    }
```

**Copy this pattern — adapted for IntegrationTokenService (no mock dependencies):**
```csharp
using EuphoriaInn.Domain.Services;

namespace EuphoriaInn.UnitTests.Services;

public class IntegrationTokenServiceTests
{
    private static readonly IntegrationTokenService _sut = new();
```

`FluentAssertions` and `Xunit` are available via GlobalUsings — no explicit `using` needed.

**Tests to write** (D-18 coverage — TOKEN-01 through TOKEN-04):

1. Endpoint path included in URL (`/api/sso/open-quest`)
2. Base URL trailing slash trimmed (no `//api/...`)
3. Username lowercased in URL parameter
4. QuestTitle percent-encoded (`Dragon's Lair` → `Dragon%27s%20Lair`)
5. Sig parameter is lowercase hex, 64 chars (SHA-256 = 32 bytes)
6. Expiry is in future (between `now + 299` and `now + 301`)
7. Different secrets produce different sigs

All tests call `_sut.GenerateSignedUrl(...)` and verify URL structure. No mock needed — pure function. Use query string parsing to extract `sig`, `expiry`, `username`, `questTitle` values from returned URL.

---

### `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` (test — new)

**Analog:** `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` + `QuestControllerIntegrationTests_Comprehensive.cs`

**File structure pattern** (lines 1–16 of AdminControllerIntegrationTests):
```csharp
using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class AdminControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public AdminControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        _client = factory.CreateNonRedirectingClient();
    }
```

**Copy this pattern — adapted (primary constructor form, matching QuestControllerIntegrationTests_Comprehensive):**
```csharp
using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class LaunchOmphalosIntegrationTests(WebApplicationFactoryBase factory)
    : IClassFixture<WebApplicationFactoryBase>
{
    private readonly HttpClient _client = factory.CreateNonRedirectingClient();
```

**GlobalUsings already provide** (from `EuphoriaInn.IntegrationTests/GlobalUsings.cs`):
- `EuphoriaInn.Repository.Entities` (covers `AdminSettingEntity`, `QuestBoardContext`)
- `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.Extensions.DependencyInjection`

**AdminSettings seeding helper pattern** (from RESEARCH.md Risk 9):
```csharp
private async Task SeedSettingsAsync(
    string? url = "https://omphalos.example.com",
    string? secret = "test-secret",
    bool isEnabled = true)
{
    using var scope = factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
    context.AdminSettings.RemoveRange(context.AdminSettings);
    await context.SaveChangesAsync();
    context.AdminSettings.AddRange(
        new AdminSettingEntity { Key = "IsEnabled", Value = isEnabled.ToString(), UpdatedAt = DateTime.UtcNow },
        new AdminSettingEntity { Key = "OmphalosUrl", Value = url ?? "", UpdatedAt = DateTime.UtcNow },
        new AdminSettingEntity { Key = "OmphalosSharedSecret", Value = secret ?? "", UpdatedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync();
}
```

`AdminSettingEntity` has string PK (`Key`), not int — no `IEntity` marker, no auto-increment.

**DM client creation pattern** (from AuthenticationHelper.cs lines 150–158):
```csharp
var (dmClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);
```

**Tests to write** (D-19 coverage — TOKEN-05, NAV-03, NAV-04, NAV-05):
1. Unauthenticated → redirect to login (302/401)
2. Player role → forbidden (403/302)
3. Integration disabled → 404
4. URL blank → 404
5. IsConfigured → 302 redirect, Location starts with `https://omphalos.example.com/api/sso/open-quest`
6. Redirect URL contains expected query params: `questId`, `username` (lowercase), `sig` (64 hex chars), `expiry` (future unix timestamp)

Use `TestContext.Current.CancellationToken` on all `GetAsync` calls (existing test convention).

---

## Shared Patterns

### Authorization Attribute
**Source:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 620–622
**Apply to:** `LaunchOmphalos` action
```csharp
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Manage(int id)
```

### NotFound / Challenge Guard Pattern
**Source:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 211–216 and 26–30
**Apply to:** `LaunchOmphalos` action
```csharp
if (quest == null)
{
    return NotFound();
}

var currentUser = await userService.GetUserAsync(User);
if (currentUser == null)
{
    return Challenge();
}
```

### Modern Card UI Pattern
**Source:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 476–486
**Apply to:** New "Session Notes" card on Manage page
```razor
<div class="card modern-card mb-3">
    <div class="card-header modern-card-header">
        <h5>...</h5>
    </div>
    <div class="card-body modern-card-body">
        <a ... class="btn btn-[color] w-100">
            <i class="fas fa-[icon] me-2"></i>
            Button Text
        </a>
    </div>
</div>
```

### ViewBag Defensive Cast Pattern
**Source:** RESEARCH.md Risk 2 (and existing `CanManage` cast)
**Apply to:** All `ShowOmphalosButton` reads in views
```razor
@if ((bool)(ViewBag.ShowOmphalosButton ?? false))
```

### Internal Service Class Pattern
**Source:** `EuphoriaInn.Domain/Services/AdminSettingService.cs` line 6
**Apply to:** `IntegrationTokenService`
```csharp
internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
```
`internal class` — not `public`. All domain service implementations are internal.

### DI AddTransient Registration Pattern
**Source:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` lines 15–23
**Apply to:** `IIntegrationTokenService` registration
```csharp
services.AddScoped<IAdminSettingService, AdminSettingService>();
// New:
services.AddTransient<IIntegrationTokenService, IntegrationTokenService>();
```
`AddTransient` (not `AddScoped`) because service is stateless pure computation.

---

## No Analog Found

None — all files have a close match.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Domain/`, `EuphoriaInn.Service/Controllers/`, `EuphoriaInn.Service/Views/`, `EuphoriaInn.IntegrationTests/`, `EuphoriaInn.UnitTests/`
**Files scanned:** 14 source files read directly
**Pattern extraction date:** 2026-06-18
