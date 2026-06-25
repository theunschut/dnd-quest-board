# Phase 20: Hangfire Infrastructure — Research

**Researched:** 2026-06-25
**Domain:** Hangfire background job framework, ASP.NET Core 10 integration, SQL Server storage, dashboard authorization
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Use a custom `IDashboardAuthorizationFilter` implementation checking the `Admin` role — NOT `LocalRequestsOnlyAuthorizationFilter` (bypassed by the Docker reverse proxy).
- **D-02:** Non-admin users (unauthenticated or wrong role) hitting `/hangfire` are redirected to the login page (consistent with the rest of the app's auth failure behavior), not returned a raw 401.
- **D-03:** The dashboard registration in `Program.cs` must appear after `UseAuthentication` and `UseAuthorization` so the auth middleware stack is in place.
- **D-04:** SQL Server storage sharing the existing database. The `[HangFire]` schema is auto-created by Hangfire on startup — no EF Core migration required.
- **D-05:** All Hangfire jobs must resolve scoped services via `IServiceScopeFactory` + `CreateAsyncScope()` inside the job method body. Scoped services must never be injected into the job class constructor.
- **D-06:** This pattern is established in Phase 20 via a smoke-test job; Phases 21–22 must follow it without exception.
- **D-07:** A smoke-test job is enqueued once on application startup (fire-and-forget via `BackgroundJob.Enqueue`) to prove `IServiceScopeFactory` resolves correctly.
- **D-08:** The smoke-test job resolves `IEmailService` via `IServiceScopeFactory`. It logs a message; no actual email is sent.
- **D-09:** The startup enqueue call is removed in Phase 21 once real jobs exist. The smoke-test job class may also be removed.
- **D-10:** A Hangfire Dashboard link is added to the admin panel navigation, visible to Admin role only.
- **D-11:** Phase 20 adds no new integration tests for Hangfire dashboard access. The existing 134 integration tests must continue to pass.

### Claude's Discretion
- Exact nav link label and position in the admin layout (e.g., "Job Dashboard", "Background Jobs", or "Hangfire")
- Hangfire worker count and polling interval (use sensible defaults for a small self-hosted app)
- Whether to guard Hangfire startup behind `!app.Environment.IsEnvironment("Testing")` (consistent with the existing `ConfigureDatabase` guard)
- Class and file naming for the custom `IDashboardAuthorizationFilter` implementation

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| JOBS-01 | Hangfire is installed with SQL Server storage sharing the existing database; the `[HangFire]` schema is auto-created on startup — no EF Core migration required | Confirmed: Hangfire.SqlServer auto-creates schema via Install.sql on first run. Uses existing `DefaultConnection` connection string. |
| JOBS-02 | The Hangfire dashboard is accessible at `/hangfire` and requires the `Admin` role; unauthenticated or non-admin requests are rejected | Confirmed: IDashboardAuthorizationFilter + context.GetHttpContext() + Response.Redirect() pattern verified from multiple sources. |
</phase_requirements>

---

## Summary

Hangfire 1.8.23 (released 2026-02-05) is the current stable version and is confirmed compatible with .NET 10 / ASP.NET Core 10 via `netstandard2.0` targeting. Three packages are needed: `Hangfire.AspNetCore`, `Hangfire.SqlServer`, and (transitively) `Hangfire.Core`. The `[HangFire]` schema auto-creates on first startup — no EF Core migration is needed and no extra database is required; the existing `DefaultConnection` is reused.

The `IDashboardAuthorizationFilter` approach is the correct authorization mechanism for this project. The modern alternative (endpoint routing with `.RequireAuthorization()`) is architecturally cleaner but requires passing an empty `Authorization` list to suppress Hangfire's default block behavior — the locked decision uses the filter approach directly, which is simpler for a single-role check. The redirect-to-login behavior is implemented by calling `context.GetHttpContext().Response.Redirect("/Account/Login")` before returning `false`.

The `IServiceScopeFactory` + `CreateAsyncScope()` pattern in job methods is the established, safe pattern for resolving `DbContext` and `IEmailService` in Hangfire jobs. Constructor injection of scoped services is unsafe because Hangfire may resolve the job class outside a proper request scope, causing the scoped service to act as a singleton (stale data, `ObjectDisposedException`). Injecting the singleton `IServiceScopeFactory` in the constructor is safe; creating a scope inside the job method is the correct boundary.

**Primary recommendation:** Install three Hangfire packages into `EuphoriaInn.Service`, register via a new `AddHangfireServices()` extension method following the `AddDomainServices` pattern, register server and dashboard in `Program.cs` after `UseAuthorization`, guard server startup with `!IsEnvironment("Testing")`, and implement the `IDashboardAuthorizationFilter` mirroring the existing `AdminHandler` role check.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Hangfire storage (schema, job tables) | Database / Storage | — | SQL Server auto-schema; no application code owns it |
| Hangfire server (job processing workers) | API / Backend (Service project) | — | IHostedService registered in Program.cs; runs in-process with the web app |
| Dashboard UI at /hangfire | API / Backend (Service project) | — | Hangfire middleware serves built-in dashboard HTML |
| Dashboard authorization check | API / Backend (Service project) | — | IDashboardAuthorizationFilter runs within ASP.NET Core middleware pipeline |
| Job class + IServiceScopeFactory pattern | API / Backend (Service project) | Domain (via IEmailService interface) | Job classes live in Service; they consume Domain interfaces via scope |
| Smoke-test job startup enqueue | API / Backend (Service project) | — | BackgroundJob.Enqueue called from Program.cs startup block |
| Admin nav link | Frontend Server (Service / Razor views) | — | _Layout.cshtml already has Admin dropdown; add entry there |

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Hangfire.AspNetCore | 1.8.23 | ASP.NET Core integration (AddHangfire, AddHangfireServer, UseHangfireDashboard, GetHttpContext extension) | The only official ASP.NET Core integration package |
| Hangfire.SqlServer | 1.8.23 | SQL Server job storage (auto-schema, distributed locking) | Official SQL Server storage provider |
| Hangfire.Core | 1.8.23 | Core abstractions (BackgroundJob, IDashboardAuthorizationFilter, IJobCancellationToken) | Pulled in as transitive dependency of both above |

[VERIFIED: nuget.org — Hangfire.AspNetCore 1.8.23 released 2026-02-05; Hangfire.SqlServer 1.8.23 same date]

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| (None additional for Phase 20) | — | — | Phase 22 may add Hangfire.MemoryStorage for test isolation if needed |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| IDashboardAuthorizationFilter | MapHangfireDashboard + .RequireAuthorization() | Endpoint routing approach is cleaner but requires empty Authorization list; locked decision uses filter |
| Hangfire.SqlServer | Hangfire.InMemory | In-memory has no persistence across restarts; SQL Server is the production choice |

**Installation (add to `EuphoriaInn.Service.csproj`):**
```bash
dotnet add EuphoriaInn.Service/EuphoriaInn.Service.csproj package Hangfire.AspNetCore --version 1.8.23
dotnet add EuphoriaInn.Service/EuphoriaInn.Service.csproj package Hangfire.SqlServer --version 1.8.23
```

**Version verification:**
```bash
dotnet list EuphoriaInn.Service/EuphoriaInn.Service.csproj package
```

[VERIFIED: npm registry check not applicable — NuGet versions confirmed via nuget.org]

---

## Architecture Patterns

### System Architecture Diagram

```
HTTP Request → /hangfire
        │
        ▼
[UseAuthentication]    ← Identity cookie parsed, ClaimsPrincipal populated
        │
[UseAuthorization]     ← Policies evaluated
        │
[UseHangfireDashboard] ← registered AFTER the above two
        │
[AdminDashboardAuthorizationFilter.Authorize(DashboardContext)]
        │
  ┌─────┴──────┐
  │ Authorized │ → Hangfire built-in dashboard HTML served
  │ Denied     │ → Response.Redirect("/Account/Login") + return false


Startup sequence (Program.cs):
  builder.Services.AddHangfire(...)     ← configure storage
  builder.Services.AddHangfireServer() ← register BackgroundJobServer as IHostedService
         │ (guarded: skip in "Testing" environment)
         ▼
  app.UseAuthentication()
  app.UseAuthorization()
  app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [...] })
         │
  if (!IsEnvironment("Testing"))
    BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None))


Job execution flow (smoke-test pattern):
  Hangfire worker picks up job
        │
  SmokeTestJob.RunAsync(CancellationToken ct) called
        │
  await using var scope = _scopeFactory.CreateAsyncScope()
        │
  var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>()
        │
  _logger.LogInformation("Smoke test: IEmailService resolved successfully")
        │
  scope disposed → IEmailService + any inner scoped deps disposed cleanly
```

### Recommended Project Structure

```
EuphoriaInn.Service/
├── Authorization/
│   ├── AdminHandler.cs               (existing)
│   ├── AdminRequirement.cs           (existing)
│   └── AdminDashboardAuthFilter.cs   (NEW — IDashboardAuthorizationFilter)
├── Jobs/
│   └── SmokeTestJob.cs               (NEW — smoke-test job class)
└── Program.cs                        (modified)

EuphoriaInn.Domain/
└── Extensions/
    └── ServiceExtensions.cs          (modified — or new AddHangfireServices extension)
```

Note: Because `Hangfire.AspNetCore` is only referenced from `EuphoriaInn.Service`, the Hangfire registration extension method should live either directly in `Program.cs` or in a new `HangfireExtensions.cs` in `EuphoriaInn.Service/Extensions/`. It must NOT go in `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — that would introduce a Service-layer package reference into the Domain project, violating the one-way dependency rule.

### Pattern 1: Hangfire Registration in Program.cs

```csharp
// Source: docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html
// + verified codebase connection string pattern

// In builder.Services section:
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
        options.WorkerCount = 2;  // Small self-hosted app; default (CPU*5) is overkill
    });
}

// In app pipeline section — AFTER UseAuthentication + UseAuthorization:
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminDashboardAuthFilter() }
});
```

[CITED: docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html]
[CITED: docs.hangfire.io/en/latest/configuration/using-sql-server.html]

### Pattern 2: IDashboardAuthorizationFilter with Role Check and Redirect

```csharp
// Source: verified from multiple community sources + GitHub issue #1112
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

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

Key notes:
- `context.GetHttpContext()` is an extension method from `Hangfire.AspNetCore` — requires `using Hangfire.Dashboard;`
- `httpContext.User` is the `ClaimsPrincipal` populated by `UseAuthentication` upstream
- `IsInRole("Admin")` matches the existing `AdminHandler` which calls `userService.IsInRoleAsync(context.User, "Admin")` — both resolve the same ASP.NET Core Identity roles
- The redirect must happen before `return false`; Hangfire does not automatically redirect on `false`

[CITED: blog.programx.co.uk/2024/09/15/securing-hangfire-dashboard-using-the-application-identity-scheme/]
[CITED: github.com/HangfireIO/Hangfire/issues/1112]

### Pattern 3: IServiceScopeFactory Pattern in Job Method

```csharp
// Source: codegenes.net/blog/hangfire-dependency-injection-lifetime-scope/
// + Microsoft Learn CreateAsyncScope docs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EuphoriaInn.Domain.Interfaces;

namespace EuphoriaInn.Service.Jobs;

public class SmokeTestJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SmokeTestJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        logger.LogInformation("Smoke test: IEmailService resolved successfully. Type: {Type}", emailService.GetType().Name);
    }
}
```

Key notes:
- `IServiceScopeFactory` is a singleton — safe to inject in the constructor
- `IEmailService` is scoped — must NOT be in constructor; resolved inside method scope
- `CreateAsyncScope()` preferred over `CreateScope()` because `IEmailService` (and its `SmtpClient` dependency) may implement `IAsyncDisposable`
- `await using` ensures async disposal even if the job throws
- `CancellationToken` (not `IJobCancellationToken`) is the correct parameter for Hangfire 1.7+; fully async, no unnecessary storage polling

[CITED: docs.hangfire.io/en/latest/background-methods/using-cancellation-tokens.html]
[CITED: learn.microsoft.com — ServiceProviderServiceExtensions.CreateAsyncScope]

### Pattern 4: Startup Smoke-Test Enqueue

```csharp
// In Program.cs, inside the existing !IsEnvironment("Testing") block:
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();
    await SeedShopDataAsync(app);

    // Smoke-test: proves IServiceScopeFactory pattern resolves before real jobs land
    // REMOVE THIS in Phase 21 once real jobs exist
    BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None));
}
```

[VERIFIED: codebase — existing `!IsEnvironment("Testing")` guard pattern in Program.cs line 111]

### Pattern 5: Admin Navigation Link

The existing Admin dropdown in `_Layout.cshtml` (lines 33–49) contains two items: User Management and Quest Management. The Hangfire dashboard link is added as a third item in this same dropdown:

```html
<li>
    <a class="dropdown-item" href="/hangfire">
        <i class="fas fa-tasks me-2"></i>Background Jobs
    </a>
</li>
```

Note: Use a plain `href="/hangfire"` — not `asp-controller` / `asp-action` tag helpers — because `/hangfire` is a Hangfire middleware route, not an MVC controller action.

[VERIFIED: codebase — _Layout.cshtml admin dropdown structure]

### Anti-Patterns to Avoid

- **Constructor injection of scoped services in job classes:** `IEmailService`, `QuestBoardContext`, or any `DbContext` injected in a constructor will be resolved from the root DI container and behave as singletons, causing stale data and `ObjectDisposedException` on repeated job runs.
- **Using `IRazorViewEngine` from job bodies:** Throws `NullReferenceException` in background job context (no HttpContext). This is a Phase 21 concern, but worth noting here as the `IServiceScopeFactory` pattern is established now.
- **Using `LocalRequestsOnlyAuthorizationFilter`:** Bypassed by Docker reverse proxy — locked out by D-01.
- **Registering `UseHangfireDashboard` before `UseAuthorization`:** The `context.GetHttpContext().User` will not have claims populated because `UseAuthentication` has not run yet.
- **Missing `AddHangfireServer` guard in Testing environment:** Without the guard, the Hangfire server starts during integration tests and its workers will pick up the smoke-test job against the SQLite in-memory test database, causing `SqlException` or race conditions. The existing `!IsEnvironment("Testing")` pattern at line 111 of Program.cs is the model.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Job persistence across restarts | Custom queue table in app schema | Hangfire.SqlServer | Handles distributed locking, visibility timeouts, retry state, schema versioning |
| Job retry logic | try/catch + manual re-enqueue | Hangfire built-in retry (configurable attempts, exponential backoff) | Handles failures, server crashes, partial execution |
| Dashboard UI for monitoring jobs | Custom admin page listing pending/failed jobs | Hangfire built-in dashboard at /hangfire | Retry, delete, re-queue, inspect job arguments — all built-in |
| Cancellation propagation | Manual flags | CancellationToken parameter in job method | Hangfire 1.7+ passes cancellation token automatically on server shutdown |

**Key insight:** Hangfire's SQL Server schema (`[HangFire]` with ~12 tables) handles all concurrency, visibility, and state transitions that are extremely difficult to get right in hand-rolled solutions.

---

## Common Pitfalls

### Pitfall 1: Registration Order — Dashboard Before Auth Middleware
**What goes wrong:** `context.GetHttpContext().User.Identity.IsAuthenticated` is always `false`; all requests are redirected to login even for Admin users.
**Why it happens:** `UseHangfireDashboard` is registered before `UseAuthentication`, so the auth cookie has not been parsed when the filter runs.
**How to avoid:** Place `app.UseHangfireDashboard(...)` after `app.UseAuthentication()` and `app.UseAuthorization()` in Program.cs — locked in D-03.
**Warning signs:** Admin user is always redirected to login when visiting /hangfire.

### Pitfall 2: Hangfire Server Starts in Testing Environment
**What goes wrong:** Integration tests fail with `SqlException` ("Invalid object name 'HangFire.Job'") because the Hangfire server is running against the SQLite in-memory test database, which has no `[HangFire]` schema.
**Why it happens:** `AddHangfireServer` is called unconditionally; the WebApplicationFactory uses `builder.UseEnvironment("Testing")` and overrides EF Core to SQLite, but the Hangfire storage still points at the configured SQL Server connection (or fails entirely).
**How to avoid:** Wrap `services.AddHangfireServer(...)` in `if (!builder.Environment.IsEnvironment("Testing"))`. This matches the existing `ConfigureDatabase` guard in Program.cs.
**Warning signs:** Tests that were green before Phase 20 start failing with Hangfire-related exceptions.

### Pitfall 3: Scoped Service in Job Constructor
**What goes wrong:** `IEmailService` works on the first job run but throws `ObjectDisposedException` on subsequent runs, or returns stale data from a disposed `DbContext`.
**Why it happens:** When Hangfire resolves the job class, it creates a scope per job execution via `AspNetCoreJobActivator`. However, if the job class has a scoped dependency in its constructor, that dependency is resolved at instantiation time — which may be from a long-lived scope that doesn't match the job execution boundary. The safe guarantee is that `IServiceScopeFactory` (a singleton) is always safe in constructors; scoped services are not.
**How to avoid:** Only inject `IServiceScopeFactory` and `ILogger<T>` in job constructors. Resolve everything else inside the method via `CreateAsyncScope()`.
**Warning signs:** `ObjectDisposedException: Cannot access a disposed context` in Hangfire job failure logs.

### Pitfall 4: Connection String TrustServerCertificate
**What goes wrong:** Hangfire storage initialization fails with SSL certificate error when using `Microsoft.Data.SqlClient` (which Hangfire.SqlServer 1.8 uses by default).
**Why it happens:** `Microsoft.Data.SqlClient` enables encryption by default; self-signed or dev SQL Server certs are not trusted.
**How to avoid:** The existing `DefaultConnection` already includes `TrustServerCertificate=true` — confirmed in `appsettings.json`. Reusing the same connection string key avoids this entirely.
**Warning signs:** `SqlException: A connection was successfully established with the server, but then an error occurred during the login process`.

### Pitfall 5: Response.Redirect After Response Body Started
**What goes wrong:** `System.InvalidOperationException: Headers are read-only, response has already started` when the auth filter tries to redirect.
**Why it happens:** Dashboard filter fires late in the pipeline if other middleware has already written to the response.
**How to avoid:** `UseHangfireDashboard` must be registered before `app.MapControllerRoute` and any other route-handling middleware. Registration order in Program.cs: UseAuthentication → UseAuthorization → UseHangfireDashboard → MapControllerRoute.
**Warning signs:** Exception in Hangfire middleware logs; dashboard requests result in 500 instead of redirect.

---

## Code Examples

### Complete Hangfire Registration Block

```csharp
// ── EuphoriaInn.Service/Program.cs ──
// Add in builder.Services section (after AddDomainServices):

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

// ── In app pipeline, after UseAuthorization: ──
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminDashboardAuthFilter() }
});

// ── In the !IsEnvironment("Testing") block: ──
BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None));
```

### Required Using Statements (Program.cs additions)

```csharp
using Hangfire;
using Hangfire.SqlServer;
using EuphoriaInn.Service.Authorization;   // AdminDashboardAuthFilter
using EuphoriaInn.Service.Jobs;            // SmokeTestJob
```

### Required Using Statements (AdminDashboardAuthFilter.cs)

```csharp
using Hangfire.Dashboard;
// No additional using needed — context.GetHttpContext() is an extension method
// in Hangfire.AspNetCore, available when Hangfire.AspNetCore is referenced
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IJobCancellationToken` in job parameters | `CancellationToken` (standard .NET) | Hangfire 1.7 | Use `CancellationToken` in all new jobs |
| `UseHangfireServer()` (deprecated) | `AddHangfireServer()` (IHostedService) | Hangfire 1.6+ | Use `AddHangfireServer` in `builder.Services` |
| `services.AddHangfire(x => x.UseSqlServerStorage("connStr"))` | Same call but with `SqlServerStorageOptions` | Hangfire 1.7 | `DisableGlobalLocks = true` removes dependency on sp_getapplock |

**Deprecated/outdated:**
- `IJobCancellationToken`: backward compat only; use `CancellationToken`
- `UseHangfireServer()` (extension on `IApplicationBuilder`): replaced by `AddHangfireServer()` (IHostedService); the old method still exists but triggers a deprecation warning

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `context.GetHttpContext()` is available at dashboard filter execution time because `UseHangfireDashboard` is placed after `UseAuthentication` | Pattern 2 | If middleware ordering fails, User will always be unauthenticated |
| A2 | `WorkerCount = 2` is appropriate for this self-hosted single-container app | Pattern 1 | Too low: slow job throughput (not a concern for Phase 20 with only one smoke job); can be tuned in Phase 22 |
| A3 | Smoke-test job is enqueued inside the existing `!IsEnvironment("Testing")` guard block | Pattern 4 | If placed outside the guard, integration tests will attempt to enqueue against the non-existent HangFire schema in the SQLite test DB |

**Low-risk assumptions:** All three are verifiable by code review during plan execution; none require external confirmation.

---

## Open Questions

1. **Does `AddHangfire` itself (without `AddHangfireServer`) attempt to create the SQL Server schema in the Testing environment?**
   - What we know: `AddHangfire` configures the storage; `AddHangfireServer` starts the worker. Schema creation happens in `SqlServerStorage` constructor, which is triggered at startup.
   - What's unclear: Whether schema creation is deferred until the first job or triggered immediately during DI configuration in the Testing environment.
   - Recommendation: Guard `AddHangfire` itself (or pass `PrepareSchemaIfNecessary = false` in `SqlServerStorageOptions`) when in Testing environment, OR accept that `AddHangfire` runs but points at a SQL Server that isn't available — this will fail at startup. The safest implementation guards both `AddHangfire` and `AddHangfireServer` behind `!IsEnvironment("Testing")`. Alternatively, only guard `AddHangfireServer` and watch for test failures; if they occur, guard `AddHangfire` too.
   - Impact: Low risk — the `!IsEnvironment("Testing")` guard is already established in the codebase for `ConfigureDatabase`.

2. **`BackgroundJob.Enqueue` vs `IBackgroundJobClient.Enqueue` — which API to use for startup enqueue?**
   - What we know: `BackgroundJob.Enqueue` is a static method on the `BackgroundJob` class (calls through to `GlobalConfiguration`). `IBackgroundJobClient` is the DI-registered interface, resolvable via `app.Services`.
   - What's unclear: Both work; `IBackgroundJobClient` is more testable but Phase 20 adds no integration tests for this.
   - Recommendation: Use `BackgroundJob.Enqueue<SmokeTestJob>(...)` for the startup one-shot; simpler and consistent with how the official docs introduce fire-and-forget jobs.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | Hangfire.AspNetCore 1.8.23 (netstandard2.0) | Yes | 10.0.301 | — |
| SQL Server | Hangfire.SqlServer storage | Yes (Docker dev, host dev) | Per appsettings.json DefaultConnection | — |
| Hangfire.AspNetCore 1.8.23 | Dashboard + ASP.NET Core integration | Not yet installed | — | — (required) |
| Hangfire.SqlServer 1.8.23 | SQL Server job storage | Not yet installed | — | — (required) |

**Missing dependencies with no fallback:**
- `Hangfire.AspNetCore` and `Hangfire.SqlServer` must be added to `EuphoriaInn.Service.csproj` as part of Wave 0/Plan 01.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xunit.v3 3.2.2 |
| Config file | none (xunit auto-discover) |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj --no-build` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| JOBS-01 | HangFire schema auto-created; app starts without EF migration error | manual / smoke | Observe app startup logs in dev | N/A — startup behavior |
| JOBS-01 | Existing 134 integration tests pass with Hangfire installed | regression | `dotnet test --no-build` | Yes — existing test files |
| JOBS-02 | Dashboard access behavior | manual-only (per D-11) | Code review confirms registration order | N/A |

Per D-11: Phase 20 adds no new integration tests for Hangfire dashboard access. The success criterion is code review of Program.cs registration order + the existing 134 tests remaining green.

### Sampling Rate
- **Per task commit:** `dotnet test EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj --no-build`
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
None — existing test infrastructure covers all phase requirements. No new test files required for Phase 20 per D-11.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | Yes | ASP.NET Core Identity (already in use); dashboard filter reads `context.GetHttpContext().User` |
| V3 Session Management | No | Dashboard uses existing session/cookie — no new session config |
| V4 Access Control | Yes | IDashboardAuthorizationFilter restricts to Admin role only |
| V5 Input Validation | No | Phase 20 adds no user-submitted forms |
| V6 Cryptography | No | No new cryptographic operations |

### Known Threat Patterns for Hangfire Dashboard

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unauthenticated dashboard access | Elevation of Privilege | IDashboardAuthorizationFilter with IsAuthenticated check |
| Non-admin user accessing job admin UI | Elevation of Privilege | IsInRole("Admin") check in filter |
| Docker reverse proxy bypassing LocalRequestsOnly filter | Elevation of Privilege | Custom IDashboardAuthorizationFilter (locked in D-01) — not LocalRequestsOnlyAuthorizationFilter |
| Information disclosure via job argument inspection | Information Disclosure | Access restricted to Admin; job args may contain email addresses — acceptable for Admin role |

---

## Sources

### Primary (HIGH confidence)
- `nuget.org/packages/Hangfire.AspNetCore` — confirmed version 1.8.23, released 2026-02-05, .NET Standard 2.0 target [VERIFIED]
- `nuget.org/packages/Hangfire.SqlServer` — confirmed version 1.8.23, same release [VERIFIED]
- `docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html` — AddHangfire + AddHangfireServer registration pattern [CITED]
- `docs.hangfire.io/en/latest/configuration/using-sql-server.html` — SqlServerStorageOptions, auto-schema creation confirmed [CITED]
- `docs.hangfire.io/en/latest/configuration/using-dashboard.html` — IDashboardAuthorizationFilter interface, DashboardOptions, GetHttpContext [CITED]
- `docs.hangfire.io/en/latest/background-methods/using-cancellation-tokens.html` — CancellationToken preferred over IJobCancellationToken since 1.7 [CITED]
- `learn.microsoft.com — ServiceProviderServiceExtensions.CreateAsyncScope` — CreateAsyncScope for IAsyncDisposable services [CITED]
- Codebase: `EuphoriaInn.Service/Program.cs` — existing Testing environment guard, middleware order [VERIFIED]
- Codebase: `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — Admin dropdown structure for nav link placement [VERIFIED]
- Codebase: `EuphoriaInn.Service/appsettings.json` — `DefaultConnection` already includes `TrustServerCertificate=true` [VERIFIED]

### Secondary (MEDIUM confidence)
- `blog.programx.co.uk/2024/09/15/securing-hangfire-dashboard-using-the-application-identity-scheme/` — Role check + Response.Redirect pattern in IDashboardAuthorizationFilter [CITED — community, cross-verified with GitHub issue #1112]
- `codegenes.net/blog/hangfire-dependency-injection-lifetime-scope/` — IServiceScopeFactory constructor + CreateScope in method pattern [CITED — community, consistent with Microsoft Learn docs]
- `sahansera.dev/securing-hangfire-dashboard-with-endpoint-routing-auth-policy-aspnetcore/` — confirms empty Authorization list requirement for endpoint-routing approach (alternative to our filter approach) [CITED]

### Tertiary (LOW confidence)
- None — all critical claims verified against official docs or codebase.

---

## Metadata

**Confidence breakdown:**
- Standard stack (versions): HIGH — confirmed against nuget.org, released 2026-02-05
- Registration patterns: HIGH — confirmed against official Hangfire docs
- IDashboardAuthorizationFilter redirect behavior: MEDIUM-HIGH — official docs confirm Authorize signature; redirect pattern confirmed from GitHub issue + community blog cross-referenced
- IServiceScopeFactory pattern: HIGH — official .NET docs + multiple community sources consistent
- Testing environment guard: HIGH — mirrored directly from existing codebase pattern

**Research date:** 2026-06-25
**Valid until:** 2026-09-25 (stable library; 90 days unless Hangfire 2.x is announced)
