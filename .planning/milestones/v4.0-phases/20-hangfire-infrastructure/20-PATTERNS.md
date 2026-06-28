# Phase 20: Hangfire Infrastructure - Pattern Map

**Mapped:** 2026-06-25
**Files analyzed:** 5 (3 new, 2 modified, 1 new-optional)
**Analogs found:** 5 / 5

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Authorization/AdminDashboardAuthFilter.cs` | middleware/authorization | request-response | `EuphoriaInn.Service/Authorization/AdminHandler.cs` | role-match |
| `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` | service/job | event-driven | `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` (`ConfigureDatabase` / `SeedShopDataAsync`) | partial-match (scope pattern) |
| `EuphoriaInn.Service/Program.cs` | config | request-response | `EuphoriaInn.Service/Program.cs` (self — existing structure) | exact |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | component | request-response | `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (self — admin dropdown, lines 33–49) | exact |
| `EuphoriaInn.Service/Extensions/HangfireExtensions.cs` (optional) | config/utility | — | `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | role-match |

---

## Pattern Assignments

### `EuphoriaInn.Service/Authorization/AdminDashboardAuthFilter.cs` (authorization filter, request-response)

**Analog:** `EuphoriaInn.Service/Authorization/AdminHandler.cs`

**Namespace and file-header pattern** (AdminHandler.cs lines 1–4):
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EuphoriaInn.Service.Authorization;
```
New file uses the same namespace (`EuphoriaInn.Service.Authorization`) and file-scoped namespace declaration. Replace the `using` block with Hangfire-specific imports.

**Auth check pattern — IsAuthenticated guard then role check** (AdminHandler.cs lines 9–28):
```csharp
if (!context.User.Identity?.IsAuthenticated == true)
{
    context.Fail();
    return;
}

var isAdmin = await userService.IsInRoleAsync(context.User, "Admin");

if (isAdmin)
{
    context.Succeed(requirement);
}
else
{
    context.Fail();
}
```
Mirror this two-step guard (unauthenticated first, then role check) in `AdminDashboardAuthFilter.Authorize`. Replace `context.Fail()` / `context.Succeed()` with `Response.Redirect` + `return false` / `return true`. Use `httpContext.User.IsInRole("Admin")` directly (no async service call needed; Identity role claims are already on the ClaimsPrincipal after `UseAuthentication`).

**Concrete implementation for the new file:**
```csharp
using Hangfire.Dashboard;

namespace EuphoriaInn.Service.Authorization;

public class AdminDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.Redirect("/Account/Login");
            return false;
        }

        if (!httpContext.User.IsInRole("Admin"))
        {
            httpContext.Response.Redirect("/Account/Login");
            return false;
        }

        return true;
    }
}
```
Note: `context.GetHttpContext()` is an extension method from `Hangfire.AspNetCore` — no additional `using` is required beyond `Hangfire.Dashboard`. The `IsInRole("Admin")` check is equivalent to the `AdminHandler` which also resolves the `"Admin"` role string.

---

### `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` (job class, event-driven)

**Analog (scope pattern):** `EuphoriaInn.Service/Program.cs` `SeedShopDataAsync` (lines 121–142) and `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` `ConfigureDatabase` (lines 33–42)

**Scope-creation pattern from SeedShopDataAsync** (Program.cs lines 121–142):
```csharp
static async Task SeedShopDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var shopSeedService = scope.ServiceProvider.GetRequiredService<EuphoriaInn.Domain.Interfaces.IShopSeedService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        // ...
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding shop data");
    }
}
```
The job extends this exact pattern into a Hangfire-executed context: create a scope, resolve a scoped service, use it, dispose scope. Key difference: the job uses `CreateAsyncScope()` (not `CreateScope()`) with `await using` to handle `IAsyncDisposable` dependencies (e.g., `SmtpClient`).

**Constructor pattern** — inject only `IServiceScopeFactory` (singleton, safe in constructors) and `ILogger<T>` (also singleton). Use primary constructor syntax matching the existing codebase style (AdminHandler, MobileDetectionMiddleware all use primary constructors):
```csharp
public class SmokeTestJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SmokeTestJob> logger)
```

**Concrete implementation for the new file:**
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EuphoriaInn.Service.Jobs;

public class SmokeTestJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SmokeTestJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        logger.LogInformation(
            "Smoke test: IEmailService resolved successfully. Type: {Type}",
            emailService.GetType().Name);
    }
}
```

---

### `EuphoriaInn.Service/Program.cs` (config, modified)

**Analog:** `EuphoriaInn.Service/Program.cs` itself — changes are additive insertions into the existing structure.

**Existing using block** (Program.cs lines 1–13) — new usings are appended here:
```csharp
using EuphoriaInn.Repository.Automapper;
using EuphoriaInn.Domain.Extensions;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Extensions;
using EuphoriaInn.Service.Authorization;
using EuphoriaInn.Service.Automapper;
using EuphoriaInn.Service.Middleware;
using EuphoriaInn.Service.ViewExpanders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
```
Add after line 13:
```csharp
using Hangfire;
using Hangfire.SqlServer;
using EuphoriaInn.Service.Jobs;
```
(`EuphoriaInn.Service.Authorization` is already imported — `AdminDashboardAuthFilter` will resolve from it.)

**Service registration anchor** (Program.cs lines 73–75) — Hangfire services are added after `AddDomainServices`:
```csharp
builder.Services
    .AddRepositoryServices(builder.Configuration)
    .AddDomainServices(builder.Configuration);
```
Insert after this block:
```csharp
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
    });
}
```

**Middleware registration anchor — UseAuthentication / UseAuthorization ordering** (Program.cs lines 98–102):
```csharp
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```
Insert `UseHangfireDashboard` immediately after `UseAuthorization` (line 102), before `MapControllerRoute`:
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminDashboardAuthFilter() }
});
```

**Testing environment guard** (Program.cs lines 111–117) — smoke-test enqueue goes inside this existing block:
```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();

    // Seed basic shop data
    await SeedShopDataAsync(app);
}
```
Becomes:
```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();

    // Seed basic shop data
    await SeedShopDataAsync(app);

    // Smoke-test: proves IServiceScopeFactory pattern resolves before real jobs land
    // REMOVE THIS in Phase 21 once real jobs exist
    BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None));
}
```

---

### `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (component, modified)

**Analog:** `_Layout.cshtml` itself — Admin dropdown, lines 37–48.

**Existing Admin dropdown items pattern** (lines 37–48):
```html
<ul class="dropdown-menu">
    <li>
        <a class="dropdown-item" asp-controller="Admin" asp-action="Users">
            <i class="fas fa-users-cog me-2"></i>User Management
        </a>
    </li>
    <li>
        <a class="dropdown-item" asp-controller="Admin" asp-action="Quests">
            <i class="fas fa-scroll me-2"></i>Quest Management
        </a>
    </li>
</ul>
```
Insert a third `<li>` after line 47 (after Quest Management `</li>`), before closing `</ul>` on line 48. Use plain `href="/hangfire"` — not `asp-controller`/`asp-action` tag helpers — because `/hangfire` is a Hangfire middleware route, not an MVC controller action:
```html
<li>
    <a class="dropdown-item" href="/hangfire">
        <i class="fas fa-tasks me-2"></i>Background Jobs
    </a>
</li>
```
Pattern compliance: `class="dropdown-item"`, FontAwesome icon, `me-2` spacing — matches every existing dropdown item in the layout.

---

### `EuphoriaInn.Service/Extensions/HangfireExtensions.cs` (optional, config/utility)

**Analog:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs`

**Extension method pattern** (ServiceExtensions.cs lines 1–26):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuphoriaInn.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings");
        services.AddScoped<IUserService, UserService>();
        // ...
        return services;
    }
}
```
Mirror this pattern for a new `AddHangfireServices` extension method in `EuphoriaInn.Service/Extensions/HangfireExtensions.cs`. The namespace becomes `EuphoriaInn.Service.Extensions`. The method signature accepts `(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)` because the Testing guard requires the environment. This class may be omitted if the Hangfire registration stays inline in `Program.cs`.

---

## Shared Patterns

### Primary constructor syntax
**Source:** `EuphoriaInn.Service/Authorization/AdminHandler.cs` line 6, `EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs` line 3
**Apply to:** `AdminDashboardAuthFilter.cs`, `SmokeTestJob.cs`
```csharp
public class AdminHandler(IUserService userService) : AuthorizationHandler<AdminRequirement>
public class MobileDetectionMiddleware(RequestDelegate next)
```
All new classes follow primary constructor syntax. No backing fields, no `readonly` declarations — the constructor parameters are used directly.

### File-scoped namespace declaration
**Source:** Every `.cs` file in the project (e.g., AdminHandler.cs line 4, MobileDetectionMiddleware.cs line 1)
**Apply to:** All new `.cs` files
```csharp
namespace EuphoriaInn.Service.Authorization;
// (not the block-scoped { } form)
```

### Testing environment guard
**Source:** `EuphoriaInn.Service/Program.cs` lines 110–117
**Apply to:** `Program.cs` modification — `AddHangfireServer` registration and startup enqueue
```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();
    await SeedShopDataAsync(app);
}
```
`AddHangfireServer` is wrapped in `if (!builder.Environment.IsEnvironment("Testing"))`. The startup enqueue is added inside the `app.Environment.IsEnvironment("Testing")` guard that already exists at lines 111–117.

### Service scope pattern (scoped deps outside request context)
**Source:** `EuphoriaInn.Service/Program.cs` `SeedShopDataAsync` (lines 121–142), `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` `ConfigureDatabase` (lines 33–42)
**Apply to:** `SmokeTestJob.cs` and all future job classes in Phases 21–22
```csharp
using var scope = app.Services.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IService>();
```
Job variant upgrades this to `await using var scope = scopeFactory.CreateAsyncScope()` for `IAsyncDisposable` support.

---

## No Analog Found

All 5 files have analogs. No entries.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Authorization/`, `EuphoriaInn.Service/Middleware/`, `EuphoriaInn.Service/Views/Shared/`, `EuphoriaInn.Domain/Extensions/`, `EuphoriaInn.Repository/Extensions/`, `EuphoriaInn.Service/Program.cs`
**Files scanned:** 6 analog files read in full
**Pattern extraction date:** 2026-06-25
