---
phase: 24-email-confirmation-flow
fixed_at: 2026-06-27T00:00:00Z
review_path: .planning/phases/24-email-confirmation-flow/24-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 5
skipped: 1
status: partial
---

# Phase 24: Code Review Fix Report

**Fixed at:** 2026-06-27T00:00:00Z
**Source review:** .planning/phases/24-email-confirmation-flow/24-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (CR-01, CR-02, CR-03, WR-01, WR-02, WR-03)
- Fixed: 5
- Skipped: 1

## Fixed Issues

### CR-01: Stored XSS in confirmation email body via unencoded `user.Name`

**Files modified:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs`
**Commit:** 137a11e
**Applied fix:** Added `System.Net.WebUtility.HtmlEncode(user.Name)` before interpolating the user name into the HTML email body. The local variable `safeName` holds the encoded value and is used in the raw string literal instead of the raw `user.Name`.

---

### CR-02: Missing `userId` validation in `ConfirmEmail` enables token probing against arbitrary accounts

**Files modified:** `EuphoriaInn.Service/Controllers/Admin/AccountController.cs`
**Commit:** 4ff687d
**Applied fix:** Extended the existing early-return guard from `string.IsNullOrEmpty(token)` to `userId <= 0 || string.IsNullOrEmpty(token)`, rejecting requests with non-positive user IDs before any Identity interaction occurs.

---

### CR-03: `CreateUserAsync` auto-signs in unconfirmed users, bypassing the `EmailConfirmed` guard

**Files modified:** `EuphoriaInn.Repository/IdentityService.cs`
**Commit:** 5da2a7a
**Applied fix:** Removed the `await signInManager.SignInAsync(entity, isPersistent: false)` call from `CreateUserAsync`. The user is now only added to the Player role on creation; sign-in must happen explicitly after email confirmation. A comment documents the intentional removal.

---

### WR-01: "Send Confirmation Email" button rendered for users without an email address

**Files modified:** `EuphoriaInn.Service/Views/Admin/Users.cshtml`
**Commit:** ca44e09
**Applied fix:** Changed the Razor condition from `@if (!userModel.EmailConfirmed)` to `@if (!userModel.EmailConfirmed && !string.IsNullOrEmpty(userModel.User.Email))`, preventing the button from rendering when the user has no email address.

---

### WR-02: Bare `catch` in `ConfirmEmail` silently swallows server errors

**Files modified:** `EuphoriaInn.Service/Controllers/Admin/AccountController.cs`
**Commit:** fb39d01
**Applied fix:** Added `ILogger<AccountController>` to the primary constructor, added `using Microsoft.Extensions.Logging`, changed the bare `catch` to `catch (Exception ex)`, and added `logger.LogError(ex, "ConfirmEmail failed for userId {UserId}", userId)` before presenting the user-facing error message.

---

### WR-03: `SessionReminderJob` — null `finalizedProposedDate` produces empty recipient set with no warning

**Files modified:** `EuphoriaInn.Service/Jobs/SessionReminderJob.cs`
**Commit:** 5a8a130
**Applied fix:** Added an explicit null guard immediately after `FirstOrDefault`: when `finalizedProposedDate` is null, logs a `LogWarning` explaining that no ProposedDate matches the FinalizedDate and returns early. The LINQ predicate was also tightened from `finalizedProposedDate?.Id` to `finalizedProposedDate.Id` (now safe because of the preceding null check).

---

## Skipped Issues

### IN-01: `User.Equals` / `GetHashCode` include mutable navigation-populated fields

**File:** `EuphoriaInn.Domain/Models/User.cs:26-38`
**Reason:** In-scope filter is `critical_warning` — Info findings (IN-*) are excluded from this run.
**Original issue:** `Equals` and `GetHashCode` include `EmailConfirmed`, `HasKey`, and `Email`, all of which are mutable. If a `User` instance is placed in a hash-based collection and then mutated, the hash changes and bucket lookups fail silently.

---

_Fixed: 2026-06-27T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
