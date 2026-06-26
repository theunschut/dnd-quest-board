---
phase: 24-email-confirmation-flow
plan: "03"
subsystem: admin-ui
tags: [admin, email-confirmation, controller, view, tempdata]
dependency_graph:
  requires:
    - 24-01 (User.EmailConfirmed domain property, UserManagementViewModel.EmailConfirmed)
    - 24-02 (IIdentityService.GenerateEmailConfirmationAsync)
  provides:
    - AdminController.SendConfirmationEmail POST action
    - AdminController.Users populates EmailConfirmed per row
    - Users.cshtml TempData success/error banner blocks
    - Users.cshtml conditional Send Confirmation Email button
  affects:
    - EuphoriaInn.Service/Controllers/Admin/AdminController.cs
    - EuphoriaInn.Service/Views/Admin/Users.cshtml
tech_stack:
  added: []
  patterns:
    - Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode for token encoding
    - Url.Action with Request.Scheme for absolute callback URL generation
    - TempData["Success"]/TempData["Error"] keys (consistent with Quest/Manage.cshtml pattern)
    - POST-Redirect-GET pattern with TempData feedback banners
key_files:
  created: []
  modified:
    - EuphoriaInn.Service/Controllers/Admin/AdminController.cs
    - EuphoriaInn.Service/Views/Admin/Users.cshtml
decisions:
  - TempData keys are Success/Error (not SuccessMessage — existing inconsistency in ResetPassword not copied)
  - View condition reads userModel.EmailConfirmed (flat ViewModel property), not userModel.User.EmailConfirmed
  - Inline HTML email body used; Phase 25 will replace with Razor template component
  - WebEncoders.Base64UrlEncode ships with ASP.NET Core framework — no package install needed
  - Request.Scheme passed to Url.Action to force absolute URL (relative URLs break in email clients)
metrics:
  duration: 7m
  completed: 2026-06-26
  tasks_completed: 2
  files_modified: 2
status: complete
---

# Phase 24 Plan 03: Admin Send Confirmation Email — Controller and View Summary

**One-liner:** `AdminController.SendConfirmationEmail` POST action wired to `IIdentityService.GenerateEmailConfirmationAsync` + `IEmailService.SendAsync`, with absolute Base64Url callback URL and TempData feedback banners on the Users management page.

## What Was Built

### Task 1: SendConfirmationEmail action and EmailConfirmed population in Users

**File:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs`

- Extended primary constructor to `AdminController(IUserService userService, IQuestService questService, IIdentityService identityService, IEmailService emailService)`
- Added `using Microsoft.AspNetCore.WebUtilities;` and `using System.Text;` for Base64Url token encoding
- Modified `Users()` to set `EmailConfirmed = user.EmailConfirmed` on each `UserManagementViewModel` initializer
- Added `[HttpPost][ValidateAntiForgeryToken] SendConfirmationEmail(int userId)` action:
  - Null-guard on `userService.GetByIdAsync(userId)` — returns to Users silently if not found
  - Null-guard on `rawToken` and empty email — sets `TempData["Error"]` and redirects
  - Encodes raw token via `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken))`
  - Builds absolute callback URL via `Url.Action("ConfirmEmail", "Account", new { userId, token = encodedToken }, Request.Scheme)`
  - Sends inline HTML email via `emailService.SendAsync` with subject "Confirm your D&D Quest Board account"
  - Sets `TempData["Success"]` and redirects to Users on success path

### Task 2: TempData banner and conditional Send Confirmation Email button in Users.cshtml

**File:** `EuphoriaInn.Service/Views/Admin/Users.cshtml`

- Added `@if (TempData["Success"] != null)` alert-success banner block and `@if (TempData["Error"] != null)` alert-danger banner block inside `card-body modern-card-body` before the `@if (Model.Any())` table block
- Added `@if (!userModel.EmailConfirmed)` conditional form with:
  - `asp-action="SendConfirmationEmail"`, `method="post"`, `class="d-inline me-2"`
  - Hidden input `name="userId" value="@userModel.User.Id"`
  - Submit button `class="btn btn-sm btn-info"` with `fas fa-envelope me-1` icon and "Send Confirmation Email" label
- Button positioned after Promote/Demote blocks and before the Edit anchor (final order: Promote/Demote → Send Confirmation Email → Edit → Delete)
- Button entirely absent (not disabled) when `EmailConfirmed` is true

## Verification

- `dotnet build EuphoriaInn.Service` — succeeded (8 pre-existing NU1510 warnings, 0 errors)
- Both tasks verified: controller compiles with new dependencies; Razor view compiles with new conditional block

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

- Inline HTML email body in `SendConfirmationEmail` — intentional; Phase 25 replaces this with a styled Razor component matching `_EmailLayout`. Documented in STATE.md decisions.

## Threat Model Coverage

| Threat ID | Mitigation Applied |
|-----------|--------------------|
| T-24-01 | `[ValidateAntiForgeryToken]` on action + antiforgery token in form via `asp-action` tag helper |
| T-24-02 | Inherited `[Authorize(Policy = "AdminOnly")]` on class restricts to admins |
| T-24-03 | `userService.GetByIdAsync` null-check + empty email check with TempData error |
| T-24-08 | `Url.Action(..., Request.Scheme)` forces absolute URL bound to request host |

## Self-Check

Files exist:
- EuphoriaInn.Service/Controllers/Admin/AdminController.cs: FOUND
- EuphoriaInn.Service/Views/Admin/Users.cshtml: FOUND

Commits:
- acfda0b: feat(24-03): add SendConfirmationEmail action and populate EmailConfirmed in Users
- 78b788e: feat(24-03): add TempData banner and conditional Send Confirmation Email button to Users.cshtml

## Self-Check: PASSED
