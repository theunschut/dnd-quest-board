---
phase: 19-admin-shop-management-views
plan: "02"
subsystem: admin-mobile-views
tags: [mobile, admin, glass-card, antiforgery, integration-tests]
dependency_graph:
  requires:
    - 19-01 (RED test stubs for ADMIN-01)
  provides:
    - Admin/Users.Mobile.cshtml — mobile glass-card user list with role badges and action buttons
    - Admin/Quests.Mobile.cshtml — mobile glass-card quest list with status badges
    - admin-users.mobile.css — glass card + sub-card styles for Users mobile view
    - admin-quests.mobile.css — glass card + sub-card styles for Quests mobile view
  affects:
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs (GetMobilePage_AdminUsers and GetMobilePage_AdminQuests now GREEN)
tech_stack:
  added: []
  patterns:
    - Mobile glass-card list pattern (admin-users-card-mobile / user-card-mobile nesting)
    - Antiforgery token via Antiforgery.GetAndStoreTokens (no @inject — global via _ViewImports)
    - Status badge variables declared inline in @foreach body (no @{} wrapper — Phase 13 rule)
    - Per-view CSS with zero media queries (device targeting at layout-selection layer)
key_files:
  created:
    - EuphoriaInn.Service/Views/Admin/Users.Mobile.cshtml
    - EuphoriaInn.Service/Views/Admin/Quests.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/admin-users.mobile.css
    - EuphoriaInn.Service/wwwroot/css/admin-quests.mobile.css
  modified: []
decisions:
  - No @inject IAntiforgery in mobile views — already globally injected via _ViewImports.cshtml (consistent with all prior mobile views)
  - No Layout= assignment in mobile views — _ViewStart.cshtml handles layout selection
  - Status badge variables (statusBadge/statusIcon/statusText) declared directly in @foreach body without @{} wrapper — Razor is already in C# code mode (Phase 13 Plan 02 rule)
  - Quest description omitted from Quests.Mobile.cshtml per D-03 (description causes overflow on narrow screens)
  - Email omitted from Users.Mobile.cshtml per plan action (D — UI-SPEC chose to omit on mobile)
metrics:
  duration: "~3 minutes"
  completed: "2026-06-25"
  tasks_completed: 2
  files_modified: 4
status: complete
---

# Phase 19 Plan 02: Admin Users & Quests Mobile Views Summary

**One-liner:** Mobile glass-card list views for Admin/Users and Admin/Quests with role/status badges, conditional action buttons, and antiforgery fetch-DELETE; both ADMIN-01 integration tests GREEN.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create Admin/Users.Mobile.cshtml + admin-users.mobile.css | 138c832 | Views/Admin/Users.Mobile.cshtml, wwwroot/css/admin-users.mobile.css |
| 2 | Create Admin/Quests.Mobile.cshtml + admin-quests.mobile.css | 112f1cf | Views/Admin/Quests.Mobile.cshtml, wwwroot/css/admin-quests.mobile.css |

## What Was Built

### Task 1 — Admin/Users.Mobile.cshtml + admin-users.mobile.css

Created a mobile glass-card list view for the Admin user management page:

- **Outer card** (`admin-users-card-mobile`) with header `<i class="fas fa-users-cog text-danger">User Management`.
- **Per-user sub-card** (`user-card-mobile`) with:
  - Top row: role icon + `<strong class="parchment-text">` user name + role badge (Administrator=`bg-danger fa-shield-alt`, Dungeon Master=`bg-warning fa-crown`, Player=`bg-primary fa-dice-d20`).
  - Button row (`d-flex flex-wrap gap-2`): conditional Promote/Demote `<form asp-action>` blocks (verbatim from desktop), Edit anchor (`btn btn-info btn-sm`), Delete button (`btn btn-danger btn-sm`).
- **Empty state**: centered `fa-users fa-3x` + "No users found. Users appear here once registered."
- **`@section Scripts`**: `deleteUser(id)` fetch DELETE to `/Admin/DeleteUser/${id}` with `'RequestVerificationToken': '@tokens.RequestToken'` header; confirm dialog + `location.reload()` on success.
- **`@section Styles`**: links `~/css/admin-users.mobile.css` with `asp-append-version="true"`.
- `admin-users.mobile.css`: glass card values for `.admin-users-card-mobile` (16px padding) and `.user-card-mobile` (12px padding); parchment text for `h5`/`.parchment-text`; faded parchment for `small`; `badge { text-shadow: none }`. Zero media queries.

### Task 2 — Admin/Quests.Mobile.cshtml + admin-quests.mobile.css

Created a mobile glass-card list view for the Admin quest management page:

- **Outer card** (`admin-quests-card-mobile`) with header `<i class="fas fa-scroll text-warning">Quest Management`.
- **Per-quest sub-card** (`quest-card-mobile`) with:
  - Status badge variables (`statusBadge`/`statusIcon`/`statusText`) computed inline in `@foreach` body (no `@{}` wrapper per Phase 13 rule); values: bg-dark/fa-flag-checkered/Done, bg-primary/fa-check-circle/Finalized, bg-success/fa-clock/Open.
  - Top row: dragon icon + `<strong class="parchment-text">` quest title + status badge.
  - Meta line: `<small class="text-muted">DM: @(quest.DungeonMaster?.Name ?? "Unknown")</small>`.
  - Button row: Edit (`btn btn-info btn-sm`, `asp-controller="Quest" asp-action="Edit"`) + Delete (`btn btn-danger btn-sm`, `onclick="deleteQuest(@quest.Id)"`).
  - Description omitted per D-03.
- **Empty state**: centered `fa-scroll fa-3x` + "No quests found. Quests appear here once created by a DM."
- **`@section Scripts`**: `deleteQuest(id)` fetch DELETE to `/Admin/DeleteQuest/${id}` with antiforgery header; confirm dialog + `location.reload()` on success.
- `admin-quests.mobile.css`: mirrors `admin-users.mobile.css` structure for consistency; `.admin-quests-card-mobile` (16px) + `.quest-card-mobile` (12px); same parchment/badge rules. Zero media queries.

## Deviations from Plan

None — plan executed exactly as written.

## Verification Results

- `dotnet build EuphoriaInn.Service` → Build succeeded (0 C# errors; MSB3492 transient Windows file-lock warning is known artifact)
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~GetMobilePage_AdminUsers"` → Passed: 1
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~GetMobilePage_AdminQuests"` → Passed: 1
- `GetMobilePage_AdminEditUser` and `GetMobilePage_AdminResetPassword` remain RED (expected — those views are Plan 03 targets)
- Users.Mobile.cshtml contains `admin-users-card-mobile`, `user-card-mobile`, links `admin-users.mobile.css`, has `deleteUser` in `@section Scripts` with `@tokens.RequestToken`; no `@inject IAntiforgery`; no `Layout =`
- Quests.Mobile.cshtml contains `admin-quests-card-mobile`, `quest-card-mobile`, links `admin-quests.mobile.css`, has `deleteQuest` in `@section Scripts` with `@tokens.RequestToken`; uses `@statusBadge`; no description property rendered; no `@inject`; no `Layout =`
- admin-users.mobile.css: contains `.admin-users-card-mobile` with `backdrop-filter` and `border-radius: 12px`; zero `@media` rules
- admin-quests.mobile.css: contains `.admin-quests-card-mobile` with `backdrop-filter` and `border-radius: 12px`; zero `@media` rules

## Known Stubs

None — all data is rendered from the model (IEnumerable<UserManagementViewModel> and IEnumerable<Quest>); no hardcoded empty values or placeholder text.

## Threat Flags

No new security-relevant surface introduced beyond what the threat model covers:
- T-19-02: `deleteUser`/`deleteQuest` use `RequestVerificationToken` from `Antiforgery.GetAndStoreTokens` — mitigated as planned.
- T-19-03: Views are purely additive; `[Authorize(Policy = "AdminOnly")]` on AdminController still enforces access.
- T-19-04: Razor auto-encodes `@quest.Title` and `@userModel.User.Name`; no `Html.Raw` used.

## Self-Check: PASSED

- [x] `EuphoriaInn.Service/Views/Admin/Users.Mobile.cshtml` created and contains `admin-users-card-mobile`
- [x] `EuphoriaInn.Service/Views/Admin/Quests.Mobile.cshtml` created and contains `admin-quests-card-mobile`
- [x] `EuphoriaInn.Service/wwwroot/css/admin-users.mobile.css` created with glass card values
- [x] `EuphoriaInn.Service/wwwroot/css/admin-quests.mobile.css` created with glass card values
- [x] Commit 138c832 exists (Task 1)
- [x] Commit 112f1cf exists (Task 2)
- [x] Both integration tests GREEN
