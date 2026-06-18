---
phase: 06-follow-up-quest
fixed_at: 2026-06-16T00:00:00Z
review_path: .planning/phases/06-follow-up-quest/06-REVIEW.md
iteration: 1
findings_in_scope: 7
fixed: 7
skipped: 0
status: all_fixed
---

# Phase 06: Code Review Fix Report

**Fixed at:** 2026-06-16
**Source review:** `.planning/phases/06-follow-up-quest/06-REVIEW.md`
**Iteration:** 1

**Summary:**
- Findings in scope: 7 (4 Critical, 3 Warning)
- Fixed: 7
- Skipped: 0

## Fixed Issues

### CR-01: `BaseRepository.AddAsync` does not propagate the generated Id back

**Files modified:** `EuphoriaInn.Repository/BaseRepository.cs`
**Commit:** 87c5634
**Applied fix:** Added `model.Id = entity.Id;` after `SaveChangesAsync` in `BaseRepository<TModel, TEntity>.AddAsync`. After EF Core saves the entity its `Id` is populated by the database; the line propagates that value back to the domain model so callers like `CreateFollowUpQuestAsync` immediately receive the correct Id instead of 0.

---

### CR-02: No guard that the original quest must be finalized before creating a follow-up

**Files modified:** `EuphoriaInn.Domain/Services/QuestService.cs`, `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs`
**Commit:** f5833cc
**Applied fix:** Added `!original.IsFinalized` guard in `QuestService.CreateFollowUpQuestAsync` (throws `InvalidOperationException`) and in both the GET and POST `CreateFollowUp` controller actions (sets `TempData["Error"]` and redirects to Manage). Placed after the existing D-11 follow-up existence guard.

---

### CR-03: Non-contiguous `datetime-local` input indices silently drop dates

**Files modified:** `EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml`
**Commit:** de5effb
**Applied fix:** Added `renumberDates()` helper function that queries all `#proposed-dates .proposed-date-entry input[type="datetime-local"]` elements and reassigns their `name` attributes as `ProposedDates[0]`, `ProposedDates[1]`, etc., then sets `dateCount = entries.length`. Modified `removeDate(button)` to call `renumberDates()` after removing the entry, ensuring ASP.NET MVC model binding always receives a zero-based contiguous index sequence.

---

### CR-04: Concurrent double-submission can hit unhandled `DbUpdateException`

**Files modified:** `EuphoriaInn.Repository/QuestRepository.cs`, `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs`
**Commit:** 59f7d53
**Applied fix:** Overrode `AddAsync` in `QuestRepository` (which has the EF Core reference) to catch `DbUpdateException` when the inner exception message references `IX_Quests_OriginalQuestId` or `unique`, rethrowing as `InvalidOperationException("A follow-up quest already exists for this quest.")`. The Domain service stays free of EF Core dependencies. In the controller POST action, wrapped `CreateFollowUpQuestAsync` in a `try/catch (InvalidOperationException ex)` that sets `TempData["Error"]` and returns the view with a user-friendly message instead of propagating a 500.

---

### WR-01: `FollowUpQuestViewModel.TotalPlayerCount` has no range validation

**Files modified:** `EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs`
**Commit:** 2588807
**Applied fix:** Added `[Required]` and `[Range(1, 20, ErrorMessage = "Player count must be between 1 and 20.")]` attributes to the `TotalPlayerCount` property, matching the existing pattern used by `ChallengeRating`.

---

### WR-02: `[MinLength(1)]` on `IList<DateTime>` does not fire through standard model validation

**Files modified:** `EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs`
**Commit:** 2588807 (same commit as WR-01)
**Applied fix:** Removed the `[MinLength(1, ErrorMessage = "...")]` attribute from `ProposedDates`. The controller already manually validates `ProposedDates.Count == 0` and adds a model error — the attribute was non-functional dead code.

---

### WR-03: Two-phase creation is fragile — partial failure leaves orphaned quest blocking retries

**Files modified:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs`
**Commit:** 79b84f7
**Applied fix:** Wrapped the `UpdateQuestPropertiesWithNotificationsAsync` call in a `try/catch` block. On exception, the catch fetches the just-created quest by Id and calls `questService.RemoveAsync(orphan)` to delete the shell quest (freeing the unique `OriginalQuestId` FK), then rethrows. This allows the DM to retry the form submission without getting a "follow-up already exists" error from a partially-initialised record.

---

## Skipped Issues

None — all 7 findings were fixed.

---

_Fixed: 2026-06-16_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
