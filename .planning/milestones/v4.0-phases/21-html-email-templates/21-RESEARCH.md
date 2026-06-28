# Phase 21: HTML Email Templates - Research

**Researched:** 2026-06-26
**Domain:** ASP.NET Core 10 HtmlRenderer, Razor Components, Hangfire email jobs, EF Core migration
**Confidence:** HIGH

---

## Summary

Phase 21 implements a render pipeline that converts Razor component templates into HTML strings using `HtmlRenderer` (part of the ASP.NET Core 10 shared framework — no additional NuGet package required) and routes all outbound emails through Hangfire fire-and-forget jobs. The existing `EmailService` SMTP delivery path is unchanged; only the call sites and body content change.

The primary technical risk is correctly threading `HtmlRenderer.Dispatcher.InvokeAsync` in the Hangfire job context, where no HTTP request scope exists. The established `IServiceScopeFactory` pattern from Phase 20 (`SmokeTestJob.cs`) solves the scope problem. `HtmlRenderer` itself must receive the `IServiceProvider` and `ILoggerFactory` from inside the job's scope — not from a singleton.

The deduplication guard (`FinalizedEmailSentForDate`) requires a straightforward EF Core migration adding a nullable `DateTime?` column. AutoMapper convention handles it — no explicit `.ForMember()` mapping needed for a simple property-to-property match with the same name.

The existing `QuestService` currently injects `IEmailService` directly and calls it synchronously in `FinalizeQuestAsync` and `UpdateQuestPropertiesWithNotificationsAsync`. Phase 21 removes those direct calls and replaces them with `BackgroundJob.Enqueue<TJob>(...)`. The existing unit tests for `QuestService` assert on `IEmailService` calls that will no longer happen — those tests must be updated to verify Hangfire enqueue instead.

**Primary recommendation:** Follow the `SmokeTestJob.cs` pattern exactly for all email jobs. Create `HtmlRenderer` inside `Dispatcher.InvokeAsync` using the scoped `IServiceProvider`. Register `IEmailRenderService` / `RazorEmailRenderService` in `ServiceExtensions.AddDomainServices` (interface in Domain, implementation in Service — the pattern established for the project's cross-layer services is not applicable here since the implementation needs Razor; add a new `AddEmailRenderService` extension in Service's `Program.cs` instead).

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** All HTML emails use the D&D quest-card aesthetic: full-background parchment scroll poster image, Cinzel font for headings, dark brown text (`#1a0f08`), gold accents (`#FFD700` / `#ffc107`), parchment body background (`#F4E4BC`).
- **D-02:** Poster image used as full-background (`background-image`, `background-size: cover`). Outlook fallback `background-color` applied. Outlook degradation is acceptable for this group.
- **D-03:** Gold CR badge (top-left) in finalization and reminder emails only.
- **D-04:** Wax seal image from `/images/Wax Seals/` as decorative element.
- **D-05:** Cinzel loaded from Google Fonts. Outlook falls back to `serif` — acceptable.
- **D-06:** All three email types upgraded to HTML in Phase 21: QuestFinalized, QuestDateChanged, SessionReminder.
- **D-07:** `SmokeTestJob.cs` removed in Phase 21.
- **D-08:** All outbound emails sent through Hangfire fire-and-forget jobs. `QuestService` enqueues jobs instead of calling `IEmailService` directly.
- **D-09:** Every Hangfire email job uses `IServiceScopeFactory` + `CreateAsyncScope()` inside the job method body — never constructor injection of scoped services.
- **D-10:** Pipeline inside each job: render → check dedup → send.
- **D-11:** `IEmailService` gains a generic `SendAsync(string toEmail, string subject, string htmlBody)`. Typed methods: Claude's discretion on removal vs deprecated wrappers.
- **D-12:** `IEmailRenderService` defined in Domain (interface only). `RazorEmailRenderService` implementation in Service (Razor/component support lives there). Uses `HtmlRenderer` — never `IRazorViewEngine`.
- **D-13:** `FinalizedEmailSentForDate` (`DateTime?`) column added to `Quest` entity via EF Core migration. Dedup check: if `FinalizedEmailSentForDate == finalizedDate`, skip send. After send, set `FinalizedEmailSentForDate = finalizedDate`.
- **D-14:** Email Razor components live in `EuphoriaInn.Service/Components/Emails/`: `_EmailLayout.razor`, `QuestFinalized.razor`, `QuestDateChanged.razor`, `SessionReminder.razor`.
- **D-15:** `SessionReminder.razor` parameters fully locked (see CONTEXT.md D-15 for full list).

### Claude's Discretion

- Exact wax seal image selection (fixed Crown Seal or randomized per questId hash)
- Whether typed email methods removed or kept as deprecated wrappers
- Exact CSS inline styles for email HTML
- Worker count and Hangfire job naming conventions

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EMAIL-01 | `IEmailRenderService` backed by `HtmlRenderer` renders Razor component templates to HTML strings; all outbound emails use this service | HtmlRenderer API confirmed in ASP.NET Core 10 shared framework; `Dispatcher.InvokeAsync` pattern documented below |
| EMAIL-02 | Quest-finalization email sent as styled HTML; duplicate guard via `FinalizedEmailSentForDate` if re-finalized with same date | EF Core migration pattern confirmed; AutoMapper convention handles new nullable column without explicit mapping |
| EMAIL-03 | Single-quest session reminder renders as styled HTML using a dedicated Razor component template | `SessionReminder.razor` parameters locked in CONTEXT.md D-15; component structure documented below |

</phase_requirements>

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| HTML rendering from Razor components | Service layer (`RazorEmailRenderService`) | — | `HtmlRenderer` requires the web SDK; Razor `.razor` files compile only in the Service project |
| Email render service interface | Domain layer (`IEmailRenderService`) | — | Interface in Domain follows project pattern; implementation detail stays in Service |
| SMTP delivery | Domain layer (`EmailService`) | — | Unchanged from prior phases; pure SMTP, no rendering concern |
| Deduplication guard state | Repository layer (`QuestEntity.FinalizedEmailSentForDate`) | Domain model (`Quest.FinalizedEmailSentForDate`) | Persisted state belongs in entity; domain model mirrors it for business logic |
| Job enqueueing | Service layer (`QuestService` via `BackgroundJob.Enqueue`) | — | `QuestService` owns quest lifecycle events; Hangfire is a service-layer concern |
| Hangfire job execution | Service layer (jobs in `EuphoriaInn.Service/Jobs/`) | — | Established in Phase 20; jobs resolve scoped services via `IServiceScopeFactory` |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.Components.Web` | 10.0.x (shared framework) | `HtmlRenderer` — renders Razor components to HTML strings | Part of ASP.NET Core shared framework; no NuGet install required in `Microsoft.NET.Sdk.Web` project |
| `Hangfire.AspNetCore` | 1.8.23 (already installed) | Fire-and-forget email jobs | Already wired in Phase 20 |
| EF Core + SQL Server | 10.0.9 (already installed) | EF migration for `FinalizedEmailSentForDate` | Project standard |

### No Additional NuGet Packages Required

`Microsoft.AspNetCore.Components.Web` is part of the ASP.NET Core 10 shared framework. It is available as a transitive reference through `Microsoft.AspNetCore.App` in any `Microsoft.NET.Sdk.Web` project.

[VERIFIED: `C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref\10.0.9\ref\net10.0\Microsoft.AspNetCore.Components.Web.dll` confirmed present]

The Service project (`Microsoft.NET.Sdk.Web`) supports `.razor` component files without switching to `Microsoft.NET.Sdk.Razor` — the Web SDK includes Razor compilation for both `.cshtml` and `.razor` files.

[CITED: https://learn.microsoft.com/en-us/aspnet/core/razor-pages/sdk?view=aspnetcore-10.0]

---

## Architecture Patterns

### System Architecture Diagram

```
QuestService.FinalizeQuestAsync()
        │
        │  BackgroundJob.Enqueue<QuestFinalizedEmailJob>(questId, date, signupIds)
        ▼
Hangfire SQL Server Queue
        │
        │  (async, after request completes)
        ▼
QuestFinalizedEmailJob.ExecuteAsync(questId, finalizedDate, playerEmails[])
        │
        ├─ IServiceScopeFactory.CreateAsyncScope()
        │        │
        │        ├─ scope → IQuestRepository.GetAsync(questId) [dedup check]
        │        │          if FinalizedEmailSentForDate == finalizedDate → STOP
        │        │
        │        ├─ scope → IEmailRenderService.RenderAsync<QuestFinalized>(params)
        │        │          │
        │        │          └─ HtmlRenderer.Dispatcher.InvokeAsync(...)
        │        │               └─ RenderComponentAsync<QuestFinalized>(ParameterView)
        │        │                    └─ returns HTML string
        │        │
        │        ├─ scope → IEmailService.SendAsync(toEmail, subject, html)
        │        │          └─ SmtpClient → Postfix → Resend relay (unchanged)
        │        │
        │        └─ scope → IQuestRepository.SetFinalizedEmailSentForDate(questId, date)
        │
        └─ (repeat per recipient)

QuestService.UpdateQuestPropertiesWithNotificationsAsync()
        │
        │  BackgroundJob.Enqueue<QuestDateChangedEmailJob>(questId, oldDate, newDate, playerEmails[])
        ▼
QuestDateChangedEmailJob → IEmailRenderService.RenderAsync<QuestDateChanged>(params)
                         → IEmailService.SendAsync(...)

SessionReminder.razor (Phase 22 consumes — template delivered, no job in Phase 21)
```

### Recommended Project Structure

```
EuphoriaInn.Domain/
├── Interfaces/
│   ├── IEmailService.cs          # ADD: SendAsync(toEmail, subject, htmlBody) method
│   └── IEmailRenderService.cs    # NEW: Task<string> RenderAsync<TComponent>(Dictionary<string, object?>) where TComponent : IComponent
│
EuphoriaInn.Service/
├── Components/
│   └── Emails/                   # NEW directory
│       ├── _EmailLayout.razor    # Shared wrapper: html/head/body/font-link
│       ├── QuestFinalized.razor  # Finalization email
│       ├── QuestDateChanged.razor# Date-changed email
│       └── SessionReminder.razor # Reminder template (Phase 22 uses this)
├── Jobs/
│   ├── SmokeTestJob.cs           # DELETE in Phase 21
│   ├── QuestFinalizedEmailJob.cs # NEW
│   └── QuestDateChangedEmailJob.cs # NEW
└── Services/
    └── RazorEmailRenderService.cs # NEW: IEmailRenderService implementation

EuphoriaInn.Repository/
└── Entities/
    └── QuestEntity.cs            # ADD: DateTime? FinalizedEmailSentForDate

EuphoriaInn.Domain/
└── Models/QuestBoard/
    └── Quest.cs                  # ADD: DateTime? FinalizedEmailSentForDate
```

---

### Pattern 1: HtmlRenderer Usage in a Hangfire Job Context

**What:** `HtmlRenderer` renders a Razor component to an HTML string inside `Dispatcher.InvokeAsync`. All rendering must happen within this dispatcher call. `HtmlRenderer` is created per-render (not singleton) using the `IServiceProvider` from the current DI scope.

**When to use:** Whenever generating an HTML email body from a Razor component template.

**Critical API detail:** `RenderComponentAsync` must be called inside `Dispatcher.InvokeAsync` — calling it outside throws. `HtmlRenderer` is `IAsyncDisposable` — always use `await using`.

[CITED: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-components-outside-of-aspnetcore?view=aspnetcore-10.0]

```csharp
// Source: Official Microsoft Learn docs (aspnetcore-10.0)
// RazorEmailRenderService.cs in EuphoriaInn.Service/Services/

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

public class RazorEmailRenderService(
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory) : IEmailRenderService
{
    public async Task<string> RenderAsync<TComponent>(
        Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var paramView = ParameterView.FromDictionary(parameters);
            var output = await htmlRenderer.RenderComponentAsync<TComponent>(paramView);
            return output.ToHtmlString();
        });
    }
}
```

**DI registration** (in `Program.cs` — NOT in `ServiceExtensions.AddDomainServices` because `RazorEmailRenderService` is in the Service project):

```csharp
// In Program.cs, after builder.Services.AddDomainServices(...)
builder.Services.AddScoped<IEmailRenderService, RazorEmailRenderService>();
```

**Why not in `ServiceExtensions`:** `ServiceExtensions.cs` lives in `EuphoriaInn.Domain` and cannot reference `RazorEmailRenderService` (which is in `EuphoriaInn.Service`). The interface `IEmailRenderService` is registered in Domain; the implementation is registered in `Program.cs`.

---

### Pattern 2: Hangfire Email Job with IServiceScopeFactory

**What:** Hangfire activates job classes outside any DI scope. Scoped services (`IEmailService`, `IEmailRenderService`, `DbContext`) must be resolved inside the job body via `IServiceScopeFactory`.

**Exact pattern from `SmokeTestJob.cs` (to replicate — then delete that file):**

[VERIFIED: Read from `EuphoriaInn.Service/Jobs/SmokeTestJob.cs`]

```csharp
// Source: SmokeTestJob.cs (established Phase 20 pattern)
// QuestFinalizedEmailJob.cs

public class QuestFinalizedEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestFinalizedEmailJob> logger)
{
    public async Task ExecuteAsync(
        int questId,
        DateTime finalizedDate,
        string[] recipientEmails,       // pre-computed by QuestService before enqueue
        string[] playerNames,
        string questTitle,
        string dmName,
        string questDescription,
        int challengeRating,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
        var renderService = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

        // Dedup check (D-13)
        var quest = await questRepository.GetAsync(questId, cancellationToken);
        if (quest?.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
        {
            logger.LogInformation("Finalized email already sent for quest {QuestId} on {Date}. Skipping.", questId, finalizedDate);
            return;
        }

        for (int i = 0; i < recipientEmails.Length; i++)
        {
            var html = await renderService.RenderAsync<QuestFinalized>(new Dictionary<string, object?>
            {
                { nameof(QuestFinalized.QuestTitle), questTitle },
                { nameof(QuestFinalized.DmName), dmName },
                { nameof(QuestFinalized.QuestDate), finalizedDate },
                { nameof(QuestFinalized.QuestDescription), questDescription },
                { nameof(QuestFinalized.ConfirmedPlayerNames), playerNames.ToList() },
                { nameof(QuestFinalized.QuestUrl), $"{emailSettings.AppUrl}/Quest/Details/{questId}" },
                { nameof(QuestFinalized.ChallengeRating), challengeRating },
                { nameof(QuestFinalized.AppUrl), emailSettings.AppUrl }
            });

            await emailService.SendAsync(
                recipientEmails[i],
                $"Your quest has been confirmed: {questTitle}",
                html);
        }

        // Mark dedup after all sends succeed
        await questRepository.SetFinalizedEmailSentForDateAsync(questId, finalizedDate, cancellationToken);
    }
}
```

**Important:** `IServiceScopeFactory` is a singleton — safe to inject via constructor. Only scoped/transient services must be resolved inside the scope.

---

### Pattern 3: Razor Component Email Template Structure

**What:** Minimal Razor components with `[Parameter]` properties. No `@page` directive (not routed). Layout handled via `_EmailLayout.razor` as a Razor component (not `@layout` — `@layout` is for Blazor pages with routing, not standalone `HtmlRenderer` rendering).

[VERIFIED: confirmed by GitHub issue #55068 — `@layout` directive is ignored by `HtmlRenderer`]

**Correct pattern — the layout is composed, not declared:**

```razor
@* QuestFinalized.razor — EuphoriaInn.Service/Components/Emails/ *@
@using EuphoriaInn.Service.Components.Emails

<_EmailLayout Subject="@($"Your quest has been confirmed: {QuestTitle}")"
              PreviewText="@($"Quest Confirmed — {QuestTitle} on {QuestDate:dddd, MMMM d}")">
    @* inner content renders inside ChildContent of _EmailLayout *@
    <table width="600" style="...poster background...">
        @* CR badge, title, description, CTA button, wax seal *@
    </table>
</_EmailLayout>

@code {
    [Parameter, EditorRequired] public string QuestTitle { get; set; } = "";
    [Parameter, EditorRequired] public string DmName { get; set; } = "";
    [Parameter, EditorRequired] public DateTime QuestDate { get; set; }
    [Parameter, EditorRequired] public string QuestDescription { get; set; } = "";
    [Parameter, EditorRequired] public IList<string> ConfirmedPlayerNames { get; set; } = [];
    [Parameter, EditorRequired] public string QuestUrl { get; set; } = "";
    [Parameter, EditorRequired] public int ChallengeRating { get; set; }
    [Parameter, EditorRequired] public string AppUrl { get; set; } = "";
}
```

**`_EmailLayout.razor` structure:**

```razor
@* _EmailLayout.razor — outputs a complete HTML document *@
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@Subject</title>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;700&display=swap" rel="stylesheet" />
</head>
<body style="margin:0;padding:0;background-color:#F4E4BC;">
    @* Gmail preview text hack *@
    @if (!string.IsNullOrEmpty(PreviewText))
    {
        <span style="display:none;font-size:1px;color:#F4E4BC;max-height:0;max-width:0;opacity:0;overflow:hidden;">@PreviewText</span>
    }
    @ChildContent
</body>
</html>

@code {
    [Parameter, EditorRequired] public string Subject { get; set; } = "";
    [Parameter] public string? PreviewText { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;
}
```

---

### Pattern 4: IEmailService.SendAsync Addition

**What:** Add a generic HTML email send method alongside existing typed methods. The typed methods become deprecated wrappers or are kept for backward compatibility with existing unit tests.

[VERIFIED: Read from `EuphoriaInn.Domain/Interfaces/IEmailService.cs` and `EmailService.cs`]

**Current interface:**
```csharp
public interface IEmailService
{
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
```

**Updated interface (D-11):**
```csharp
public interface IEmailService
{
    // New generic method — used by all Hangfire jobs
    Task SendAsync(string toEmail, string subject, string htmlBody);

    // Legacy typed methods — keep as deprecated wrappers to avoid breaking existing tests
    // Jobs no longer call these; they now call SendAsync with pre-rendered HTML
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
```

**`EmailService.SendAsync` implementation:**
```csharp
public async Task SendAsync(string toEmail, string subject, string htmlBody)
{
    try
    {
        using var client = CreateSmtpClient();
        if (client == null) return;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true   // key difference from legacy methods
        };
        mailMessage.To.Add(toEmail);
        await client.SendMailAsync(mailMessage);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
    }
}
```

---

### Pattern 5: QuestService Decoupling from IEmailService

**What:** Replace direct `emailService.Send*` calls with `BackgroundJob.Enqueue`. `QuestService` must inject `IBackgroundJobClient` (or use static `BackgroundJob.Enqueue`) instead of `IEmailService`.

[VERIFIED: Read from `EuphoriaInn.Domain/Services/QuestService.cs`]

**Current `FinalizeQuestAsync` sends email inline (must change):**
```csharp
// BEFORE — direct call (to be removed):
await emailService.SendQuestFinalizedEmailAsync(
    signup.Player.Email!, signup.Player.Name, quest.Title,
    quest.DungeonMaster?.Name ?? "Unknown DM", finalizedDate);

// AFTER — enqueue Hangfire job:
BackgroundJob.Enqueue<QuestFinalizedEmailJob>(j => j.ExecuteAsync(
    quest.Id,
    finalizedDate,
    recipientEmails,
    playerNames,
    quest.Title,
    quest.DungeonMaster?.Name ?? "Unknown DM",
    quest.Description,
    quest.ChallengeRating,
    CancellationToken.None));
```

**Hangfire serialization constraint:** All job parameters must be serializable by Hangfire's JSON serializer (Newtonsoft.Json). Primitive types, strings, arrays, and DateTime are safe. Do not pass domain objects (`Quest`, `PlayerSignup`) directly — extract primitives before enqueue.

**`QuestService` constructor change:** Remove `IEmailService emailService` parameter. Add `IBackgroundJobClient jobClient` if using instance-based enqueue, or rely on static `BackgroundJob.Enqueue` (both are established patterns; static is simpler and consistent with Phase 20's `Program.cs` startup enqueue).

**Unit test impact:** `QuestServiceTests.cs` currently asserts `_emailService.Received(2).SendQuestFinalizedEmailAsync(...)` — these assertions will fail after the decoupling. The test file must be updated to remove `IEmailService` mock and instead verify Hangfire enqueue behavior (e.g., using `IBackgroundJobClient` mock or accepting that the test scope narrows to non-email behavior).

---

### Pattern 6: EF Core Migration for FinalizedEmailSentForDate

**What:** Add nullable `DateTime?` column to `Quests` table.

[VERIFIED: Read from `EuphoriaInn.Repository/Entities/QuestEntity.cs` and `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs`]

**Entity change:**
```csharp
// QuestEntity.cs — add after IsFinalized:
public DateTime? FinalizedEmailSentForDate { get; set; }
```

**Domain model change:**
```csharp
// Quest.cs — add after IsFinalized:
public DateTime? FinalizedEmailSentForDate { get; set; }
```

**AutoMapper:** The `EntityProfile.cs` maps `QuestEntity → Quest` and `Quest → QuestEntity`. The existing mapping uses convention (same name, same type). `FinalizedEmailSentForDate` is a simple `DateTime?` property with identical names — AutoMapper convention handles it without any explicit `.ForMember()` call.

[VERIFIED: `EntityProfile.cs` has explicit maps only for navigation properties and enum conversions. Simple scalar properties are convention-mapped.]

**Migration command (run from `EuphoriaInn.Service/`):**
```bash
dotnet ef migrations add AddFinalizedEmailSentForDate --project ../EuphoriaInn.Repository
```

**Required repository method addition:** `QuestService` (or the job) needs to save `FinalizedEmailSentForDate` after send. The job must call a repository method to set this. Either:
- Add `SetFinalizedEmailSentForDateAsync(int questId, DateTime date, CancellationToken)` to `IQuestRepository` and `QuestRepository`, or
- Re-fetch the quest entity and update inline (less clean but avoids new repository method)

The former is cleaner and consistent with the existing repository pattern.

---

### Anti-Patterns to Avoid

- **Injecting `IEmailRenderService` via constructor into a Hangfire job.** `IEmailRenderService` is scoped — Hangfire activates jobs without a scope. Must use `IServiceScopeFactory`.
- **Using `@layout` directive in email Razor components.** `HtmlRenderer` ignores `@layout` (confirmed in GitHub issue #55068). Compose layout by nesting `<_EmailLayout>` as a component inside the template.
- **Using `IRazorViewEngine` instead of `HtmlRenderer`.** `IRazorViewEngine` throws `NullReferenceException` in background job contexts (no `HttpContext`). Locked decision D-12.
- **Passing domain objects as Hangfire job parameters.** Serialize primitives only. `Quest`, `PlayerSignup` objects cannot be round-tripped safely through Hangfire's JSON serializer.
- **Creating `HtmlRenderer` as a singleton.** `HtmlRenderer` is `IAsyncDisposable` and should be created per-render inside `await using`. It is NOT thread-safe for concurrent renders on the same instance.
- **Using `position: absolute` or `display: flex/grid` in email HTML.** Email clients strip or ignore these. Use `<table>`, `<td>`, `valign`, and `padding` instead (confirmed in UI-SPEC.md).
- **Using `<style>` blocks in email body.** Gmail strips `<style>` from `<head>` in forwarded emails. All styles must be inline `style=""` attributes (confirmed in UI-SPEC.md).
- **Using relative image URLs in emails.** Email clients cannot resolve relative paths. All `src` attributes must use absolute URLs with `AppUrl` prefix.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Razor-to-HTML rendering | Custom template string builder | `HtmlRenderer` | Already in framework; handles component lifecycle, cascading parameters, async rendering |
| Background job queue | `IHostedService` with `Channel<T>` | Hangfire (already installed) | Persists across restarts; retry, monitoring, dedup support |
| SMTP email delivery | New SmtpClient wrapper | Existing `EmailService.CreateSmtpClient()` | Already handles credentials check, SSL, error logging |
| EF Core migration | Raw SQL ALTER TABLE | `dotnet ef migrations add` | Auto-applied on startup via `context.Database.Migrate()` |

**Key insight:** `HtmlRenderer` is the framework's own solution for non-interactive Razor rendering. Using it avoids the `NullReferenceException` trap of `IRazorViewEngine` in background job contexts.

---

## Common Pitfalls

### Pitfall 1: @layout Ignored by HtmlRenderer

**What goes wrong:** Developer adds `@layout _EmailLayout` at top of `QuestFinalized.razor`. The rendered output omits the layout entirely — no `<html>`, no `<head>`, no font link.

**Why it happens:** `@layout` is a Blazor routing feature. `HtmlRenderer` renders components directly without the Blazor router and ignores layout directives.

**How to avoid:** Compose layout by rendering `_EmailLayout` as a top-level component that wraps the content via `ChildContent` render fragment. The job renders `QuestFinalized` which internally uses `<_EmailLayout>`.

**Warning signs:** Email received has no `<html>` wrapper; fonts missing; parchment background missing.

---

### Pitfall 2: Scoped Service in Hangfire Job Constructor

**What goes wrong:** `QuestFinalizedEmailJob` injects `IEmailRenderService` via constructor. App starts, first enqueue attempt throws `InvalidOperationException: Cannot consume scoped service from singleton`.

**Why it happens:** Hangfire activates jobs as singletons (or at least outside any DI scope). Scoped services cannot be constructor-injected.

**How to avoid:** Only `IServiceScopeFactory` and `ILogger<T>` in constructor. All scoped services resolved inside `await using var scope = scopeFactory.CreateAsyncScope()`.

**Warning signs:** Job fails immediately on first execution with scope-lifetime exception in Hangfire dashboard.

---

### Pitfall 3: Hangfire Serialization of Complex Types

**What goes wrong:** `BackgroundJob.Enqueue<QuestFinalizedEmailJob>(j => j.ExecuteAsync(quest, signups, ...))` — job fails on retry because `Quest` object serialized by Hangfire cannot be deserialized (circular references, missing constructors).

**Why it happens:** Hangfire uses JSON serialization for job arguments. Complex EF-tracked objects with navigation properties serialize incorrectly.

**How to avoid:** Extract primitives from `Quest` and `PlayerSignup` before enqueueing. Pass `int questId`, `string[]` arrays, `string`, `DateTime` only.

**Warning signs:** Hangfire dashboard shows `JsonSerializationException` or `NullReferenceException` on job deserialization.

---

### Pitfall 4: Unit Tests Asserting on Removed IEmailService Calls

**What goes wrong:** After removing `IEmailService emailService` from `QuestService` constructor, `QuestServiceTests.cs` fails to compile (missing constructor parameter).

**Why it happens:** `QuestServiceTests` currently mocks `IEmailService` and asserts it was called by `FinalizeQuestAsync`. Phase 21 removes that call.

**How to avoid:** Update `QuestServiceTests.cs` as part of the phase:
- Remove `IEmailService` mock from test setup
- Remove `IEmailService` parameter from `QuestService` constructor instantiation in tests
- Add `IBackgroundJobClient` mock (if using instance-based enqueue)
- Update assertions: verify job was enqueued, not that email was sent

**Warning signs:** Build failure in `EuphoriaInn.UnitTests` project.

---

### Pitfall 5: Inline Styles vs External Stylesheets

**What goes wrong:** Developer adds `<link rel="stylesheet">` or `<style>` block to email `<head>`. Email renders correctly in browser preview but arrives in Gmail without any styling.

**Why it happens:** Gmail strips `<style>` blocks from `<head>` in most contexts (especially forwarded emails). Some other clients do too.

**How to avoid:** Every style attribute must be an inline `style="..."` on the element. No external stylesheets, no `<style>` blocks. UI-SPEC.md specifies all values.

---

## Code Examples

### Verified: Rendering a Component in a Job

```csharp
// Source: Microsoft Learn (aspnetcore-10.0) + confirmed HtmlRenderer API
var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
{
    var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        { "QuestTitle", "The Dragon's Lair" },
        { "DmName", "Thomas" },
        { "QuestDate", new DateTime(2026, 7, 12) },
        { "ChallengeRating", 5 },
        { "AppUrl", "https://questboard.example.com" }
        // ... etc
    });

    var output = await htmlRenderer.RenderComponentAsync<QuestFinalized>(parameters);
    return output.ToHtmlString();
});
```

### Verified: CR Badge Inline Styles (matches quests.css .cr-badge exactly)

```html
<!-- Source: Verified from EuphoriaInn.Service/wwwroot/css/quests.css lines 114-127 -->
<td style="
    background: linear-gradient(135deg, #8B4513, #A0522D);
    color: #FFD700;
    font-weight: bold;
    font-size: 14px;
    padding: 8px 12px;
    border-radius: 8px;
    border: 2px solid #FFD700;
    box-shadow: 0 4px 8px rgba(0,0,0,0.3);
    text-shadow: 1px 1px 2px rgba(0,0,0,0.7);
    display: inline-block;
">CR @ChallengeRating</td>
```

### Verified: Poster Image Absolute URL Pattern

```razor
@* Source: UI-SPEC.md — image URLs must be absolute for email clients *@
<table width="600" style="
    background-image: url('@(AppUrl)/images/Blanks/Blanks%20w%20Shadow/Poster1.png');
    background-size: cover;
    background-position: center top;
    background-color: #F4E4BC;
    margin: 0 auto;
">
```

Note: URL-encode spaces in path (`Blanks w Shadow` → `Blanks%20w%20Shadow`). Email clients are strict about URL encoding in `style` attribute values.

### Verified: Subject Lines (from UI-SPEC.md)

```csharp
// QuestFinalized
subject: $"Your quest has been confirmed: {questTitle}"
// QuestDateChanged
subject: $"Session date changed: {questTitle}"
// SessionReminder
subject: $"Reminder: {questTitle} is tomorrow"
```

---

## Image Assets Verification

[VERIFIED: Listed `EuphoriaInn.Service/wwwroot/images/Blanks/Blanks w Shadow/` and `wwwroot/images/Wax Seals/`]

| Asset | Path | Exists | Size |
|-------|------|--------|------|
| Poster1.png (primary — tall portrait 1000×1400) | `wwwroot/images/Blanks/Blanks w Shadow/Poster1.png` | Yes | 1.8 MB |
| Poster6.png (shorter — 600×800, QuestDateChanged) | `wwwroot/images/Blanks/Blanks w Shadow/Poster6.png` | Yes | 562 KB |
| Crown Seal.png (default wax seal) | `wwwroot/images/Wax Seals/Crown Seal.png` | Yes | 149 KB |
| Eagle Seal.png | `wwwroot/images/Wax Seals/Eagle Seal.png` | Yes | 556 KB |
| Flower Seal.png | `wwwroot/images/Wax Seals/Flower Seal.png` | Yes | 166 KB |
| Sigil Seal.png | `wwwroot/images/Wax Seals/Sigil Seal.png` | Yes | 171 KB |

All four wax seal variants exist. "Crown Seal.png" is the default per UI-SPEC.md. The path has a space: `Wax Seals` — URL-encode as `Wax%20Seals` in the `src` attribute of img tags in email templates.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IRazorViewEngine` for background email rendering | `HtmlRenderer` from `Microsoft.AspNetCore.Components.Web` | .NET 8+ | `IRazorViewEngine` throws in non-HTTP contexts; `HtmlRenderer` works anywhere |
| Plain text email bodies (`IsBodyHtml = false`) | HTML email with inline CSS, table layout | Phase 21 | Requires `IsBodyHtml = true` in `MailMessage` |
| Direct synchronous email send in service layer | Hangfire fire-and-forget job | Phase 21 | Resilience to transient SMTP failures; no email lost on app restart |
| `@layout` directive for shared email layout | Composed `<_EmailLayout>` component with `ChildContent` | Phase 21 | `@layout` is Blazor-router–only; composition is the correct pattern for `HtmlRenderer` |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | AutoMapper convention maps `FinalizedEmailSentForDate` without explicit `.ForMember()` because property names and types match | Pattern 6 (EF Core Migration) | If wrong: mapper throws at startup. Fix: add explicit `.ForMember()` in `EntityProfile.cs` |
| A2 | `Microsoft.NET.Sdk.Web` compiles `.razor` component files without switching to `Microsoft.NET.Sdk.Razor` | Standard Stack | If wrong: build fails on `.razor` files in Components/Emails/. Fix: confirm with `dotnet build`; the Web SDK does include Razor compilation for components |
| A3 | Static `BackgroundJob.Enqueue<T>()` is acceptable for Phase 21 (vs. injecting `IBackgroundJobClient`) | Pattern 5 | If wrong: unit-testing becomes harder (static calls are harder to mock). Mitigation: use `IBackgroundJobClient` from constructor |

---

## Open Questions

1. **`QuestService` Hangfire injection: static vs `IBackgroundJobClient`**
   - What we know: Phase 20 uses static `BackgroundJob.Enqueue` in `Program.cs` startup code
   - What's unclear: Whether `QuestService` (a Domain-layer service) should depend on `IBackgroundJobClient` (a Hangfire abstraction) — this adds a Hangfire dependency to Domain
   - Recommendation: Keep `QuestService` in Domain but inject `IBackgroundJobClient` via interface. Alternatively, refactor the enqueueing responsibility to a thin Service-layer wrapper. The cleanest option is to inject `IBackgroundJobClient` into `QuestService` — Hangfire.Core's `IBackgroundJobClient` is the standard DI-injectable alternative to the static `BackgroundJob` class.

2. **Dedup check: `FinalizedEmailSentForDate.Date` vs exact `DateTime` match**
   - What we know: D-13 says "if `FinalizedEmailSentForDate == finalizedDate`, skip send"
   - What's unclear: Whether date-only comparison (`.Date`) or full `DateTime` equality is intended
   - Recommendation: Use `.Date` comparison — the dedup intent is "same session date", not "same exact millisecond". A quest re-finalized at 9:00 AM and again at 2:00 PM for the same session day should still dedup.

3. **Repository method for updating `FinalizedEmailSentForDate`**
   - What we know: The job needs to persist this value after sending
   - What's unclear: Whether to add a dedicated `SetFinalizedEmailSentForDateAsync` repository method or update via a re-fetched entity
   - Recommendation: Add a dedicated method `SetFinalizedEmailSentForDateAsync(int questId, DateTime date)` to `IQuestRepository`/`QuestRepository` — consistent with the existing repository pattern (e.g., `UpdateQuestRecapAsync`).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | All compilation | Yes | 10.0.301 | — |
| `Microsoft.AspNetCore.Components.Web` | `HtmlRenderer` / Razor components | Yes (shared framework) | 10.0.9 | — |
| Hangfire 1.8.23 | Email jobs | Yes (in Service.csproj) | 1.8.23 | — |
| EF Core 10.0.9 | Migration | Yes (in Repository.csproj) | 10.0.9 | — |
| SQL Server (local) | Migration apply + Hangfire storage | Yes (from Phase 20) | — | — |
| Image assets (Poster1, Poster6, Crown Seal) | Email template rendering | Yes (verified in wwwroot) | — | — |

**No missing dependencies.** All required components are available in the current environment.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xunit.v3 3.2.2 + FluentAssertions 8.10.0 + NSubstitute 5.3.0 |
| Config file | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --no-build -x` |
| Full suite command | `dotnet test --no-build` (runs UnitTests + IntegrationTests) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EMAIL-01 | `IEmailRenderService.RenderAsync<T>` returns non-empty HTML string for each component | Unit | `dotnet test EuphoriaInn.UnitTests -x --filter "EmailRender"` | Wave 0 |
| EMAIL-01 | `HtmlRenderer` used (not `IRazorViewEngine`) — verified by type check | Unit | Part of render service tests | Wave 0 |
| EMAIL-02 | Dedup guard: second enqueue for same `questId` + `date` skips send | Unit | `dotnet test EuphoriaInn.UnitTests -x --filter "QuestFinalizedEmailJob"` | Wave 0 |
| EMAIL-02 | `FinalizedEmailSentForDate` persisted after successful send | Unit | Part of job tests | Wave 0 |
| EMAIL-03 | `SessionReminder.razor` renders with all locked parameters (D-15) | Unit | `dotnet test EuphoriaInn.UnitTests -x --filter "SessionReminder"` | Wave 0 |

**Note on existing `QuestServiceTests.cs`:** Tests that assert `IEmailService.Received()` on `FinalizeQuestAsync` will fail after Phase 21 removes that call. These must be updated in the same wave that changes `QuestService`.

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests --no-build -x`
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.UnitTests/Services/EmailRenderServiceTests.cs` — covers EMAIL-01
- [ ] `EuphoriaInn.UnitTests/Jobs/QuestFinalizedEmailJobTests.cs` — covers EMAIL-02 dedup logic
- [ ] `EuphoriaInn.UnitTests/Components/SessionReminderTests.cs` — covers EMAIL-03 parameter contract
- [ ] `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` — UPDATE to remove `IEmailService` mock assertions that will break

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Email is outbound only; no auth involved |
| V3 Session Management | No | No session state in email pipeline |
| V4 Access Control | No | Job enqueueing is server-side; no user-controlled path |
| V5 Input Validation | Yes | `QuestTitle`, `DmName`, `QuestDescription` rendered into HTML — must be HTML-escaped. Razor components handle this by default (`@variable` auto-escapes) |
| V6 Cryptography | No | SMTP credentials already managed via `EmailSettings`; no new crypto |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via user-supplied content in email HTML | Tampering | Razor component `@variable` syntax auto-escapes HTML. Never use `@((MarkupString)variable)` for user-controlled content |
| Email header injection via `subject` or `toEmail` | Tampering | `MailMessage.Subject` and `To.Add()` in `System.Net.Mail` sanitize headers automatically |
| Open relay / SSRF via `AppUrl` | Spoofing | `AppUrl` is server-configured in `appsettings.json`, not user-supplied |

---

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn: Render Razor components outside of ASP.NET Core (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-components-outside-of-aspnetcore?view=aspnetcore-10.0) — `HtmlRenderer` API, `Dispatcher.InvokeAsync`, `ParameterView.FromDictionary`, `ToHtmlString()`
- [Microsoft.AspNetCore.Components.Web DLL verified in ASP.NET Core 10.0.9 shared framework] — `C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref\10.0.9\ref\net10.0\Microsoft.AspNetCore.Components.Web.dll`
- Codebase reads: `SmokeTestJob.cs`, `QuestService.cs`, `EmailService.cs`, `IEmailService.cs`, `EntityProfile.cs`, `ServiceExtensions.cs`, `Program.cs`, `QuestEntity.cs`, `Quest.cs`, `EmailSettings.cs` — all verified from source

### Secondary (MEDIUM confidence)
- [End Point Dev (August 2025): Using Razor templates to render HTML emails in ASP.NET Core](https://www.endpointdev.com/blog/2025/08/using-razor-templates-to-render-html-emails-in-asp-net/) — practical `IEmailRenderService` implementation pattern
- [GitHub aspnetcore issue #55068: @layout directive ignored by HtmlRenderer](https://github.com/dotnet/aspnetcore/issues/55068) — confirmed via search result; explains composition-over-layout-directive pattern

### Tertiary (LOW confidence)
- None — all claims verified from official docs or codebase inspection.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — verified from framework installation and project .csproj
- HtmlRenderer API: HIGH — cited from official Microsoft Learn docs (aspnetcore-10.0)
- Architecture patterns: HIGH — derived from existing codebase (SmokeTestJob, QuestService, EmailService)
- AutoMapper convention behavior: MEDIUM — inferred from EntityProfile.cs inspection; low risk, easily verified at build time
- Email client compatibility: MEDIUM — cited from UI-SPEC.md which was already approved

**Research date:** 2026-06-26
**Valid until:** 2026-07-26 (stable APIs — 30 days)
