---
gsd_state_version: 1.0
milestone: v2.0
milestone_name: Omphalos Integration
current_phase: 30
current_phase_name: ready to plan
status: executing
stopped_at: Phase 30 UI-SPEC approved
last_updated: "2026-06-30T19:51:21.454Z"
last_activity: 2026-06-30
last_activity_desc: Phase 29 complete (UAT passed; 219/219 tests; advancing to Phase 30)
progress:
  total_phases: 5
  completed_phases: 4
  total_plans: 13
  completed_plans: 13
  percent: 80
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-29 — v5.0 Multi-Tenancy started)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 30 — Group UX & Admin User Creation

## Current Position

Phase: 30 — ready to plan
Plan: Not started
Status: Ready to execute
Last activity: 2026-06-30 — Phase 29 complete (UAT passed; 219/219 tests; advancing to Phase 30)

```
v5.0 Progress [██████████] 100% (4/5 phases complete — Phase 30 not started)
Phase 26 Namespace Rename        [x] complete (2026-06-29)
Phase 27 Group Schema Foundation [x] complete (2026-06-30)
Phase 28 Tenant Isolation        [x] complete (2026-06-30)
Phase 29 SuperAdmin + Mgmt Area  [x] complete (2026-06-30)
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
- Hangfire jobs resolve ActiveGroupContextService (concrete, not interface) to call SetGroupId(groupId) before any repo call; SetGroupId is on the concrete class only (D-09)
- GetQuestsForTomorrowAllGroupsAsync uses IgnoreQueryFilters() for cross-group sweep — method name makes intent explicit; only one call site in codebase (D-08)
- QuestController passes activeGroupContext.ActiveGroupId ?? 1 to EnqueueSessionReminder — null means no session (Phase 28 temporary); GroupId=1 is correct single-group fallback until Phase 30 enforces group selection
- TestDataHelper.ClearDatabaseAsync preferred over factory.ResetDatabase() in isolation tests — former also seeds roles and Group 1 FK dependency preventing FK constraint failures
- Phase 28 human verify: quest list, shop, Send Reminder all confirmed working; empty /players is pre-existing dev-DB issue (AspNetUserRoles empty after Phase 26 rename + DB reset) not caused by Phase 28
- Phase 29 Plan 01: AuthenticationHelper must seed UserGroups rows for DM/Admin test users (group ID 1) alongside AspNetUserRoles — auth handlers now read UserGroups.GroupRole exclusively; tests that set "DungeonMaster"/"Admin" in the auth header must have matching UserGroups membership
- Phase 29 Plan 01: xUnit v3 IAsyncLifetime requires ValueTask return types (not Task) — TenantIsolationTests fixed
- Phase 29 Plan 03: IGroupRepository interface lives in QuestBoard.Domain/Interfaces/ (same as IUserRepository pattern) — Domain must not reference Repository
- Phase 29 Plan 05: SessionKeys.ActiveGroupId is never written pre-Phase-30 — GetAllPlayers, GetAllDungeonMasters, and AdminController.Users use `?? 1` fallback so queries work against EuphoriaInn group; **Phase 30 must remove these three `?? 1` fallbacks** once the group-picker sets the session key at login, otherwise group isolation breaks for multi-group deployments
- Phase 29 Plan 03: GroupWithMemberCount is a plain DTO (not AutoMapper-mapped) — LINQ projection from GroupEntity.UserGroups.Count in a single query; no EntityProfile mapping needed
- Phase 29 Plan 03: GroupService.AddAsync overrides base to enforce non-blank name and stamp CreatedAt; DbUpdateException for unique name violation bubbles to GroupController (plan 29-04)
- Phase 29 Plan 04: UserGroup.User? navigation property added to domain model + EntityProfile mapping — GetMembersAsync uses .Include(ug => ug.User) so data was available but AutoMapper did not surface it; Members view requires Name/Email per row
- Phase 29 Plan 04: _Layout.Platform.cshtml links only site.css — page-specific CSS files (calendar.css, quests.css, etc.) excluded from platform area
- Phase 29 Plan 02 (D-11): First SuperAdmin user assignment is a manual post-deploy step — run once after deployment:
  ```sql
  -- Assign first SuperAdmin user (run once after deploy)
  -- Find userId in AspNetUsers WHERE UserName = '<username>'
  INSERT INTO AspNetUserRoles (UserId, RoleId)
  VALUES (<userId>, 4);
  ```

### Pending for Next Milestone

- Profile picture crop/avatar selection (issue #78) — paused from v2.x; verify SkiaSharp native lib on aspnet:10 Debian Bookworm
- Digest batching (EMAIL-04/REMIND-02) — revisit when same-day quest scheduling becomes common
- Any backlog items in ROADMAP.md

## Session Continuity

**Resume file:** .planning/phases/30-group-ux-admin-user-creation/30-UI-SPEC.md

Last session: 2026-06-30T19:17:00.939Z
Stopped at: Phase 30 UI-SPEC approved
Next step: /gsd-plan-phase 30

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 26 P02 | 12 | 3 tasks | 0 files |
| Phase 27 P01 | 15 | 2 tasks | 10 files |
| Phase 27 P02 | 25 | 2 tasks + checkpoint | 4 files |
| Phase 27 P03 | 15 | 1 task + checkpoint | 1 file |
| Phase 28 P01 | 4 | 2 tasks | 9 files |
| Phase 28 P02 | 6 | 2 tasks | 17 files |
| Phase 28 P03 | 41 | 1 task + checkpoint | 1 file |
| Phase 29 P01 | 8 | 3 tasks | 10 files |
| Phase 29 P02 | 5 | 1 task | 3 files |
| Phase 29 P03 | 7 | 2 tasks | 7 files |
| Phase 29 P04 | 5 | 2 tasks | 17 files |
| Phase 29 P05 | 8 | 2 tasks + checkpoint | 5 files |
