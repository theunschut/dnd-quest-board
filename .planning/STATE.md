---
gsd_state_version: 1.0
milestone: v4.0
milestone_name: email-notifications
current_phase: 20
current_phase_name: hangfire-infrastructure
status: context-ready
stopped_at: "Phase 20 context gathered — ready to plan"
last_updated: "2026-06-25T00:00:00.000Z"
last_activity: 2026-06-25
last_activity_desc: Phase 20 context captured (Hangfire Infrastructure)
progress:
  total_phases: 4
  completed_phases: 0
  total_plans: ~
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-25)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Milestone v4.0 — Email Notifications — roadmap ready, Phase 20 (Hangfire Infrastructure) next

## Current Position

Phase: 20 — Hangfire Infrastructure (context ready)
Plan: —
Status: Context captured, ready for planning
Last activity: 2026-06-25 — Phase 20 context captured

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

### Pending Todos

- Verify FinalizedDate timezone storage (UTC vs. local) before implementing Phase 22 reminder job date comparison
- Provision Resend API key before implementing Phase 23 admin stats

### Blockers/Concerns

- **Resend API key:** Stats dashboard requires a Resend API key (separate from SMTP credentials). User must provision it before implementing Phase 23.
- **FinalizedDate timezone:** If stored as local time but Docker container runs UTC, quests may be missed or triggered a day early. Must check EF Core entity config or query the DB before coding Phase 22 job.
- **Paused from Milestone 2 — Phase 8 (avatar crop):** Deferred to a future milestone. When resuming, verify SkiaSharp native lib (`libSkiaSharp`) is available in `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian Bookworm).

## Session Continuity

Last session: 2026-06-25
Stopped at: Roadmap created — Phase 20 ready to plan
Resume file: .planning/phases/20-hangfire-infrastructure/20-CONTEXT.md
