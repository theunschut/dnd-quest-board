---
gsd_state_version: 1.0
milestone: v4.0
milestone_name: Email Notifications
current_phase: 25
current_phase_name: confirmation-email-razor-template
status: complete
stopped_at: Milestone v4.0 archived (2026-06-28)
last_updated: "2026-06-28T18:00:00.000Z"
last_activity: 2026-06-28
last_activity_desc: Milestone v4.0 complete — archived to .planning/milestones/
progress:
  total_phases: 6
  completed_phases: 6
  total_plans: 22
  completed_plans: 22
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-28 after v4.0 milestone)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Milestone complete — start `/gsd-new-milestone` for next milestone

## Current Position

Milestone: v4.0 — COMPLETE AND ARCHIVED
All 6 phases (20–25), 22 plans — shipped 2026-06-28.

## Deferred Items

Items acknowledged and deferred at milestone close on 2026-06-28:

| Category | Item | Status |
|----------|------|--------|
| requirement | EMAIL-04 — digest session reminder (multiple same-day quests → one email) | Deferred — same-day quests have never occurred in one year of operation |
| requirement | REMIND-02 — combined reminder for multi-quest days | Deferred — same as EMAIL-04 |

## Accumulated Context

### Key Architectural Decisions (v4.0)

- HtmlRenderer (not IRazorViewEngine) for email templates in background job context
- IServiceScopeFactory + CreateAsyncScope() in every Hangfire job — scoped services cannot be constructor-injected
- IDashboardAuthorizationFilter (not LocalRequestsOnlyAuthorizationFilter) — Docker reverse proxy bypasses localhost check
- NullObject dispatchers (NullQuestEmailDispatcher, NullReminderJobDispatcher) for Testing environment isolation
- FinalizedDate stored as server local time — DateTime.Today.AddDays(1) comparison correct for CET/CEST host
- Resend stats: plain HttpClient GET /emails with Bearer token; no SDK; 5-min IMemoryCache

### Pending for Next Milestone

- Profile picture crop/avatar selection (issue #78) — paused from v2.x; verify SkiaSharp native lib on aspnet:10 Debian Bookworm
- Digest batching (EMAIL-04/REMIND-02) — revisit when same-day quest scheduling becomes common
- Any backlog items in ROADMAP.md

## Session Continuity

Last session: 2026-06-28
Stopped at: Milestone v4.0 archived
Next step: /gsd-new-milestone
