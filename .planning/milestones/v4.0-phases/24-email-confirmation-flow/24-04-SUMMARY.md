---
phase: 24-email-confirmation-flow
plan: "04"
subsystem: identity
tags: [identity, email-confirmation, callback, tempdata, ui]
dependency_graph:
  requires:
    - 24-02 (IIdentityService.ConfirmEmailAsync)
  provides:
    - AccountController.ConfirmEmail GET action
    - Login.cshtml TempData banner blocks
  affects:
    - EuphoriaInn.Service/Controllers/Admin/AccountController.cs
    - EuphoriaInn.Service/Views/Account/Login.cshtml
tech_stack:
  added: []
  patterns:
    - Base64Url token decode with WebEncoders + Encoding.UTF8 (mirrors plan 03 encode path)
    - Try/catch around decode for malformed-token robustness (T-24-10)
    - TempData["Success"] / TempData["Error"] banner pattern (UI-SPEC Component 3)
    - Fixed redirect target (nameof(Login)) — prevents open redirect (T-24-09)
key_files:
  created: []
  modified:
    - EuphoriaInn.Service/Controllers/Admin/AccountController.cs
    - EuphoriaInn.Service/Views/Account/Login.cshtml
decisions:
  - Malformed Base64Url token caught in try/catch and surfaced as error banner — not an unhandled 500 (T-24-10 per threat model note)
  - No [Authorize] on ConfirmEmail — endpoint must be reachable by unauthenticated email link clickers
  - Always redirect to nameof(Login), never to a request-supplied URL (T-24-09 open-redirect mitigation)
metrics:
  duration: 4m
  completed: 2026-06-26
  tasks_completed: 2
  files_modified: 2
status: complete
---

# Phase 24 Plan 04: ConfirmEmail Callback Endpoint and Login Banners Summary

**One-liner:** Added `GET /Account/ConfirmEmail` with Base64Url token decode + Identity confirmation, and TempData Success/Error banners to `Login.cshtml` for all confirmation outcomes.

## What Was Built

**AccountController (Task 1):**

- Added `IIdentityService identityService` to the primary constructor: `AccountController(IUserService userService, IIdentityService identityService)`
- Added `using Microsoft.AspNetCore.WebUtilities` and `using System.Text` for token decoding
- New `[HttpGet] public async Task<IActionResult> ConfirmEmail(int userId, string token)` action:
  - Empty/null token guard: sets `TempData["Error"]` and redirects immediately without calling `ConfirmEmailAsync`
  - Token decode: `Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token))` — reverses the Base64Url encode applied in plan 03's `SendConfirmationEmail` action
  - Decode wrapped in `try/catch` — malformed Base64Url input yields error banner rather than a 500 (T-24-10)
  - Calls `identityService.ConfirmEmailAsync(userId, decodedToken)`
  - Success path: `TempData["Success"] = "Email confirmed — you can now log in."`
  - Failure path: `TempData["Error"] = "Email confirmation failed. The link may be expired or invalid. Contact an administrator."`
  - All paths end with `return RedirectToAction(nameof(Login))` — fixed target, no open redirect (T-24-09)
  - No `[Authorize]` attribute — unauthenticated users must reach this endpoint

**Login.cshtml (Task 2):**

- TempData Success banner (`alert-success`, `fa-check-circle`) added inside `card-body modern-card-body`, before the login `<form>`
- TempData Error banner (`alert-danger`, `fa-exclamation-triangle`) added in same position
- Both banners are dismissible (`alert-dismissible fade show`, `btn-close`)
- Existing form, validation summary, and Create Account section unchanged

## Tasks

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 1 | Add ConfirmEmail GET action to AccountController | f08fbd0 | Done |
| 2 | Add TempData Success/Error banners to Login.cshtml | 782a613 | Done |

## Acceptance Criteria Verification

- `AccountController` constructor includes `IIdentityService identityService` — PASS
- `ConfirmEmail` is `[HttpGet]` and has NO `[Authorize]` attribute — PASS
- `ConfirmEmail` decodes with `WebEncoders.Base64UrlDecode` + `Encoding.UTF8.GetString` — PASS
- Missing/empty token sets `TempData["Error"]` and redirects to Login without calling `ConfirmEmailAsync` — PASS
- Both success and failure paths end in `RedirectToAction(nameof(Login))` — PASS
- `Login.cshtml` contains `@if (TempData["Success"] != null)` alert-success block — PASS
- `Login.cshtml` contains `@if (TempData["Error"] != null)` alert-danger block — PASS
- Both blocks sit inside `card-body modern-card-body`, before the `asp-action="Login"` form — PASS
- `dotnet build EuphoriaInn.Service` exits 0 — PASS (8 warnings, 0 errors; warnings are pre-existing NU1510 package pruning notices)

## Deviations from Plan

**1. [Rule 2 - Security] Wrapped Base64Url decode in try/catch**
- **Found during:** Task 1 implementation
- **Issue:** The plan's threat model (T-24-10) explicitly noted: "Executor should confirm `Base64UrlDecode` of a malformed value surfaces as an error banner, not an unhandled exception — wrap the decode if needed."
- **Fix:** Added `try/catch` around the decode+ConfirmEmailAsync call; catch block sets `TempData["Error"]` and falls through to `RedirectToAction(nameof(Login))`
- **Files modified:** `EuphoriaInn.Service/Controllers/Admin/AccountController.cs`
- **Commit:** f08fbd0

## Threat Model Coverage

| Threat ID | Mitigation Applied |
|-----------|-------------------|
| T-24-02 | `ConfirmEmailAsync` delegates token-userId binding validation to Identity (security stamp check) |
| T-24-07 | Identity tokens are single-use; second click returns failure → error banner |
| T-24-09 | Always `RedirectToAction(nameof(Login))` — no redirect to request-supplied URL |
| T-24-10 | Empty-token guard + try/catch around decode → error banner, never unhandled exception |

## Known Stubs

None. The ConfirmEmail endpoint is fully wired to `IIdentityService.ConfirmEmailAsync`. The Login banners are fully wired to `TempData["Success"]`/`TempData["Error"]`.

## Threat Flags

None. No new network endpoints or auth paths beyond what was planned.

## Self-Check: PASSED

- D:/repos/dnd-quest-board/EuphoriaInn.Service/Controllers/Admin/AccountController.cs — FOUND
- D:/repos/dnd-quest-board/EuphoriaInn.Service/Views/Account/Login.cshtml — FOUND
- Commit f08fbd0 — verified (Task 1)
- Commit 782a613 — verified (Task 2)
