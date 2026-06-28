# Phase 22: Session Reminders - Context

**Gathered:** 2026-06-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a Hangfire recurring job that runs daily at 09:00 (server local time) and sends `SessionReminder` emails to all confirmed players for quests whose `FinalizedDate` falls the following day. A DM can also manually trigger a reminder from the Quest Manage page, which sends to Yes + Maybe voters via a Hangfire fire-and-forget job. All reminder sends are tracked in a new `ReminderLog` table for idempotent retry behavior.

**Scope reduction — digest dropped:** EMAIL-04 (digest email) and REMIND-02 (one combined digest per player) are removed from scope. Same-day quests have never occurred in one year of operation and are not expected. Always send one `SessionReminder.razor` email per quest per player. If digest becomes necessary in the future, it can be added as its own phase.

</domain>

<decisions>
## Implementation Decisions

### Idempotency Tracking
- **D-01:** Use a new `ReminderLog` table — NOT a `ReminderSentAt` column on `Quest`. This gives per-player granularity: if the job sends reminders to 10 players and crashes after 7, only the 3 un-notified players are retried.
- **D-02:** `ReminderLog` schema: `(Id, QuestId, PlayerId, SentAt)`. One row per quest+player combination. Before sending to a player, check if a row exists for `(questId, playerId)`; skip if found.
- **D-03:** `ReminderLog` is added via EF Core migration (same pattern as `FinalizedEmailSentForDate` in Phase 21).

### Automated Daily Job
- **D-04:** Hangfire recurring job registered with CRON expression `"0 9 * * *"` (09:00 server local time). Server is a dedicated LXC container using system local time — no timezone conversion needed.
- **D-05:** Date comparison: `FinalizedDate.Value.Date == DateTime.Today.AddDays(1)` — finds all finalized quests whose session date is tomorrow. Quest time-of-day is ignored (same-day window is adequate; sessions are always evening).
- **D-06:** "Confirmed players" for the automated job = players who were selected at finalization (not all Yes/Maybe voters). Use the existing confirmed player list from the quest's PlayerSignups.

### DM Manual Trigger
- **D-07:** "Send Reminder" button is visible on Quest Manage **only when `IsFinalized = true`**. Placed near the other action buttons in the finalized section (alongside Open Quest, Follow-Up, Refresh).
- **D-08:** The DM trigger sends to **Yes + Maybe voters** (not just the finalized confirmed list). No voters are excluded. This is intentional — DMs use the manage page before full finalization sometimes and want to remind all likely attendees.
- **D-09:** Respects idempotency: before enqueuing, check if all eligible players already have `ReminderLog` entries. If so, show a warning: "Reminders have already been sent. Send again?" with a confirm button that forces a re-send (bypasses the log check for that invocation).
- **D-10:** Feedback on enqueue: TempData success message on the Manage page ("Reminder queued for X players."). Stays on the Manage page — no redirect.
- **D-11:** The DM trigger enqueues a Hangfire fire-and-forget job (`BackgroundJob.Enqueue<SessionReminderJob>`), using the same `IServiceScopeFactory` pattern from Phase 20. The job itself checks the `ReminderLog` per-player before sending.

### Email Template
- **D-12:** Use `SessionReminder.razor` (delivered in Phase 21) without modification. Parameter contract already defined. One email per player per quest.
- **D-13:** No `DigestReminder.razor` component — digest email is dropped from scope.

### Job Architecture
- **D-14:** Two job classes:
  1. `DailyReminderJob` — recurring job for the 09:00 automated sweep; queries the DB for tomorrow's quests and enqueues individual per-quest reminder tasks (or sends inline if player count is small).
  2. `SessionReminderJob` — per-quest fire-and-forget job invoked by both the daily sweep and the DM manual trigger; receives questId and optional `forceResend` flag; handles per-player dedup via `ReminderLog`.
- **D-15:** `SessionReminderJob` follows the Phase 20 `IServiceScopeFactory` pattern (never constructor-inject scoped services).

### Claude's Discretion
- Exact column names and indexes on `ReminderLog` table (e.g., unique index on `(QuestId, PlayerId)`)
- Whether `DailyReminderJob` enqueues individual `SessionReminderJob`s per quest or processes all quests inline
- Exact button label ("Send Reminder", "Remind Players", etc.)
- Whether the `forceResend` flag is passed as a job parameter or implemented as a separate `ForceSessionReminderJob`
- Controller action name for the DM trigger endpoint

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap & Requirements
- `.planning/ROADMAP.md` §"Phase 22: Session Reminders" — goal, requirements (EMAIL-04 dropped, REMIND-01, REMIND-03, REMIND-04), and success criteria
- `.planning/REQUIREMENTS.md` §"Session Reminders" — REMIND-01 through REMIND-04 requirement text (note: REMIND-02/EMAIL-04 dropped per discussion)

### Prior Phase Decisions
- `.planning/phases/20-hangfire-infrastructure/20-CONTEXT.md` — IServiceScopeFactory pattern (D-05, D-06), dashboard auth pattern
- `.planning/phases/21-html-email-templates/21-CONTEXT.md` — SessionReminder.razor parameter contract (D-15), HtmlRenderer usage, email visual style

### Existing Jobs (pattern reference)
- `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` — canonical pattern to follow: IServiceScopeFactory, per-player loop, dedup check before send, mark after send
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` (Manage action, line 620) — existing Manage page to add the "Send Reminder" button to
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — Manage view; button goes in the finalized section near Open Quest / Follow-Up / Refresh buttons

### Existing Email Components
- `EuphoriaInn.Service/Components/Emails/SessionReminder.razor` — single-quest reminder template; use as-is
- `EuphoriaInn.Service/Components/Emails/_EmailLayout.razor` — shared layout wrapper

### Existing Entity Patterns
- `EuphoriaInn.Repository/Entities/QuestEntity.cs` — `FinalizedDate` is `DateTime?` (line 27); comparison uses `.Date` not exact tick
- `EuphoriaInn.Domain/Models/EmailSettings.cs` — `AppUrl` field needed for `QuestUrl` in email parameters

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `QuestFinalizedEmailJob.cs` — direct template for `SessionReminderJob`: same IServiceScopeFactory pattern, same render → dedup → send pipeline
- `SessionReminder.razor` (Phase 21) — ready to consume, parameter contract locked: `QuestTitle`, `DmName`, `QuestDate`, `QuestDescription`, `ConfirmedPlayerNames`, `QuestUrl`, `ChallengeRating`
- `IQuestRepository.GetQuestWithDetailsAsync` — already fetches quest with player signups; reuse for the reminder job's data load
- `EmailSettings.AppUrl` — already in DI; use for constructing `QuestUrl`

### Established Patterns
- Hangfire fire-and-forget: `BackgroundJob.Enqueue<TJob>(job => job.ExecuteAsync(...))` — Phase 20 pattern
- Recurring job: `RecurringJob.AddOrUpdate<TJob>("job-id", job => job.ExecuteAsync(), "0 9 * * *")` — register in `Program.cs` after Hangfire middleware
- EF Core migration: `dotnet ef migrations add AddReminderLog --project ../EuphoriaInn.Repository` from `EuphoriaInn.Service/`
- TempData messages: existing pattern in QuestController for success/error feedback on the Manage page

### Integration Points
- `Program.cs` — register `RecurringJob.AddOrUpdate` for `DailyReminderJob` after `UseHangfireServer` (inside `!IsEnvironment("Testing")` guard)
- `QuestController` — add `[HttpPost] SendReminder(int id)` action (DungeonMasterOnly policy)
- `Manage.cshtml` — add form/button in the finalized section alongside existing action buttons
- `EuphoriaInn.Repository/QuestBoardContext.cs` — add `DbSet<ReminderLog>` and configure the table
- AutoMapper `EntityProfile.cs` — map `ReminderLog` if domain model wrapper needed

</code_context>

<specifics>
## Specific Ideas

- **Deployment note:** App runs on a dedicated LXC server (not Docker for production). Server uses system local time (CET/CEST). No timezone conversion needed — `DateTime.Today` and `DateTime.Now` are already server local time.
- **Digest dropped explicitly:** User confirmed same-day quests have never happened in one year. REMIND-02 and EMAIL-04 are deferred indefinitely. Do not implement `DigestReminder.razor`.
- **DM trigger sends to Yes+Maybe:** The DM manual trigger sends to all players who voted Yes or Maybe, not just the finalized "confirmed" list. The automated daily job uses the confirmed list only. These two paths differ intentionally.

</specifics>

<deferred>
## Deferred Ideas

- **EMAIL-04 (Digest email):** A combined digest email for players confirmed for multiple same-day quests. Dropped from Phase 22 scope — same-day quests have never occurred in one year of use. Revisit if the group ever runs parallel campaigns.
- **REMIND-02 (Digest send behavior):** One combined reminder per player for same-day quests. Dropped with EMAIL-04 above.

</deferred>

---

*Phase: 22-session-reminders*
*Context gathered: 2026-06-26*
