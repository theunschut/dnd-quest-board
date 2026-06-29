---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: Multi-Tenancy
current_phase: 26
current_phase_name: namespace-rename
status: verifying
stopped_at: Phase 26 plan 01 complete — awaiting plan 02 (test gate + atomic commit)
last_updated: "2026-06-29T21:02:24.758Z"
last_activity: 2026-06-29
last_activity_desc: "Plan 01 complete: 5 dirs renamed, 343 files patched, build green"
progress:
  total_phases: 1
  completed_phases: 1
  total_plans: 2
  completed_plans: 2
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-29 — v5.0 Multi-Tenancy started)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 26 — namespace-rename

## Current Position

Phase: 26 (namespace-rename) — EXECUTING
Plan: 2 of 2 (plan 01 complete; plan 02 owns test gate + atomic commit)
Status: Phase complete — ready for verification
Last activity: 2026-06-29 — Plan 01 complete: 5 dirs renamed, 343 files patched, build green

```
v5.0 Progress [                    ] 0% (0/5 phases)
Phase 26 Namespace Rename       [ ] not started
Phase 27 Group Schema Foundation [ ] not started
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

### Pending for Next Milestone

- Profile picture crop/avatar selection (issue #78) — paused from v2.x; verify SkiaSharp native lib on aspnet:10 Debian Bookworm
- Digest batching (EMAIL-04/REMIND-02) — revisit when same-day quest scheduling becomes common
- Any backlog items in ROADMAP.md

## Session Continuity

**Resume file:** .planning/phases/26-namespace-rename/26-01-SUMMARY.md

Last session: 2026-06-29T21:02:24.749Z
Stopped at: Phase 26 plan 01 complete — directories renamed, 343 files patched, dotnet build green; NO commit yet
Next step: Execute plan 02 (dotnet test gate + single atomic commit D-05)

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 26 P02 | 12 | 3 tasks | 0 files |
