# Stack Research

**Domain:** ASP.NET Core 10 MVC — Milestone 4: Email Notifications (Hangfire + HTML Email + Resend Stats)
**Researched:** 2026-06-25
**Confidence:** HIGH

---

## What Already Exists (Do Not Re-add)

The project already sends email via Resend SMTP relay. No new SMTP or transport package is
needed. `EuphoriaInn.Domain` owns the existing plain-text email dispatch code. Do not add
MailKit, MimeKit, or a second SMTP client.

---

## Recommended Stack — New Packages Only

### Core Technologies (New)

| Technology | Version | Project | Purpose | Why Recommended |
|------------|---------|---------|---------|-----------------|
| `Hangfire.AspNetCore` | 1.8.23 | EuphoriaInn.Service | DI integration, dashboard middleware, `IBackgroundJobClient`/`IRecurringJobManager` | Entry point for ASP.NET Core; pulls in `Hangfire.Core` as a transitive dependency; adds `UseHangfireDashboard()` and `AddHangfire()`/`AddHangfireServer()` |
| `Hangfire.SqlServer` | 1.8.23 | EuphoriaInn.Service | Job persistence in SQL Server | Project already uses SQL Server; Hangfire creates its own `[HangFire]` schema in the existing database — no second DB or connection string |
| `Resend` | 0.5.1 | EuphoriaInn.Domain | Official .NET SDK for Resend API | Typed `IResend` interface with DI support; exposes `EmailSendAsync`, `EmailListAsync`, and `EmailRetrieveAsync` — covering both sending (alternative to raw SMTP for new notifications) and the stats dashboard query |

### Supporting Libraries (New)

None. HTML email rendering uses the built-in `HtmlRenderer` class from
`Microsoft.AspNetCore.Components.Web`, which is already transitively referenced by the
ASP.NET Core web SDK. No additional NuGet package is required.

---

## Per-Project Package Placement

| Package | EuphoriaInn.Service | EuphoriaInn.Domain | EuphoriaInn.Repository |
|---------|--------------------|--------------------|------------------------|
| `Hangfire.AspNetCore` | YES | NO | NO |
| `Hangfire.SqlServer` | YES | NO | NO |
| `Resend` | NO | YES | NO |

**Rationale:**

- `Hangfire.*` belongs in **Service**. Hangfire registers middleware (`UseHangfireDashboard`),
  DI services (`AddHangfire`, `AddHangfireServer`), and a dashboard UI — all Service-layer
  concerns. The Domain layer defines job interfaces; the Service layer enqueues and schedules
  them via `IBackgroundJobClient` and `IRecurringJobManager`.
- `Resend` belongs in **Domain** because `IResend` is the abstraction the email service
  consumes to send messages and query delivery status. Domain already owns email dispatch
  logic. The Service layer never touches `IResend` directly.
- **Repository gets nothing.** Hangfire manages its own schema entirely outside EF Core; no
  migrations, no entities, no DbContext involvement.

---

## HTML Email Rendering — Zero-Dependency Approach

Since .NET 8, `Microsoft.AspNetCore.Components.Web` exposes `HtmlRenderer`, which renders
any Razor component (`IComponent`) to an HTML string:

```csharp
// Injected from DI — no NuGet package needed
public class EmailRenderer(HtmlRenderer htmlRenderer)
{
    public async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        var output = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameterView = ParameterView.FromDictionary(parameters);
            var content = await htmlRenderer.RenderComponentAsync<TComponent>(parameterView);
            return content.ToHtmlString();
        });
        return output;
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddScoped<HtmlRenderer>();
builder.Services.AddScoped<EmailRenderer>();
```

Email templates are `.razor` files in `EuphoriaInn.Service/EmailTemplates/`. They inherit
from `ComponentBase` and accept `[Parameter]` properties as their view model. No layout
file or `_ViewStart.cshtml` involved — the template is self-contained HTML.

---

## Hangfire Setup Pattern

```csharp
// Program.cs — Service registration
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Program.cs — Middleware (after UseAuthentication/UseAuthorization)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});
```

Dashboard auth filter — no additional package needed:

```csharp
// EuphoriaInn.Service/Hangfire/HangfireAdminAuthorizationFilter.cs
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
```

Hangfire will create its own tables (`[HangFire]` schema) on first startup — no EF Core
migration required.

---

## Resend Stats — Important API Limitation

Resend does **not** expose an aggregate statistics endpoint returning total sent/bounced/failed
counts as numbers. What the API does expose:

- `GET /emails` — paginated list of emails, each with a `last_event` field
  (`delivered`, `bounced`, `failed`, `opened`, etc.). No aggregate counts.
- `GET /emails/{id}` — single email status with `last_event`.

The admin stats dashboard must count statuses client-side by iterating paginated results
from `IResend.EmailListAsync()`. For a deployment sending fewer than 100 emails/day this
is acceptable: the list will never be large enough to require chunked processing.

For real-time event tracking, Resend fires webhooks (`email.bounced`, `email.failed`, etc.)
at a registered URL — no additional NuGet package needed to receive them, just a POST
controller endpoint.

---

## Installation

```bash
# Run from solution root

# Service project — Hangfire
dotnet add EuphoriaInn.Service package Hangfire.AspNetCore --version 1.8.23
dotnet add EuphoriaInn.Service package Hangfire.SqlServer --version 1.8.23

# Domain project — Resend SDK
dotnet add EuphoriaInn.Domain package Resend --version 0.5.1
```

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Background jobs | Hangfire 1.8.23 | Quartz.NET | Hangfire has a built-in dashboard and SQL Server storage out of the box; simpler for small deployments; Quartz is more powerful but requires more configuration and has no built-in UI |
| Background jobs | Hangfire | `IHostedService` + `Timer` | No persistence — jobs lost on restart; no dashboard; not suitable for enqueue-on-demand DM trigger |
| Email stats | `Resend` SDK 0.5.1 | Raw `HttpClient` | SDK provides typed `IResend` with DI support; `EmailListAsync` and `EmailRetrieveAsync` return typed models; no manual JSON deserialization; maintained by the Resend team (updated May 2026) |
| HTML email rendering | Built-in `HtmlRenderer` (net8.0+) | RazorLight 2.3.1 | RazorLight last released January 2023; no explicit .NET 10 target; maintenance effectively halted; HtmlRenderer is in-box, zero additional dependencies |
| HTML email rendering | Built-in `HtmlRenderer` | FluentEmail.Razor | Adds FluentEmail + RazorLight chain; both are less-maintained than the built-in approach; unnecessary for a project that manages its own sending |
| HTML email rendering | Built-in `HtmlRenderer` | MailKit + custom Razor renderer | MailKit is a transport library; the project already uses Resend SMTP relay; adding MailKit would duplicate the transport layer for no gain |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `RazorLight` | Last release January 2023; no .NET 10 explicit support; maintenance uncertain | Built-in `HtmlRenderer` from `Microsoft.AspNetCore.Components.Web` |
| `FluentEmail.*` | Pulls in RazorLight transitively; adds abstraction over already-working email pipeline | Native `HtmlRenderer` + `IResend` directly |
| `MailKit` / `MimeKit` | Project already has Resend SMTP relay working; adding MailKit replaces a working transport with a new one | Keep existing SMTP dispatch; use `IResend` for new API calls |
| `Hangfire.Core` (direct reference) | `Hangfire.AspNetCore` depends on it; adding it directly causes version drift if you don't pin both in sync | Rely on transitive dependency |
| `Hangfire.Dashboard.Authorization` (NuGet) | Unnecessary — `IDashboardAuthorizationFilter` is in `Hangfire.Core`; role check is three lines of C# | Implement `IDashboardAuthorizationFilter` directly |
| A second/separate database for Hangfire | Operational overhead for no benefit at this scale | Use `UseSqlServerStorage` with the existing `DefaultConnection` string; Hangfire scopes itself to `[HangFire]` schema |

---

## Version Compatibility

| Package | Targets | .NET 10 Compatible | Notes |
|---------|---------|--------------------|-------|
| `Hangfire.AspNetCore` 1.8.23 | netstandard1.3, net451 | YES (computed via NuGet) | Updated February 5, 2026; works with any .NET Standard 1.3+ compatible runtime |
| `Hangfire.SqlServer` 1.8.23 | netstandard1.3, net451 | YES (computed via NuGet) | Same release cycle as AspNetCore package; ships own bundled SqlClient |
| `Resend` 0.5.1 | net8.0 | YES (explicit net10.0 listed) | Released May 13, 2026; NuGet page lists net10.0 as supported target |

---

## Sources

- [NuGet: Hangfire.AspNetCore 1.8.23](https://www.nuget.org/packages/Hangfire.AspNetCore/) — version, .NET 10 compatibility confirmed (HIGH)
- [NuGet: Hangfire.SqlServer 1.8.23](https://www.nuget.org/packages/Hangfire.SqlServer/) — version, .NET 10 compatibility confirmed (HIGH)
- [NuGet: Resend 0.5.1](https://www.nuget.org/packages/Resend/) — version and net10.0 target confirmed (HIGH)
- [Hangfire ASP.NET Core docs](https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html) — package list, Program.cs configuration pattern (HIGH)
- [resend/resend-dotnet — IResend.cs](https://github.com/resend/resend-dotnet/blob/main/src/Resend/IResend.cs) — `EmailListAsync`, `EmailRetrieveAsync` methods confirmed (HIGH)
- [Resend List Emails API reference](https://resend.com/docs/api-reference/emails/list-emails) — no aggregate stats endpoint; per-email `last_event` only (HIGH)
- [End Point Dev: Razor email rendering in .NET 8](https://www.endpointdev.com/blog/2024/04/using-razor-templates-to-render-emails-dotnet/) — `HtmlRenderer` built-in approach, zero additional packages (MEDIUM)
- [Hangfire dashboard auth — Sahan Serasinghe](https://sahansera.dev/securing-hangfire-dashboard-with-endpoint-routing-auth-policy-aspnetcore/) — `IDashboardAuthorizationFilter` pattern with ASP.NET Core Identity (MEDIUM)

---

*Stack research for: Milestone 4 Email Notifications (Hangfire + Resend SDK + HTML Email)*
*Researched: 2026-06-25*
