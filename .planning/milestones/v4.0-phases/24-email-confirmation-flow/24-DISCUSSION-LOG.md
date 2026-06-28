# Phase 24: Email Confirmation Flow - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-26
**Phase:** 24-email-confirmation-flow
**Areas discussed:** EmailConfirmed exposure, Confirmation callback endpoint, Admin Users page UI, Job guard implementation

---

## EmailConfirmed exposure

| Option | Description | Selected |
|--------|-------------|----------|
| Add to User domain model | Add bool EmailConfirmed to User, wire through AutoMapper EntityProfile. Same pattern as HasKey. | ✓ |
| Add IsEmailConfirmed to IIdentityService | New async method on interface. Keeps User lean but adds per-recipient lookup. | |
| Jobs query UserManager directly | Jobs resolve UserManager<UserEntity> from scope. Breaks Domain/Repository layering rule. | |

**User's choice:** Add to User domain model
**Notes:** Consistent with how HasKey is already exposed on User.

---

## Confirmation callback endpoint

| Option | Description | Selected |
|--------|-------------|----------|
| AccountController | Already owns Login, Register, Profile, ChangePassword — natural home for identity flows. | ✓ |
| New dedicated EmailConfirmationController | Clean separation, but overkill for a single endpoint. | |

**User's choice:** AccountController

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect to login with TempData message | Success: "Email confirmed" banner. Failure: error banner. Lightweight, no extra views. | ✓ |
| Dedicated ConfirmEmailSuccess / ConfirmEmailFailed views | More polished but adds two rarely-seen view files. | |

**User's choice:** Redirect to login with TempData message

---

## Admin Users page UI

| Option | Description | Selected |
|--------|-------------|----------|
| Inline in Actions column | Button next to Promote/Demote/Reset. Only shown when EmailConfirmed == false. | ✓ |
| Separate status column | Dedicated column with status badge + button. Widens the table. | |

**User's choice:** Inline in Actions column

| Option | Description | Selected |
|--------|-------------|----------|
| TempData banner | Same pattern as existing admin actions. Page reload with success/error banner. | ✓ |
| Inline AJAX response | In-place button update. Requires JS, more complexity for a rare admin action. | |

**User's choice:** TempData banner

---

## Job guard implementation

| Option | Description | Selected |
|--------|-------------|----------|
| Simple per-job check | One-liner if (!user.EmailConfirmed) continue; in each job. No abstraction. | |
| Shared helper/extension method | WhereEmailConfirmed() filters recipient list. One place for the guard logic. | ✓ |

**User's choice:** Shared helper/extension method

| Option | Description | Selected |
|--------|-------------|----------|
| Extension method on IEnumerable<User> in Domain | users.WhereEmailConfirmed(). Lives alongside User model. Testable in isolation. | ✓ |
| Static helper class in Service layer | EmailJobHelpers.FilterConfirmed(users). Further from the User model. | |

**User's choice:** Extension method on IEnumerable<User> in Domain

---

## Claude's Discretion

- Email body for the confirmation email in Phase 24: inline HTML string (Phase 25 upgrades to Razor component)
- Token URL encoding approach

## Deferred Ideas

- Styled Razor email template → Phase 25 (already planned)
- Auto-send confirmation on registration → out of scope
