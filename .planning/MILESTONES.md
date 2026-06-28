# Milestones — D&D Quest Board

## v4.0.0 — Email Notifications

**Shipped:** 2026-06-28
**Phases:** 20–25 | **Plans:** 22 | **Tests:** 191 (52 unit + 139 integration)
**Timeline:** 4 days (2026-06-25 → 2026-06-28)
**Files changed:** ~211 | **Lines added:** ~31 000

### Delivered

Upgraded all outbound emails from plain text to styled HTML (Razor components + HtmlRenderer), added automated 24h session reminders via a Hangfire recurring job, a DM manual reminder trigger from the quest manage page, idempotent dedup via a ReminderLog table, an admin email stats dashboard (sent/delivered/bounced/failed) backed by the Resend REST API, and an email confirmation flow with admin resend button and job-level guards for unconfirmed users.

### Key Accomplishments

1. Hangfire background job infrastructure — SQL Server storage, admin-only dashboard at `/hangfire`, `IServiceScopeFactory` pattern established for all jobs
2. All outbound emails upgraded to styled HTML — `_EmailLayout`, `QuestFinalized`, `QuestDateChanged`, `SessionReminder`, `ConfirmEmail` Razor components
3. Quest-finalization dedup guard via `FinalizedEmailSentForDate` column — no duplicate emails on re-finalization
4. 24h automated session reminders — daily CRON job + DM manual trigger, idempotent via ReminderLog table
5. Admin email stats dashboard — live Resend API pull with 5-minute cache, graceful degraded states for missing key / API error
6. Email confirmation flow — admin resend button, `EmailConfirmed` guard on all four email jobs, ASP.NET Identity token callback endpoint

### Known Deferred Items at Close: 2 (see STATE.md Deferred Items)

EMAIL-04 / REMIND-02 — digest batching (single combined email for multiple same-day quests). Explicitly dropped from scope; same-day quests have never occurred in one year of operation.

### Archive

- `.planning/milestones/v4.0-ROADMAP.md`
- `.planning/milestones/v4.0-REQUIREMENTS.md`
