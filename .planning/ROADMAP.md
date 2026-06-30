# Roadmap: D&D Quest Board

## Milestones

- ✅ **v1.0 Architecture & Features** — Phases 1–7, 9 (shipped prior to 2026-06)
- 🚧 **v2.0 Omphalos Integration** — Phases 10–11 (in progress — branch: `milestone/3-omphalos-integration`)
- ✅ **v3.0 Mobile Version** — Phases 12–19 (shipped 2026-06-25)
- ✅ **v4.0 Email Notifications** — Phases 20–25 (shipped 2026-06-28)
- 🚧 **v5.0 Multi-Tenancy** — Phases 26–30 (in progress)

_Note: Phase 8 (profile picture avatar crop) was scoped in v1.0 but deferred; it is not assigned to any active milestone._

## Phases

<details>
<summary>✅ v1.0 Architecture & Features (Phases 1–7, 9) — SHIPPED prior to 2026-06</summary>

**Overview:** Restored correct layer boundaries (Domain ← Repository), consolidated business logic into services, removed dead code and security gaps, then added four backlog features on the clean architecture. Phase 8 (avatar crop) was deferred.

- [x] Phase 1: Layer Dependency Fix — 2/2 plans — complete
- [x] Phase 2: Email & Service Consolidation — 3/3 plans — complete
- [x] Phase 3: Code Quality & Dead Code — 2/2 plans — complete
- [x] Phase 4: Security Hardening — 4/4 plans — complete
- [x] Phase 5: Shop Filter & Sort — 2/2 plans — completed 2026-04-21
- [x] Phase 6: Follow-Up Quest — 2/2 plans — completed 2026-06-16
- [x] Phase 7: DM Profile Page — 2/2 plans — completed 2026-06-17
- [ ] Phase 8: Profile Picture Avatar Crop — deferred (SkiaSharp native lib unverified on host)
- [x] Phase 9: Shop Pagination — 2/2 plans — complete

</details>

<details>
<summary>🚧 v2.0 Omphalos Integration (Phases 10–11) — IN PROGRESS (branch: milestone/3-omphalos-integration)</summary>

**Overview:** Integrates the Omphalos SSO system for guest navigation token generation. Work is on a separate branch and will be merged after v4.0 lands on main.

- [ ] Phase 10: Omphalos Integration (details on branch `milestone/3-omphalos-integration`)
- [ ] Phase 11: Navigation Token Generation (details on branch `milestone/3-omphalos-integration`)

</details>

<details>
<summary>✅ v3.0 Mobile Version (Phases 12–19) — SHIPPED 2026-06-25</summary>

**Overview:** Added purpose-built `.Mobile.cshtml` view variants alongside all desktop views via a mobile detection middleware + view-location expander. No controllers, ViewModels, repositories, or domain services were modified.

- [x] Phase 12: Mobile Infrastructure — 3/3 plans — completed 2026-06-24
- [x] Phase 13: Core Player Views — 4/4 plans — completed 2026-06-24
- [x] Phase 14: Calendar — 3/3 plans — completed 2026-06-24
- [x] Phase 15: DM Views — 4/4 plans — completed 2026-06-24
- [x] Phase 16: Account & Browse — 4/4 plans — completed 2026-06-25
- [x] Phase 17: Character & Player Views — 4/4 plans — completed 2026-06-25
- [x] Phase 18: DM Editing & Secondary Quest Views — 5/5 plans — completed 2026-06-25
- [x] Phase 19: Admin & Shop Management Views — 7/7 plans — completed 2026-06-25

</details>

<details>
<summary>✅ v4.0 Email Notifications (Phases 20–25) — SHIPPED 2026-06-28</summary>

**Overview:** Styled HTML email templates (Razor + HtmlRenderer), Hangfire background jobs for automated session reminders, admin email stats dashboard backed by Resend REST API, and email confirmation flow with admin resend button.

- [x] Phase 20: Hangfire Infrastructure — 4/4 plans — completed 2026-06-25
- [x] Phase 21: HTML Email Templates — 4/4 plans — completed 2026-06-26
- [x] Phase 22: Session Reminders — 5/5 plans — completed 2026-06-26
- [x] Phase 23: Admin Email Stats — 2/2 plans — completed 2026-06-27
- [x] Phase 24: Email Confirmation Flow — 5/5 plans — completed 2026-06-26
- [x] Phase 25: Confirmation Email Razor Template — 2/2 plans — completed 2026-06-27

</details>

<details>
<summary>🚧 v5.0 Multi-Tenancy (Phases 26–30) — IN PROGRESS</summary>

**Overview:** Transform the Quest Board from a single-tenant EuphoriaInn app into a generic, rebrandable multi-group platform. Namespace rename, group schema with EF Core Global Query Filters, SuperAdmin role and management area, group-picker UX, and admin-only user creation.

- [x] **Phase 26: Namespace Rename** - Rename all EuphoriaInn.* namespaces and project files to QuestBoard.* with zero behavior change (completed 2026-06-29)
- [ ] **Phase 27: Group Schema Foundation** - GroupEntity + UserGroups junction table + GroupId FKs + data migration seeding EuphoriaInn group
- [x] **Phase 28: Tenant Isolation** - IActiveGroupContext + EF Core Global Query Filters + Hangfire adaptation + test factory stub (completed 2026-06-30)
- [~] **Phase 29: SuperAdmin Role & Management Area** - SuperAdmin Identity role + updated authorization handlers + /platform MVC Area for group management (4/5 plans done: auth handlers done; AddSuperAdminRole migration done; group service layer done; platform UI done)
- [ ] **Phase 30: Group UX & Admin User Creation** - Group-picker flow + navigation + self-registration removal + admin user creation

</details>

## Phase Details

### Phase 26: Namespace Rename

**Goal**: The codebase uses QuestBoard.* namespaces consistently with no behavior change and all 191 tests pass
**Depends on**: Nothing (first phase of v5.0)
**Requirements**: RENAME-01, RENAME-02, RENAME-03, RENAME-04
**Success Criteria** (what must be TRUE):

  1. Every C# file uses QuestBoard.* namespaces — no EuphoriaInn.* string remains in source or migration Designer files
  2. All project files (.csproj), the solution file (.slnx), and directory names reflect the QuestBoard naming
  3. All config files (appsettings*.json), GitHub Actions workflows, and deployment references are updated
  4. `dotnet build` succeeds and all 191 existing tests pass with zero behavioral change

**Plans**: 2/2 plans complete
**Wave 1**

- [x] 26-01-PLAN.md — git mv directory/solution/project renames + bulk EuphoriaInn→QuestBoard content replace + dotnet build gate

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 26-02-PLAN.md — full 191-test gate + final grep-clean + human verify + single atomic commit (D-05) + documented production systemd pre-deploy step

### Phase 27: Group Schema Foundation

**Goal**: The database schema supports multiple groups — GroupEntity and UserGroups tables exist, GroupId FKs are on shared-resource entities, and all existing data is correctly seeded into the EuphoriaInn group
**Depends on**: Phase 26
**Requirements**: GROUP-01, GROUP-02, GROUP-03, GROUP-04, GROUP-05, GROUP-06
**Success Criteria** (what must be TRUE):

  1. GroupEntity table exists with Id, Name, CreatedAt; EuphoriaInn group is seeded as GroupId = 1
  2. UserGroups junction table exists with UserId, GroupId, GroupRole (Player/DungeonMaster/Admin enum); all existing users are assigned to EuphoriaInn with their current role
  3. QuestEntity and ShopItemEntity have a non-nullable GroupId FK pointing to GroupEntity
  4. AspNetUserRoles contains no Player, DungeonMaster, or Admin entries after migration — only SuperAdmin assignments remain
  5. All migrations apply cleanly on a fresh database and on the existing production schema

**Plans**: 3 plans

**Wave 1**

- [x] 27-01-PLAN.md — GroupRole enum + Group/UserGroup entities & domain models + GroupId FK on Quest/ShopItem + UserGroups nav + QuestBoardContext config + EntityProfile mapping (model layer, build gate) — completed 2026-06-30

**Wave 2** *(blocked on Wave 1)*

- [x] 27-02-PLAN.md — atomic AddGroupSchema migration (8 FK-safe steps) + TestDataHelper GroupId=1 + full 194-test gate — completed 2026-06-30

**Wave 3** *(blocked on Wave 2)*

- [x] 27-03-PLAN.md — apply migration on dev SQL Server + verify GROUP-04/05/06 seeding + document Phase 27-29 co-deployment constraint — completed 2026-06-30

### Phase 28: Tenant Isolation

**Goal**: All quests and shop items are scoped to the active group via EF Core Global Query Filters; Hangfire jobs cross-group correctly; all existing tests pass with the filter in place
**Depends on**: Phase 27
**Requirements**: TENANT-01, TENANT-02, TENANT-03, TENANT-04, TENANT-05
**Success Criteria** (what must be TRUE):

  1. IActiveGroupContext is defined in the Domain layer; ActiveGroupContextService reads the active group from ASP.NET Core Session in the Service layer
  2. A user in Group A cannot see quests or shop items belonging to Group B under any normal navigation path
  3. All four Hangfire email jobs send correctly scoped emails without relying on Session (explicit groupId parameter or cross-group sweep where appropriate)
  4. The integration test factory registers a stub IActiveGroupContext returning GroupId = 1; all existing tests pass after filter addition
  5. UserEntity has no query filter — login, password reset, and email confirmation continue to work correctly

**Plans**: 3 plans

**Wave 1**

- [x] 28-01-PLAN.md — IActiveGroupContext (Domain) + ActiveGroupContextService + SessionKeys + MutableGroupContext + TestDatabase fix + WebApplicationFactoryBase stub + QuestBoardContext HasQueryFilter + Program.cs DI registration — completed 2026-06-30

**Wave 2** *(blocked on Wave 1)*

- [x] 28-02-PLAN.md — IQuestRepository cross-group method + QuestRepository IgnoreQueryFilters impl + dispatcher interface/concrete/null groupId threading + job SetGroupId wiring + DailyReminderJob cross-group sweep + unit test updates — completed 2026-06-30

**Wave 3** *(blocked on Wave 2)*

- [x] 28-03-PLAN.md — cross-group isolation integration tests + full suite gate + human verify checkpoint — completed 2026-06-30

### Phase 29: SuperAdmin Role & Management Area

**Goal**: A SuperAdmin can log in, reach a dedicated management area, and fully manage groups and their members; existing Admin and DungeonMaster authorization continues to work via per-group roles in UserGroups
**Depends on**: Phase 28
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05, MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05, MGMT-06
**Success Criteria** (what must be TRUE):

  1. SuperAdmin Identity role exists in AspNetRoles and is seedable at startup; a SuperAdminOnly authorization policy protects the management area
  2. Admin-scoped pages (AdminOnly policy) correctly authorize users whose UserGroups.GroupRole is Admin for the active group; SuperAdmin bypasses group role checks entirely
  3. DM-scoped pages (DungeonMasterOnly policy) correctly authorize users whose UserGroups.GroupRole is DungeonMaster or Admin for the active group; SuperAdmin bypasses
  4. SuperAdmin can view all groups with member counts, create a new group, edit a group name, and delete an empty group via the /platform area
  5. SuperAdmin can add any existing user to any group with a specified GroupRole and remove a user from a group via the /platform area

**Plans**: 5 plans

**Wave 1** *(parallel — no overlap in files_modified)*

- [x] 29-01-PLAN.md — Auth handler rewrite (AdminHandler, DungeonMasterHandler) + IUserService/UserRepository extensions (GetGroupRoleAsync, SetGroupRoleAsync) + UserRepository IActiveGroupContext injection + GetAllPlayers/GetAllDMs fix + AdminController IActiveGroupContext + promote/demote fix + Users role badges fix + SuperAdminOnly policy (completed 2026-06-30)
- [x] 29-02-PLAN.md — EF Core migration: SuperAdmin role seeding (AspNetRoles Id=4, Name="SuperAdmin") (completed 2026-06-30)

**Wave 2** *(blocked on 29-01)*

- [x] 29-03-PLAN.md — Group service layer: IGroupService, IGroupRepository (Domain), GroupWithMemberCount DTO, GroupService, GroupRepository, DI registrations (completed 2026-06-30)

**Wave 3** *(blocked on 29-03)*

- [x] 29-04-PLAN.md — Platform MVC Area: GroupController (5 actions), 5 Razor views, _Layout.Platform.cshtml, _ViewImports.cshtml, _ViewStart.cshtml, PlatformViewModels, area route in Program.cs (completed 2026-06-30)

**Wave 4** *(blocked on 29-01, 29-02, 29-03, 29-04)*

- [ ] 29-05-PLAN.md — Integration tests: AdminHandlerIntegrationTests, PlatformAreaIntegrationTests, GroupManagementIntegrationTests + TestDataHelper SuperAdmin role + AuthenticationHelper SuperAdmin helper + full suite gate + human verify

### Phase 30: Group UX & Admin User Creation

**Goal**: Users land in the right group context after login, can switch groups, see the active group in navigation, and group admins can create new users — self-registration is no longer publicly available
**Depends on**: Phase 29
**Requirements**: UX-01, UX-02, UX-03, UX-04, UX-05, MGMT-07, MGMT-08, REG-01, REG-02, REG-03
**Success Criteria** (what must be TRUE):

  1. A user in exactly one group is automatically placed in that group's context after login with no picker shown; a user in multiple groups sees a group-picker page
  2. SuperAdmin always sees the group-picker after login and can enter any group or navigate directly to the management area
  3. The active group name and a "Switch group" link are visible in the navigation bar; clicking "Switch group" returns the user to the group-picker
  4. The active group selection persists in ASP.NET Core Session across requests until the session expires or the user switches groups
  5. A group admin can create a new user account within their group (assigning a GroupRole), which triggers the existing email confirmation flow; that user cannot self-register via the public registration page
  6. A group admin can promote or demote users within their group between Player, DungeonMaster, and Admin roles

**Plans**: TBD
**UI hint**: yes

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Layer Dependency Fix | v1.0 | 2/2 | Complete | — |
| 2. Email & Service Consolidation | v1.0 | 3/3 | Complete | — |
| 3. Code Quality & Dead Code | v1.0 | 2/2 | Complete | — |
| 4. Security Hardening | v1.0 | 4/4 | Complete | — |
| 5. Shop Filter & Sort | v1.0 | 2/2 | Complete | 2026-04-21 |
| 6. Follow-Up Quest | v1.0 | 2/2 | Complete | 2026-06-16 |
| 7. DM Profile Page | v1.0 | 2/2 | Complete | 2026-06-17 |
| 8. Profile Picture Avatar Crop | v1.0 | 0/? | Deferred | — |
| 9. Shop Pagination | v1.0 | 2/2 | Complete | — |
| 10. Omphalos Integration | v2.0 | — | In progress (other branch) | — |
| 11. Navigation Token Generation | v2.0 | — | In progress (other branch) | — |
| 12. Mobile Infrastructure | v3.0 | 3/3 | Complete | 2026-06-24 |
| 13. Core Player Views | v3.0 | 4/4 | Complete | 2026-06-24 |
| 14. Calendar | v3.0 | 3/3 | Complete | 2026-06-24 |
| 15. DM Views | v3.0 | 4/4 | Complete | 2026-06-24 |
| 16. Account & Browse | v3.0 | 4/4 | Complete | 2026-06-25 |
| 17. Character & Player Views | v3.0 | 4/4 | Complete | 2026-06-25 |
| 18. DM Editing & Secondary Quest Views | v3.0 | 5/5 | Complete | 2026-06-25 |
| 19. Admin & Shop Management Views | v3.0 | 7/7 | Complete | 2026-06-25 |
| 20. Hangfire Infrastructure | v4.0 | 4/4 | Complete | 2026-06-25 |
| 21. HTML Email Templates | v4.0 | 4/4 | Complete | 2026-06-26 |
| 22. Session Reminders | v4.0 | 5/5 | Complete | 2026-06-26 |
| 23. Admin Email Stats | v4.0 | 2/2 | Complete | 2026-06-27 |
| 24. Email Confirmation Flow | v4.0 | 5/5 | Complete | 2026-06-26 |
| 25. Confirmation Email Razor Template | v4.0 | 2/2 | Complete | 2026-06-27 |
| 26. Namespace Rename | v5.0 | 2/2 | Complete    | 2026-06-29 |
| 27. Group Schema Foundation | v5.0 | 3/3 | Complete | 2026-06-30 |
| 28. Tenant Isolation | v5.0 | 3/3 | Complete | 2026-06-30 |
| 29. SuperAdmin Role & Management Area | v5.0 | 3/5 | In progress | — |
| 30. Group UX & Admin User Creation | v5.0 | 0/? | Not started | — |
