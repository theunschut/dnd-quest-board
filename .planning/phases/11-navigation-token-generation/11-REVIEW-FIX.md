---
phase: 11-navigation-token-generation
fixed_at: 2026-06-19T00:00:00Z
review_path: .planning/phases/11-navigation-token-generation/11-REVIEW.md
iteration: 1
findings_in_scope: 3
fixed: 2
skipped: 1
status: partial
---

# Phase 11: Code Review Fix Report

**Fixed at:** 2026-06-19
**Source review:** `.planning/phases/11-navigation-token-generation/11-REVIEW.md`
**Iteration:** 1

**Summary:**
- Findings in scope: 3 (WR-01, WR-02, WR-03)
- Fixed: 2 (WR-02, WR-03)
- Skipped: 1 (WR-01)

## Fixed Issues

### WR-02: Double username lowercasing — caller pre-lowercases before passing to service that also lowercases

**Files modified:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs`
**Commit:** c64f441
**Applied fix:** Removed `.ToLower()` from `currentUser.Name.ToLower()` at the `LaunchOmphalos` call site (line 827). The `IntegrationTokenService.GenerateSignedUrl` already lowercases the username internally. The service now exclusively owns normalisation; callers pass the name as-is.

---

### WR-03: `_Layout.cshtml` dereferences `currentUser.Name` without null guard

**Files modified:** `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`
**Commit:** 86bfe0a
**Applied fix:** Wrapped the authenticated user dropdown (lines 116-153) in a `if (currentUser == null) / else` block. When `GetUserAsync` returns null for an authenticated session (stale cookie after account deletion or identity migration), a plain "Sign out" form button is rendered instead, allowing the user to clear their cookie without triggering a `NullReferenceException`.

---

## Skipped Issues

### WR-01: OmphalosNavItem links raw base URL, not a signed token — bypasses SSO

**File:** `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml:4`
**Reason:** skipped_by_design — Design decision D-08 in CONTEXT.md explicitly locks this behaviour: "The navbar 'Open Omphalos' link navigates to the Omphalos base URL directly (plain link, new tab) — no SSO token needed for the home page." Modifying `Default.cshtml` to route through `LaunchOmphalos` would violate the agreed design. The Details and Manage pages provide quest-scoped SSO links via `LaunchOmphalos`; the nav item intentionally links the base URL for general access.
**Original issue:** Nav item renders `href="@Model.OmphalosUrl"` directly without an HMAC token, bypassing SSO. The Details/Manage pages correctly use `LaunchOmphalos` for signed redirects.

---

_Fixed: 2026-06-19_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
