---
phase: 22-session-reminders
plan: "03"
subsystem: service
tags: [hangfire, jobs, recurring-job, idempotency, session-reminders]
dependency_graph:
  requires:
    - 22-01 (IReminderLogRepository, GetFinalizedQuestsForDateAsync)
    - 22-02 (IReminderJobDispatcher, HangfireReminderJobDispatcher)
  provides:
    - SessionReminderJob with per-player idempotency and dual-path support
    - DailyReminderJob recurring sweep at CRON 0 9 * * *
    - RecurringJob 'daily-session-reminders' registered in Program.cs
  affects:
    - EuphoriaInn.Service (new jobs, Program.cs registration)
    - EuphoriaInn.Domain (IReminderJobDispatcher updated with useYesMaybeVoters param)
tech_stack:
  added: []
  patterns:
    - IServiceScopeFactory + CreateAsyncScope() inside job ExecuteAsync (Phase 20 pattern)
    - IBackgroundJobClient constructor injection into recurring job (singleton-safe)
    - Per-player idempotency via IReminderLogRepository.ExistsAsync before send
    - Dual-path player filtering (IsSelected vs Yes+Maybe voters) via useYesMaybeVoters flag
    - using alias (IReminderLogRepository = EuphoriaInn.Repository.Interfaces.IReminderLogRepository) to avoid namespace ambiguity
key_files:
  created:
    - EuphoriaInn.Service/Jobs/SessionReminderJob.cs
    - EuphoriaInn.Service/Jobs/DailyReminderJob.cs
  modified:
    - EuphoriaInn.Service/Program.cs
    - EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs
    - EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs
    - EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs
decisions:
  - "IReminderLogRepository resolved using C# using alias (IReminderLogRepository = EuphoriaInn.Repository.Interfaces.IReminderLogRepository) in SessionReminderJob to avoid ambiguous reference with Domain.Interfaces namespace"
  - "useYesMaybeVoters parameter added to IReminderJobDispatcher.EnqueueSessionReminder to support DM manual trigger (Yes+Maybe voters) vs automated path (IsSelected only) without a second job class"
  - "confirmedNames list always uses IsSelected players for email body context even when useYesMaybeVoters=true — represents who will attend, not who receives reminder"
  - "finalizedProposedDate lookup filters by Date.Date equality to correctly scope Yes+Maybe votes to the finalized proposed date only (RESEARCH.md Pitfall 1)"
metrics:
  duration: 3m
  completed_date: "2026-06-26"
  tasks_completed: 2
  files_changed: 6
---

# Phase 22 Plan 03: Hangfire Jobs Summary

**One-liner:** SessionReminderJob with per-player idempotency and dual-path (automated/DM-trigger) player filtering, plus DailyReminderJob recurring sweep registered at CRON "0 9 * * *".

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | SessionReminderJob with idempotency and useYesMaybeVoters flag | 96b1d65 | SessionReminderJob.cs, IReminderJobDispatcher.cs, HangfireReminderJobDispatcher.cs, NullReminderJobDispatcher.cs |
| 2 | DailyReminderJob and Program.cs recurring job registration | b843eac | DailyReminderJob.cs, Program.cs |

## What Was Built

### SessionReminderJob

Core send unit for both the automated daily sweep and the DM manual trigger. Per-quest, per-player email dispatch with idempotency:

- Resolves `IQuestRepository`, `IReminderLogRepository`, `IEmailRenderService`, `IEmailService`, and `IOptions<EmailSettings>` from an `IServiceScopeFactory`-created async scope (never constructor-injected).
- Guards against null/empty player email addresses with a `string.IsNullOrEmpty` check and logs a warning (T-22-05 threat mitigation).
- Before sending to each player, calls `IReminderLogRepository.ExistsAsync(questId, playerId)` — if a log entry exists and `forceResend` is false, the player is skipped. This implements REMIND-04 idempotency: Hangfire retries on exception will not re-email already-notified players.
- After a successful send, inserts a `ReminderLog` row via `IReminderLogRepository.AddAsync`.
- Renders the `SessionReminder.razor` email template with all 8 `EditorRequired` parameters including `AppUrl`.
- Supports dual-path player filtering via `useYesMaybeVoters` (bool, default false):
  - `false` (automated path, D-06): iterates `quest.PlayerSignups.Where(ps => ps.IsSelected)`
  - `true` (DM trigger path, D-08): iterates players with Yes or Maybe votes on the finalized proposed date, scoped by `ProposedDate.Id` to avoid including voters on other proposed dates (RESEARCH.md Pitfall 1).

### DailyReminderJob

Recurring sweep job registered at `"0 9 * * *"` (09:00 server local time, CET/CEST):

- Constructor-injects `IServiceScopeFactory` and `IBackgroundJobClient` (both singletons — safe for Hangfire activation).
- Uses `DateTime.Today.AddDays(1)` for tomorrow's date (server local time matches FinalizedDate storage — D-05, no timezone conversion needed).
- Queries `IQuestRepository.GetFinalizedQuestsForDateAsync(tomorrow)` for finalized quests scheduled for the next day.
- Enqueues one `SessionReminderJob` per quest via `backgroundJobClient.Enqueue<SessionReminderJob>`.
- Logs informational entries per enqueued quest; returns early with a log message if no quests found.

### Program.cs Registration

`RecurringJob.AddOrUpdate<DailyReminderJob>("daily-session-reminders", ...)` added inside the `!IsEnvironment("Testing")` block, after `app.Services.ConfigureDatabase()` and `await SeedShopDataAsync(app)`. Placement after `ConfigureDatabase()` ensures EF migrations have applied before the job could fire (RESEARCH.md Pitfall 4).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Namespace ambiguity for IReminderLogRepository in SessionReminderJob**

- **Found during:** Task 1 build verification
- **Issue:** Adding `using EuphoriaInn.Repository.Interfaces` to resolve `IReminderLogRepository` caused `CS0104` ambiguous reference for `IQuestRepository` (exists in both `EuphoriaInn.Domain.Interfaces` and `EuphoriaInn.Repository.Interfaces`). Build failed.
- **Fix:** Removed the blanket `using` directive; used a C# using alias `IReminderLogRepository = EuphoriaInn.Repository.Interfaces.IReminderLogRepository` instead. `IQuestRepository` resolves unambiguously from `EuphoriaInn.Domain.Interfaces`. Same approach as the Plan 01 ServiceExtensions fix.
- **Files modified:** `EuphoriaInn.Service/Jobs/SessionReminderJob.cs`
- **Commit:** 96b1d65

**2. [Rule 2 - Missing interface update] IReminderJobDispatcher signature updated for useYesMaybeVoters**

- **Found during:** Task 1 implementation
- **Issue:** The plan action explicitly calls for updating `IReminderJobDispatcher.EnqueueSessionReminder` signature to include `bool useYesMaybeVoters` and passing it through. `NullReminderJobDispatcher` also needed updating to implement the updated interface.
- **Fix:** Updated all three files: `IReminderJobDispatcher.cs`, `HangfireReminderJobDispatcher.cs`, and `NullReminderJobDispatcher.cs` in the same commit.
- **Files modified:** All three files listed above
- **Commit:** 96b1d65

## Verification

- `dotnet build` — 0 errors, 8 warnings (pre-existing NU1510 warnings) — PASSED
- `dotnet test EuphoriaInn.UnitTests` — 28/28 passed — PASSED

## Known Stubs

None. Both jobs are fully wired — `SessionReminderJob` resolves live services via scope and calls real repository/email service methods. `DailyReminderJob` queries real DB via `GetFinalizedQuestsForDateAsync`.

## Threat Flags

None. T-22-05 (null email skip guard) is implemented in SessionReminderJob. T-22-06 (Hangfire retry + per-player dedup) is the core idempotency design. T-22-07 (DateTime.Today local time) is documented in both DailyReminderJob comments and RESEARCH.md.

## Self-Check: PASSED

- EuphoriaInn.Service/Jobs/SessionReminderJob.cs — EXISTS
- EuphoriaInn.Service/Jobs/DailyReminderJob.cs — EXISTS
- EuphoriaInn.Service/Program.cs — MODIFIED (RecurringJob.AddOrUpdate<DailyReminderJob> added)
- EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs — MODIFIED (useYesMaybeVoters param added)
- EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs — MODIFIED (useYesMaybeVoters passed through)
- EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs — MODIFIED (useYesMaybeVoters param added)
- Task 1 commit 96b1d65 — EXISTS
- Task 2 commit b843eac — EXISTS
