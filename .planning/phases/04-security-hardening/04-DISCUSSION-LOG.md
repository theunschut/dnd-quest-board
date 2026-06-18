# Phase 4: Security Hardening - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-20
**Phase:** 04-security-hardening
**Areas discussed:** Lockout error UX, .env git cleanup scope, Password length — existing users

---

## Lockout error UX

| Option | Description | Selected |
|--------|-------------|----------|
| Friendly inline message | Show: "Account locked due to too many failed attempts. Try again in 15 minutes." — clear, no security theatre for a private group app. | ✓ |
| Generic error only | Show same "Invalid login attempt." as before — doesn't reveal lockout state, but unhelpful for legitimate users. | |
| Redirect to lockout page | Separate /Account/Lockout page — overkill for a small private app. | |

**User's choice:** Friendly inline message
**Notes:** Private group app — no reason to obscure the lockout state from legitimate users.

---

## .env git cleanup scope

| Option | Description | Selected |
|--------|-------------|----------|
| gitignore + stop tracking | Add .env to .gitignore AND run git rm --cached .env — file stays local, stops appearing in git status. Note: .env content stays in git history. | ✓ |
| gitignore only | Add .env to .gitignore — .env will still show as tracked until manually removed. | |
| gitignore + history rewrite | Rewrite git history to remove .env from all commits — disruptive if others have cloned. | |

**User's choice:** Initially selected "gitignore + history rewrite", then reconsidered after being informed that it rewrites all commit SHAs and requires team members to re-clone. Settled on gitignore + stop tracking only.
**Notes:** Accept that .env content remains in git history. No history rewrite.

---

## Password length — existing users

| Option | Description | Selected |
|--------|-------------|----------|
| Nothing — silent enforcement | Existing users only notice the new rule when they next change their password. For a small group this is fine — no migration or forced reset needed. | ✓ |
| Note in change-password page | Add a small note on the Change Password page: "Passwords must be at least 8 characters." | |
| Force password reset for short passwords | Flag users with password length < 8 and require a reset on next login. | |

**User's choice:** Silent enforcement
**Notes:** RequiredLength 6→8 applies on new registration and password changes only. Existing short passwords continue to work for login.

---

## Claude's Discretion

- Exact placement of `LockoutOptions` within the `AddIdentity` options block
- Whether to add `[MinLength(8)]` annotation to `RegisterViewModel` for client-side consistency
- Order of the six cleanup tasks within the plan

## Deferred Ideas

None.
