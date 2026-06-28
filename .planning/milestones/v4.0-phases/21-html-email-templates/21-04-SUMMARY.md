---
phase: 21-html-email-templates
plan: 04
subsystem: testing
tags: [nsubstitute, xunit, ibackgroundjobclient, hangfire, iemailservice, emailservice, integration-tests]

# Dependency graph
requires:
  - phase: 21-03
    provides: "IQuestEmailDispatcher interface; HangfireQuestEmailDispatcher; QuestService decoupled from IEmailService"
  - phase: 21-01
    provides: "IEmailService.SendAsync method implementation in EmailService"
provides:
  - "EmailServiceTests: SendAsync_WhenSmtpNotConfigured_ReturnsWithoutException test covering the new generic HTML send method"
  - "NullQuestEmailDispatcher: no-op IQuestEmailDispatcher for Testing environment (Hangfire unavailable)"
  - "Program.cs: conditional DI registration — HangfireQuestEmailDispatcher in non-Testing, NullQuestEmailDispatcher in Testing"
  - "Full test suite GREEN: 28 unit tests + 134 integration tests, 0 failures"
affects:
  - phase-22-session-reminder

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "NullObject pattern for Testing environment: NullQuestEmailDispatcher avoids requiring IBackgroundJobClient (Hangfire) in test hosts"
    - "Conditional DI registration: IQuestEmailDispatcher registered per-environment to match Hangfire availability"

key-files:
  created:
    - EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs
  modified:
    - EuphoriaInn.UnitTests/Services/EmailServiceTests.cs
    - EuphoriaInn.Service/Program.cs

key-decisions:
  - "NullQuestEmailDispatcher registered in Testing environment — HangfireQuestEmailDispatcher depends on IBackgroundJobClient which Hangfire only registers in non-Testing"
  - "QuestServiceTests already updated in Plan 03 deviation — Plan 04's QuestServiceTests work was a no-op; primary work was EmailServiceTests + integration test fix"

patterns-established:
  - "NullObject dispatchers for test isolation: when a Service-layer implementation depends on infrastructure not available in Testing env, register a no-op via the same interface"

requirements-completed: [EMAIL-02]

# Metrics
duration: 3min
completed: 2026-06-26
---

# Phase 21 Plan 04: Test Fixes — SendAsync Coverage and Integration Test Regression Gate Summary

**NullQuestEmailDispatcher restores 134 integration tests broken by missing IBackgroundJobClient in Testing environment; EmailServiceTests gains SendAsync coverage; full suite 162/162 GREEN**

## Performance

- **Duration:** 3 min
- **Started:** 2026-06-26T11:21:36Z
- **Completed:** 2026-06-26T11:24:09Z
- **Tasks:** 1
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments

- `EmailServiceTests`: new `SendAsync_WhenSmtpNotConfigured_ReturnsWithoutException` test — verifies no exception when SMTP not configured (CreateSmtpClient returns null → early return)
- `NullQuestEmailDispatcher` created: no-op implementation of `IQuestEmailDispatcher` — registered in the Testing environment where `IBackgroundJobClient` (Hangfire) is unavailable
- `Program.cs` updated: `HangfireQuestEmailDispatcher` moved inside `!IsEnvironment("Testing")` guard; `NullQuestEmailDispatcher` registered in the `else` branch
- Full test suite: 28 unit tests + 134 integration tests — 162/162 GREEN, 0 failures

Note: `QuestServiceTests.cs` was already fully updated in Plan 03 (auto-fixed deviation in Task 2 of that plan). The IQuestEmailDispatcher mock, Received/DidNotReceive dispatcher assertions, and removal of all IEmailService references were completed there. This plan verified those changes remain correct and focused on the EmailServiceTests gap + the integration test regression.

## Task Commits

1. **Task 1: Add SendAsync test; fix integration tests broken by missing IBackgroundJobClient** - `596a675` (test)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` — added `SendAsync_WhenSmtpNotConfigured_ReturnsWithoutException` test
- `EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs` — NEW: no-op IQuestEmailDispatcher for Testing environment
- `EuphoriaInn.Service/Program.cs` — moved HangfireQuestEmailDispatcher registration inside `!IsEnvironment("Testing")` block; added `NullQuestEmailDispatcher` for Testing

## Decisions Made

- **NullQuestEmailDispatcher pattern** — The `HangfireQuestEmailDispatcher` constructor requires `IBackgroundJobClient` which is only registered when `AddHangfire()` runs (inside the `!IsEnvironment("Testing")` guard). Registering `HangfireQuestEmailDispatcher` outside that guard caused all 134 integration tests to fail with "Unable to resolve service for type 'Hangfire.IBackgroundJobClient'". The NullObject pattern is the cleanest solution: it satisfies the `IQuestEmailDispatcher` contract in tests without any Hangfire dependency.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Integration tests failing — IBackgroundJobClient not registered in Testing environment**
- **Found during:** Task 1 verification (full test suite run)
- **Issue:** `HangfireQuestEmailDispatcher` was registered unconditionally in `Program.cs` (line 84), but depends on `IBackgroundJobClient` which Hangfire only registers in non-Testing environments. All 134 integration tests failed with `InvalidOperationException: Unable to resolve service for type 'Hangfire.IBackgroundJobClient' while attempting to activate 'HangfireQuestEmailDispatcher'`.
- **Fix:** Created `NullQuestEmailDispatcher` (no-op); moved `HangfireQuestEmailDispatcher` registration inside `!IsEnvironment("Testing")` guard; added `NullQuestEmailDispatcher` registration in Testing else-branch.
- **Files modified:** `EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs` (created), `EuphoriaInn.Service/Program.cs`
- **Verification:** `dotnet test` exits 0 — 28 unit + 134 integration = 162/162 GREEN
- **Committed in:** `596a675` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — broken integration tests)
**Impact on plan:** Required fix for test suite regression introduced in Plan 03 (IQuestEmailDispatcher registration was outside the Hangfire guard). No scope change, no architectural concern. NullObject pattern is the established .NET idiom for this scenario.

## Issues Encountered

None beyond the IBackgroundJobClient DI issue (auto-fixed above).

## Threat Surface Scan

No new trust boundaries. `NullQuestEmailDispatcher` is test-infrastructure only — it is registered exclusively in `IsEnvironment("Testing")` and never deployed. No production security surface change.

## Known Stubs

None — all email paths are fully wired in production:
- Production: `IQuestEmailDispatcher` → `HangfireQuestEmailDispatcher` → `IBackgroundJobClient.Enqueue<T>` → Hangfire job → `IEmailService.SendAsync`
- Testing: `IQuestEmailDispatcher` → `NullQuestEmailDispatcher` (no-op, safe isolation)

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 21 is complete. All 4 plans executed, full test suite GREEN.
- Phase 22 (session reminder job) can use `IQuestEmailDispatcher` pattern or implement its own Hangfire job directly
- `SessionReminder.razor` is ready for Phase 22 to consume (D-15 parameter contract locked in Plan 02)
- `NullQuestEmailDispatcher` pattern is established for any future Hangfire jobs that need test isolation

## Self-Check

Verified:
- `EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs` exists with `EnqueueFinalizedEmail` and `EnqueueDateChangedEmail` no-ops
- `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` contains `SendAsync_WhenSmtpNotConfigured_ReturnsWithoutException`
- `EuphoriaInn.Service/Program.cs` contains `NullQuestEmailDispatcher` registration in `else` branch
- Commit `596a675` exists
- `dotnet test` exits 0: 162/162 passed

## Self-Check: PASSED

---
*Phase: 21-html-email-templates*
*Completed: 2026-06-26*
