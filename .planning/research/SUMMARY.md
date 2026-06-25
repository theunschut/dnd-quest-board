# Project Research Summary

**Project:** D&D Quest Board — Milestone 4 Email Notifications
**Domain:** Background jobs + transactional email + admin ops
**Researched:** 2026-06-25
**Confidence:** HIGH

## Executive Summary

Milestone 4 adds three capabilities to an existing ASP.NET Core 10 MVC app: styled HTML email templates for all outbound notifications, automated and DM-triggered session reminders via Hangfire, and an admin email stats dashboard backed by the Resend REST API. The app already sends email through Postfix → Resend SMTP relay and that path stays unchanged — no new sending mechanism is needed.

The critical architectural decision is **how to render HTML email templates inside Hangfire background jobs**. `IRazorViewEngine` throws `NullReferenceException` in that context because `IHttpContextAccessor.HttpContext` is null. The correct approach is `HtmlRenderer` from `Microsoft.AspNetCore.Components.Web` (built into .NET 10), which renders Razor components without an HTTP context. This decision gates every email send in the milestone.

The two biggest implementation risks are (1) scoped DbContext leaking into Hangfire jobs — every job method must resolve services via `IServiceScopeFactory`, not constructor injection — and (2) duplicate emails on Hangfire retry — a `ReminderSentAt` timestamp column must be added before any reminder job is wired up.

## Key Findings

### Recommended Stack

Two NuGet packages are the entire new footprint: `Hangfire.AspNetCore` 1.8.23 and `Hangfire.SqlServer` 1.8.23, both in `EuphoriaInn.Service`. Hangfire shares the existing SQL Server database under a separate `[HangFire]` schema — no EF Core migration, no second connection string. The `Hangfire.Dashboard.Authorization` package is unnecessary; a custom `IDashboardAuthorizationFilter` is three lines of C#.

The Resend SDK (`Resend` NuGet) is **not added**. Email continues to go out via SmtpClient → Postfix → Resend SMTP relay. For the admin stats dashboard, a plain `HttpClient` calls `GET https://api.resend.com/emails` with a Bearer token — confirmed viable because Postfix-relayed emails do appear in the Resend API response.

**Core technologies:**
- `Hangfire.AspNetCore` 1.8.23: recurring + on-demand background jobs — chosen over `IHostedService` for dashboard, retry, and persistence
- `Hangfire.SqlServer` 1.8.23: job storage on existing DB — no extra infrastructure
- `HtmlRenderer` (`Microsoft.AspNetCore.Components.Web`): Razor-component-to-string rendering without HTTP context — built into .NET 10, zero dependencies
- `HttpClient` (built-in): Resend stats API calls — avoids Resend SDK dependency for read-only use

### Expected Features

**Must have (table stakes):**
- HTML email templates replacing plain text for all outbound notifications
- Quest-finalization email upgraded to HTML (existing feature, new look)
- 24h auto session reminder delivered by Hangfire recurring job (cron `0 9 * * *`)
- DM manual reminder trigger (button on quest manage page → Hangfire enqueue)
- Digest batching: players confirmed for multiple same-day quests get one combined email

**Should have:**
- Admin email stats dashboard: daily/monthly sent/bounced/failed counts from Resend API
- Hangfire dashboard at `/hangfire` (admin-only, via custom `IDashboardAuthorizationFilter`)

**Defer (v2+):**
- Webhook-based delivery tracking (requires registered webhook URL in Resend, more complex than polling)
- Resend batch API sending (irrelevant at 17-member scale using SMTP relay)
- Vote reminders (deferred by user decision)

### Architecture Approach

New components slot cleanly into the existing three-layer architecture. `IEmailRenderService` (interface in Domain) and `RazorEmailRenderService` (implementation in Service using `HtmlRenderer`) keep the rendering concern out of Domain. `SessionReminderJob` lives in Service and is the only class that touches Hangfire directly — Domain and Repository know nothing about jobs. Digest batching logic lives inside `SessionReminderJob` (group players before calling `IEmailService`), not in `IEmailService` itself. The Resend stats HTTP client is a typed `HttpClient` registered in Service DI; Domain exposes an `IEmailStatsService` interface.

**Major components:**
1. `IEmailRenderService` / `RazorEmailRenderService` — renders Razor component templates to HTML strings; used by `IEmailService`
2. `SessionReminderJob` — Hangfire job class; resolves services via `IServiceScopeFactory`; groups players, calls `IEmailService`
3. `HangfireAdminAuthFilter` — `IDashboardAuthorizationFilter`; checks `Admin` role; registered after `UseAuthorization`
4. `IEmailStatsService` / `ResendEmailStatsService` — typed HttpClient wrapper for `GET /emails`; paginates and counts by `last_event`

### Critical Pitfalls

1. **Scoped DbContext in Hangfire jobs** — Inject `IServiceScopeFactory`, call `CreateAsyncScope()` inside the job method, resolve services from the scope. Never inject scoped services directly into the job constructor.

2. **Duplicate emails on job retry** — Hangfire retries up to 10 times by default. Add `ReminderSentAt` (nullable DateTime) to the quest or a `ReminderLog` table. Check before sending. This migration must land before the first reminder job runs.

3. **Hangfire dashboard auth bypassed by Docker reverse proxy** — `LocalRequestsOnlyAuthorizationFilter` sees every Docker-proxied request as local. Always use a custom filter checking `httpContext.User.IsInRole("Admin")`. Register the dashboard **after** `UseAuthentication` + `UseAuthorization`.

4. **`IRazorViewEngine` throws in background jobs** — `IHttpContextAccessor.HttpContext` is null outside an HTTP request. Use `HtmlRenderer` (Razor components) — no HTTP context dependency.

5. **UTC vs. local date boundary for reminders** — If `FinalizedDate` is stored as local time but the Docker container runs UTC, quests may be missed or triggered a day early. Verify storage timezone before coding the job comparison.

## Implications for Roadmap

Phases 20+ (continuing from Phase 19):

### Phase 20: Hangfire Infrastructure
**Rationale:** Every subsequent phase depends on Hangfire being wired up safely. Pitfalls 1, 2, 3 must be resolved before any job is registered.
**Delivers:** `AddHangfire` + `AddHangfireServer`, `HangfireAdminAuthFilter`, dashboard at `/hangfire` (admin-only), `IServiceScopeFactory` pattern established.
**Avoids:** Dashboard auth bypass (Pitfall 3).

### Phase 21: HTML Email Templates
**Rationale:** All email sends in subsequent phases depend on the rendering approach. Validating `HtmlRenderer` with the existing finalization email de-risks Phase 22.
**Delivers:** `IEmailRenderService` + `RazorEmailRenderService`, `_EmailLayout.razor`, `QuestFinalized.razor` component, existing finalization send path switched to HTML.
**Avoids:** `IRazorViewEngine` NullReferenceException (Pitfall 4).

### Phase 22: Session Reminders
**Rationale:** Builds on Phase 20 (Hangfire) and Phase 21 (HTML templates). Digest batching included here — it is a grouping loop, not a separate system.
**Delivers:** `ReminderSentAt` column + migration, `GetQuestsDueTomorrowAsync` repository query, `SessionReminderJob` (recurring `0 9 * * *` + `DisableConcurrentExecution`), `SessionReminder.razor` + digest variant, DM manual trigger button on quest manage page.
**Avoids:** Duplicate email on retry (Pitfall 2), scoped DbContext leak (Pitfall 1).

### Phase 23: Admin Email Stats
**Rationale:** Fully independent of Phases 21–22; only needs Resend API key config. Lowest risk, slotted last.
**Delivers:** `IEmailStatsService` + `ResendEmailStatsService` (typed HttpClient, paginates `GET /emails`, counts by `last_event`), `ResendApiKey` in `EmailSettings`, admin stats view.

### Phase Ordering Rationale
- Phase 20 first: Hangfire safety patterns must be in place before any job ships
- Phase 21 second: rendering approach gates all HTML sends; upgrading finalization email validates end-to-end
- Phase 22 third: core value delivery of the milestone
- Phase 23 last: independent, can be dropped if time-constrained without affecting other phases

### Research Flags

Standard patterns (skip research-phase during planning):
- **Phase 20:** Official Hangfire docs + Context7 verified — no surprises
- **Phase 21:** `HtmlRenderer` is official Microsoft API — confirmed working
- **Phase 23:** Resend `GET /emails` is simple REST — no surprises expected

Needs care during planning:
- **Phase 22:** Verify `FinalizedDate` timezone storage before writing the date comparison in the job (Pitfall 5)

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack (packages + versions) | HIGH | NuGet confirmed; .NET 10 compatibility verified |
| Features | HIGH | All behaviors verified against official docs |
| Architecture | HIGH | Layer placements derived from existing codebase dependency rules |
| Pitfalls | HIGH | All 5 verified against official Hangfire docs + GitHub issues |

**Overall confidence:** HIGH

### Gaps to Address

- **FinalizedDate timezone:** Query the DB or check EF Core entity config to confirm UTC vs. local storage before writing the reminder job.
- **HtmlRenderer CSS inlining:** Outlook strips `<style>` blocks. Decide during Phase 21 whether to inline CSS manually or use `PreMailer.Net` — simple first.

---
*Research completed: 2026-06-25*
*Ready for roadmap: yes*
