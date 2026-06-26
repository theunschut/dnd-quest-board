---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 23
current_phase_name: next
status: Ready to plan Phase 23
stopped_at: Phase 23 context gathered
last_updated: "2026-06-26T20:07:17.516Z"
last_activity: 2026-06-26
last_activity_desc: Phase 22 verified and human-approved (172 tests green, 5 plans complete)
progress:
  total_phases: 14
  completed_phases: 11
  total_plans: 52
  completed_plans: 47
  percent: 79
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-25)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 23 — Admin Email Stats

## Current Position

Phase: 23 — Admin Email Stats (next)
Plan: Not started
Status: Ready to plan Phase 23 — requires Resend API key provisioning first
Last activity: 2026-06-26 — Phase 22 verified and human-approved (172 tests green, 5 plans complete)

```
Progress: [████████████████████] Phase 22 complete — advancing to Phase 23
```

## Accumulated Context

### Decisions

- Email provider: Resend.com (already in use as SMTP relay); API key needed separately for stats dashboard
- Background jobs: Hangfire (user has prior experience; preferred over plain IHostedService)
- Reminder timing: 24 hours before confirmed session date
- Digest batching: one combined email per player when multiple quests share the same session date
- Email limit: 100/day, 3000/month; 17 members (growing) — batch-first design avoids hitting limits
- Template rendering: HtmlRenderer (Microsoft.AspNetCore.Components.Web, built into .NET 10) — NOT IRazorViewEngine (throws NullReferenceException in background job context)
- Hangfire dashboard auth: custom IDashboardAuthorizationFilter checking Admin role — NOT LocalRequestsOnlyAuthorizationFilter (bypassed by Docker reverse proxy)
- Hangfire job scope: IServiceScopeFactory + CreateAsyncScope() inside job method — never inject scoped services via constructor
- Resend stats: plain HttpClient calling GET /emails with Bearer token — no Resend SDK added
- FinalizedDate timezone: must verify UTC vs. local storage before writing the Phase 22 job date comparison
- [Phase 20]: UseHangfireDashboard must also be guarded by !IsEnvironment(Testing) — Hangfire calls ThrowIfNotConfigured inside UseHangfireDashboard which fails when AddHangfire was skipped
- [Phase 20]: Pre-Hangfire redirect middleware placed inside !IsEnvironment('Testing') block to preserve test isolation
- [Phase 20]: AdminDashboardAuthFilter Response.Redirect() calls removed — filter now returns true/false only (defense-in-depth)
- [Phase 21 P01]: Domain.csproj requires FrameworkReference to Microsoft.AspNetCore.App for IEmailRenderService to constrain on IComponent — plain Microsoft.NET.Sdk doesn't include aspnetcore types
- [Phase 21 P01]: Legacy typed email methods (SendQuestFinalizedEmailAsync, SendQuestDateChangedEmailAsync) marked [Obsolete] and retained until QuestService decoupling (Plan 03)
- [Phase 21 P03]: IQuestEmailDispatcher pattern used to decouple Domain from Service job types — define interface in Domain, implement in Service, inject at startup
- [Phase 21 P03]: Pre-update quest fetch in UpdateQuestPropertiesWithNotificationsAsync — GetQuestWithDetailsAsync called before repo update to capture old proposed dates for email oldDate param
- [Phase 21 P04]: NullQuestEmailDispatcher registered in Testing environment — HangfireQuestEmailDispatcher requires IBackgroundJobClient which Hangfire only provides in non-Testing; NullObject pattern avoids Hangfire dependency in test hosts
- [Phase 22 P01]: IReminderLogRepository registered with fully-qualified Interfaces.IReminderLogRepository in ServiceExtensions to avoid namespace ambiguity (Domain.Interfaces and Repository.Interfaces both define repository interfaces with same names)
- [Phase 22 P02]: IReminderJobDispatcher placed in Domain.Interfaces (same layer as IQuestEmailDispatcher) — QuestController injects the interface without Service-layer dependency; HangfireReminderJobDispatcher forward-references SessionReminderJob (resolved in Plan 03)
    - [Phase 22 P03]: IReminderLogRepository resolved via C# using alias in SessionReminderJob to avoid ambiguous reference with Domain.Interfaces; same namespace isolation pattern as Plan 01 ServiceExtensions fix
    - [Phase 22 P03]: useYesMaybeVoters parameter added to IReminderJobDispatcher.EnqueueSessionReminder — automated path uses IsSelected, DM trigger path uses Yes+Maybe voters scoped to finalized proposed date
    - [Phase 22 P04]: No controller-side log query for D-09 "already sent" warning — job handles per-player dedup; forceResend=true (from inline confirm form in TempData success block) bypasses log check inside SessionReminderJob
    - [Phase 22 P05]: IBackgroundJobClient.Enqueue<T> is an extension method — unit tests assert via underlying .Create() call count on IBackgroundJobClient mock (NSubstitute cannot intercept extension methods)
    - [Phase 22 P05]: AsyncServiceScope is a struct — mocked via new AsyncServiceScope(Substitute.For<IServiceScope>()) wrapping a substitute scope

### Roadmap Evolution

- Phase 24 added: Email Confirmation Flow — admin button to manually resend confirmation email, EmailConfirmed guard in all email jobs to skip unconfirmed users, confirmation landing endpoint using ASP.NET Identity token flow
- Phase 25 added: Confirmation Email Razor Template — styled Razor component for confirmation email, matching QuestDateChanged style and the shared _EmailLayout

### Pending Todos

- Provision Resend API key before implementing Phase 23 admin stats
- [Resolved] FinalizedDate timezone: confirmed stored as server local time; DateTime.Today.AddDays(1) comparison is correct

### Blockers/Concerns

- **Resend API key:** Stats dashboard requires a Resend API key (separate from SMTP credentials). User must provision it before implementing Phase 23.
- **FinalizedDate timezone:** RESOLVED — FinalizedDate stored as server local time; DateTime.Today.AddDays(1) comparison correct for CET/CEST LXC server.
- **Paused from Milestone 2 — Phase 8 (avatar crop):** Deferred to a future milestone. When resuming, verify SkiaSharp native lib (`libSkiaSharp`) is available in `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian Bookworm).

## Session Continuity

Last session: 2026-06-26T20:07:17.487Z
Stopped at: Phase 23 context gathered
Resume file: .planning/phases/23-admin-email-stats/23-CONTEXT.md

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 20 P03 | 3 | 2 tasks | 1 files |
| Phase 20 P20.1-01 | 5m | 3 tasks | 2 files |
| Phase 21 P01 | 5m | 2 tasks | 9 files |
| Phase 21 P02 | 3m | 2 tasks | 4 files |
| Phase 21 P03 | 7m | 2 tasks | 8 files |
| Phase 21 P04 | 3m | 1 task | 3 files |
| Phase 22 P01 | 4m | 2 tasks | 13 files |
| Phase 22 P02 | 2m | 2 tasks | 4 files |
| Phase 22 P03 | 3m | 2 tasks | 6 files |
| Phase 22 P04 | 5m | 2 tasks | 2 files |
| Phase 22 P05 | 8m | 2 tasks | 3 files |
