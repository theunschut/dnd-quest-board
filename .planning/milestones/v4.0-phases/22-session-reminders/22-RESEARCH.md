# Phase 22: Session Reminders - Research

**Researched:** 2026-06-26
**Domain:** Hangfire recurring jobs, EF Core migration, ASP.NET Core MVC controller POST action
**Confidence:** HIGH

---

## Summary

Phase 22 adds session reminder emails by introducing three new components: a `ReminderLog` entity for per-player idempotency, a `DailyReminderJob` recurring Hangfire job, and a `SessionReminderJob` fire-and-forget job that both the daily sweep and a new DM controller action invoke.

All patterns have live precedents in the codebase. `QuestFinalizedEmailJob` is a drop-in template for `SessionReminderJob` — same `IServiceScopeFactory` scope, same render→dedup→send pipeline, and the same IQuestRepository load pattern. The `ReminderLog` entity mirrors the existing EF pattern. The `QuestController.Manage` action already carries TempData error display and is ready for a new POST action alongside the existing `Open` and `CreateFollowUp` actions.

The one pending concern from STATE.md — FinalizedDate timezone storage — is now resolved by code inspection: `FinalizedDate` has no explicit UTC conversion (`= DateTime.UtcNow` appears only on `CreatedAt`; `FinalizedDate` is set from `selectedDate.Date` which comes from user-entered proposed date datetimes stored as-is). The comparison `FinalizedDate.Value.Date == DateTime.Today.AddDays(1)` will work correctly because both sides are server local time. The app runs on a dedicated LXC container with system local time (CET/CEST); `DateTime.Today` and `DateTime.Now` already reflect that.

**Primary recommendation:** Clone `QuestFinalizedEmailJob.cs` to create `SessionReminderJob.cs`, add `DailyReminderJob.cs` with a per-quest inner loop, register both in `Program.cs`, add `ReminderLog` entity + migration, and add `SendReminder` POST action to `QuestController`.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Use a new `ReminderLog` table — NOT a `ReminderSentAt` column on `Quest`. Per-player granularity for retry safety.
- **D-02:** `ReminderLog` schema: `(Id, QuestId, PlayerId, SentAt)`. Check row existence for `(questId, playerId)` before sending.
- **D-03:** `ReminderLog` added via EF Core migration (same pattern as `FinalizedEmailSentForDate` in Phase 21).
- **D-04:** Hangfire recurring job CRON: `"0 9 * * *"` (09:00 server local time). No timezone conversion needed.
- **D-05:** Date comparison: `FinalizedDate.Value.Date == DateTime.Today.AddDays(1)`.
- **D-06:** Automated job confirmed players = `IsSelected == true` from quest PlayerSignups.
- **D-07:** "Send Reminder" button visible on Manage page only when `IsFinalized = true`. Placed near Open Quest / Follow-Up / Refresh buttons.
- **D-08:** DM trigger sends to Yes + Maybe voters (not finalized confirmed list). Intentional.
- **D-09:** Respects idempotency; if all eligible players already have log entries, show a warning + confirm button for forced re-send.
- **D-10:** TempData success message on enqueue ("Reminder queued for X players."). Stays on Manage page — no redirect.
- **D-11:** DM trigger enqueues `BackgroundJob.Enqueue<SessionReminderJob>` using the `IServiceScopeFactory` pattern.
- **D-12:** Use `SessionReminder.razor` as-is — no modification.
- **D-13:** No `DigestReminder.razor` — digest dropped.
- **D-14:** Two job classes: `DailyReminderJob` (recurring, sweep) and `SessionReminderJob` (per-quest, dedup).
- **D-15:** `SessionReminderJob` follows Phase 20 `IServiceScopeFactory` pattern.

### Claude's Discretion

- Exact column names and indexes on `ReminderLog` (e.g., unique index on `(QuestId, PlayerId)`)
- Whether `DailyReminderJob` enqueues individual `SessionReminderJob`s per quest or processes all inline
- Exact button label
- Whether `forceResend` is a job parameter or separate controller path
- Controller action name for the DM trigger

### Deferred Ideas (OUT OF SCOPE)

- **EMAIL-04:** Digest email for multiple same-day quests — dropped indefinitely.
- **REMIND-02:** One combined digest per player for same-day quests — dropped with EMAIL-04.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| REMIND-01 | Hangfire recurring job runs daily at 09:00, sends session reminder emails to all players confirmed for quests whose session is the following day | DailyReminderJob + RecurringJob.AddOrUpdate pattern verified in Program.cs; date comparison pattern confirmed |
| REMIND-03 | DM can manually trigger a reminder from quest manage page; dispatches Hangfire background job using same send logic | QuestController POST action pattern verified; Manage.cshtml finalized section anchor point confirmed |
| REMIND-04 | Reminders are idempotent — retries don't re-email already-notified players | ReminderLog entity pattern; per-player check before send; verified via QuestFinalizedEmailJob template |
</phase_requirements>

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| ReminderLog entity + migration | Repository | — | EF Core entity; same tier as all other entities |
| ReminderLog domain model | Domain | — | Thin wrapper; follows existing entity→model pattern |
| IReminderLogRepository (if needed) | Repository | Domain (interface) | Domain defines interface, Repository implements |
| SessionReminderJob | Service (Jobs/) | Repository via scope | Hangfire jobs live in Service; resolve DB via IServiceScopeFactory |
| DailyReminderJob | Service (Jobs/) | Repository via scope | Same as above |
| RecurringJob registration | Service (Program.cs) | — | Hangfire middleware is in Program.cs |
| SendReminder controller action | Service (Controllers/) | Domain via IQuestService | Controller triggers job enqueue |
| Manage.cshtml button | Service (Views/) | — | View layer only |

---

## Standard Stack

### Core (already installed)

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| Hangfire | 1.8.x | Background job server; SQL Server storage | [VERIFIED: EuphoriaInn.Service/Program.cs] |
| Hangfire.SqlServer | 1.8.x | SQL Server persistence | [VERIFIED: Program.cs AddHangfire config] |
| EF Core | 10.x | ORM; migration scaffolding | [VERIFIED: EuphoriaInn.Repository.csproj] |
| Microsoft.AspNetCore.Components.Web | built-in (.NET 10) | HtmlRenderer for email rendering | [VERIFIED: Phase 21 delivered] |

### No New Packages Required

All required packages were installed in Phases 20–21. Phase 22 adds no new NuGet dependencies. [VERIFIED: codebase inspection]

---

## Architecture Patterns

### System Architecture Diagram

```
DM clicks "Send Reminder" (POST /Quest/SendReminder/{id})
    └─► QuestController.SendReminder
           ├─ Load quest via IQuestService (Yes+Maybe voters)
           ├─ Check existing ReminderLog entries → all sent? → TempData warning + forceResend flag
           └─ BackgroundJob.Enqueue<SessionReminderJob>(questId, forceResend)
                    │
                    ▼
09:00 CRON (DailyReminderJob)
    └─► Query DB: finalized quests where FinalizedDate.Date == DateTime.Today.AddDays(1)
           └─ For each quest:
                BackgroundJob.Enqueue<SessionReminderJob>(questId, forceResend=false)
                    │
                    ▼
SessionReminderJob.ExecuteAsync(questId, forceResend)
    └─► IServiceScopeFactory.CreateAsyncScope()
           ├─ IQuestRepository.GetQuestWithDetailsAsync(questId)  ← loads PlayerSignups + Player
           ├─ For each eligible player:
           │     ├─ Check ReminderLog (questId, playerId) exists? → skip if yes (unless forceResend)
           │     ├─ IEmailRenderService.RenderAsync<SessionReminder>(parameters)
           │     ├─ IEmailService.SendAsync(player.Email, subject, html)
           │     └─ Insert ReminderLog row
           └─ Done
```

### Recommended Project Structure

```
EuphoriaInn.Repository/
├── Entities/
│   └── ReminderLogEntity.cs          # new
├── Migrations/
│   └── [timestamp]_AddReminderLog/   # new (generated)
└── ReminderLogRepository.cs          # new (or inline in SessionReminderJob)

EuphoriaInn.Domain/
├── Models/
│   └── ReminderLog.cs                # new domain model
└── Interfaces/
    └── IReminderLogRepository.cs     # new (if full repo pattern used)

EuphoriaInn.Service/
├── Jobs/
│   ├── DailyReminderJob.cs           # new
│   └── SessionReminderJob.cs         # new
└── Controllers/QuestBoard/
    └── QuestController.cs            # add SendReminder action
EuphoriaInn.Service/Views/Quest/
    └── Manage.cshtml                 # add Send Reminder button in finalized section
```

### Pattern 1: SessionReminderJob — Clone of QuestFinalizedEmailJob

**What:** Hangfire fire-and-forget job with `IServiceScopeFactory`, per-player loop, dedup check before send.
**When to use:** Both DM manual trigger and DailyReminderJob enqueue this.

```csharp
// Source: EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs (direct template)
public class SessionReminderJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionReminderJob> logger)
{
    public async Task ExecuteAsync(int questId, bool forceResend = false, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository  = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
        var reminderLog      = scope.ServiceProvider.GetRequiredService<IReminderLogRepository>();
        var renderService    = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
        var emailService     = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSettings    = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

        var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);
        if (quest == null) return;

        // Automated job: confirmed players (IsSelected). DM trigger: already filtered before enqueue.
        // SessionReminderJob always works with whoever was passed — filter is the caller's job.
        var questUrl = $"{emailSettings.AppUrl}/Quest/Details/{questId}";
        var confirmedNames = quest.PlayerSignups
            .Where(ps => ps.IsSelected)
            .Select(ps => ps.Character?.Name ?? ps.Player.Name)
            .ToList();

        foreach (var signup in quest.PlayerSignups.Where(ps => ps.IsSelected))
        {
            if (!forceResend && await reminderLog.ExistsAsync(questId, signup.Player.Id, cancellationToken))
            {
                logger.LogInformation("Reminder already sent for quest {QuestId} player {PlayerId}, skipping.", questId, signup.Player.Id);
                continue;
            }

            var html = await renderService.RenderAsync<SessionReminder>(new Dictionary<string, object?>
            {
                { nameof(SessionReminder.QuestTitle),           quest.Title },
                { nameof(SessionReminder.DmName),               quest.DungeonMaster?.Name ?? string.Empty },
                { nameof(SessionReminder.QuestDate),            quest.FinalizedDate!.Value },
                { nameof(SessionReminder.QuestDescription),     quest.Description },
                { nameof(SessionReminder.ConfirmedPlayerNames), confirmedNames },
                { nameof(SessionReminder.QuestUrl),             questUrl },
                { nameof(SessionReminder.ChallengeRating),      quest.ChallengeRating },
                { nameof(SessionReminder.AppUrl),               emailSettings.AppUrl }
            });

            await emailService.SendAsync(signup.Player.Email!, $"Reminder: {quest.Title} is tomorrow", html);
            await reminderLog.AddAsync(questId, signup.Player.Id, cancellationToken);
        }
    }
}
```

[VERIFIED: pattern from QuestFinalizedEmailJob.cs]

### Pattern 2: DailyReminderJob — Recurring Sweep

**What:** Registered as a Hangfire recurring job at `"0 9 * * *"`. Queries for tomorrow's finalized quests; enqueues a `SessionReminderJob` per quest.
**When to use:** Registered once in `Program.cs`.

```csharp
// Source: [ASSUMED] — standard Hangfire recurring job pattern
public class DailyReminderJob(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<DailyReminderJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();

        var tomorrow = DateTime.Today.AddDays(1);
        var quests = await questRepository.GetFinalizedQuestsForDateAsync(tomorrow, cancellationToken);

        foreach (var quest in quests)
        {
            backgroundJobClient.Enqueue<SessionReminderJob>(job => job.ExecuteAsync(quest.Id, false, CancellationToken.None));
            logger.LogInformation("Queued SessionReminderJob for quest {QuestId} on {Date}", quest.Id, tomorrow);
        }
    }
}
```

[VERIFIED pattern structure from Program.cs; IBackgroundJobClient is registered by Hangfire]

### Pattern 3: RecurringJob Registration in Program.cs

**What:** Register `DailyReminderJob` inside the `!IsEnvironment("Testing")` guard, after `UseHangfireServer`. Place after migrations to ensure DB is ready.

```csharp
// Source: EuphoriaInn.Service/Program.cs (existing guard block)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();  // already present
    // ... existing code ...

    RecurringJob.AddOrUpdate<DailyReminderJob>(
        "daily-session-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 9 * * *");
}
```

[VERIFIED: guard block and placement pattern from Program.cs lines 179–185]

### Pattern 4: QuestController SendReminder POST Action

**What:** New POST action on `QuestController`, `DungeonMasterOnly` policy. Loads quest, determines Yes+Maybe voters, checks if all already have log entries, enqueues job, sets TempData, redirects back to Manage.

```csharp
// Source: QuestController.cs — existing pattern from Open/CreateFollowUp actions
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> SendReminder(int id, bool forceResend = false, CancellationToken token = default)
{
    var quest = await questService.GetQuestWithDetailsAsync(id, token);
    if (quest == null) return NotFound();

    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null) return Challenge();

    var isQuestDm = currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase);
    var isAdmin = await userService.IsInRoleAsync(User, "Admin");
    if (!isQuestDm && !isAdmin) return Forbid();

    if (!quest.IsFinalized)
    {
        TempData["Error"] = "Only finalized quests can send reminders.";
        return RedirectToAction("Manage", new { id });
    }

    // Yes + Maybe voters (D-08)
    var eligibleSignups = quest.PlayerSignups
        .Where(ps => ps.DateVotes.Any(dv =>
            dv.Vote == VoteType.Yes || dv.Vote == VoteType.Maybe))
        .ToList();

    backgroundJobClient.Enqueue<SessionReminderJob>(
        job => job.ExecuteAsync(id, forceResend, CancellationToken.None));

    TempData["Success"] = $"Reminder queued for {eligibleSignups.Count} players.";
    return RedirectToAction("Manage", new { id });
}
```

[VERIFIED: TempData["Error"] pattern from QuestController.cs; TempData["Success"] pattern from ShopController.cs]

**IMPORTANT:** `IBackgroundJobClient` must be injected into QuestController constructor — it is the Hangfire DI-registered client. Add it alongside existing constructor params. Only do this in non-Testing environments (NullQuestEmailDispatcher pattern already shows how the project handles this; consider using `IBackgroundJobClient` directly or wrapping in an abstraction).

### Pattern 5: ReminderLog Entity

**What:** New EF Core entity in Repository. Uniquely identifies a "sent reminder" per quest+player combination.

```csharp
// Source: [VERIFIED: existing entity patterns in Entities/]
[Table("ReminderLogs")]
public class ReminderLogEntity : IEntity
{
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
}
```

Add unique index on `(QuestId, PlayerId)` in `QuestBoardContext.OnModelCreating` (matches `PlayerDateVoteEntity` unique index pattern). [VERIFIED: QuestBoardContext.cs line 75–77]

### Pattern 6: Manage.cshtml Send Reminder Button

**Where:** Lines 492–507 of Manage.cshtml — the `d-flex justify-content-between` button bar in the finalized (`Model.IsFinalized`) branch. The button sits alongside "Open Quest" and "Create Follow-Up Quest".

```html
<!-- Source: Manage.cshtml lines 492–507 — existing finalized action button pattern -->
<form asp-action="SendReminder" asp-route-id="@Model.Id" method="post">
    @Html.AntiForgeryToken()
    <button type="submit" class="btn btn-info">
        <i class="fas fa-envelope me-1"></i>Send Reminder
    </button>
</form>
```

The existing Manage view already displays `TempData["Error"]` (line 33–39). A `TempData["Success"]` block must be added alongside it. [VERIFIED: Manage.cshtml lines 32–40]

### Anti-Patterns to Avoid

- **Constructor-inject scoped services into Hangfire jobs:** Hangfire creates job instances outside a DI scope. Always use `IServiceScopeFactory` inside the method body. [VERIFIED: Phase 20 D-05, D-06]
- **Inject `IBackgroundJobClient` into jobs:** Jobs that enqueue other jobs via constructor-injected `IBackgroundJobClient` are fine, but be careful — if DailyReminderJob is a recurring job activated by Hangfire, `IBackgroundJobClient` is a singleton and safe to constructor-inject.
- **Using `DateTime.UtcNow` for the date window:** `DateTime.Today` is server local time, which matches how `FinalizedDate` was stored. Using `DateTime.UtcNow.Date` would shift the comparison by the CET/CEST offset.
- **Missing `!IsEnvironment("Testing")` guard:** `RecurringJob.AddOrUpdate` calls Hangfire internals that throw if AddHangfire was skipped. [VERIFIED: Phase 20 D-11, Program.cs pattern]
- **Querying all quest signups for "Yes+Maybe":** The DM manual trigger needs players who voted Yes/Maybe on the finalized proposed date. `PlayerSignup.DateVotes` contains all votes across all proposed dates — filter by the finalized date's `ProposedDate.Id` to get correct voters.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Idempotency check | Custom flag on Quest | `ReminderLog` table (D-01–D-03) | Per-player granularity; partial retry recovery |
| CRON scheduling | IHostedService with Timer | `RecurringJob.AddOrUpdate` | Hangfire persists schedule, survives app restart, visible in dashboard |
| Email rendering | String.Format HTML | `IEmailRenderService.RenderAsync<SessionReminder>` | Already delivered in Phase 21 |
| Background job execution | Task.Run | `BackgroundJob.Enqueue<T>` | Persisted, retryable, observable |

---

## FinalizedDate Timezone — Resolved

**STATE.md pending item:** "Verify FinalizedDate timezone storage (UTC vs. local) before implementing Phase 22 job date comparison."

**Findings:**
- `QuestEntity.FinalizedDate` is `DateTime?` with no explicit UTC conversion — `= DateTime.UtcNow` is only on `CreatedAt`. [VERIFIED: QuestEntity.cs lines 25, 27]
- `FinalizedDate` is set from `selectedDate.Date` (QuestController.cs line 582), which comes from a user-selected `ProposedDateEntity.Date`. Proposed dates are submitted by users via form posts and stored as-is — they are local time values.
- Comparison `FinalizedDate.Value.Date == DateTime.UtcNow.AddDays(-1).Date` used elsewhere in QuestRepository.cs (line 56) is an approximate "is this quest from yesterday?" heuristic — not the same as the reminder comparison.
- The server runs on CET/CEST (LXC container). `DateTime.Today` returns server local time.

**Conclusion:** The D-05 comparison `FinalizedDate.Value.Date == DateTime.Today.AddDays(1)` is correct. Both sides are server local time. No timezone conversion needed. [VERIFIED: code inspection + CONTEXT.md specifics]

---

## Common Pitfalls

### Pitfall 1: Yes+Maybe voter lookup hits wrong proposed date

**What goes wrong:** `PlayerSignup.DateVotes` holds votes for all proposed dates. If a quest had 3 proposed dates and you filter by `VoteType.Yes || VoteType.Maybe` without date context, you include players who voted Yes on a *different* date than the finalized one.
**Why it happens:** The finalized date is one specific `ProposedDate` record; votes are per-signup per-proposed-date.
**How to avoid:** Join through `ProposedDate` whose `Date.Date == quest.FinalizedDate.Value.Date` to get the correct `PlayerDateVoteEntity` records.
**Warning signs:** More players get reminder emails than expected.

### Pitfall 2: IBackgroundJobClient unavailable in Testing environment

**What goes wrong:** If `QuestController` constructor injects `IBackgroundJobClient`, the Testing environment startup (which skips Hangfire registration) throws a DI resolution exception.
**Why it happens:** Hangfire only registers `IBackgroundJobClient` when `AddHangfire` + `AddHangfireServer` run, which is guarded by `!IsEnvironment("Testing")`.
**How to avoid:** Introduce an abstraction layer (e.g., `IReminderJobDispatcher` with `NullReminderJobDispatcher` in Testing) — matching the `IQuestEmailDispatcher`/`NullQuestEmailDispatcher` pattern already in the codebase. [VERIFIED: Program.cs lines 85–115]
**Warning signs:** Integration tests fail with `InvalidOperationException: No service for type IBackgroundJobClient`.

### Pitfall 3: ReminderLog cascade delete not configured

**What goes wrong:** Deleting a `Quest` or `User` that has `ReminderLog` rows throws a FK violation.
**Why it happens:** SQL Server requires explicit cascade or no-action configuration; EF Core defaults to cascade for required relationships.
**How to avoid:** Configure `OnDelete(DeleteBehavior.NoAction)` for both FK relationships in `QuestBoardContext.OnModelCreating` to match the existing `PlayerSignups` pattern (lines 51–54). [VERIFIED: QuestBoardContext.cs]

### Pitfall 4: RecurringJob.AddOrUpdate called before ConfigureDatabase

**What goes wrong:** The recurring job fires immediately if Hangfire determines it's overdue, but the DB schema hasn't been migrated yet.
**Why it happens:** `RecurringJob.AddOrUpdate` can trigger near-instant execution on first registration.
**How to avoid:** Place `RecurringJob.AddOrUpdate` *after* `app.Services.ConfigureDatabase()` in Program.cs. [VERIFIED: existing pattern lines 179–185]

### Pitfall 5: Email sent to null Email address

**What goes wrong:** `IEmailService.SendAsync` called with a null `Email` on a player who registered before email was required.
**Why it happens:** `UserEntity` inherits `Email` from `IdentityUser<int>` where Email is nullable.
**How to avoid:** Filter eligible players by `!string.IsNullOrEmpty(player.Email)` before sending. Log a warning for skipped players.

---

## Code Examples

### ReminderLog Entity with Unique Index

```csharp
// QuestBoardContext.OnModelCreating — unique index matching PlayerDateVote pattern
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

[VERIFIED: QuestBoardContext.cs lines 51–77 as pattern reference]

### EF Core Migration Command

```bash
# Run from EuphoriaInn.Service/ directory
dotnet ef migrations add AddReminderLog --project ../EuphoriaInn.Repository
```

[VERIFIED: CLAUDE.md and Phase 21 CONTEXT.md D-03]

### TempData Success display in Manage.cshtml

The existing `TempData["Error"]` block (lines 32–40) must be joined by a parallel success block:

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

[VERIFIED: ShopController.cs + Shop views as pattern; Manage.cshtml lines 32–40 as insertion point]

### SessionReminder.razor parameter keys (exact names)

```csharp
// Source: EuphoriaInn.Service/Components/Emails/SessionReminder.razor lines 96–103
// ALL 8 parameters are required (EditorRequired)
{ nameof(SessionReminder.QuestTitle),           quest.Title },
{ nameof(SessionReminder.DmName),               quest.DungeonMaster?.Name ?? string.Empty },
{ nameof(SessionReminder.QuestDate),            quest.FinalizedDate!.Value },
{ nameof(SessionReminder.QuestDescription),     quest.Description },
{ nameof(SessionReminder.ConfirmedPlayerNames), confirmedNames },   // IList<string>
{ nameof(SessionReminder.QuestUrl),             questUrl },
{ nameof(SessionReminder.ChallengeRating),      quest.ChallengeRating },
{ nameof(SessionReminder.AppUrl),               emailSettings.AppUrl },  // ← required! not in QuestFinalized
```

[VERIFIED: SessionReminder.razor lines 96–103 — note AppUrl is an explicit parameter unlike some other email components]

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| `IHostedService` with `Timer` for recurring tasks | `RecurringJob.AddOrUpdate` with CRON | Visible in Hangfire dashboard, persists across restarts |
| Per-quest dedup column (`ReminderSentAt`) | Per-player `ReminderLog` table | Partial retry recovery if job crashes mid-send |

---

## Runtime State Inventory

Not applicable — this is a greenfield feature phase. No rename/refactor involved.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Hangfire (SQL Server) | DailyReminderJob, SessionReminderJob | Yes | 1.8.x | — |
| SQL Server | ReminderLog migration | Yes | shared DB | — |
| dotnet ef tools | Migration generation | Yes | .NET 10 | — |
| IEmailService (SmtpClient) | Email send | Yes | Phase 21 delivered | — |
| IEmailRenderService | Email rendering | Yes | Phase 21 delivered | — |
| SessionReminder.razor | Template | Yes | Phase 21 delivered | — |

No missing dependencies. All prerequisites are in place. [VERIFIED: codebase inspection]

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit (EuphoriaInn.IntegrationTests + EuphoriaInn.UnitTests) |
| Config file | `EuphoriaInn.IntegrationTests/xunit.runner.json` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| REMIND-01 | DailyReminderJob queries tomorrow's finalized quests | unit | `dotnet test EuphoriaInn.UnitTests --filter "DailyReminderJob"` | Wave 0 |
| REMIND-03 | QuestController.SendReminder POST action accessible to DM | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "SendReminder"` | Wave 0 |
| REMIND-04 | SessionReminderJob skips player with existing ReminderLog entry | unit | `dotnet test EuphoriaInn.UnitTests --filter "SessionReminderJob"` | Wave 0 |
| REMIND-04 | ReminderLog unique index prevents duplicate rows | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "ReminderLog"` | Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` — covers REMIND-04 (dedup logic)
- [ ] `EuphoriaInn.UnitTests/Services/DailyReminderJobTests.cs` — covers REMIND-01 (tomorrow date filter)
- [ ] `EuphoriaInn.IntegrationTests/Controllers/QuestReminderTests.cs` — covers REMIND-03 (POST action auth + enqueue)

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | N/A — existing auth stack unchanged |
| V3 Session Management | no | N/A |
| V4 Access Control | yes | `[Authorize(Policy = "DungeonMasterOnly")]` on SendReminder action |
| V5 Input Validation | yes | `questId` is int (parsed), no raw string user input |
| V6 Cryptography | no | N/A |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF on SendReminder POST | Tampering | `[ValidateAntiForgeryToken]` — mandatory per project convention |
| Unauthorized DM triggering reminder for another DM's quest | Elevation of Privilege | Existing DM identity check (`currentUser.Equals(quest.DungeonMaster)`) — same pattern as Open/CreateFollowUp |
| Email enumeration via error messages | Information Disclosure | No email-specific errors surfaced to user; job runs async |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `DailyReminderJob` can constructor-inject `IBackgroundJobClient` (singleton) | Architecture Patterns | Low — IBackgroundJobClient is registered as singleton by Hangfire; verified by Hangfire docs behavior |
| A2 | `GetQuestWithDetailsAsync` loads enough data (PlayerSignups → Player → Email, DateVotes) for reminder job | Code Examples | Medium — if Email field isn't loaded, NullRef at send time. Mitigation: `ProjectWithoutCharacterImages` verified to Include PlayerSignups → Player (QuestRepository.cs lines 307–316) |
| A3 | `User.Email` (from IdentityUser) is populated for all users who can receive email | Code Examples | Low — Identity requires unique email on registration (`RequireUniqueEmail = true` in Program.cs line 50) |

---

## Open Questions

1. **IReminderLogRepository vs inline DbContext access in job**
   - What we know: The existing jobs (`QuestFinalizedEmailJob`) call `IQuestRepository` methods rather than direct DbContext. `QuestRepository.SetFinalizedEmailSentForDateAsync` is a narrow targeted method.
   - What's unclear: Should `ReminderLog` get a full `IReminderLogRepository` interface + implementation, or can the job access DbContext directly through a scope?
   - Recommendation: Use a minimal `IReminderLogRepository` with `ExistsAsync(questId, playerId)` and `AddAsync(questId, playerId)` — keeps the layer boundary clean and is consistent with existing patterns.

2. **GetFinalizedQuestsForDateAsync — new repository method needed**
   - What we know: No existing `IQuestRepository` method returns quests filtered by `FinalizedDate.Date == someDate`. `GetQuestsWithSignupsAsync` has a different filter.
   - What's unclear: Should this be a new method on `IQuestRepository` or on a new `IReminderRepository`?
   - Recommendation: Add `GetFinalizedQuestsForDateAsync(DateTime date)` to `IQuestRepository` — it's a quest query and belongs with other quest queries.

3. **forceResend implementation for D-09**
   - What we know: D-09 requires a "send again?" confirm path when all reminders already sent.
   - What's unclear: Whether to implement as a hidden form field `forceResend=true` on the same form, or a separate `/Quest/SendReminderForce/{id}` action.
   - Recommendation: Hidden form field `forceResend` in the confirm form — simpler, same action, avoids route proliferation.

---

## Sources

### Primary (HIGH confidence)

- `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` — canonical job pattern (IServiceScopeFactory, render loop, dedup check)
- `EuphoriaInn.Service/Program.cs` — Hangfire registration, `!IsEnvironment("Testing")` guard placement
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — unique index pattern, FK delete behavior
- `EuphoriaInn.Repository/Entities/QuestEntity.cs` — FinalizedDate as `DateTime?`, no UTC annotation
- `EuphoriaInn.Service/Components/Emails/SessionReminder.razor` — exact parameter contract (8 params including AppUrl)
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — finalized action button section (lines 492–507), TempData display (lines 32–40)
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — TempData pattern, DM auth check, constructor

### Secondary (MEDIUM confidence)

- `EuphoriaInn.Repository/QuestRepository.cs` — ProjectWithoutCharacterImages confirms PlayerSignups + Player loaded
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` — Testing environment skips Hangfire (confirms IBackgroundJobClient absent)

### Tertiary (LOW confidence)

None.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages verified present in codebase
- Architecture: HIGH — all patterns have live precedents in the repo
- Pitfalls: HIGH — timezone concern resolved by code inspection; IBackgroundJobClient concern verified by WebApplicationFactoryBase
- FinalizedDate timezone: HIGH — resolved; no UTC annotation on FinalizedDate, stored as local time

**Research date:** 2026-06-26
**Valid until:** 2026-07-26 (stable codebase, 30-day window)
