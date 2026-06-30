---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: Multi-Tenancy
status: ready to execute
stopped_at: Phase 28 planned
last_updated: "2026-06-30T00:00:00Z"
last_activity: 2026-06-30 — Phase 28 planned (3 plans, 3 waves)
progress:
  total_phases: 5
  completed_phases: 2
  total_plans: 8
  completed_plans: 6
  percent: 40
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-29 — v5.0 Multi-Tenancy started)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 28 — tenant-isolation

## Current Position

Phase: 28
Plan: Not started
Status: Ready to execute
Last activity: 2026-06-30 — Phase 28 planned (3 plans)

```
v5.0 Progress [========            ] 40% (2/5 phases)
Phase 26 Namespace Rename       [x] complete (2026-06-29)
Phase 27 Group Schema Foundation [x] complete (2026-06-30)
Phase 28 Tenant Isolation        [ ] not started
Phase 29 SuperAdmin + Mgmt Area  [ ] not started
Phase 30 Group UX + User Mgmt   [ ] not started
```

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

### Key Architectural Decisions (v5.0)

- IActiveGroupContext defined in Domain layer (not Service) — QuestBoardContext in Repository must consume it; Repository depends on Domain
- ActiveGroupContextService in Service layer reads ActiveGroupId from ASP.NET Core Session; returns null when user holds SuperAdmin role
- EF Core Global Query Filters applied to QuestEntity and ShopItemEntity only — UserEntity must NOT receive a filter (breaks Identity)
- Per-group roles live in UserGroups.GroupRole — AspNetUserRoles is used only for SuperAdmin (system-wide)
- AdminHandler and DungeonMasterHandler read GroupRole from UserGroups for the active group; SuperAdmin Identity role bypasses both handlers
- SuperAdmin management area routed at /platform (not /superadmin)
- Phase 26 is a pure rename — zero behavior change required; all 191 tests must pass before merging
- Phase 28 is highest complexity — test factory stub and Hangfire adaptation must land in same PR as HasQueryFilter
- GroupRole stored as int on UserGroupEntity; enum cast at AutoMapper boundary (consistent with SignupRole/CharacterStatus/CharacterRole patterns)
- UserGroupEntity uses auto-increment int PK with composite unique index on (UserId, GroupId) — not a composite PK (avoids EF composite-PK pitfall)
- Quest/ShopItem→Group FKs use NoAction delete to prevent SQL Server cascade cycle errors
- UserGroup→User and UserGroup→Group FKs use Cascade so membership rows clean up automatically
- Groups.Name has a DB-layer unique index (D-08)

### Pending for Next Milestone

- Profile picture crop/avatar selection (issue #78) — paused from v2.x; verify SkiaSharp native lib on aspnet:10 Debian Bookworm
- Digest batching (EMAIL-04/REMIND-02) — revisit when same-day quest scheduling becomes common
- Any backlog items in ROADMAP.md

## Session Continuity

Last session: 2026-06-30T10:30:00Z
Stopped at: Phase 27 complete — all 3 plans verified
Next step: Plan Phase 28 — Tenant Isolation (IActiveGroupContext + EF Global Query Filters)

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 26 P02 | 12 | 3 tasks | 0 files |
| Phase 27 P01 | 15 | 2 tasks | 10 files |
| Phase 27 P02 | 25 | 2 tasks + checkpoint | 4 files |
| Phase 27 P03 | 15 | 1 task + checkpoint | 1 file |
