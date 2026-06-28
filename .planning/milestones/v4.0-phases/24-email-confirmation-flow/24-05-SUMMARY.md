---
phase: 24-email-confirmation-flow
plan: "05"
subsystem: email-guard
tags: [email-confirmation, quest-service, session-reminder, unit-tests, security]
dependency_graph:
  requires:
    - User.EmailConfirmed (from plan 01)
    - UserExtensions.WhereEmailConfirmed (from plan 01)
    - EmailConfirmationJobGuardTests scaffold (from plan 01)
  provides:
    - EmailConfirmed guard at QuestService.FinalizeQuestAsync dispatch site
    - EmailConfirmed guard at QuestService.UpdateQuestPropertiesWithNotificationsAsync dispatch site
    - EmailConfirmed guard in SessionReminderJob.ExecuteAsync (covers DailyReminderJob)
    - EmailConfirmationJobGuardTests (4 real assertions replacing placeholder)
    - SessionReminderJobTests (2 new EmailConfirmed tests + updated MakeSignup)
  affects:
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Service/Jobs/SessionReminderJob.cs
    - EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs
    - EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs
    - EuphoriaInn.UnitTests/Services/QuestServiceTests.cs
tech_stack:
  added: []
  patterns:
    - LINQ inline predicate extension for EmailConfirmed guard on PlayerSignup.Player
    - WhereEmailConfirmed() extension applied to IEnumerable<User> in UpdateQuestPropertiesWithNotificationsAsync
    - NSubstitute Arg.Is<string[]> assertion for recipient list exclusion
key_files:
  created: []
  modified:
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Service/Jobs/SessionReminderJob.cs
    - EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs
    - EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs
    - EuphoriaInn.UnitTests/Services/QuestServiceTests.cs
decisions:
  - FinalizeQuestAsync guard placed inline in selectedSignups Where predicate (not via WhereEmailConfirmed extension, since the source is IEnumerable<PlayerSignup> not IEnumerable<User>)
  - UpdateQuestPropertiesWithNotificationsAsync uses WhereEmailConfirmed() extension on affectedPlayers (IEnumerable<User>) satisfying D-06
  - SessionReminderJob guard placed as a rebind of targetSignups after the if/else branch, before the foreach — covers both useYesMaybeVoters and automated IsSelected paths
  - QuestServiceTests MakeSignup updated to default EmailConfirmed=true so pre-existing tests pass with the new guard (Rule 1 fix)
metrics:
  duration: 3m
  completed: 2026-06-26
status: complete
---

# Phase 24 Plan 05: EmailConfirmed Guard on All Email Paths Summary

**One-liner:** `EmailConfirmed` guard applied at both `QuestService` string[]-array dispatch sites and inline in `SessionReminderJob`, with 4 new guard tests in `EmailConfirmationJobGuardTests` and 2 in `SessionReminderJobTests` — REQ-24-04 fully closed.

## What Was Built

### Task 1: Guard both QuestService dispatch sites on EmailConfirmed

- Added `using EuphoriaInn.Domain.Extensions;` to `QuestService.cs`
- Extended `FinalizeQuestAsync` `selectedSignups` predicate to add `&& ps.Player.EmailConfirmed` — unconfirmed players are excluded before the `recipientEmails` array is built for `EnqueueFinalizedEmail`
- Applied `affectedPlayers.WhereEmailConfirmed()` in `UpdateQuestPropertiesWithNotificationsAsync` before the email-non-empty filter — unconfirmed users are excluded before `EnqueueDateChangedEmail` is called
- The existing `if (selectedSignups.Count == 0) return;` and `if (withEmail.Count == 0) return ServiceResult<int>.Ok(0);` guards already handle the all-unconfirmed edge case correctly

### Task 2: Guard SessionReminderJob and fill in both test files

- Added `targetSignups = targetSignups.Where(ps => ps.Player != null && ps.Player.EmailConfirmed);` in `SessionReminderJob.ExecuteAsync` after the `if/else` branch assignment and before the `foreach` loop — covers both the automated (IsSelected) and DM-trigger (useYesMaybeVoters) branches. Since `DailyReminderJob` only enqueues `SessionReminderJob`, this guard covers all four email paths.
- Replaced the plan-01 placeholder Fact in `EmailConfirmationJobGuardTests` with 4 real assertions:
  - `FinalizeQuestAsync_ExcludesUnconfirmedPlayerEmail_FromRecipientArray` — mixed confirmed/unconfirmed, asserts unconfirmed email excluded from `EnqueueFinalizedEmail`
  - `FinalizeQuestAsync_WhenAllPlayersUnconfirmed_DoesNotDispatch` — all-unconfirmed, asserts `EnqueueFinalizedEmail` never called
  - `UpdateQuestPropertiesWithNotificationsAsync_ExcludesUnconfirmedPlayerEmail_FromDateChangedDispatch` — mixed, asserts unconfirmed excluded from `EnqueueDateChangedEmail`
  - `UpdateQuestPropertiesWithNotificationsAsync_WhenAllPlayersUnconfirmed_DoesNotDispatch` — all-unconfirmed, asserts no dispatch
- Updated `SessionReminderJobTests`:
  - `MakeSignup` now accepts `emailConfirmed = true` parameter (default true preserves prior tests)
  - Added `ExecuteAsync_WhenPlayerEmailNotConfirmed_SkipsPlayer` — IsSelected=true, email present, EmailConfirmed=false → no `SendAsync`
  - Added `ExecuteAsync_WhenPlayerEmailConfirmed_SendsEmailRegression` — EmailConfirmed=true → `SendAsync` called once

## Verification

- `dotnet build EuphoriaInn.Domain` — succeeded (8 pre-existing NU1510 warnings, 0 errors)
- `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~EmailConfirmationJobGuard|FullyQualifiedName~SessionReminderJobTests"` — 11/11 passed
- `dotnet test EuphoriaInn.UnitTests` (full suite) — 44/44 passed, 0 failed

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed QuestServiceTests MakeSignup missing EmailConfirmed=true**
- **Found during:** Task 2 full-suite run
- **Issue:** Two existing tests in `QuestServiceTests` built `User` and `PlayerSignup` objects without `EmailConfirmed = true`. After the guard was applied, both tests failed because the default `bool` is `false`, causing the new filter to exclude all players from the dispatch calls.
- **Fix:** Updated `MakeSignup` helper in `QuestServiceTests` to accept `emailConfirmed = true` parameter (default true). Also updated the `affectedPlayers` list in `UpdateQuestPropertiesWithNotificationsAsync_WithAffectedPlayers_DispatchesJobAndReturnsCount` to set `EmailConfirmed = true` on all three users.
- **Files modified:** `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs`
- **Commit:** b1c75d8

## Known Stubs

None — all four email paths now carry live guards; no placeholder logic remains.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, or schema changes introduced. The `EmailConfirmed` filter is pure in-memory LINQ applied before any SMTP dispatch. Mitigates T-24-04 (email to unverified address) and T-24-11 (quest details disclosed to unverified inbox) as specified in the plan's threat register.

## Self-Check

Files exist:
- EuphoriaInn.Domain/Services/QuestService.cs: FOUND
- EuphoriaInn.Service/Jobs/SessionReminderJob.cs: FOUND
- EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs: FOUND
- EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs: FOUND
- EuphoriaInn.UnitTests/Services/QuestServiceTests.cs: FOUND

Commits:
- 8e1b6a0: feat(24-05): guard both QuestService dispatch sites on EmailConfirmed
- b1c75d8: feat(24-05): guard SessionReminderJob and fill in both test files

## Self-Check: PASSED
