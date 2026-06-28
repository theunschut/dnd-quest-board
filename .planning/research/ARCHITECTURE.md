# Architecture Research

**Domain:** Hangfire background jobs + HTML email system in ASP.NET Core 10 MVC clean architecture
**Researched:** 2026-06-25
**Confidence:** HIGH

---

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  EuphoriaInn.Service  (Presentation Layer)                          │
│                                                                     │
│  Controllers/          Views/Emails/          Hangfire              │
│  ┌─────────────┐       ┌──────────────────┐   ┌───────────────────┐ │
│  │ QuestCtrl   │       │ QuestFinalized   │   │ Dashboard /hangfire│ │
│  │ AdminCtrl   │       │ SessionReminder  │   │ (AdminOnly policy) │ │
│  │ (enqueue)   │       │ DigestReminder   │   └────────┬──────────┘ │
│  └──────┬──────┘       └──────────────────┘            │            │
│         │              (Razor → HTML string)            │            │
│  ┌──────▼──────────────────────────────────────────────▼──────────┐ │
│  │  Jobs/                                                          │ │
│  │  SessionReminderJob   (IServiceScopeFactory consumer)           │ │
│  └──────────────────────────────────┬───────────────────────────── │ │
│                                     │                              │ │
│  IBackgroundJobClient (enqueue)     │                              │ │
│  IRecurringJobManager (register)    │                              │ │
└─────────────────────────────────────│──────────────────────────────┘
                                      │ (resolves scoped services)
┌─────────────────────────────────────▼──────────────────────────────┐
│  EuphoriaInn.Domain  (Business Logic Layer)                        │
│                                                                     │
│  Interfaces/                    Services/                           │
│  ┌─────────────────────┐        ┌──────────────────────────────┐    │
│  │ IEmailService       │◄───────│ EmailService                 │    │
│  │  SendQuestFinalized │        │  (switches SMTP→Resend API)  │    │
│  │  SendReminder       │        └──────────────────────────────┘    │
│  │  SendDigestReminder │                                             │
│  │  GetEmailStats      │        ┌──────────────────────────────┐    │
│  └─────────────────────┘        │ QuestService                 │    │
│                                 │  FinalizeQuestAsync          │    │
│  ┌─────────────────────┐        │  GetQuestsDueTomorrow (new)  │    │
│  │ IEmailRenderService │        └──────────────────────────────┘    │
│  │  RenderToString<T>  │                                             │
│  └─────────────────────┘                                             │
│                                                                     │
│  Models/                                                            │
│  ┌─────────────┐  ┌──────────────────┐  ┌────────────────────┐     │
│  │ EmailSettings│  │ ReminderEmailData│  │ EmailStatsSummary  │     │
│  │ (+ ResendKey)│  │ (digest model)   │  │ (sent/bounced/fail)│     │
│  └─────────────┘  └──────────────────┘  └────────────────────┘     │
└────────────────────────────────────────────────────────────────────┘
                                      │
┌─────────────────────────────────────▼──────────────────────────────┐
│  EuphoriaInn.Repository  (Data Layer)                              │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  QuestRepository.GetQuestsDueTomorrowAsync() (new method)    │   │
│  │  Returns: Quest[] with PlayerSignups, DungeonMaster included │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  SQL Server (same DB — Hangfire adds its own [HangFire] schema)     │
└────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Layer | New or Modified |
|-----------|----------------|-------|-----------------|
| `SessionReminderJob` | Recurring job: scan quests due tomorrow, group by player for digest, call `IEmailService` | Service | NEW |
| `IEmailService` (expanded) | Contract for all email operations including reminder, digest, stats | Domain | MODIFIED |
| `EmailService` (replaced) | Send HTML emails via Resend SDK; fetch stats from Resend API | Domain | REPLACED |
| `IEmailRenderService` | Render a Razor view name + model to an HTML string | Domain | NEW (interface) |
| `RazorEmailRenderService` | Implement `IEmailRenderService` using `IRazorViewEngine` | Service | NEW (impl) |
| `IQuestService.GetQuestsDueTomorrowAsync` | Return finalized quests with `FinalizedDate.Date == tomorrow` | Domain | NEW method |
| `IQuestRepository.GetQuestsDueTomorrowAsync` | EF Core query for tomorrow's finalized quests | Domain interface | NEW method |
| `QuestRepository.GetQuestsDueTomorrowAsync` | EF Core implementation with eager-loaded signups + DM | Repository | NEW method |
| `EmailSettings` | Add `ResendApiKey` property | Domain | MODIFIED |
| `EmailStatsSummary` | Model for admin stats (sent, delivered, bounced, failed counts) | Domain | NEW |
| Hangfire Dashboard | `/hangfire` endpoint, admin-only `IDashboardAuthorizationFilter` | Service | NEW |
| Email Razor templates | `Views/Emails/QuestFinalized.cshtml`, `SessionReminder.cshtml`, `DigestReminder.cshtml` | Service | NEW |
| Admin stats view | `Views/Admin/EmailStats.cshtml` — calls `IEmailService.GetEmailStatsAsync` | Service | NEW |

---

## Layer-by-Layer Integration Analysis

### What goes in Repository

**Only:** The new query method for tomorrow's quests.

```
EuphoriaInn.Repository/
  Interfaces → IQuestRepository (existing) gains:
    Task<IList<QuestEntity>> GetQuestsDueTomorrowAsync(CancellationToken token = default);

  Repositories → QuestRepository gains the implementation:
    - WHERE IsFinalized = true
    - AND CAST(FinalizedDate AS DATE) = CAST(DATEADD(day, 1, GETUTCDATE()) AS DATE)
    - Include PlayerSignups → Player (Email, Name)
    - Include DungeonMaster
```

Hangfire's SQL Server storage schema is **NOT** managed through EF Core migrations. Hangfire creates its own `[HangFire]` schema tables (`[HangFire].[Job]`, `[HangFire].[State]`, etc.) via `PrepareSchemaIfNecessary = true` (the default), which runs on first startup. This requires no EF migration and does not interfere with the application schema.

### What goes in Domain

**Interfaces (public contracts):**

```
EuphoriaInn.Domain/Interfaces/
  IEmailService.cs         ← MODIFIED: add SendSessionReminderAsync, SendDigestReminderAsync,
                                        GetEmailStatsAsync
  IEmailRenderService.cs   ← NEW: Task<string> RenderAsync<TModel>(string viewName, TModel model)
  IQuestRepository.cs      ← MODIFIED: add GetQuestsDueTomorrowAsync
  IQuestService.cs         ← MODIFIED: add GetQuestsDueTomorrowAsync
```

**Models (data contracts):**

```
EuphoriaInn.Domain/Models/
  EmailSettings.cs         ← MODIFIED: add ResendApiKey, ResendAudienceId (optional)
  EmailStatsSummary.cs     ← NEW: record { int Sent; int Delivered; int Bounced; int Failed; }
  ReminderQuestData.cs     ← NEW: POCO for passing quest data to reminder email templates
```

**Services (business logic — internal implementations):**

```
EuphoriaInn.Domain/Services/
  EmailService.cs          ← REPLACED: now uses IResend (Resend SDK) instead of SmtpClient
                              Calls IEmailRenderService to get HTML body
                              Implements GetEmailStatsAsync by calling Resend list/retrieve endpoints
```

The `EmailService` constructor changes from `SmtpClient` to `IResend` + `IEmailRenderService`. Both `IResend` and `IEmailRenderService` are injected — Domain does not reference Resend SDK directly. See integration decision below.

**Digest batching logic lives in Domain.** `SessionReminderJob` (Service layer) calls `IQuestService.GetQuestsDueTomorrowAsync()` to get all quests. The grouping by player — "player X appears in quest A and quest B, send one email not two" — is a business rule and belongs in Domain (specifically inside the `SendSessionReminderAsync` overload that accepts a list of quests for one player, or inside the job itself before delegating to the service).

Recommended: job groups by `player.Email` in memory, then calls `IEmailService.SendDigestReminderAsync(player, quests[])`. The batching decision is made in the job (Service layer) because it requires aggregating results from a repository query — pure orchestration, not business logic.

### What goes in Service

**Hangfire job class:**

```
EuphoriaInn.Service/Jobs/
  SessionReminderJob.cs    ← NEW
```

`SessionReminderJob` takes `IServiceScopeFactory` in its constructor (registered as a singleton-safe dependency). Each execution creates its own scope to resolve `IQuestService` and `IEmailService`. This is mandatory because both services are registered as `Scoped` and Hangfire resolves job classes from the root container — direct `Scoped` injection would result in a captured/stale scope.

**Razor email render service (implementation):**

```
EuphoriaInn.Service/Services/
  RazorEmailRenderService.cs  ← NEW: implements IEmailRenderService
```

`RazorEmailRenderService` depends on `IRazorViewEngine`, `ITempDataProvider`, `IServiceScopeFactory`. It creates a synthetic `ActionContext` with an empty `HttpContext`, locates the view by name, renders to a `StringWriter`, and returns the HTML string. This lives in Service because it depends on `IRazorViewEngine` — an ASP.NET Core MVC type — which must not appear in Domain.

**Razor email template views:**

```
EuphoriaInn.Service/Views/Emails/
  QuestFinalized.cshtml       ← NEW (replaces plain-text body)
  SessionReminder.cshtml      ← NEW
  DigestReminder.cshtml       ← NEW (multiple quests listed)
  _EmailLayout.cshtml         ← NEW (shared HTML shell: header, footer, D&D styling)
```

Email template ViewModels (small, focused):

```
EuphoriaInn.Service/ViewModels/EmailViewModels/
  QuestFinalizedEmailViewModel.cs
  SessionReminderEmailViewModel.cs
  DigestReminderEmailViewModel.cs
```

These ViewModels are in Service (not Domain) because they feed Razor views — a presentation-layer concern. They are mapped from Domain models inside `EmailService.SendXxxAsync()` methods (or passed directly since `EmailService` is in Domain and will receive the domain models — see design decision below).

**Hangfire Dashboard authorization:**

```
EuphoriaInn.Service/Authorization/
  HangfireAdminAuthFilter.cs  ← NEW: IDashboardAuthorizationFilter
```

```csharp
public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole("Admin");
    }
}
```

**Program.cs additions:**

1. `services.AddHangfire(...)` with `UseSqlServerStorage` pointing at `DefaultConnection`
2. `services.AddHangfireServer()` — registers background server as `IHostedService`
3. `services.AddScoped<IEmailRenderService, RazorEmailRenderService>()`
4. `services.AddResend(o => o.ApiToken = config["EmailSettings:ResendApiKey"]!)`
5. `app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new HangfireAdminAuthFilter()] })`
6. Register `SessionReminderJob` recurring job after `app.Build()`

**Admin stats controller action:**

The existing `AdminController` gains a new `EmailStats` action that calls `IEmailService.GetEmailStatsAsync()` and returns a view. No new controller needed.

---

## Design Decisions with Rationale

### Decision 1: EmailService stays in Domain, IEmailRenderService is in Domain (interface), RazorEmailRenderService is in Service (implementation)

**Problem:** `EmailService` is an internal Domain service. It needs to produce HTML email bodies. Razor rendering requires `IRazorViewEngine`, which is an `Microsoft.AspNetCore.Mvc.Razor` type — allowed only in Service.

**Solution:** Domain defines `IEmailRenderService` (a pure C# interface with no MVC types). Service provides `RazorEmailRenderService` implementing it. `EmailService` constructor receives `IEmailRenderService` via DI — Domain calls it without ever knowing about Razor. Layer dependency rules remain intact.

**Alternative rejected:** Put `EmailService` in Service. This would require moving `IEmailService` to Service too, breaking the existing injection pattern throughout Domain (e.g., `QuestService` calls `IEmailService` directly — it would need to take a Service-layer dependency, inverting the dependency direction).

### Decision 2: Resend SDK (IResend) is injected into EmailService — not called from Domain directly

**Problem:** Domain must not reference the `Resend` NuGet package because it would introduce a third-party assembly into the Domain project.

**Resolution:** `IResend` is registered in DI from Program.cs (Service layer). Domain's `EmailService` declares `IResend` in its constructor — `IResend` is a pure interface from the `Resend` package. Because the `Resend` package is added as a dependency to the **Domain** project (one assembly reference), this is acceptable given the project's pragmatic constraints: the alternative (wrapping `IResend` in yet another Domain interface) adds a layer with no benefit at this scale.

**If stricter isolation is needed:** Define `IResendEmailClient` in Domain, implement it as `ResendEmailClientAdapter` in Service, inject that instead. The current milestone does not require this level of isolation.

### Decision 3: Hangfire uses the same SQL Server database as EF Core

**Problem:** The project uses one `DefaultConnection` connection string, one database. Adding a separate Hangfire database would require additional Docker configuration and operational burden.

**Solution:** Pass `DefaultConnection` to `UseSqlServerStorage`. Hangfire creates its own `[HangFire]` schema — completely separate from the EF Core tables (which use `[dbo]`). No migration conflict. No EF awareness of Hangfire tables.

**Pitfall:** Do NOT add Hangfire EF Core entity types to `QuestBoardContext`. Hangfire manages its own schema independently.

### Decision 4: Digest batching is orchestrated in SessionReminderJob, not inside IEmailService

**Rationale:** Digest batching requires: (1) load all tomorrow's quests from the repository, (2) group by player email in memory, (3) for single-quest players call `SendSessionReminderAsync`, (4) for multi-quest players call `SendDigestReminderAsync`. Steps 1-2 are orchestration logic that depends on a repository result — appropriate for the job class (Service layer). The email services (Domain) receive pre-grouped data and do not know about batching.

### Decision 5: Email template ViewModels are thin POCOs in Service/ViewModels/EmailViewModels/

**Rationale:** Domain models already contain all needed data (Quest, PlayerSignup, User). The EmailViewModels are flattening projections for Razor template consumption — a presentation concern. They do not contain business logic. AutoMapper is not used here; the job maps domain objects to email ViewModels inline before calling `IEmailService` methods.

---

## Data Flow Diagrams

### Flow 1: Recurring daily reminder (automated)

```
[Hangfire scheduler — daily at 08:00]
    │
    ▼
SessionReminderJob.ExecuteAsync()
    creates IServiceScope
    │
    ▼
IQuestService.GetQuestsDueTomorrowAsync()
    │
    ▼
IQuestRepository.GetQuestsDueTomorrowAsync()
    EF Core query: IsFinalized=true AND FinalizedDate.Date = tomorrow
    includes: PlayerSignups.Player, DungeonMaster
    │
    ▼
[In-memory grouping by player email]
    player → list of Quest
    │
    ├── single quest → IEmailService.SendSessionReminderAsync(player, quest)
    │       │
    │       ▼
    │   IEmailRenderService.RenderAsync<SessionReminderEmailViewModel>
    │       ("Emails/SessionReminder", viewModel)
    │       │
    │       ▼
    │   IRazorViewEngine locates Views/Emails/SessionReminder.cshtml
    │   Renders to string
    │       │
    │       ▼
    │   IResend.EmailSendAsync(EmailMessage { HtmlBody = renderedHtml })
    │       → Resend API → player inbox
    │
    └── multiple quests → IEmailService.SendDigestReminderAsync(player, quests[])
            → same render + send flow, using DigestReminder.cshtml
```

### Flow 2: DM manual reminder trigger

```
DM clicks "Send Reminder" on Quest/Manage page
    │ POST /Quest/SendReminder/{id}
    ▼
QuestController.SendReminder(int id)
    [Authorize(Policy = "DungeonMasterOnly")]
    │
    ▼
IBackgroundJobClient.Enqueue<SessionReminderJob>(
    job => job.SendReminderForQuestAsync(id))
    │ (non-blocking — returns immediately to user)
    ▼
HTTP redirect back to Quest/Manage/{id}
    │
    [Background — Hangfire executes within seconds]
    ▼
SessionReminderJob.SendReminderForQuestAsync(questId)
    creates IServiceScope
    │
    ▼
IQuestService.GetQuestWithManageDetailsAsync(questId)
    selects only IsSelected=true players
    │
    ▼
IEmailService.SendSessionReminderAsync(player, quest)
    per selected player — no digest (single quest context)
```

### Flow 3: HTML email rendering (shared sub-flow)

```
IEmailService.SendXxxAsync(...)
    │
    ▼
builds EmailViewModel (POCO, inline mapping from domain model)
    │
    ▼
IEmailRenderService.RenderAsync<TViewModel>("Emails/ViewName", viewModel)
    │
    ▼
RazorEmailRenderService.RenderAsync<T>(viewName, model)
    creates ActionContext with synthetic HttpContext (no real request)
    calls IRazorViewEngine.FindView(actionContext, viewName, isMainPage: false)
    creates ViewContext with StringWriter
    calls view.RenderAsync(viewContext)
    returns writer.ToString()
    │
    ▼
HTML string passed to IResend.EmailSendAsync as HtmlBody
```

### Flow 4: Admin email stats

```
Admin navigates to /Admin/EmailStats
    │
    ▼
AdminController.EmailStats()
    [Authorize(Policy = "AdminOnly")]
    │
    ▼
IEmailService.GetEmailStatsAsync(DateTime from, DateTime to)
    │
    ▼
IResend.EmailsList() — paginated list of sent emails
    iterates, counts by last_event field:
      "delivered" → Delivered
      "bounced"   → Bounced
      "failed"    → Failed
    │
    ▼
EmailStatsSummary { Sent, Delivered, Bounced, Failed }
    │
    ▼
AdminController maps to EmailStatsViewModel
    returns View("EmailStats", viewModel)
```

Note: Resend's `/emails` list endpoint returns individual email records with `last_event` status — no aggregate endpoint exists. The stats query loops over pages, grouping by status. For the small email volume (100/day limit), this is acceptable without caching.

---

## Recommended Project Structure (new files only)

```
EuphoriaInn.Domain/
  Interfaces/
    IEmailService.cs              ← MODIFIED (add 3 new method signatures)
    IEmailRenderService.cs        ← NEW
    IQuestRepository.cs           ← MODIFIED (add GetQuestsDueTomorrowAsync)
    IQuestService.cs              ← MODIFIED (add GetQuestsDueTomorrowAsync)
  Models/
    EmailSettings.cs              ← MODIFIED (add ResendApiKey)
    EmailStatsSummary.cs          ← NEW
  Services/
    EmailService.cs               ← REPLACED (SMTP → Resend + render service)

EuphoriaInn.Repository/
  Repositories/
    QuestRepository.cs            ← MODIFIED (add GetQuestsDueTomorrowAsync)

EuphoriaInn.Service/
  Authorization/
    HangfireAdminAuthFilter.cs    ← NEW
  Jobs/
    SessionReminderJob.cs         ← NEW
  Services/
    RazorEmailRenderService.cs    ← NEW
  ViewModels/
    EmailViewModels/
      QuestFinalizedEmailViewModel.cs    ← NEW
      SessionReminderEmailViewModel.cs   ← NEW
      DigestReminderEmailViewModel.cs    ← NEW
  Views/
    Emails/
      _EmailLayout.cshtml               ← NEW (HTML shell, D&D styling)
      QuestFinalized.cshtml             ← NEW
      SessionReminder.cshtml            ← NEW
      DigestReminder.cshtml             ← NEW
    Admin/
      EmailStats.cshtml                 ← NEW
  Program.cs                           ← MODIFIED (Hangfire setup, Resend DI, job registration)

EuphoriaInn.Service/
  EuphoriaInn.Service.csproj            ← MODIFIED (add Hangfire.AspNetCore, Hangfire.SqlServer)
EuphoriaInn.Domain/
  EuphoriaInn.Domain.csproj             ← MODIFIED (add Resend package)
```

---

## Architectural Patterns

### Pattern 1: Hangfire job with scoped services via IServiceScopeFactory

**What:** Hangfire resolves job classes from the root DI container. Scoped services (`IQuestService`, `IEmailService`, `QuestBoardContext`) cannot be constructor-injected directly into a job class registered as singleton (which Hangfire treats job classes as). The correct pattern is to inject `IServiceScopeFactory` (singleton-safe) and create a scope per job execution.

**When to use:** Every Hangfire job class that needs scoped services (any EF Core / domain service access).

**Example:**

```csharp
// EuphoriaInn.Service/Jobs/SessionReminderJob.cs
public class SessionReminderJob(IServiceScopeFactory scopeFactory)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [300, 600, 1800])]
    public async Task ExecuteDailyReminderAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questService = scope.ServiceProvider.GetRequiredService<IQuestService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var quests = await questService.GetQuestsDueTomorrowAsync();

        // Group confirmed players across all tomorrow's quests
        var playerQuestGroups = quests
            .SelectMany(q => q.PlayerSignups
                .Where(ps => ps.IsSelected && !string.IsNullOrEmpty(ps.Player.Email))
                .Select(ps => (Player: ps.Player, Quest: q)))
            .GroupBy(x => x.Player.Email);

        foreach (var group in playerQuestGroups)
        {
            var player = group.First().Player;
            var playerQuests = group.Select(x => x.Quest).ToList();

            if (playerQuests.Count == 1)
                await emailService.SendSessionReminderAsync(player, playerQuests[0]);
            else
                await emailService.SendDigestReminderAsync(player, playerQuests);
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendReminderForQuestAsync(int questId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questService = scope.ServiceProvider.GetRequiredService<IQuestService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var quest = await questService.GetQuestWithManageDetailsAsync(questId);
        if (quest == null) return;

        var confirmedPlayers = quest.PlayerSignups
            .Where(ps => ps.IsSelected && !string.IsNullOrEmpty(ps.Player.Email));

        foreach (var signup in confirmedPlayers)
            await emailService.SendSessionReminderAsync(signup.Player, quest);
    }
}
```

**Trade-offs:** Slightly more verbose than direct injection. The `CreateAsyncScope()` call is cheap and ensures proper disposal of `DbContext` after each job run — essential for correctness.

### Pattern 2: IRazorViewEngine render-to-string (no real HTTP context required)

**What:** Render a `.cshtml` file to an HTML string by constructing a synthetic `ActionContext` and `ViewContext`, bypassing the request pipeline. The view engine resolves the file from `Views/Emails/` using standard view location conventions.

**When to use:** Generating HTML for emails, PDFs, or any out-of-band rendering that is not a direct response to an HTTP request.

**Implementation:**

```csharp
// EuphoriaInn.Service/Services/RazorEmailRenderService.cs
public class RazorEmailRenderService(
    IRazorViewEngine viewEngine,
    ITempDataProvider tempDataProvider,
    IServiceScopeFactory scopeFactory) : IEmailRenderService
{
    public async Task<string> RenderAsync<TModel>(string viewName, TModel model)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var httpContext = new DefaultHttpContext
            { RequestServices = scope.ServiceProvider };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var viewResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);
        if (!viewResult.Success)
            throw new InvalidOperationException($"Email view '{viewName}' not found.");

        var viewData = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary()) { Model = model };

        await using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            new TempDataDictionary(httpContext, tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
```

**Razor view location:** `Views/Emails/SessionReminder.cshtml` is found by the default view engine convention `Views/{viewName}.cshtml` when `viewName` is passed as `"Emails/SessionReminder"`. The email layout is set in `_ViewStart.cshtml` **only for the Emails folder** — or each template sets `@{ Layout = "~/Views/Emails/_EmailLayout.cshtml"; }` explicitly (preferred to avoid affecting the global `_ViewStart`).

**Important:** Email templates must NOT inherit the main site `_Layout.cshtml`. Each email `.cshtml` file sets its own layout explicitly:

```cshtml
@* Views/Emails/SessionReminder.cshtml *@
@model EuphoriaInn.Service.ViewModels.EmailViewModels.SessionReminderEmailViewModel
@{ Layout = "~/Views/Emails/_EmailLayout.cshtml"; }

<h2>Reminder: @Model.QuestTitle</h2>
...
```

### Pattern 3: Hangfire dashboard with role-based authorization

**What:** The Hangfire dashboard is exposed at `/hangfire`. Access is restricted to the `Admin` role using a custom `IDashboardAuthorizationFilter`. The filter reads `HttpContext.User` from the dashboard context.

**When to use:** Any time the Hangfire dashboard is added to an app with existing ASP.NET Core Identity authentication.

**Registration:**

```csharp
// Program.cs — after app.UseAuthentication(); app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthFilter()],
    DashboardTitle = "Quest Board — Background Jobs"
});
```

**Filter:**

```csharp
// EuphoriaInn.Service/Authorization/HangfireAdminAuthFilter.cs
public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole("Admin");
    }
}
```

**Ordering matters:** `app.UseHangfireDashboard` must come AFTER `app.UseAuthentication()` and `app.UseAuthorization()` so `HttpContext.User` is populated when the filter runs.

### Pattern 4: Recurring job registration at startup

**What:** The daily reminder job is registered as a recurring Hangfire job using `IRecurringJobManager`. Registration happens after `app.Build()` (not in services) so the Hangfire storage is already initialized.

```csharp
// Program.cs — after app.Run() setup, before app.Run()
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<SessionReminderJob>(
        "daily-session-reminder",
        job => job.ExecuteDailyReminderAsync(),
        "0 8 * * *",  // 08:00 UTC daily
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
```

**Why not `RecurringJob.AddOrUpdate` static API:** The static API uses `GlobalConfiguration` which is less testable. The `IRecurringJobManager` interface is DI-injectable and follows the project's DI-first conventions.

---

## Integration Points

### External Services

| Service | Integration Pattern | Layer | Notes |
|---------|---------------------|-------|-------|
| Resend API (send) | `IResend` from `Resend` NuGet package via DI | Domain (uses interface) | API key from `EmailSettings:ResendApiKey` env var |
| Resend API (stats) | `IResend.EmailsList()` + iterate `last_event` field | Domain (`EmailService`) | No aggregate endpoint; must page through sent emails |
| Hangfire SQL storage | `services.AddHangfire(...).UseSqlServerStorage(connStr)` | Service (Program.cs) | Shares `DefaultConnection`; creates `[HangFire]` schema automatically |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Job → Domain services | `IServiceScopeFactory` → create scope → resolve | Mandatory for scoped services in Hangfire jobs |
| EmailService → Razor rendering | `IEmailRenderService.RenderAsync<T>` (Domain interface) | Keeps MVC types out of Domain |
| Controller → Hangfire | `IBackgroundJobClient.Enqueue<SessionReminderJob>(...)` | Fire-and-forget; controller does not await job completion |
| Domain → Resend SDK | `IResend` constructor injection | Domain.csproj references Resend package |
| Admin stats → Resend list API | `IResend.EmailsList()` | Per-page iteration; not cached at this scale |

### Program.cs Registration Order (complete additions)

```csharp
// --- After existing builder.Services.AddControllersWithViews() ---

// Hangfire
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
builder.Services.AddHangfireServer();

// Resend email API
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["EmailSettings:ResendApiKey"] ?? string.Empty;
});
builder.Services.AddTransient<IResend, ResendClient>();

// Razor email renderer
builder.Services.AddScoped<IEmailRenderService, RazorEmailRenderService>();

// --- In middleware pipeline, after app.UseAuthorization() ---

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthFilter()],
    DashboardTitle = "Quest Board — Background Jobs"
});

// --- After existing startup tasks (migrations, seed) ---

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<SessionReminderJob>(
        "daily-session-reminder",
        job => job.ExecuteDailyReminderAsync(),
        Cron.Daily(8),  // 08:00 UTC
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
```

---

## Build Order

The features have two independent tracks that merge at the email send step. Recommended sequence:

| Step | Deliverable | Depends On | Notes |
|------|-------------|------------|-------|
| 1 | `IEmailRenderService` interface + `RazorEmailRenderService` | Nothing (new types) | Foundation for all HTML emails |
| 2 | `_EmailLayout.cshtml` + `QuestFinalized.cshtml` template | Step 1 | First template proves rendering pipeline end-to-end |
| 3 | `EmailService` rewrite (SMTP → Resend + render) | Steps 1–2 | Replace existing, keep same interface contract |
| 4 | `QuestFinalized.cshtml` wired into existing finalize flow | Step 3 | Upgrade existing email; no new features yet |
| 5 | Hangfire: install packages, `AddHangfire`, `AddHangfireServer`, dashboard | Step 3 (shared DB) | Infrastructure; no jobs yet |
| 6 | `GetQuestsDueTomorrowAsync` in Repository + Domain interface | Step 5 (needs DB) | New query needed by reminder job |
| 7 | `SessionReminderJob` (both `ExecuteDailyReminderAsync` and `SendReminderForQuestAsync`) | Steps 3, 5, 6 | Core job logic; needs Hangfire + email + query |
| 8 | `SessionReminder.cshtml` + `DigestReminder.cshtml` templates | Steps 1–2, 7 | Templates needed for job to send anything |
| 9 | Recurring job registration at startup + DM manual trigger button | Steps 5, 7, 8 | Activates the automation |
| 10 | Admin email stats (`GetEmailStatsAsync` + `EmailStats.cshtml` view) | Step 3 (IResend already wired) | Independent of job track |

Steps 1–4 are the "upgrade existing email" track — they can be validated independently.
Steps 5–9 are the "Hangfire automation" track — they depend on Steps 1–4 for the email sending.
Step 10 is independent after Step 3.

---

## Anti-Patterns

### Anti-Pattern 1: Inject scoped services directly into Hangfire job constructors

**What people do:** Constructor-inject `IQuestService` or `IEmailService` directly into the job class, relying on Hangfire's DI integration.

**Why it's wrong:** Hangfire resolves job instances from the root `IServiceProvider`. Scoped services resolved from the root become effective singletons for the lifetime of the application — they are never disposed. `QuestBoardContext` holds a stale connection and stale change tracker state across all job executions. This causes incorrect query results, `ObjectDisposedException`, or silent data corruption on the second run.

**Do this instead:** Inject `IServiceScopeFactory` (which IS singleton-safe). Create `await using var scope = scopeFactory.CreateAsyncScope()` at the top of each job method. Resolve scoped services from `scope.ServiceProvider`.

### Anti-Pattern 2: Hangfire dashboard added before UseAuthentication

**What people do:** Mount `app.UseHangfireDashboard(...)` early in the pipeline, before `app.UseAuthentication()`.

**Why it's wrong:** `HttpContext.User` is not populated until `UseAuthentication` runs. `HangfireAdminAuthFilter.Authorize()` will see an unauthenticated user and return `false` for every request — even admins cannot access the dashboard.

**Do this instead:** `app.UseHangfireDashboard` must come after `app.UseAuthentication()` and `app.UseAuthorization()`.

### Anti-Pattern 3: Email templates inherit the site layout

**What people do:** Rely on `_ViewStart.cshtml` to set the layout for email templates — the email template is rendered with the full `_Layout.cshtml` (navigation bar, scripts, etc.).

**Why it's wrong:** The rendered HTML string includes the full site chrome (nav, Bootstrap scripts, etc.). The email client receives a 50 KB HTML document with JavaScript and server-relative static file URLs. Both are unusable in email.

**Do this instead:** Each email template explicitly sets its own layout: `@{ Layout = "~/Views/Emails/_EmailLayout.cshtml"; }`. The email layout contains only inline-compatible HTML — no `<script>` tags, no server-relative static file references, CSS inlined or from absolute CDN URLs that work in email clients.

### Anti-Pattern 4: Storing sent email IDs in QuestBoardContext for stats

**What people do:** Add an `EmailLog` EF Core entity to track every sent email's Resend ID, then query the log table for stats.

**Why it's wrong:** Unnecessary schema addition. Resend already stores sent email records and exposes them via the list API. Duplicate storage creates sync problems (what if an email was sent but the DB write failed?).

**Do this instead:** Query Resend's list API directly for stats. At 100 emails/day the pagination overhead is negligible. If the quota grows, add a lightweight caching layer (`IMemoryCache` with a 15-minute TTL) in `GetEmailStatsAsync`.

### Anti-Pattern 5: Registering recurring jobs in AddDomainServices()

**What people do:** Call `RecurringJob.AddOrUpdate(...)` inside the Domain's `AddDomainServices` extension method.

**Why it's wrong:** `RecurringJob.AddOrUpdate` is a static API that depends on Hangfire storage being initialized. At service registration time, `AddHangfire` may not have run yet (ordering dependency). Also, it introduces a Hangfire dependency into Domain's extension method.

**Do this instead:** Register recurring jobs in `Program.cs` after `app.Build()`, using the `IRecurringJobManager` interface resolved from a scope. This guarantees Hangfire storage is ready and keeps the Domain extension method free of Hangfire knowledge.

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (small group, 100 emails/day) | Single Hangfire server co-located with web process; SQL Server storage; no caching on stats API |
| Medium (multiple DMs, 500+ emails/day) | Add `IMemoryCache` TTL on `GetEmailStatsAsync`; consider dedicated Hangfire worker process |
| Large (multi-tenant) | Separate Hangfire database; Redis storage; multiple worker processes; Resend webhooks instead of polling for stats |

---

## Sources

- Hangfire ASP.NET Core integration: Context7 `/hangfireio/hangfire.documentation` — HIGH confidence
- Hangfire dashboard authorization (`IDashboardAuthorizationFilter`): `https://docs.hangfire.io/en/latest/configuration/using-dashboard.html` — HIGH confidence
- Hangfire SQL Server storage options: Context7 `/hangfireio/hangfire.documentation` — HIGH confidence
- Hangfire DI scoped services / `IServiceScopeFactory` pattern: `https://www.codegenes.net/blog/hangfire-dependency-injection-lifetime-scope/` — MEDIUM confidence (verified against Hangfire docs)
- Resend .NET SDK (`IResend`, `ResendClient`): `https://resend.com/docs/send-with-dotnet` + `https://github.com/resend/resend-dotnet` — HIGH confidence
- Resend list emails endpoint (`last_event` field): `https://resend.com/docs/api-reference/emails/list-emails` — HIGH confidence
- Razor render-to-string pattern (`IRazorViewEngine`, synthetic `ActionContext`): `https://www.endpointdev.com/blog/2024/04/using-razor-templates-to-render-emails-dotnet/` — MEDIUM confidence (pattern is well-established; verified against ASP.NET Core source)

---

*Architecture research for: Hangfire + enhanced email system, ASP.NET Core 10 MVC clean architecture*
*Researched: 2026-06-25*
