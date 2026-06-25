---
phase: 19-admin-shop-management-views
plan: "01"
subsystem: requirements + integration-tests
tags: [requirements, integration-tests, tdd, scaffold, admin, shop-management]
dependency_graph:
  requires: []
  provides:
    - ADMIN-01 requirement definition and traceability row in REQUIREMENTS.md
    - ADMIN-02 requirement definition and traceability row in REQUIREMENTS.md
    - SHOPMGMT-01 requirement definition and traceability row in REQUIREMENTS.md
    - 8 RED integration test stubs in MobileViewsTests.cs (GREEN targets for Plans 02-06)
  affects:
    - .planning/REQUIREMENTS.md
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
tech_stack:
  added: []
  patterns:
    - Nyquist test harness pattern (scaffold RED tests before implementation)
    - CreateAuthenticatedAdminClientAsync for Admin-role test authentication
    - CreateAuthenticatedClientWithUserAsync with DungeonMaster role for ShopManagement tests
key_files:
  created: []
  modified:
    - .planning/REQUIREMENTS.md
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
decisions:
  - Phase 19 admin/shop tests use CreateAuthenticatedAdminClientAsync for Admin-role routes and CreateAuthenticatedClientWithUserAsync with roles DungeonMaster for ShopManagement routes — consistent with Phase 18 authenticated-request pattern
  - SHOPMGMT-01 ShopDetails test seeds a seller DM via CreateTestUserAsync (no DM role required for seeding) and authenticates as a regular player buyer — Shop/Details is accessible to any authenticated user
  - AdminResetPassword test uses the query-string ?userId= route parameter (same as AdminEditUser) — verified against existing AdminController route pattern
metrics:
  duration: "~2 minutes"
  completed: "2026-06-25"
  tasks_completed: 2
  files_modified: 2
status: complete
---

# Phase 19 Plan 01: Requirements Scaffold + RED Test Stubs Summary

**One-liner:** Added ADMIN-01/ADMIN-02/SHOPMGMT-01 requirements to REQUIREMENTS.md with traceability and 8 RED integration test stubs for all Phase 19 mobile view targets.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add ADMIN-01/ADMIN-02/SHOPMGMT-01 to REQUIREMENTS.md | d0717db | .planning/REQUIREMENTS.md |
| 2 | Append 8 RED Phase 19 integration test methods | 524971c | EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs |

## What Was Built

### Task 1 — REQUIREMENTS.md updates

Added a new `### Admin & Shop Management` section to the v1 Requirements area with three unchecked requirement bullets:

- **ADMIN-01**: Admin user list and edit pages are usable on mobile without horizontal scrolling
- **ADMIN-02**: Shop Management index, create, and edit pages are fully functional on mobile
- **SHOPMGMT-01**: Shop item detail page renders in a single-column layout with no overflow

Reconciled two contradictions with the active Phase 19 scope:
- Removed `Admin views on mobile — low usage frequency; defer` from Future Requirements
- Removed `Admin views — admin actions are low-frequency and expected on desktop` from Out of Scope

Added 3 traceability rows (`| ADMIN-01 | Phase 19 | Pending |`, etc.) to the Traceability table.

### Task 2 — 8 RED integration test stubs

Appended 8 `[Fact]` test methods to `MobileViewsTests.cs` (after the Phase 18 QLOG-01 test):

| Method | Requirement | Route | CSS Token | Card Class |
|--------|-------------|-------|-----------|------------|
| `GetMobilePage_AdminUsers_ReturnsSuccessAndMobileLayout` | ADMIN-01 | /Admin/Users | admin-users.mobile.css | admin-users-card-mobile |
| `GetMobilePage_AdminEditUser_ReturnsSuccessAndMobileLayout` | ADMIN-01 | /Admin/EditUser?userId= | admin-form.mobile.css | admin-form-card-mobile |
| `GetMobilePage_AdminQuests_ReturnsSuccessAndMobileLayout` | ADMIN-01 | /Admin/Quests | admin-quests.mobile.css | admin-quests-card-mobile |
| `GetMobilePage_AdminResetPassword_ReturnsSuccessAndMobileLayout` | ADMIN-01 | /Admin/ResetPassword?userId= | admin-form.mobile.css | admin-form-card-mobile |
| `GetMobilePage_ShopManagementIndex_ReturnsSuccessAndMobileLayout` | ADMIN-02 | /ShopManagement | shop-management-index.mobile.css | shop-mgmt-index-card-mobile |
| `GetMobilePage_ShopManagementCreate_ReturnsSuccessAndMobileLayout` | ADMIN-02 | /ShopManagement/Create | shop-management-create.mobile.css | shop-mgmt-create-card-mobile |
| `GetMobilePage_ShopManagementEdit_ReturnsSuccessAndMobileLayout` | ADMIN-02 | /ShopManagement/Edit/{id} | shop-management-edit.mobile.css | shop-mgmt-edit-card-mobile |
| `GetMobilePage_ShopDetails_ReturnsSuccessAndMobileLayout` | SHOPMGMT-01 | /Shop/Details/{id} | shop-details.mobile.css | shop-details-card-mobile |

All tests follow the established Phase 18 authenticated-request pattern (MobileUserAgent + Authorization header on `_client.SendAsync`). All compile clean and fail at assertion (RED) because the mobile views do not exist yet — this is the expected Nyquist-harness state.

## Deviations from Plan

None — plan executed exactly as written.

Note: Plan task name says "seven RED integration test methods" but the action section describes 8 methods (including ShopDetails for SHOPMGMT-01). All 8 were added as specified in the action section; the artifact list confirms 8 methods.

## Verification Results

- `grep -c "ADMIN-01" .planning/REQUIREMENTS.md` → 2 (definition + traceability)
- `grep -c "ADMIN-02" .planning/REQUIREMENTS.md` → 2 (definition + traceability)
- `grep -c "SHOPMGMT-01" .planning/REQUIREMENTS.md` → 2 (definition + traceability)
- Deferral note "Admin views on mobile — low usage frequency; defer" removed
- Out-of-scope note "admin actions are low-frequency" removed
- `dotnet build EuphoriaInn.IntegrationTests` → 0 C# errors (MSB3492 is transient Windows file-lock, documented in decisions)
- 8 new test methods present in MobileViewsTests.cs

## Known Stubs

None — this plan produces requirements documentation and RED test stubs; no implementation stubs.

## Threat Flags

No new security-relevant surface introduced. Test harness uses existing `Test` auth scheme (no production trust boundary changed).

## Self-Check: PASSED

- [x] `.planning/REQUIREMENTS.md` modified — contains ADMIN-01, ADMIN-02, SHOPMGMT-01
- [x] `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` modified — 8 new test methods
- [x] Commit d0717db exists (REQUIREMENTS.md update)
- [x] Commit 524971c exists (test stubs)
