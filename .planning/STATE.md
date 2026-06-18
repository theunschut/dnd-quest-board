---
gsd_state_version: 1.0
milestone: v2.0
milestone_name: Omphalos Integration
status: Ready to execute
stopped_at: Phase 10 context gathered — ready to plan
last_updated: "2026-06-18T00:00:00.000Z"
progress:
  total_phases: 3
  completed_phases: 0
  total_plans: 6
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-18)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Milestone v2.0 — Phase 10: Admin Settings (Quest Board)

## Current Position

Phase: Phase 10 — Admin Settings
Plan: Not started
Status: Ready to execute
Last activity: 2026-06-18 — Milestone v2.0 roadmap defined; Phase 10 is next

Progress bar: [----------] 0% (0/6 plans complete)

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

Milestone 2 decisions carried forward as context:
- IIdentityService pattern: Domain defines interface, Repository implements with UserEntity — keeps Identity coupling out of Domain layer
- BaseRepository<TModel, TEntity> implements IBaseRepository<TModel> — all CRUD methods return domain models via AutoMapper
- IOptions<EmailSettings> pattern — EmailService is Scoped, settings are static at startup
- AppUrl fallback to '[Quest Board URL]' literal when empty — preserves existing behavior for unconfigured deployments

Milestone 3 decisions:
- HMAC-SHA256 shared secret for cross-app auth — symmetric, supports future bidirectional API calls
- Username-based user matching between Quest Board and Omphalos — auto-provision on first SSO
- Phase 8 (avatar crop) deferred from Milestone 2 — no strong user demand, SkiaSharp Docker risk
- Admin Settings uses key-value EF entity (AdminSettingEntity: Key nvarchar(200) PK, Value nvarchar(max), UpdatedAt datetime2) — avoids a new migration per settings key in future milestones
- IAdminSettingService registered as Scoped — secret read from DB per-request so settings changes take effect without restart
- Token format contract (TOKEN-02): canonical MAC message is alphabetical query string `expiry={unix_ts}&questId={id}&questTitle={url_encoded_title}&username={lower}`; algorithm HMAC-SHA256; signature lowercase hex; username normalised to lowercase both sides; TTL 300 seconds
- ViewComponent for navbar Omphalos link — injects IAdminSettingService directly, fires once per layout render; avoids base-controller inheritance anti-pattern
- ViewBag for quest-page Omphalos flag — consistent with existing pattern (five other ViewBag flags already on Details/Manage); no new ViewModel wrapper
- Asymmetric secret storage by design: Quest Board stores secret in DB (Admin UI editable); Omphalos reads from QUEST_BOARD_SECRET env var (fail-fast on startup)

### Pending Todos

None yet.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260420-bqj | Fix stale checkboxes and progress table in ROADMAP.md and PROJECT.md | 2026-04-20 | 791d099 | [260420-bqj-fix-stale-checkboxes-and-progress-table-](./quick/260420-bqj-fix-stale-checkboxes-and-progress-table-/) |
| 260420-n5f | Restore HasKey checkbox to user-facing Account/Edit form | 2026-04-20 | e934add | [260420-n5f-restore-haskey-checkbox-to-user-facing-a](./quick/260420-n5f-restore-haskey-checkbox-to-user-facing-a/) |
| 260617-d8u | Proposed dates UX improvement — default to today at 18:00 and auto-advance by 1 day per addition | 2026-06-17 | dd290ab | [260617-d8u-proposed-dates-ux-today-and-next-day](./quick/260617-d8u-proposed-dates-ux-today-and-next-day/) |
| 260617-w1w | Fix #89: Quest Log page shows DM session quests — filter them out | 2026-06-17 | b424fef | [260617-w1w-fix-89-quest-log-page-shows-dm-session-q](./quick/260617-w1w-fix-89-quest-log-page-shows-dm-session-q/) |

### Roadmap Evolution

- Milestone 2 phases 1-9 complete (Phase 8 avatar crop deferred)
- Milestone 3 roadmap defined 2026-06-18: Phases 10-12

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-06-18T00:00:00.000Z
Stopped at: Milestone v2.0 roadmap created — ready to execute Phase 10
Resume file: .planning/ROADMAP.md
