# Phase 20: Hangfire Infrastructure - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 20-hangfire-infrastructure
**Areas discussed:** Smoke-test job design, Non-admin access behavior, Integration test coverage, Admin nav link

---

## Smoke-test job design

| Option | Description | Selected |
|--------|-------------|----------|
| Enqueue once at startup, remove in Phase 21 | Fire-and-forget when app starts — visible in Hangfire dashboard immediately. Startup enqueue call removed in Phase 21 once real jobs exist. | ✓ |
| Enqueue once at startup, keep permanently | Same trigger, but left in place as an ongoing heartbeat job | |
| You decide | Claude picks simplest approach satisfying the success criterion | |

**User's choice:** Enqueue once at startup, remove in Phase 21

| Option | Description | Selected |
|--------|-------------|----------|
| IEmailService (via IServiceScopeFactory) | Resolves the same service real reminder jobs will use. Logs a message — no actual email sent. | ✓ |
| IQuestService | Resolves the main domain service as a proxy for any real scoped service | |
| You decide | Claude picks any scoped service | |

**User's choice:** IEmailService

**Notes:** User initially asked why a smoke test is needed in production. Clarified: it proves the IServiceScopeFactory pattern works correctly before Phases 21–22 build real jobs on top of it. The job just resolves IEmailService and logs — no side effects.

---

## Non-admin access behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect to login page | Consistent with how the app handles all other auth failures | ✓ |
| Return 401 Unauthorized | Raw HTTP 401 — simpler filter, but inconsistent with app behavior | |

**User's choice:** Redirect to login page
**Notes:** The custom IDashboardAuthorizationFilter will redirect rather than returning false (which would result in Hangfire's own 401 response).

---

## Integration test coverage

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — add auth tests | Two tests: admin GET /hangfire → 200, non-admin → 302 to login | |
| No — code review only | Success criterion says verifiable by code review; skip Hangfire-specific tests | ✓ |

**User's choice:** No — code review only
**Notes:** Keep Phase 20 lean. All 134 existing tests must still pass.

---

## Admin nav link

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — add to admin nav | Admins see a link in the admin panel navigation alongside User/Quest Management | ✓ |
| No — URL only | Admins navigate to /hangfire directly; no nav change | |

**User's choice:** Yes — add to admin nav
**Notes:** Exact label and placement left to Claude's discretion.

---

## Claude's Discretion

- Exact nav link label and position in the admin layout
- Hangfire worker count and polling interval
- Whether to guard Hangfire startup behind the existing Testing environment check
- Class and file naming for the custom IDashboardAuthorizationFilter

## Deferred Ideas

None raised during discussion.
