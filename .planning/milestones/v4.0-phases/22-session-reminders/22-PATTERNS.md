# Phase 22: Session Reminders - Pattern Map

**Mapped:** 2026-06-26
**Files analyzed:** 13 new/modified files
**Analogs found:** 13 / 13

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Repository/Entities/ReminderLogEntity.cs` | entity | CRUD | `EuphoriaInn.Repository/Entities/PlayerDateVoteEntity.cs` | exact |
| `EuphoriaInn.Repository/QuestBoardContext.cs` | config | CRUD | self (add `DbSet` + `OnModelCreating` block) | self-extend |
| `EuphoriaInn.Repository/Interfaces/IReminderLogRepository.cs` | interface | CRUD | `EuphoriaInn.Repository/Interfaces/IQuestRepository.cs` | role-match |
| `EuphoriaInn.Repository/ReminderLogRepository.cs` | repository | CRUD | `EuphoriaInn.Repository/BaseRepository.cs` | exact |
| `EuphoriaInn.Repository/Automapper/EntityProfile.cs` | config | transform | self (add `ReminderLog` mapping) | self-extend |
| `EuphoriaInn.Domain/Models/ReminderLog.cs` | model | CRUD | `EuphoriaInn.Domain/Models/QuestBoard/PlayerDateVote.cs` | exact |
| `EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs` | interface | event-driven | `EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs` | exact |
| `EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs` | service | event-driven | `EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs` | exact |
| `EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs` | service | event-driven | `EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs` | exact |
| `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` | job | event-driven | `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` | exact |
| `EuphoriaInn.Service/Jobs/DailyReminderJob.cs` | job | batch | `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` | role-match |
| `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` | controller | request-response | self (add `SendReminder` POST action alongside `Open`) | self-extend |
| `EuphoriaInn.Service/Views/Quest/Manage.cshtml` | view | request-response | self (add button + TempData success block) | self-extend |

---

## Pattern Assignments

### `EuphoriaInn.Repository/Entities/ReminderLogEntity.cs` (entity, CRUD)

**Analog:** `EuphoriaInn.Repository/Entities/PlayerDateVoteEntity.cs`

**Imports + declaration pattern** (PlayerDateVoteEntity.cs lines 1–6):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;

[Table("ReminderLogs")]
public class ReminderLogEntity : IEntity
```

**Core field pattern** (PlayerDateVoteEntity.cs lines 9–25 — Id, two required FKs, two navigation properties):
```csharp
[Key]
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
public int Id { get; set; }

[Required]
public int QuestId { get; set; }

[Required]
public int PlayerId { get; set; }

public DateTime SentAt { get; set; } = DateTime.UtcNow;

[ForeignKey(nameof(QuestId))]
public virtual QuestEntity Quest { get; set; } = null!;

[ForeignKey(nameof(PlayerId))]
public virtual UserEntity Player { get; set; } = null!;
```

---

### `EuphoriaInn.Repository/QuestBoardContext.cs` (config, CRUD — self-extend)

**Analog:** `EuphoriaInn.Repository/Entities/QuestBoardContext.cs`

**DbSet addition pattern** (QuestBoardContext.cs lines 9–30 — all existing `DbSet<T>` declarations):
```csharp
public DbSet<ReminderLogEntity> ReminderLogs { get; set; }
// Add alongside existing DbSet declarations
```

**Unique index pattern** (QuestBoardContext.cs lines 74–77 — `PlayerDateVoteEntity` unique index):
```csharp
// Ensure unique vote per player per date
modelBuilder.Entity<PlayerDateVoteEntity>()
    .HasIndex(pdv => new { pdv.PlayerSignupId, pdv.ProposedDateId })
    .IsUnique();
```
Apply the same shape for `ReminderLogEntity`. Then add FK configuration using `OnDelete(DeleteBehavior.NoAction)` matching the `PlayerSignups` pattern at lines 50–54.

**Full OnModelCreating block to add:**
```csharp
modelBuilder.Entity<ReminderLogEntity>()
    .HasOne(r => r.Quest)
    .WithMany()
    .HasForeignKey(r => r.QuestId)
    .OnDelete(DeleteBehavior.NoAction);

modelBuilder.Entity<ReminderLogEntity>()
    .HasOne(r => r.Player)
    .WithMany()
    .HasForeignKey(r => r.PlayerId)
    .OnDelete(DeleteBehavior.NoAction);

modelBuilder.Entity<ReminderLogEntity>()
    .HasIndex(r => new { r.QuestId, r.PlayerId })
    .IsUnique();
```

---

### `EuphoriaInn.Repository/Interfaces/IReminderLogRepository.cs` (interface, CRUD)

**Analog:** `EuphoriaInn.Repository/Interfaces/IQuestRepository.cs` (lines 1–20)

**Imports + declaration pattern:**
```csharp
using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IReminderLogRepository
{
    Task<bool> ExistsAsync(int questId, int playerId, CancellationToken token = default);
    Task AddAsync(int questId, int playerId, CancellationToken token = default);
}
```
Note: This is a narrow interface — not extending `IBaseRepository<T>` because callers only need two targeted operations. Consistent with `SetFinalizedEmailSentForDateAsync` being a focused method on `IQuestRepository`.

---

### `EuphoriaInn.Repository/ReminderLogRepository.cs` (repository, CRUD)

**Analog:** `EuphoriaInn.Repository/BaseRepository.cs` (lines 1–45) for constructor pattern; `QuestRepository.cs` lines 185–192 for targeted EF save pattern.

**Imports + constructor pattern** (BaseRepository.cs lines 1–18):
```csharp
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class ReminderLogRepository(QuestBoardContext dbContext) : IReminderLogRepository
```

**Core targeted-operation pattern** (QuestRepository.cs lines 185–192 — `SetFinalizedEmailSentForDateAsync`):
```csharp
public async Task SetFinalizedEmailSentForDateAsync(int questId, DateTime date, CancellationToken token = default)
{
    var entity = await DbContext.Quests.FindAsync([questId], cancellationToken: token);
    if (entity == null) return;

    entity.FinalizedEmailSentForDate = date;
    await DbContext.SaveChangesAsync(token);
}
```
The `AddAsync` and `ExistsAsync` implementations follow the same direct-DbContext pattern (no AutoMapper needed for these primitive operations).

---

### `EuphoriaInn.Repository/Automapper/EntityProfile.cs` (config, transform — self-extend)

**Analog:** `EuphoriaInn.Repository/Automapper/EntityProfile.cs` (lines 56–60 — `PlayerDateVote` mapping as simplest comparable map)

**Mapping pattern** (EntityProfile.cs lines 56–63):
```csharp
// PlayerDateVote mapping
CreateMap<PlayerDateVote, PlayerDateVoteEntity>()
    .ForMember(dest => dest.Vote, opt => opt.MapFrom(...));

CreateMap<PlayerDateVoteEntity, PlayerDateVote>()
    ...
```
`ReminderLog` is simpler — all fields map by name, no enum conversion, so `ReverseMap()` suffices:
```csharp
// ReminderLog mapping
CreateMap<ReminderLog, ReminderLogEntity>().ReverseMap();
```
Add in `EntityProfile.cs` constructor alongside the `ProposedDate` mapping (line 53–54) that also uses `ReverseMap()`.

---

### `EuphoriaInn.Domain/Models/ReminderLog.cs` (model, CRUD)

**Analog:** `EuphoriaInn.Domain/Models/QuestBoard/PlayerDateVote.cs` (full file, 20 lines)

**Full pattern** (PlayerDateVote.cs lines 1–20):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Models.QuestBoard;

public class ReminderLog : IModel
{
    public int Id { get; set; }
    public int QuestId { get; set; }
    public int PlayerId { get; set; }
    public DateTime SentAt { get; set; }
}
```
Navigation properties (`Quest`, `Player`) are omitted — the job only needs to insert and check existence; no graph navigation is needed in the domain model.

---

### `EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs` (interface, event-driven)

**Analog:** `EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs` (full file, 27 lines)

**Full pattern** (IQuestEmailDispatcher.cs lines 1–27):
```csharp
namespace EuphoriaInn.Domain.Interfaces;

/// <summary>
/// Dispatches reminder jobs to the background job infrastructure.
/// Defined in Domain so QuestController can call it without taking a dependency on Service-layer types.
/// </summary>
public interface IQuestEmailDispatcher
{
    void EnqueueFinalizedEmail(...);
}
```
Apply same shape — one `void EnqueueSessionReminder(int questId, bool forceResend)` method. `void` return matches existing dispatcher pattern (fire-and-forget with no result needed by caller).

---

### `EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs` (service, event-driven)

**Analog:** `EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs` (full file, 43 lines)

**Full pattern** (HangfireQuestEmailDispatcher.cs lines 1–43):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.Jobs;
using Hangfire;

namespace EuphoriaInn.Service.Services;

public class HangfireQuestEmailDispatcher(IBackgroundJobClient jobClient) : IQuestEmailDispatcher
{
    public void EnqueueFinalizedEmail(...)
    {
        jobClient.Enqueue<QuestFinalizedEmailJob>(j => j.ExecuteAsync(...));
    }
}
```
Apply same shape with `IBackgroundJobClient jobClient` constructor param and `jobClient.Enqueue<SessionReminderJob>` call.

---

### `EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs` (service, event-driven)

**Analog:** `EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs` (full file, 35 lines)

**Full pattern** (NullQuestEmailDispatcher.cs lines 1–35):
```csharp
using EuphoriaInn.Domain.Interfaces;

namespace EuphoriaInn.Service.Services;

/// <summary>
/// No-op implementation used in test environments
/// where Hangfire is not registered (IBackgroundJobClient is unavailable).
/// </summary>
public class NullQuestEmailDispatcher : IQuestEmailDispatcher
{
    public void EnqueueFinalizedEmail(...)
    {
        // No-op — Hangfire not available in Testing environment
    }
}
```
Apply same shape for `NullReminderJobDispatcher : IReminderJobDispatcher`. The comment is load-bearing — it explains the Testing guard reason.

---

### `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` (job, event-driven)

**Analog:** `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` (full file, 65 lines)

**Imports + constructor pattern** (QuestFinalizedEmailJob.cs lines 1–12):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Service.Components.Emails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuphoriaInn.Service.Jobs;

public class QuestFinalizedEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestFinalizedEmailJob> logger)
```

**IServiceScopeFactory scope + service resolution pattern** (QuestFinalizedEmailJob.cs lines 25–29):
```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
var renderService   = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();
var emailSettings   = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
```
Add `IReminderLogRepository` to the resolution list.

**Dedup check pattern** (QuestFinalizedEmailJob.cs lines 33–39 — existing per-quest guard):
```csharp
var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);
if (quest?.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
{
    logger.LogInformation("Finalized email already sent for quest {QuestId} on {Date}. Skipping.", questId, finalizedDate);
    return;
}
```
`SessionReminderJob` does per-player dedup inside the loop instead of a single up-front check. Pattern:
```csharp
if (!forceResend && await reminderLog.ExistsAsync(questId, signup.Player.Id, cancellationToken))
{
    logger.LogInformation("Reminder already sent for quest {QuestId} player {PlayerId}, skipping.", questId, signup.Player.Id);
    continue;
}
```

**Email render + send pattern** (QuestFinalizedEmailJob.cs lines 45–61):
```csharp
var html = await renderService.RenderAsync<QuestFinalized>(new Dictionary<string, object?>
{
    { nameof(QuestFinalized.QuestTitle),           questTitle },
    { nameof(QuestFinalized.DmName),               dmName },
    { nameof(QuestFinalized.QuestDate),            finalizedDate },
    { nameof(QuestFinalized.QuestDescription),     questDescription },
    { nameof(QuestFinalized.ConfirmedPlayerNames), playerNames.ToList() },
    { nameof(QuestFinalized.QuestUrl),             questUrl },
    { nameof(QuestFinalized.ChallengeRating),      challengeRating },
    { nameof(QuestFinalized.AppUrl),               emailSettings.AppUrl }
});

await emailService.SendAsync(recipientEmails[i], $"Your quest has been confirmed: {questTitle}", html);
```
Replace component type with `SessionReminder`. All 8 parameter keys are identical — see RESEARCH.md Code Examples section for the exact key list. Note `AppUrl` is a required explicit parameter for `SessionReminder` (unlike some other templates).

**After-send dedup mark** (QuestFinalizedEmailJob.cs line 63):
```csharp
await questRepository.SetFinalizedEmailSentForDateAsync(questId, finalizedDate, cancellationToken);
```
Replace with `await reminderLog.AddAsync(questId, signup.Player.Id, cancellationToken)` inside the per-player loop, after `emailService.SendAsync`.

---

### `EuphoriaInn.Service/Jobs/DailyReminderJob.cs` (job, batch)

**Analog:** `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` for IServiceScopeFactory constructor + scope pattern (lines 10–29). No existing batch/sweep job; this is a new shape.

**Constructor + scope pattern** (QuestFinalizedEmailJob.cs lines 10–29):
```csharp
public class DailyReminderJob(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<DailyReminderJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
```
`IBackgroundJobClient` is a Hangfire singleton — safe to constructor-inject into a Hangfire-activated job.

**Date comparison pattern** (derived from CONTEXT.md D-05 + QuestEntity.cs line 27):
```csharp
var tomorrow = DateTime.Today.AddDays(1);
// FinalizedDate is stored as server local time (no UTC annotation on QuestEntity)
// DateTime.Today is already server local time on the LXC container
```

**Per-quest enqueue loop:**
```csharp
foreach (var quest in quests)
{
    backgroundJobClient.Enqueue<SessionReminderJob>(
        job => job.ExecuteAsync(quest.Id, false, CancellationToken.None));
    logger.LogInformation("Queued SessionReminderJob for quest {QuestId} on {Date}", quest.Id, tomorrow);
}
```

**New IQuestRepository method needed:** `GetFinalizedQuestsForDateAsync(DateTime date)` — add to `IQuestRepository` interface and implement in `QuestRepository`. The implementation follows the existing `GetQuestsWithDetailsAsync` pattern (lines 62–70 in QuestRepository.cs) with an additional `.Where(q => q.FinalizedDate.HasValue && q.FinalizedDate.Value.Date == date.Date)` filter.

---

### `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` (controller, request-response — self-extend)

**Analog:** `QuestController.cs` — `Open` POST action (lines 590–618) and constructor (lines 13–19).

**Constructor extension pattern** (QuestController.cs lines 13–19):
```csharp
public class QuestController(
    IUserService userService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService
    ) : Controller
```
Add `IReminderJobDispatcher reminderJobDispatcher` as a new constructor parameter. This interface lives in Domain (same as `IQuestEmailDispatcher`), resolves to `HangfireReminderJobDispatcher` in non-Testing and `NullReminderJobDispatcher` in Testing environments.

**POST action attribute pattern** (QuestController.cs lines 590–592):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
```
All three attributes are required for `SendReminder`.

**DM identity check pattern** (QuestController.cs lines 602–612 in `Open`):
```csharp
var currentUser = await userService.GetUserAsync(User);
if (currentUser == null)
{
    return Challenge();
}

// Verify DM authorization
if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin"))
{
    return Forbid();
}
```
Apply same guard in `SendReminder` before any job enqueue.

**TempData error pattern** (QuestController.cs lines 573–581):
```csharp
TempData["Error"] = "Please select a date."; return RedirectToAction("Manage", new { id });
```
Use same shape for the not-finalized guard and all error cases.

**TempData success pattern** (ShopController.cs — referenced in RESEARCH.md):
```csharp
TempData["Success"] = $"Reminder queued for {eligibleSignups.Count} players.";
return RedirectToAction("Manage", new { id });
```

**Yes+Maybe voter filter** (see RESEARCH.md Pitfall 1 — filter by the finalized proposed date, not all dates):
```csharp
var finalizedProposedDate = quest.ProposedDates
    .FirstOrDefault(pd => pd.Date.Date == quest.FinalizedDate!.Value.Date);

var eligibleSignups = quest.PlayerSignups
    .Where(ps => ps.DateVotes.Any(dv =>
        dv.ProposedDate?.Id == finalizedProposedDate?.Id &&
        (dv.Vote == VoteType.Yes || dv.Vote == VoteType.Maybe)))
    .ToList();
```

---

### `EuphoriaInn.Service/Views/Quest/Manage.cshtml` (view, request-response — self-extend)

**Analog:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml`

**TempData error block pattern** (Manage.cshtml lines 32–39 — insertion anchor for success block):
```html
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="fas fa-exclamation-triangle me-2"></i>
        @TempData["Error"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```
Add a parallel `TempData["Success"]` block immediately after (before line 41's `@if (Model.IsFinalized)`):
```html
@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="fas fa-check-circle me-2"></i>
        @TempData["Success"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```

**Finalized action button bar pattern** (Manage.cshtml lines 490–507 — the `d-flex justify-content-between` bar where "Open Quest" and "Create Follow-Up Quest" live):
```html
<hr>
<div class="d-flex justify-content-between align-items-center">
    <div class="d-flex gap-2">
        <form asp-action="Open" method="post" style="display: inline;">
            <input type="hidden" name="id" value="@Model.Id" />
            <button type="submit" class="btn btn-warning" onclick="return confirm(...);">Open Quest</button>
        </form>
        @if (Model.FollowUpQuest == null)
        {
            <a href="@Url.Action("CreateFollowUp", ...)" class="btn btn-primary">
                <i class="fas fa-scroll me-2"></i>Create Follow-Up Quest
            </a>
        }
    </div>
    <button type="button" class="btn btn-secondary" onclick="window.location.reload()">Refresh Data</button>
</div>
```
Insert the "Send Reminder" form inside `<div class="d-flex gap-2">` alongside Open and Create Follow-Up:
```html
<form asp-action="SendReminder" asp-route-id="@Model.Id" method="post">
    @Html.AntiForgeryToken()
    <button type="submit" class="btn btn-info">
        <i class="fas fa-envelope me-1"></i>Send Reminder
    </button>
</form>
```
For the force-resend confirm path (D-09), add a second form with a hidden `forceResend=true` field, conditionally shown via `TempData["ReminderAlreadySent"]`.

---

## Shared Patterns

### IServiceScopeFactory (never constructor-inject scoped services into jobs)

**Source:** `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` lines 25–29
**Apply to:** `SessionReminderJob`, `DailyReminderJob`

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
var renderService   = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();
var emailSettings   = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
```

### Testing Environment Guard (IBackgroundJobClient unavailable in Tests)

**Source:** `EuphoriaInn.Service/Program.cs` lines 85–115
**Apply to:** `Program.cs` dispatcher registration, `IReminderJobDispatcher` interface + `HangfireReminderJobDispatcher`/`NullReminderJobDispatcher` implementations

```csharp
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<IReminderJobDispatcher, HangfireReminderJobDispatcher>();
    // ... AddHangfire already registered here
}
else
{
    builder.Services.AddScoped<IReminderJobDispatcher, NullReminderJobDispatcher>();
}
```

### RecurringJob Registration (after ConfigureDatabase, inside Testing guard)

**Source:** `EuphoriaInn.Service/Program.cs` lines 179–185
**Apply to:** `Program.cs` after `app.Services.ConfigureDatabase()`

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();
    // existing SeedShopDataAsync ...

    RecurringJob.AddOrUpdate<DailyReminderJob>(
        "daily-session-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 9 * * *");
}
```

### CSRF + Authorization on POST actions

**Source:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 590–592
**Apply to:** `QuestController.SendReminder`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
```

### DM Identity Check

**Source:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 602–612 (Open action)
**Apply to:** `QuestController.SendReminder`

```csharp
var currentUser = await userService.GetUserAsync(User);
if (currentUser == null) return Challenge();
if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin")) return Forbid();
```

### EF Entity Pattern (IEntity + Table attribute + FK navigation)

**Source:** `EuphoriaInn.Repository/Entities/PlayerDateVoteEntity.cs` lines 1–26
**Apply to:** `ReminderLogEntity`

### OnDelete NoAction FK Configuration

**Source:** `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` lines 50–54 (PlayerSignups → Quest FK)
**Apply to:** Both FK relationships in `ReminderLogEntity` (`QuestId`, `PlayerId`)

---

## No Analog Found

All files have analogs. No entries in this section.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Repository/`, `EuphoriaInn.Domain/`, `EuphoriaInn.Service/Jobs/`, `EuphoriaInn.Service/Services/`, `EuphoriaInn.Service/Controllers/QuestBoard/`, `EuphoriaInn.Service/Views/Quest/`
**Files scanned:** 14 source files read directly
**Pattern extraction date:** 2026-06-26
