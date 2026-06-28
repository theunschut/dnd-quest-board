---
phase: 22-session-reminders
plan: "04"
subsystem: service
tags: [hangfire, mvc-controller, csrf, authorization, tempdata, razor-views]
dependency_graph:
  requires:
    - 22-01 (IReminderLogRepository, ReminderLog data layer)
    - 22-02 (IReminderJobDispatcher interface and HangfireReminderJobDispatcher)
    - 22-03 (SessionReminderJob with useYesMaybeVoters flag)
  provides:
    - QuestController.SendReminder POST action with DM auth check
    - Send Reminder button in Manage.cshtml finalized section
    - TempData success alert with force-resend confirm form
  affects:
    - EuphoriaInn.Service (QuestController, Manage view)
tech-stack:
  added: []
  patterns:
    - Controller primary constructor extended with new DI parameter (no field needed)
    - Yes+Maybe voter filter scoped to finalized proposed date (RESEARCH Pitfall 1)
    - TempData["Success"] + inline force-resend form for idempotency-bypass UX
    - asp-action + asp-route-id tag helpers with @Html.AntiForgeryToken() in view forms

key-files:
  created: []
  modified:
    - EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs
    - EuphoriaInn.Service/Views/Quest/Manage.cshtml

key-decisions:
  - "forceResend confirm form embedded in TempData[Success] block — visible only after a successful send, allowing DM to bypass duplicate check without a separate page"
  - "Eligible player count computed in controller before enqueue so TempData message gives meaningful feedback even though job runs asynchronously"
  - "No controller log query for D-09 already-sent warning — architecture defers per-player dedup to the job itself; controller always enqueues and the job skips already-notified players unless forceResend=true"

patterns-established:
  - "Send Reminder form placed inside IsFinalized block in view — gate maintained in both view (visibility) and controller (enforcement)"
  - "CSRF enforced on all POST forms in Manage.cshtml via @Html.AntiForgeryToken() and [ValidateAntiForgeryToken] on action"

requirements-completed:
  - REMIND-03

duration: 5m
completed: "2026-06-26"
---

# Phase 22 Plan 04: DM Manual Reminder Trigger Summary

**DM-facing "Send Reminder" button on Quest Manage page enqueues SessionReminderJob for Yes+Maybe voters with CSRF protection, DM identity guard, and TempData feedback including a force-resend confirm form.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-26T19:00:00Z
- **Completed:** 2026-06-26T19:21:16Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- QuestController.SendReminder POST action with three security attributes (DungeonMasterOnly policy, ValidateAntiForgeryToken, Challenge/Forbid identity checks)
- Yes+Maybe voter filtering scoped to finalized proposed date (avoids RESEARCH.md Pitfall 1 — voters on other proposed dates excluded)
- Send Reminder button added inside IsFinalized block in Manage.cshtml alongside existing Open Quest and Create Follow-Up buttons
- TempData success alert with embedded force-resend form (forceResend=true) implementing D-09 idempotency-bypass UX
- All 134 integration tests pass after changes

## Task Commits

1. **Task 1: QuestController.SendReminder POST action** - `c6065ae` (feat)
2. **Task 2: Manage.cshtml Send Reminder button and TempData feedback** - `7f11e09` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — Added IReminderJobDispatcher to primary constructor; added SendReminder POST action with all security guards
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — Added Send Reminder form in finalized button bar; TempData success alert; force-resend confirm form

## Decisions Made

- No controller-side log query for "already sent" warning: the job handles per-player dedup via IReminderLogRepository. Controller always enqueues; forceResend=true bypasses the log check inside the job. This avoids resolving a scoped repository directly in the controller and keeps auth/enqueue separation clean.
- Force-resend form embedded in TempData["Success"] block (visible only after a successful send) rather than a separate endpoint or modal — minimal UI, naturally scoped to post-send state.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Known Stubs

None. The Send Reminder button enqueues a real SessionReminderJob via HangfireReminderJobDispatcher (which calls BackgroundJob.Enqueue). The eligible player count shown in TempData is computed from live quest data.

## Threat Flags

None. All three threats from the plan's threat register are mitigated:

- T-22-08 (CSRF tampering): `[ValidateAntiForgeryToken]` on action; `@Html.AntiForgeryToken()` in both forms
- T-22-09 (EoP non-DM sends): `currentUser.Equals(quest.DungeonMaster) || User.IsInRole("Admin")` check with `DungeonMasterOnly` policy as outer gate
- T-22-10 (forceResend by unauthenticated): `[Authorize(Policy = "DungeonMasterOnly")]` + DM identity check covers all paths including the force-resend form

## Self-Check: PASSED

- EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs — EXISTS, contains SendReminder action
- EuphoriaInn.Service/Views/Quest/Manage.cshtml — EXISTS, contains asp-action="SendReminder"
- Task 1 commit c6065ae — EXISTS
- Task 2 commit 7f11e09 — EXISTS
- dotnet build — 0 errors
- dotnet test EuphoriaInn.IntegrationTests — 134/134 passed

## Next Phase Readiness

Plan 05 (integration tests for SendReminder) can now proceed. The SendReminder action is fully implemented and wired. QuestController now accepts IReminderJobDispatcher via DI — the NullReminderJobDispatcher registered in the Testing environment (Phase 21 P04 pattern) will handle test host startup without Hangfire.

---
*Phase: 22-session-reminders*
*Completed: 2026-06-26*
