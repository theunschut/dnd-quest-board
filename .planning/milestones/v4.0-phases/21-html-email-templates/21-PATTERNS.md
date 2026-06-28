# Phase 21: HTML Email Templates - Pattern Map

**Mapped:** 2026-06-26
**Files analyzed:** 15 new/modified files
**Analogs found:** 12 / 15

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Components/Emails/_EmailLayout.razor` | component (layout) | request-response | No existing Razor components | none |
| `EuphoriaInn.Service/Components/Emails/QuestFinalized.razor` | component (email template) | request-response | No existing Razor components | none |
| `EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor` | component (email template) | request-response | No existing Razor components | none |
| `EuphoriaInn.Service/Components/Emails/SessionReminder.razor` | component (email template) | request-response | No existing Razor components | none |
| `EuphoriaInn.Domain/Interfaces/IEmailRenderService.cs` | interface | request-response | `EuphoriaInn.Domain/Interfaces/IEmailService.cs` | role-match |
| `EuphoriaInn.Service/Services/RazorEmailRenderService.cs` | service | request-response | `EuphoriaInn.Domain/Services/EmailService.cs` | role-match |
| `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` | job (fire-and-forget) | event-driven | `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` | exact |
| `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` | job (fire-and-forget) | event-driven | `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` | exact |
| `EuphoriaInn.Domain/Interfaces/IEmailService.cs` | interface (modify) | request-response | self (existing file) | self |
| `EuphoriaInn.Domain/Services/EmailService.cs` | service (modify) | request-response | self (existing file) | self |
| `EuphoriaInn.Domain/Services/QuestService.cs` | service (modify) | event-driven | self (existing file) | self |
| `EuphoriaInn.Repository/Entities/QuestEntity.cs` | entity (modify) | CRUD | `EuphoriaInn.Repository/Entities/QuestEntity.cs` (existing Recap field) | self |
| `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` | model (modify) | CRUD | `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` (existing Recap field) | self |
| EF Core migration file (new) | migration | CRUD | `EuphoriaInn.Repository/Migrations/20260127153158_AddRecapToQuest.cs` | exact |
| `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` | job (delete) | — | — | delete |

---

## Pattern Assignments

### `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` (job, event-driven)

**Analog:** `EuphoriaInn.Service/Jobs/SmokeTestJob.cs`

**Imports pattern** (SmokeTestJob.cs lines 1-5):
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EuphoriaInn.Service.Jobs;
```

**Core IServiceScopeFactory pattern** (SmokeTestJob.cs lines 7-19):
```csharp
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

**Adapted for QuestFinalizedEmailJob — constructor follows same pattern:**
```csharp
// Constructor: only IServiceScopeFactory (singleton-safe) and ILogger<T>
public class QuestFinalizedEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestFinalizedEmailJob> logger)
```

**Service resolution inside scope — adapt from SmokeTestJob pattern:**
```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
var renderService   = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();
var emailSettings   = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
```

**Dedup check pattern (D-13) — use `.Date` comparison per RESEARCH.md recommendation:**
```csharp
var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);
if (quest?.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
{
    logger.LogInformation(
        "Finalized email already sent for quest {QuestId} on {Date}. Skipping.",
        questId, finalizedDate);
    return;
}
```

**Render + send loop:**
```csharp
for (int i = 0; i < recipientEmails.Length; i++)
{
    var html = await renderService.RenderAsync<QuestFinalized>(new Dictionary<string, object?>
    {
        { nameof(QuestFinalized.QuestTitle),            questTitle },
        { nameof(QuestFinalized.DmName),                dmName },
        { nameof(QuestFinalized.QuestDate),             finalizedDate },
        { nameof(QuestFinalized.QuestDescription),      questDescription },
        { nameof(QuestFinalized.ConfirmedPlayerNames),  playerNames.ToList() },
        { nameof(QuestFinalized.QuestUrl),              $"{emailSettings.AppUrl}/Quest/Details/{questId}" },
        { nameof(QuestFinalized.ChallengeRating),       challengeRating },
        { nameof(QuestFinalized.AppUrl),                emailSettings.AppUrl }
    });

    await emailService.SendAsync(
        recipientEmails[i],
        $"Your quest has been confirmed: {questTitle}",
        html);
}

// Persist dedup guard after all sends succeed
await questRepository.SetFinalizedEmailSentForDateAsync(questId, finalizedDate, cancellationToken);
```

**Error handling pattern — consistent with EmailService.cs:**
```csharp
// No try/catch wrapping the job body — Hangfire catches and retries on exception.
// Do log informational messages for skip conditions (dedup).
// Individual send errors propagate to Hangfire's retry queue.
```

**Hangfire job parameters — only serializable primitives (per RESEARCH.md Pitfall 3):**
```csharp
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
```

---

### `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` (job, event-driven)

**Analog:** `EuphoriaInn.Service/Jobs/SmokeTestJob.cs`

**Same constructor pattern as QuestFinalizedEmailJob.** No dedup guard for date-changed emails.

**Job parameters:**
```csharp
public async Task ExecuteAsync(
    int questId,
    string[] recipientEmails,
    string[] playerNames,
    string questTitle,
    string dmName,
    string oldDate,     // human-readable string pre-formatted by QuestService
    string newDate,
    string appUrl,
    CancellationToken cancellationToken = default)
```

**Render + send (no dedup loop):**
```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var renderService = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
var emailService  = scope.ServiceProvider.GetRequiredService<IEmailService>();

for (int i = 0; i < recipientEmails.Length; i++)
{
    var html = await renderService.RenderAsync<QuestDateChanged>(new Dictionary<string, object?>
    {
        { nameof(QuestDateChanged.QuestTitle),   questTitle },
        { nameof(QuestDateChanged.DmName),       dmName },
        { nameof(QuestDateChanged.AppUrl),       appUrl }
    });

    await emailService.SendAsync(
        recipientEmails[i],
        $"Session date changed: {questTitle}",
        html);
}
```

---

### `EuphoriaInn.Domain/Interfaces/IEmailRenderService.cs` (interface, request-response)

**Analog:** `EuphoriaInn.Domain/Interfaces/IEmailService.cs` (lines 1-7)

**Namespace and interface structure pattern:**
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
```

**New interface follows same one-file, one-method-group pattern:**
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailRenderService
{
    Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent;
}
```

Note: `IComponent` lives in `Microsoft.AspNetCore.Components` (shared framework). Domain project references
`Microsoft.AspNetCore.App` framework — the interface reference is safe.

---

### `EuphoriaInn.Service/Services/RazorEmailRenderService.cs` (service, request-response)

**Analog:** `EuphoriaInn.Domain/Services/EmailService.cs`

**Imports pattern from EmailService.cs (lines 1-8):**
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EuphoriaInn.Domain.Services;
```

**Adapted imports for RazorEmailRenderService:**
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace EuphoriaInn.Service.Services;
```

**Core HtmlRenderer pattern (from RESEARCH.md Pattern 1 — verified against Microsoft Learn aspnetcore-10.0):**
```csharp
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
            var output    = await htmlRenderer.RenderComponentAsync<TComponent>(paramView);
            return output.ToHtmlString();
        });
    }
}
```

**DI registration** — goes in `Program.cs` after `AddDomainServices`, NOT in `ServiceExtensions.AddDomainServices`
(because `RazorEmailRenderService` is in the Service project, which `ServiceExtensions` in Domain cannot reference):

```csharp
// In Program.cs (EuphoriaInn.Service/Program.cs) — after line 78 (AddDomainServices call):
builder.Services.AddScoped<IEmailRenderService, RazorEmailRenderService>();
```

---

### `EuphoriaInn.Domain/Interfaces/IEmailService.cs` (modify — add SendAsync)

**Source file:** `EuphoriaInn.Domain/Interfaces/IEmailService.cs` (lines 1-7)

**Current state (full file):**
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
```

**Add `SendAsync` at top of the method list; keep typed methods as deprecated wrappers:**
```csharp
namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    // Generic method — used by all Hangfire jobs (Phase 21+)
    Task SendAsync(string toEmail, string subject, string htmlBody);

    // Legacy typed methods — kept to avoid breaking QuestServiceTests until they are updated
    [Obsolete("Use SendAsync with pre-rendered HTML. Will be removed in a future phase.")]
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    [Obsolete("Use SendAsync with pre-rendered HTML. Will be removed in a future phase.")]
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
```

---

### `EuphoriaInn.Domain/Services/EmailService.cs` (modify — implement SendAsync)

**Source file:** `EuphoriaInn.Domain/Services/EmailService.cs` (lines 32-69)

**Existing send pattern to replicate for `SendAsync` (from `SendQuestFinalizedEmailAsync`):**
```csharp
public async Task SendQuestFinalizedEmailAsync(...)
{
    try
    {
        using var client = CreateSmtpClient();
        if (client == null) return;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = $"Quest Finalized: {questTitle}",
            Body = $@"...",
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail);
        await client.SendMailAsync(mailMessage);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send quest finalized email for quest {QuestTitle}", questTitle);
    }
}
```

**New `SendAsync` method follows exact same try/catch + CreateSmtpClient() pattern, with `IsBodyHtml = true`:**
```csharp
public async Task SendAsync(string toEmail, string subject, string htmlBody)
{
    try
    {
        using var client = CreateSmtpClient();
        if (client == null) return;

        var mailMessage = new MailMessage
        {
            From       = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true    // key difference — HTML email
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

### `EuphoriaInn.Domain/Services/QuestService.cs` (modify — replace email calls with Hangfire enqueue)

**Source file:** `EuphoriaInn.Domain/Services/QuestService.cs`

**Current constructor (line 9-13):**
```csharp
internal class QuestService(
    IQuestRepository repository,
    IPlayerSignupRepository playerSignupRepository,
    IEmailService emailService,
    IMapper mapper) : BaseService<Quest>(repository, mapper), IQuestService
```

**New constructor — remove `IEmailService`, add `IBackgroundJobClient`:**
```csharp
using Hangfire;

internal class QuestService(
    IQuestRepository repository,
    IPlayerSignupRepository playerSignupRepository,
    IBackgroundJobClient jobClient,
    IMapper mapper) : BaseService<Quest>(repository, mapper), IQuestService
```

Note: `IBackgroundJobClient` is from `Hangfire.Core`, already installed. This adds a Hangfire dependency to
the Domain project — acceptable per RESEARCH.md Open Question 1 recommendation.

**`FinalizeQuestAsync` — replace direct email call (lines 28-35) with Hangfire enqueue:**
```csharp
// BEFORE (remove):
await emailService.SendQuestFinalizedEmailAsync(
    signup.Player.Email!,
    signup.Player.Name,
    quest.Title,
    quest.DungeonMaster?.Name ?? "Unknown DM",
    finalizedDate);

// AFTER — collect primitives first, then enqueue once outside the loop:
var recipientEmails = selectedSignups.Select(s => s.Player.Email!).ToArray();
var playerNames     = selectedSignups.Select(s => s.Player.Name).ToArray();

jobClient.Enqueue<QuestFinalizedEmailJob>(j => j.ExecuteAsync(
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

**`UpdateQuestPropertiesWithNotificationsAsync` — replace per-player email loop (lines 116-120):**
```csharp
// BEFORE (remove per-player loop calling emailService):
foreach (var player in affectedPlayers.Where(p => !string.IsNullOrEmpty(p.Email)))
{
    await emailService.SendQuestDateChangedEmailAsync(...);
    emailed++;
}

// AFTER — single enqueue with arrays:
var withEmail = affectedPlayers.Where(p => !string.IsNullOrEmpty(p.Email)).ToList();
if (withEmail.Count > 0)
{
    jobClient.Enqueue<QuestDateChangedEmailJob>(j => j.ExecuteAsync(
        questId,
        withEmail.Select(p => p.Email!).ToArray(),
        withEmail.Select(p => p.Name).ToArray(),
        quest.Title,
        quest.DungeonMaster?.Name ?? "Unknown DM",
        emailSettings.AppUrl,   // needs IOptions<EmailSettings> injected too, or pass AppUrl from settings
        CancellationToken.None));
    emailed = withEmail.Count;
}
```

Note: `QuestService` is in Domain and currently has no access to `EmailSettings.AppUrl`. The cleanest solution
is to also inject `IOptions<EmailSettings>` into `QuestService` for `AppUrl` (already registered in Domain's
`ServiceExtensions`), or pass AppUrl as a constructor dependency. Alternatively, embed AppUrl lookup in the job
using `IServiceScopeFactory` (already done by the job pattern).

**Simpler approach** — omit `appUrl` from the enqueue arguments; the job resolves `EmailSettings` itself via scope:
```csharp
jobClient.Enqueue<QuestDateChangedEmailJob>(j => j.ExecuteAsync(
    questId,
    withEmail.Select(p => p.Email!).ToArray(),
    withEmail.Select(p => p.Name).ToArray(),
    quest.Title,
    quest.DungeonMaster?.Name ?? "Unknown DM",
    CancellationToken.None));
```

---

### `EuphoriaInn.Repository/Entities/QuestEntity.cs` (modify — add column)

**Source file:** `EuphoriaInn.Repository/Entities/QuestEntity.cs`

**Existing nullable DateTime field pattern (line 27):**
```csharp
public DateTime? FinalizedDate { get; set; }
```

**Existing nullable string field pattern (line 35):**
```csharp
public string? Recap { get; set; }
```

**Add after `IsFinalized` (line 29), matching exact style:**
```csharp
public bool IsFinalized { get; set; }

public DateTime? FinalizedEmailSentForDate { get; set; }   // ADD — dedup guard (Phase 21)
```

No `[Column]`, `[Required]`, or `[StringLength]` attribute — nullable scalar follows existing convention.

---

### `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` (modify — add property)

**Source file:** `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs`

**Existing nullable DateTime field pattern (line 24):**
```csharp
public DateTime? FinalizedDate { get; set; }
```

**Add after `IsFinalized` (line 26), matching exact style:**
```csharp
public bool IsFinalized { get; set; }

public DateTime? FinalizedEmailSentForDate { get; set; }   // ADD — dedup guard (Phase 21)
```

**AutoMapper:** No explicit `.ForMember()` needed. The `EntityProfile.cs` `QuestEntity → Quest` mapping uses
convention for scalar properties. `FinalizedEmailSentForDate` is `DateTime?` in both — convention maps it.
(Confirmed: `EntityProfile.cs` only has explicit maps for navigation properties and enum conversions; lines 18-30.)

---

### EF Core migration file (new)

**Analog:** `EuphoriaInn.Repository/Migrations/20260127153158_AddRecapToQuest.cs` (exact pattern)

**Migration pattern (full analog file, lines 1-29):**
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuphoriaInn.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRecapToQuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Recap",
                table: "Quests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recap",
                table: "Quests");
        }
    }
}
```

**New migration Up/Down follows exact same structure with `DateTime?` type:**
```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "FinalizedEmailSentForDate",
    table: "Quests",
    type: "datetime2",
    nullable: true);
```

**Migration command (run from `EuphoriaInn.Service/` directory):**
```bash
dotnet ef migrations add AddFinalizedEmailSentForDate --project ../EuphoriaInn.Repository
```

Migration is auto-applied on startup via `context.Database.Migrate()` in `Repository/Extensions/ServiceExtensions.cs` line 38.

---

### `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs` (modify — add SetFinalizedEmailSentForDateAsync)

**Source file:** `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs`

**Existing targeted update method pattern (line 29):**
```csharp
Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default);
```

**Add new method following same signature pattern:**
```csharp
Task SetFinalizedEmailSentForDateAsync(int questId, DateTime date, CancellationToken token = default);
```

---

### `EuphoriaInn.Repository/QuestRepository.cs` (modify — implement SetFinalizedEmailSentForDateAsync)

**Source file:** `EuphoriaInn.Repository/QuestRepository.cs`

**Existing targeted field update pattern — `UpdateQuestRecapAsync` (lines 176-183):**
```csharp
public async Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default)
{
    var entity = await DbContext.Quests.FindAsync([questId], cancellationToken: token);
    if (entity == null) return;

    entity.Recap = recap;
    await DbContext.SaveChangesAsync(token);
}
```

**New method follows exact same find-by-PK, assign, SaveChanges pattern:**
```csharp
public async Task SetFinalizedEmailSentForDateAsync(int questId, DateTime date, CancellationToken token = default)
{
    var entity = await DbContext.Quests.FindAsync([questId], cancellationToken: token);
    if (entity == null) return;

    entity.FinalizedEmailSentForDate = date;
    await DbContext.SaveChangesAsync(token);
}
```

---

### `EuphoriaInn.Service/Components/Emails/_EmailLayout.razor` (no analog — new pattern)

No existing Razor components in the codebase. Follow RESEARCH.md Pattern 3 exactly.

**Key constraint:** No `@page` directive. No `@layout` directive (ignored by `HtmlRenderer`).
Component outputs a complete `<!DOCTYPE html>` document.

**Structure from RESEARCH.md (verified against aspnetcore-10.0 docs):**
```razor
@* _EmailLayout.razor — EuphoriaInn.Service/Components/Emails/ *@
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@Subject</title>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;700&display=swap" rel="stylesheet" />
</head>
<body style="margin:0;padding:0;background-color:#F4E4BC;">
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

### `EuphoriaInn.Service/Components/Emails/QuestFinalized.razor` (no analog — new pattern)

**Structure from RESEARCH.md Pattern 3:**
```razor
@* QuestFinalized.razor — EuphoriaInn.Service/Components/Emails/ *@
@using EuphoriaInn.Service.Components.Emails

<_EmailLayout Subject="@($"Your quest has been confirmed: {QuestTitle}")"
              PreviewText="@($"Quest Confirmed — {QuestTitle} on {QuestDate:dddd, MMMM d}")">
    @* table-based layout — no flexbox/grid (email clients strip it) *@
    <table width="600" style="
        background-image: url('@(AppUrl)/images/Blanks/Blanks%20w%20Shadow/Poster1.png');
        background-size: cover;
        background-position: center top;
        background-color: #F4E4BC;
        margin: 0 auto;">
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

**CR badge inline styles (from RESEARCH.md verified against `wwwroot/css/quests.css` lines 114-127):**
```html
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

**Wax seal image (verified: `Crown Seal.png` at `wwwroot/images/Wax Seals/Crown Seal.png`):**
```html
<img src="@(AppUrl)/images/Wax%20Seals/Crown%20Seal.png"
     width="80"
     alt="Wax Seal"
     style="display:block;" />
```

**All styles must be inline** — no `<style>` blocks (Gmail strips them per RESEARCH.md Pitfall 5).
All image URLs must be absolute with `AppUrl` prefix and URL-encoded spaces.

---

### `EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor` (no analog — new pattern)

Same `@code` parameter structure as QuestFinalized minus `ConfirmedPlayerNames` and `ChallengeRating`.
Uses `Poster6.png` (600×800, shorter) as background per CONTEXT.md specifics.

**Parameters:**
```razor
@code {
    [Parameter, EditorRequired] public string QuestTitle { get; set; } = "";
    [Parameter, EditorRequired] public string DmName { get; set; } = "";
    [Parameter, EditorRequired] public string AppUrl { get; set; } = "";
}
```

---

### `EuphoriaInn.Service/Components/Emails/SessionReminder.razor` (no analog — new pattern)

Locked parameter contract from CONTEXT.md D-15 (Phase 22 will consume this without changes):

```razor
@code {
    [Parameter, EditorRequired] public string QuestTitle { get; set; } = "";
    [Parameter, EditorRequired] public string DmName { get; set; } = "";
    [Parameter, EditorRequired] public DateTime QuestDate { get; set; }
    [Parameter, EditorRequired] public string QuestDescription { get; set; } = "";
    [Parameter, EditorRequired] public IList<string> ConfirmedPlayerNames { get; set; } = [];
    [Parameter, EditorRequired] public string QuestUrl { get; set; } = "";
    [Parameter, EditorRequired] public int ChallengeRating { get; set; }
}
```

Full quest description included without truncation (per CONTEXT.md D-15 and specifics).
CR badge included (per D-03). Character name with player-name fallback implemented in component.

---

### `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` (delete)

Delete entire file. Remove the `BackgroundJob.Enqueue<SmokeTestJob>` call from `Program.cs` line 174.
Remove the `using EuphoriaInn.Service.Jobs;` import from `Program.cs` line 16 only if no other jobs are
referenced there (new email jobs will need that namespace retained or re-added).

---

### `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` (modify — update broken assertions)

**Source file:** `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs`

**Breaking changes caused by Phase 21 `QuestService` modifications:**

Constructor (line 26) currently passes `_emailService` — will no longer compile:
```csharp
// BEFORE (line 26 — remove):
_sut = new QuestService(_repository, _playerSignupRepository, _emailService, _mapper);

// AFTER — replace _emailService with IBackgroundJobClient mock:
_jobClient = Substitute.For<IBackgroundJobClient>();
_sut = new QuestService(_repository, _playerSignupRepository, _jobClient, _mapper);
```

**Assertions to remove (lines 68-70, 95-103, 125-127, 157-164):**
All `await _emailService.Received(...)` and `await _emailService.DidNotReceive(...)` assertions.

**New assertions to add — verify Hangfire enqueue was called:**
```csharp
// xunit + NSubstitute pattern for IBackgroundJobClient:
_jobClient.Received(1).Enqueue<QuestFinalizedEmailJob>(
    Arg.Any<Expression<Action<QuestFinalizedEmailJob>>>());
```

Test filter for the new job tests: `--filter "QuestFinalizedEmailJob|EmailRender|SessionReminder"`.

---

## Shared Patterns

### IServiceScopeFactory in Hangfire Jobs
**Source:** `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` (lines 7-19)
**Apply to:** `QuestFinalizedEmailJob.cs`, `QuestDateChangedEmailJob.cs`

```csharp
// Constructor: singleton-safe only
public class XxxEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<XxxEmailJob> logger)

// Body: create scope, resolve scoped services inside
await using var scope = scopeFactory.CreateAsyncScope();
var service = scope.ServiceProvider.GetRequiredService<IXxx>();
```

### Error Logging Pattern
**Source:** `EuphoriaInn.Domain/Services/EmailService.cs` (lines 66-69)
**Apply to:** `RazorEmailRenderService.cs`, `EmailService.SendAsync`

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
}
```

### Targeted Repository Update Pattern
**Source:** `EuphoriaInn.Repository/QuestRepository.cs` (lines 176-183)
**Apply to:** `QuestRepository.SetFinalizedEmailSentForDateAsync`

```csharp
var entity = await DbContext.Quests.FindAsync([questId], cancellationToken: token);
if (entity == null) return;
entity.TheField = value;
await DbContext.SaveChangesAsync(token);
```

### Nullable Property Addition (Entity + Domain Model)
**Source:** `QuestEntity.cs` line 27 (`FinalizedDate`) and `Quest.cs` line 24 (`FinalizedDate`)
**Apply to:** Adding `FinalizedEmailSentForDate` to both files

```csharp
public DateTime? FinalizedDate { get; set; }    // existing — shows the style
public DateTime? FinalizedEmailSentForDate { get; set; }   // new — same style
```

### DI Registration — Scoped Service
**Source:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (lines 15-23)
**Apply to:** `IEmailRenderService` / `RazorEmailRenderService` registration in `Program.cs`

```csharp
services.AddScoped<IEmailService, EmailService>();          // existing pattern
builder.Services.AddScoped<IEmailRenderService, RazorEmailRenderService>();  // new — same pattern in Program.cs
```

### Subject Line Conventions (from RESEARCH.md verified against UI-SPEC.md)
**Apply to:** All three job `SendAsync` calls

```csharp
// QuestFinalized:    $"Your quest has been confirmed: {questTitle}"
// QuestDateChanged:  $"Session date changed: {questTitle}"
// SessionReminder:   $"Reminder: {questTitle} is tomorrow"
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `_EmailLayout.razor` | component (layout) | request-response | No Razor components exist in the codebase; first `.razor` file |
| `QuestFinalized.razor` | component (template) | request-response | No email template components exist; first of this type |
| `QuestDateChanged.razor` | component (template) | request-response | Same — no existing template components |
| `SessionReminder.razor` | component (template) | request-response | Same — no existing template components |

All four Razor component files follow the RESEARCH.md Pattern 3 structure (verified against aspnetcore-10.0 docs).

---

## Key Anti-Patterns to Flag for Planner

These are failure modes documented in RESEARCH.md that the plan actions must explicitly avoid:

1. **`@layout` directive in email `.razor` files** — ignored by `HtmlRenderer`. Compose layout via `<_EmailLayout>` child content instead.
2. **Scoped service in Hangfire job constructor** — only `IServiceScopeFactory` + `ILogger<T>` in constructor; all other services via `scopeFactory.CreateAsyncScope()`.
3. **Complex objects as Hangfire job parameters** — only primitives: `int`, `string`, `string[]`, `DateTime`. No `Quest`, `PlayerSignup`, or other domain objects.
4. **`<style>` blocks in email HTML** — Gmail strips them. All styles must be inline `style=""` attributes.
5. **Relative image URLs in emails** — must be absolute: `@(AppUrl)/images/...` with URL-encoded spaces.
6. **`IRazorViewEngine` instead of `HtmlRenderer`** — throws `NullReferenceException` in non-HTTP contexts.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Jobs/`, `EuphoriaInn.Domain/Interfaces/`, `EuphoriaInn.Domain/Services/`, `EuphoriaInn.Repository/Entities/`, `EuphoriaInn.Repository/Migrations/`, `EuphoriaInn.Repository/`, `EuphoriaInn.UnitTests/Services/`
**Files scanned:** 19 source files read in full
**Pattern extraction date:** 2026-06-26
