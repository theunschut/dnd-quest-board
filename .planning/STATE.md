---
gsd_state_version: 1.0
milestone: v4.0
milestone_name: email-notifications
current_phase: 21
current_phase_name: HTML Email Templates
status: planning
stopped_at: Phase 21 UI-SPEC approved
last_updated: "2026-06-26T00:00:00.000Z"
last_activity: 2026-06-26
last_activity_desc: Phase 21 UI design contract approved — ready to plan
progress:
  total_phases: 12
  completed_phases: 9
  total_plans: 38
  completed_plans: 38
  percent: 75
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-25)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 21 — html-email-templates

## Current Position

Phase: 21 — HTML Email Templates
Plan: Not started
Status: Ready to plan
Last activity: 2026-06-26 — Phase 21 context discussion complete, ready to plan

```
Progress: [░░░░░░░░░░░░░░░░░░░░] 0/4 phases complete
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

### Pending Todos

- Verify FinalizedDate timezone storage (UTC vs. local) before implementing Phase 22 reminder job date comparison
- Provision Resend API key before implementing Phase 23 admin stats

### Blockers/Concerns

- **Resend API key:** Stats dashboard requires a Resend API key (separate from SMTP credentials). User must provision it before implementing Phase 23.
- **FinalizedDate timezone:** If stored as local time but Docker container runs UTC, quests may be missed or triggered a day early. Must check EF Core entity config or query the DB before coding Phase 22 job.
- **Paused from Milestone 2 — Phase 8 (avatar crop):** Deferred to a future milestone. When resuming, verify SkiaSharp native lib (`libSkiaSharp`) is available in `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian Bookworm).

## Session Continuity

Last session: 2026-06-25
Stopped at: Phase 20 complete, ready to plan Phase 21
Resume file: None

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 20 P03 | 3 | 2 tasks | 1 files |
| Phase 20 P20.1-01 | 5m | 3 tasks | 2 files |
