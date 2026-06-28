---
phase: 25-confirmation-email-razor-template
plan: "02"
subsystem: email-service
tags: [cleanup, dead-code-removal, email, unit-tests]
status: complete

dependency_graph:
  requires: [25-01]
  provides: [clean-IEmailService-contract]
  affects: [EuphoriaInn.Domain.Interfaces.IEmailService, EuphoriaInn.Domain.Services.EmailService, EuphoriaInn.UnitTests.Services.EmailServiceTests]

tech_stack:
  added: []
  patterns: [interface-trimming, dead-code-removal]

key_files:
  modified:
    - EuphoriaInn.Domain/Interfaces/IEmailService.cs
    - EuphoriaInn.Domain/Services/EmailService.cs
    - EuphoriaInn.UnitTests/Services/EmailServiceTests.cs

decisions:
  - "IEmailService now exposes only the single generic SendAsync contract; all Hangfire jobs already used SendAsync exclusively"
  - "EmailServiceSource_ContainsAppUrlSubstitution renamed to EmailServiceSource_SendAsyncSendsHtmlBody: _settings.AppUrl was only in the deleted SendQuestDateChangedEmailAsync body; EMAIL-03 intent now verified via IsBodyHtml=true assertion on SendAsync"
  - "SendAsync_WhenSmtpNotConfigured test fixed: null-guard in CreateSmtpClient checks FromEmail (not SmtpUsername); test now passes empty FromEmail to correctly skip SMTP connection"

metrics:
  duration: 4m
  completed: 2026-06-27
  tasks_completed: 2
  files_modified: 3
---

# Phase 25 Plan 02: Remove Obsolete Email Methods Summary

Removed `SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync` from `IEmailService` and `EmailService`, deleted their two test counterparts, and fixed two stale test assertions that broke after removal.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Remove obsolete typed methods from IEmailService and EmailService | c8e0170 | IEmailService.cs, EmailService.cs |
| 2 | Delete the two EmailServiceTests covering the removed methods | d2d671d | EmailServiceTests.cs |

## Verification Results

- `dotnet build` (full solution): succeeded, 0 errors
- `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~EmailServiceTests"`: 5 passed, 0 failed
- `dotnet test EuphoriaInn.UnitTests` (full unit suite): 44 passed, 0 failed
- No `.cs` file in the solution references `SendQuestFinalizedEmailAsync` or `SendQuestDateChangedEmailAsync`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed stale `EmailServiceSource_ContainsAppUrlSubstitution` test**
- **Found during:** Task 2 pre-analysis
- **Issue:** `EmailServiceSource_ContainsAppUrlSubstitution` asserted `_settings.AppUrl` exists in `EmailService.cs`. That reference lived exclusively in `SendQuestDateChangedEmailAsync`, which was deleted in Task 1. The test would have failed after Task 1.
- **Fix:** Renamed test to `EmailServiceSource_SendAsyncSendsHtmlBody`; now asserts `IsBodyHtml = true` in the source, which is the correct EMAIL-03 invariant for the trimmed EmailService — SendAsync renders HTML passed by Hangfire jobs.
- **Files modified:** `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs`
- **Commit:** d2d671d

**2. [Rule 1 - Bug] Fixed `SendAsync_WhenSmtpNotConfigured_ReturnsWithoutException` test**
- **Found during:** Task 2 execution (test run)
- **Issue:** Test comment said "empty SmtpUsername causes CreateSmtpClient to return null" — but `CreateSmtpClient()` only returns null when `FromEmail` is empty. The test set `FromEmail = "x@x"`, so SMTP connection was actually attempted, throwing `SmtpException` which propagated (SendAsync re-throws). Test was pre-existing broken but masked because `SendAsync` was never run during the EmailService test suite before.
- **Fix:** Changed test to use `FromEmail = ""` (the actual null-guard condition). Renamed to `SendAsync_WhenEmailNotConfigured_ReturnsWithoutException` to match the correct behavior.
- **Files modified:** `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs`
- **Commit:** d2d671d

## Known Stubs

None — this plan removes dead code only; no new runtime surface introduced.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced. This plan removes dead API surface only.

## Self-Check: PASSED

- IEmailService.cs: `SendQuestFinalizedEmailAsync` count = 0, `SendQuestDateChangedEmailAsync` count = 0, `Task SendAsync` present
- EmailService.cs: `SendQuestFinalizedEmailAsync` count = 0, `SendQuestDateChangedEmailAsync` count = 0, `public async Task SendAsync` present, `private SmtpClient` present
- EmailServiceTests.cs: `SendQuestFinalizedEmailAsync` count = 0, `SendQuestDateChangedEmailAsync` count = 0, 5 tests present and passing
- Commits c8e0170 and d2d671d verified in git log
- Full solution build: succeeded
