---
gsd_state_version: 1.0
milestone: v4.0
milestone_name: email-notifications
current_phase: ~
current_phase_name: ~
status: defining-requirements
stopped_at: "Milestone v4.0 started — defining requirements"
last_updated: "2026-06-25T00:00:00.000Z"
last_activity: 2026-06-25
last_activity_desc: Milestone v4.0 (Email Notifications) started
progress:
  total_phases: ~
  completed_phases: 0
  total_plans: ~
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-25)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Defining requirements for Milestone v4.0 — Email Notifications

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-06-25 — Milestone v4.0 (Email Notifications) started

## Accumulated Context

### Decisions

- Email provider: Resend.com (already in use as SMTP relay); API key needed separately for stats dashboard
- Background jobs: Hangfire (user has prior experience; preferred over plain IHostedService)
- Reminder timing: 24 hours before confirmed session date
- Digest batching: one combined email per player when multiple quests share the same session date
- Email limit: 100/day, 3000/month; 17 members (growing) — batch-first design avoids hitting limits

### Pending Todos

None yet.

### Blockers/Concerns

- **Paused from Milestone 2 — Phase 8 (avatar crop):** Deferred to a future milestone. When resuming, verify SkiaSharp native lib (`libSkiaSharp`) is available in `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian Bookworm). Fallback: CSS `object-position` crop-display without server-side crop.
- **Resend API key:** Stats dashboard requires a Resend API key (separate from SMTP credentials). User must provision it before implementing the admin stats feature.

## Session Continuity

Last session: 2026-06-25
Stopped at: Milestone v4.0 started — requirements being defined
Resume file: None
