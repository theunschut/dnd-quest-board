---
phase: 22-session-reminders
fixed_at: 2026-06-26T00:00:00Z
review_path: .planning/phases/22-session-reminders/22-REVIEW.md
iteration: 1
findings_in_scope: 4
fixed: 2
skipped: 2
status: partial
---

# Phase 22: Code Review Fix Report

**Fixed at:** 2026-06-26
**Source review:** .planning/phases/22-session-reminders/22-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 4 (CR-01, CR-02, WR-01, WR-02)
- Fixed: 2 (WR-01, WR-02)
- Skipped: 2 (CR-01, CR-02 — already fixed in prior commit)

## Fixed Issues

### WR-01: TOCTOU race in duplicate-send guard

**Files modified:** `EuphoriaInn.Repository/ReminderLogRepository.cs`
**Commit:** 02fc941
**Applied fix:** Wrapped `AddAsync` body in a `try/catch (DbUpdateException)` with a `when` filter that matches the unique constraint index name `IX_ReminderLogs_QuestId_PlayerId` or the word "unique". A concurrent insertion that races past the `ExistsAsync` check now silently succeeds rather than propagating an unhandled exception to Hangfire.

### WR-02: Null-forgiving operator on FinalizedDate in QuestController.SendReminder

**Files modified:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs`
**Commit:** 2bcad98
**Applied fix:** Added an explicit `if (!quest.FinalizedDate.HasValue)` null guard after the DM/admin authorization block (line 644-647) and before the `FinalizedDate.Value` dereference. The guard redirects to Manage with a user-visible error message. The null-forgiving operator `!` was removed from the `FinalizedDate!.Value.Date` expression so the compiler can enforce the null-safety invariant.

## Skipped Issues

### CR-01: AutoMapper commercial license key committed to source

**File:** `EuphoriaInn.Service/Program.cs:122`
**Reason:** Already fixed — prior commit `fd4aa2c` ("fix(22): CR-01 move AutoMapper license key to configuration") moved the key to `builder.Configuration["AutoMapper:LicenseKey"]`. Line 122 now reads from configuration, not a hardcoded string.

### CR-02: Service layer directly references a Repository-layer interface

**File:** `EuphoriaInn.Service/Jobs/SessionReminderJob.cs:8`
**Reason:** Already fixed — `SessionReminderJob.cs` imports `EuphoriaInn.Domain.Interfaces` (no Repository alias present). `IReminderLogRepository` exists in `EuphoriaInn.Domain/Interfaces/IReminderLogRepository.cs` and `ReminderLogRepository.cs` implements the domain interface directly. The cross-layer dependency violation no longer exists.

---

_Fixed: 2026-06-26_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
