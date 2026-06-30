# Requirements: D&D Quest Board v5.0 Multi-Tenancy

**Defined:** 2026-06-29
**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## v5.0 Requirements

### Rename

- [x] **RENAME-01**: All `EuphoriaInn.*` namespaces renamed to `QuestBoard.*` across every C# file
- [x] **RENAME-02**: Project files (`.csproj`), solution file (`.slnx`), and directory names renamed to match `QuestBoard.*`
- [x] **RENAME-03**: All config files (`appsettings*.json`), GitHub Actions workflows, and any deployment references updated
- [x] **RENAME-04**: All EF Core migration `*.Designer.cs` files updated with new namespace; `dotnet build` + all 191 tests pass with zero behavior change

### Group Schema

- [x] **GROUP-01**: `GroupEntity` table exists with `Id`, `Name`, `CreatedAt` columns *(model layer complete — plan 27-01; migration in plan 27-02)*
- [x] **GROUP-02**: `UserGroups` junction table exists with `UserId`, `GroupId`, `GroupRole` (enum: Player / DungeonMaster / Admin) *(model layer complete — plan 27-01; migration in plan 27-02)*
- [x] **GROUP-03**: `GroupId` FK added to `QuestEntity` and `ShopItemEntity` (the two shared-resource entities not naturally scoped through user ownership) *(model layer complete — plan 27-01; migration in plan 27-02)*
- [ ] **GROUP-04**: Data migration seeds the `"EuphoriaInn"` group as `GroupId = 1`
- [ ] **GROUP-05**: All existing users are assigned to EuphoriaInn in `UserGroups`; each user's `GroupRole` is seeded from their current `AspNetUserRoles` entry
- [ ] **GROUP-06**: `AspNetUserRoles` entries for Player / DungeonMaster / Admin are removed after migration; only SuperAdmin assignments remain in `AspNetUserRoles`

### Tenant Isolation

- [ ] **TENANT-01**: `IActiveGroupContext` interface defined in the Domain layer with an `ActiveGroupId` property (`int?` — `null` means SuperAdmin, see all)
- [ ] **TENANT-02**: `ActiveGroupContextService` in the Service layer implements `IActiveGroupContext`; reads `ActiveGroupId` from ASP.NET Core Session; returns `null` when the user holds the SuperAdmin role
- [ ] **TENANT-03**: EF Core Global Query Filters applied to `QuestEntity` and `ShopItemEntity` using `IActiveGroupContext.ActiveGroupId`; `UserEntity` does NOT receive a query filter
- [ ] **TENANT-04**: All four Hangfire email jobs bypass the group filter (cross-tenant by design) or receive an explicit `groupId` parameter where needed
- [ ] **TENANT-05**: Integration test factory registers a stub `IActiveGroupContext` returning `GroupId = 1` by default; all 191 existing tests pass after filter addition

### Authorization

- [ ] **AUTH-01**: `SuperAdmin` role added to `AspNetRoles` and seedable at startup
- [ ] **AUTH-02**: `AdminHandler` updated to check `UserGroups.GroupRole == Admin` for the active group (instead of `AspNetUserRoles`)
- [ ] **AUTH-03**: `DungeonMasterHandler` updated to check `UserGroups.GroupRole` is DungeonMaster or Admin for the active group
- [ ] **AUTH-04**: Both handlers grant access when the user holds the SuperAdmin Identity role, regardless of active group
- [ ] **AUTH-05**: A `SuperAdminOnly` authorization policy exists, used to protect the management area

### Group UX

- [ ] **UX-01**: User belonging to exactly one group is automatically redirected to that group's content after login (no picker shown)
- [ ] **UX-02**: User belonging to multiple groups sees a group-picker page after login and selects which group to enter
- [ ] **UX-03**: SuperAdmin always lands on the group-picker page after login and can enter any group or go to the management area
- [ ] **UX-04**: Active group is stored in ASP.NET Core Session per request; selected group persists across requests until session expires or user exits
- [ ] **UX-05**: Navigation displays the current group name and a "Switch group" link; clicking it returns to the group-picker

### Management Area

- [ ] **MGMT-01**: A dedicated MVC Area exists for SuperAdmin group management; route is not `/superadmin` (planner recommends a name, e.g. `/groups` or `/platform`)
- [ ] **MGMT-02**: SuperAdmin can view a list of all groups with member counts
- [ ] **MGMT-03**: SuperAdmin can create a new group (name required)
- [ ] **MGMT-04**: SuperAdmin can edit a group's name or delete an empty group
- [ ] **MGMT-05**: SuperAdmin can add any existing user to any group and assign their `GroupRole`
- [ ] **MGMT-06**: SuperAdmin can remove a user from a group
- [ ] **MGMT-07**: Group admin (Admin `GroupRole` in a group) can create new user accounts within their group
- [ ] **MGMT-08**: Group admin can promote or demote users within their group (Player ↔ DungeonMaster ↔ Admin)

### User Creation

- [ ] **REG-01**: Public self-registration (`AccountController.Register`) is removed or restricted to SuperAdmin / Admin users only
- [ ] **REG-02**: Newly created user accounts are automatically assigned to the creating admin's active group with the specified `GroupRole`
- [ ] **REG-03**: The existing email confirmation flow is triggered when a group admin or SuperAdmin creates a new user account

## Future Requirements

Deferred to v5.x or later — tracked but not in current roadmap.

- Per-group email configuration (custom sender address per group)
- Group invitation flow (invite by email link rather than admin-created account)
- Cross-group quest browsing or character directory
- Digest batching for session reminders (EMAIL-04/REMIND-02 — deferred since v4.0)
- Profile picture crop / avatar selection (issue #78 — deferred since v2.x)

## Out of Scope

- **Per-group Identity roles in `AspNetUserRoles`** — per-group roles live in `UserGroups.GroupRole`; `AspNetUserRoles` is used only for SuperAdmin
- **Database-per-tenant or schema-per-tenant** — shared-database shared-schema with EF Query Filters is sufficient at current scale
- **Third-party multi-tenancy frameworks** (Finbuckle, SaasKit, Abp.io) — existing EF Core 10 capabilities cover all requirements
- **Group billing or subscription management** — out of scope for this app
- **Email non-uniqueness / separate account per group** — one global user identity with per-group roles (see design decision in session 2026-06-29)

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| RENAME-01 | Phase 26 | Complete |
| RENAME-02 | Phase 26 | Complete |
| RENAME-03 | Phase 26 | Complete |
| RENAME-04 | Phase 26 | Complete |
| GROUP-01 | Phase 27 | Complete (model) |
| GROUP-02 | Phase 27 | Complete (model) |
| GROUP-03 | Phase 27 | Complete (model) |
| GROUP-04 | Phase 27 | Pending |
| GROUP-05 | Phase 27 | Pending |
| GROUP-06 | Phase 27 | Pending |
| TENANT-01 | Phase 28 | Pending |
| TENANT-02 | Phase 28 | Pending |
| TENANT-03 | Phase 28 | Pending |
| TENANT-04 | Phase 28 | Pending |
| TENANT-05 | Phase 28 | Pending |
| AUTH-01 | Phase 29 | Pending |
| AUTH-02 | Phase 29 | Pending |
| AUTH-03 | Phase 29 | Pending |
| AUTH-04 | Phase 29 | Pending |
| AUTH-05 | Phase 29 | Pending |
| MGMT-01 | Phase 29 | Pending |
| MGMT-02 | Phase 29 | Pending |
| MGMT-03 | Phase 29 | Pending |
| MGMT-04 | Phase 29 | Pending |
| MGMT-05 | Phase 29 | Pending |
| MGMT-06 | Phase 29 | Pending |
| MGMT-07 | Phase 30 | Pending |
| MGMT-08 | Phase 30 | Pending |
| UX-01 | Phase 30 | Pending |
| UX-02 | Phase 30 | Pending |
| UX-03 | Phase 30 | Pending |
| UX-04 | Phase 30 | Pending |
| UX-05 | Phase 30 | Pending |
| REG-01 | Phase 30 | Pending |
| REG-02 | Phase 30 | Pending |
| REG-03 | Phase 30 | Pending |
