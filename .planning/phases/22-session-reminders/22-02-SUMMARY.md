---
phase: 22-session-reminders
plan: "02"
subsystem: service
tags: [dispatcher-pattern, hangfire, di, null-object-pattern, testing-isolation]
dependency_graph:
  requires:
    - Phase 21 Plan 03 IQuestEmailDispatcher pattern
  provides:
    - IReminderJobDispatcher interface in Domain layer
    - HangfireReminderJobDispatcher (production implementation)
    - NullReminderJobDispatcher (test no-op implementation)
    - Program.cs environment-conditional DI registration for both implementations
  affects:
    - EuphoriaInn.Domain (new interface)
    - EuphoriaInn.Service (new implementations, Program.cs registration)
tech_stack:
  added: []
  patterns:
    - Null Object Pattern for test environment isolation (NullReminderJobDispatcher)
    - Environment-conditional DI registration (!IsEnvironment("Testing") guard)
    - Primary constructor injection (IBackgroundJobClient) in Hangfire dispatcher
key_files:
  created:
    - EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs
    - EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs
    - EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs
  modified:
    - EuphoriaInn.Service/Program.cs
decisions:
  - "IReminderJobDispatcher placed in Domain.Interfaces (same layer as IQuestEmailDispatcher) so QuestController can use it without a Service dependency"
  - "HangfireReminderJobDispatcher forward-references SessionReminderJob (Plan 03); build error is expected until Plan 03 completes"
  - "NullReminderJobDispatcher registered in Testing else-block to prevent IBackgroundJobClient resolution failure in test hosts"
metrics:
  duration: 2m
  completed_date: "2026-06-26"
  tasks_completed: 2
  files_changed: 4
---

# Phase 22 Plan 02: IReminderJobDispatcher Abstraction Summary

**One-liner:** IReminderJobDispatcher dispatcher interface in Domain with Hangfire and Null Object implementations registered via environment-conditional DI — exact replica of the IQuestEmailDispatcher pattern from Phase 21.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | IReminderJobDispatcher interface and both implementations | a24adca | IReminderJobDispatcher.cs, HangfireReminderJobDispatcher.cs, NullReminderJobDispatcher.cs |
| 2 | Wire IReminderJobDispatcher into Program.cs with environment-conditional registration | f8a5c58 | Program.cs |

## What Was Built

The dispatcher abstraction that decouples QuestController from Hangfire's IBackgroundJobClient:

- **IReminderJobDispatcher** — Single-method interface (`void EnqueueSessionReminder(int questId, bool forceResend = false)`) in `EuphoriaInn.Domain/Interfaces/`. QuestController can inject this interface without requiring Hangfire's IBackgroundJobClient to be registered.

- **HangfireReminderJobDispatcher** — Production implementation in `EuphoriaInn.Service/Services/`. Uses primary constructor injection of `IBackgroundJobClient` and enqueues `SessionReminderJob` (forward reference — will be defined in Plan 03) via `jobClient.Enqueue<SessionReminderJob>`.

- **NullReminderJobDispatcher** — Test no-op in `EuphoriaInn.Service/Services/`. No-op body with comment `// No-op — Hangfire not available in Testing environment`. Prevents `IBackgroundJobClient` resolution failure in integration test hosts where Hangfire is skipped.

- **Program.cs wiring** — Two lines added to the existing environment-conditional block: `AddScoped<IReminderJobDispatcher, HangfireReminderJobDispatcher>()` inside `!IsEnvironment("Testing")` (same block as AddHangfire), and `AddScoped<IReminderJobDispatcher, NullReminderJobDispatcher>()` inside the `else` (same block as NullQuestEmailDispatcher). No new using directives needed — `EuphoriaInn.Domain.Interfaces` and `EuphoriaInn.Service.Services` are already imported.

## Deviations from Plan

None — plan executed exactly as written. The expected forward-reference compile error (CS0246: SessionReminderJob not found) was observed and is intentional per plan documentation.

## Known Stubs

None. This plan delivers pure infrastructure — no UI or rendering involved.

## Threat Flags

None. T-22-03 (Elevation of Privilege on EnqueueSessionReminder) and T-22-04 (Tampering via forceResend flag) mitigations are delegated to QuestController's DungeonMasterOnly authorization policy, which will be enforced in Plan 04.

## Self-Check: PASSED

- EuphoriaInn.Domain/Interfaces/IReminderJobDispatcher.cs — EXISTS
- EuphoriaInn.Service/Services/HangfireReminderJobDispatcher.cs — EXISTS
- EuphoriaInn.Service/Services/NullReminderJobDispatcher.cs — EXISTS
- EuphoriaInn.Service/Program.cs — MODIFIED (HangfireReminderJobDispatcher + NullReminderJobDispatcher registrations added)
- Task 1 commit a24adca — EXISTS
- Task 2 commit f8a5c58 — EXISTS
