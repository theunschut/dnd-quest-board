---
phase: 25-confirmation-email-razor-template
plan: "01"
subsystem: email-jobs
tags: [tests, unit-tests, confirmation-email, nsubstitute]
dependency_graph:
  requires: [EuphoriaInn.Service/Jobs/ConfirmationEmailJob.cs, EuphoriaInn.Service/Components/Emails/ConfirmEmail.razor]
  provides: [EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs]
  affects: []
tech_stack:
  added: []
  patterns: [NSubstitute IServiceScopeFactory mock chain, AsyncServiceScope struct wrapping, Arg.Is with object.Equals predicate]
key_files:
  created: [EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs]
  modified: []
decisions:
  - "Used object.Equals() instead of null-propagating ?. operator in Arg.Is predicate — expression tree lambdas cannot contain null-propagating operators (CS8072)"
metrics:
  duration: "2m"
  completed: "2026-06-27"
  tasks: 1
  files: 1
status: complete
---

# Phase 25 Plan 01: Add ConfirmationEmailJobTests Summary

## One-liner

xUnit tests proving ConfirmationEmailJob wires IEmailRenderService (render-parameter dictionary) and IEmailService (subject + HTML) correctly using the canonical SessionReminderJobTests IServiceScopeFactory mock chain.

## What Was Built

Added `EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs` with two `[Fact]` tests:

1. `ExecuteAsync_CallsRenderAsync_WithCorrectParameters` (D-02) — asserts `RenderAsync<ConfirmEmail>` is called once with a dictionary containing `UserName="TestUser"`, `CallbackUrl="https://example.com/confirm?token=abc"`, and `AppUrl="https://example.com"` sourced from the mocked `EmailSettings`.
2. `ExecuteAsync_CallsSendAsync_WithRenderedHtml` (D-03) — asserts `SendAsync` is called once with `"player@example.com"`, the exact subject `"Confirm your D&D Quest Board account"`, and the sentinel HTML string returned by the render mock.

The constructor reproduces the `SessionReminderJobTests` `IServiceScopeFactory → IServiceScope → IServiceProvider` mock chain verbatim (D-05), registering `IEmailRenderService`, `IEmailService`, and `IOptions<EmailSettings>`, and wraps `IServiceScope` in `new AsyncServiceScope(scope)` (struct pattern).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Replaced null-propagating operator with object.Equals in Arg.Is predicate**
- **Found during:** Task 1 first test run
- **Issue:** Plan specified using `?.ToString()` comparisons inside `Arg.Is<Dictionary<string, object?>>()`, but the C# compiler rejects null-propagating operators (`?.`) inside expression tree lambdas (CS8072 error).
- **Fix:** Replaced `d[key]?.ToString() == value` with `object.Equals(d[key], value)` in the `Arg.Is` predicate. This avoids the expression tree restriction and correctly compares boxed string values.
- **Files modified:** `EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs`
- **Commit:** 35cd497

## Verification Results

- `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~ConfirmationEmailJobTests"` → 2 passed, 0 failed
- No production code modified

## Self-Check: PASSED
- File `EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs` exists: FOUND
- Commit `35cd497` exists: FOUND
- 2 tests passed, 0 failed
