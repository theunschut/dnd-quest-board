---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: Multi-Tenancy
status: executing
stopped_at: Phase 29 Plan 03 complete — Group service layer (IGroupService, IGroupRepository, GroupService, GroupRepository, DI registrations)
last_updated: "2026-06-30T13:37:00Z"
last_activity: 2026-06-30 — Phase 29 Plan 03 executed (Group service layer with EF Core GroupRepository; 197/197 tests pass)
progress:
  total_phases: 5
  completed_phases: 3
  total_plans: 14
  completed_plans: 11
  percent: 71
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-29 — v5.0 Multi-Tenancy started)

**Core value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.
**Current focus:** Phase 29 — SuperAdmin Role & Management Area

## Current Position

Phase: 29 executing — Plan 03 complete
Plan: 29-04 is next (Platform MVC Area: GroupController, 5 views, _Layout.Platform.cshtml)
Status: Executing Phase 29 (3/5 plans done)
Last activity: 2026-06-30 — Phase 29 Plan 03 complete (Group service layer; IGroupService/IGroupRepository/GroupService/GroupRepository; 197/197 tests pass)

```
v5.0 Progress [===========         ] 67% (3/5 phases, 10/14 plans in phases 26-29)
Phase 26 Namespace Rename        [x] complete (2026-06-29)
Phase 27 Group Schema Foundation [x] complete (2026-06-30)
Phase 28 Tenant Isolation        [x] complete (2026-06-30)
Phase 29 SuperAdmin + Mgmt Area  [~] executing (3/5 plans done — auth handlers, migration, group service done)
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
- Phase 29 Plan 03: GroupWithMemberCount is a plain DTO (not AutoMapper-mapped) — LINQ projection from GroupEntity.UserGroups.Count in a single query; no EntityProfile mapping needed
- Phase 29 Plan 03: GroupService.AddAsync overrides base to enforce non-blank name and stamp CreatedAt; DbUpdateException for unique name violation bubbles to GroupController (plan 29-04)
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

Last session: 2026-06-30T13:37:00Z
Stopped at: Phase 29 Plan 03 complete — Group service layer (IGroupService, IGroupRepository, GroupWithMemberCount, GroupService, GroupRepository, DI registrations; 197/197 tests pass)
Next step: Execute Phase 29 Plan 04 — Platform MVC Area (/platform): GroupController, 5 Razor views, _Layout.Platform.cshtml, _ViewImports.cshtml, _ViewStart.cshtml, PlatformViewModels, area route in Program.cs

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
