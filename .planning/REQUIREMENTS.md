# Requirements: D&D Quest Board — Milestone 4: Email Notifications

**Defined:** 2026-06-25
**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## v1 Requirements

### Email Templates

- [ ] **EMAIL-01**: An `IEmailRenderService` backed by `HtmlRenderer` (built-in .NET 10) renders Razor component templates to HTML strings; all outbound emails use this service
- [ ] **EMAIL-02**: The quest-finalization email is sent as styled HTML; if a quest is re-opened and re-finalized with the **same confirmed date**, no duplicate notification is sent (tracked via `FinalizedEmailSentForDate` stored on the quest)
- [ ] **EMAIL-03**: A single-quest session reminder renders as styled HTML using a dedicated Razor component template
- [ ] **EMAIL-04**: A digest session reminder renders multiple quests (same confirmed date, same player) as a single styled HTML email

### Session Reminders

- [ ] **REMIND-01**: A Hangfire recurring job runs daily at 09:00 and sends session reminder emails to all players confirmed for quests whose session is the following day
- [ ] **REMIND-02**: A player confirmed for multiple quests on the same day receives one combined digest email, not a separate email per quest
- [ ] **REMIND-03**: A DM can manually trigger a session reminder from the quest manage page; it dispatches a Hangfire background job using the same send logic as the recurring job
- [ ] **REMIND-04**: Reminders are idempotent — if the Hangfire job retries after a partial failure, already-notified players are not emailed again (tracked via `ReminderSentAt` stored on the quest or a `ReminderLog` table)

### Hangfire Infrastructure

- [x] **JOBS-01**: Hangfire is installed with SQL Server storage sharing the existing database; the `[HangFire]` schema is auto-created on startup — no EF Core migration required
- [x] **JOBS-02**: The Hangfire dashboard is accessible at `/hangfire` and requires the `Admin` role; unauthenticated or non-admin requests are rejected

### Admin Email Stats

- [ ] **STATS-01**: Admin users can view email delivery statistics (sent, delivered, bounced, and failed counts) pulled from the Resend API using a plain `HttpClient` and a configurable `ResendApiKey` in `appsettings`

## Future Requirements

- Vote reminders (remind non-voters to vote on proposed dates) — deferred by user decision
- Webhook-based delivery tracking (real-time bounce/complaint events from Resend) — more complex than polling; revisit at scale
- Per-user email preferences (opt-out of reminders) — defer; group is small and trusting

## Out of Scope

- **Resend SDK for sending** — email delivery continues via SmtpClient → Postfix → Resend SMTP relay; no sending SDK is added
- **Resend batch API** — irrelevant at current scale (17 members, SMTP relay)
- **Email verification on registration** — previously deferred; small trusted group
- **Profile picture avatar crop** (paused from Milestone 2) — resumes in a future milestone

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| EMAIL-01 | Phase 21 | Pending |
| EMAIL-02 | Phase 21 | Pending |
| EMAIL-03 | Phase 21 | Pending |
| EMAIL-04 | Phase 22 | Pending |
| REMIND-01 | Phase 22 | Pending |
| REMIND-02 | Phase 22 | Pending |
| REMIND-03 | Phase 22 | Pending |
| REMIND-04 | Phase 22 | Pending |
| JOBS-01 | Phase 20 | Complete |
| JOBS-02 | Phase 20 | Complete |
| STATS-01 | Phase 23 | Pending |
