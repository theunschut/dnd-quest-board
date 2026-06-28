# Milestones — D&D Quest Board

## v1.0 — Architecture & Features

**Shipped:** prior to 2026-06
**Phases:** 1–7, 9 (Phase 8 deferred) | **Plans:** 19

### Delivered

Restored correct layer boundaries (Domain compiles without Repository reference), moved business logic from controllers into services, removed dead code and security gaps, then added four features on the clean architecture: shop filter/sort, follow-up quest creation, DM profile page, and server-side shop pagination. Phase 8 (avatar crop) was deferred pending SkiaSharp native lib verification.

### Key Accomplishments

1. Domain layer no longer references Repository — `EntityProfile.cs` moved, all repository interfaces relocated to Domain
2. `QuestController.Finalize` reduced to <20 lines — all email and finalization logic moved to QuestService
3. Dead code removed: `SecurityConfiguration`, `UpdateQuestPropertiesAsync`, magic number in `SignupRole == 1`
4. Account lockout (5 attempts, 15-min), 8-character minimum password, `.env` removed from git
5. Shop filter/sort by rarity and price with URL-persisted state
6. Follow-up quest creation pre-filling finalized player list
7. DM profile page with bio and photo, editable by Admin

### Archive

- `.planning/milestones/v1.0-ROADMAP.md`
- `.planning/milestones/v1.0-phases/` (phases 01–07, 09)

---

## v3.0 — Mobile Version

**Shipped:** 2026-06-25
**Phases:** 12–19 | **Plans:** 34 | **Tests:** 139 integration tests

### Delivered

Added purpose-built `.Mobile.cshtml` view variants for all player, DM, admin, and shop pages via a `MobileDetectionMiddleware` + `MobileViewLocationExpander` pipeline. Zero changes to controllers, ViewModels, repositories, or domain services — the entire feature is additive to the Service layer's Views directory and static assets.

### Key Accomplishments

1. Mobile detection + view-expander infrastructure — one-time middleware enabling all subsequent phases
2. `mobile.css` baseline with 44px touch targets enforced site-wide
3. Agenda-style mobile calendar replacing 7-column grid — fully readable on small screens
4. 19 `.Mobile.cshtml` view files + 14 dedicated mobile CSS files across all app areas
5. 139 integration tests covering all mobile view routes with mobile User-Agent header

### Archive

- `.planning/milestones/v3.0-ROADMAP.md`
- `.planning/milestones/v3.0-phases/` (phases 12–19)

---

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
- `.planning/milestones/v4.0-phases/` (phases 20–25)
