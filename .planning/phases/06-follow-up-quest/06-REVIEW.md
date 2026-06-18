---
phase: 06-follow-up-quest
reviewed: 2026-06-16T00:00:00Z
depth: standard
files_reviewed: 14
files_reviewed_list:
  - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
  - EuphoriaInn.Domain/Interfaces/IQuestService.cs
  - EuphoriaInn.Domain/Models/QuestBoard/Quest.cs
  - EuphoriaInn.Domain/Services/QuestService.cs
  - EuphoriaInn.Repository/Automapper/EntityProfile.cs
  - EuphoriaInn.Repository/Entities/QuestBoardContext.cs
  - EuphoriaInn.Repository/Entities/QuestEntity.cs
  - EuphoriaInn.Repository/Migrations/20260616205501_AddFollowUpQuestLink.cs
  - EuphoriaInn.Repository/QuestRepository.cs
  - EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs
  - EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs
  - EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml
  - EuphoriaInn.Service/Views/Quest/Details.cshtml
  - EuphoriaInn.Service/Views/Quest/Manage.cshtml
findings:
  critical: 4
  warning: 3
  info: 1
  total: 8
status: issues_found
---

# Phase 06: Code Review Report

**Reviewed:** 2026-06-16
**Depth:** standard
**Files Reviewed:** 14
**Status:** issues_found

## Summary

This phase adds the follow-up quest feature: a DM can create a continuation quest that inherits the original quest's metadata and pre-imports selected players. The schema migration, domain model, entity, context configuration, repository, service, controller, view model, and views were all reviewed.

The feature design is coherent at the surface, but there is a fundamental data-flow defect that renders the entire feature broken at runtime: `BaseRepository.AddAsync` does not propagate the database-generated Id back to the domain model object, so the new quest's Id is always 0 after `CreateFollowUpQuestAsync` returns. This causes cascading failures — all imported player signups hit an FK violation, the subsequent property update silently does nothing, and the redirect targets quest 0. There are also a missing finalization guard, a stale index counter in the JavaScript date picker that silently drops user input, and a missing range validator on `TotalPlayerCount`.

---

## Critical Issues

### CR-01: `BaseRepository.AddAsync` does not propagate the generated Id back — `followUp.Id` is always 0

**File:** `EuphoriaInn.Domain/Services/QuestService.cs:172-193`

**Issue:** `BaseRepository<TModel, TEntity>.AddAsync` (in `EuphoriaInn.Repository/BaseRepository.cs:20-25`) maps the domain model to an entity, saves it, and discards the entity. The database-assigned `Id` is never written back to the original domain model. Consequently, after `await repository.AddAsync(followUp, token)` at line 172, `followUp.Id` remains 0.

This has three immediate runtime failures:

1. The loop that imports player signups at lines 179-191 sets `Quest = followUp` on each `PlayerSignupEntity` built by `EntityProfile`. The mapping `src.Quest.Id` resolves to 0. The `SaveChangesAsync` call inside each `playerSignupRepository.AddAsync` hits an FK violation (`QuestId = 0` references a non-existent quest) and throws a `DbUpdateException`. The entire follow-up creation fails with an unhandled 500.

2. `return followUp.Id` at line 193 returns 0.

3. The controller receives `newQuestId = 0`, calls `UpdateQuestPropertiesWithNotificationsAsync(0, ...)` which silently returns early (no quest with Id 0 found), and then redirects to `Manage?id=0` which returns 404.

**Fix:** Override `AddAsync` in `QuestRepository` to map the entity back after save, or fix the base implementation:

```csharp
// BaseRepository.cs — fix AddAsync to propagate the generated Id
public virtual async Task AddAsync(TModel model, CancellationToken token = default)
{
    var entity = Mapper.Map<TEntity>(model);
    await DbSet.AddAsync(entity, token);
    await DbContext.SaveChangesAsync(token);
    // Propagate DB-generated Id back to the domain model
    model.Id = entity.Id;
}
```

Alternatively, override `AddAsync` in `QuestRepository` specifically for the follow-up scenario, or return the entity from `AddAsync` and retrieve its Id in `CreateFollowUpQuestAsync`.

---

### CR-02: No guard that the original quest must be finalized before creating a follow-up

**File:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:648` (GET) and `696` (POST); `EuphoriaInn.Domain/Services/QuestService.cs:148`

**Issue:** Neither the `CreateFollowUp` GET action, the POST action, nor `QuestService.CreateFollowUpQuestAsync` checks whether `original.IsFinalized == true`. The "Continue in" design implies a follow-up is only meaningful after the original quest has been played. More concretely, the pre-approved player import at lines 175-191 of `QuestService` filters `ps.IsSelected` — but `IsSelected` is only set during finalization. If a DM calls this endpoint on an open quest, `selectedSignups` will be empty (no players are selected on an open quest), and the follow-up will be created with no pre-approved players — silently diverging from the spec.

**Fix:** Add a finalization guard in both controller actions and in the service:

```csharp
// In QuestController CreateFollowUp GET and POST, after the null check:
if (!original.IsFinalized)
{
    TempData["Error"] = "Only finalized quests can have a follow-up created.";
    return RedirectToAction("Manage", new { id });
}

// In QuestService.CreateFollowUpQuestAsync, after the null check:
if (!original.IsFinalized)
    throw new InvalidOperationException("Cannot create a follow-up for a quest that has not been finalized.");
```

---

### CR-03: Non-contiguous `datetime-local` input indices silently drop dates

**File:** `EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml:124-144`

**Issue:** The JavaScript `dateCount` variable is incremented on every `addDate()` call but never decremented when `removeDate()` removes an entry. After adding three dates (indices 0, 1, 2), removing index 1, then adding a fourth, the DOM will contain inputs named `ProposedDates[0]`, `ProposedDates[2]`, `ProposedDates[3]`. ASP.NET MVC model binding requires zero-based contiguous indices for `IList<T>` — it stops at the first missing index. `ProposedDates[2]` and `ProposedDates[3]` will not be bound; only `ProposedDates[0]` survives. The user sees their dates in the form but the server receives only the first one, and the validation error message for `ProposedDates` will not appear because the single surviving entry satisfies `Count >= 1`.

This is a silent data loss bug in the UI.

**Fix:** Renumber all remaining inputs on every `removeDate()` call:

```javascript
function removeDate(button) {
    button.closest('.proposed-date-entry').remove();
    renumberDates();
}

function renumberDates() {
    const entries = document.querySelectorAll('#proposed-dates .proposed-date-entry input[type="datetime-local"]');
    entries.forEach((input, i) => {
        input.name = `ProposedDates[${i}]`;
    });
    dateCount = entries.length;
}
```

---

### CR-04: `CreateFollowUp` POST is not protected against concurrent double-submission resulting in an unhandled 500

**File:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:713-716`

**Issue:** The D-11 uniqueness guard in the POST action reads `original.FollowUpQuest` from a navigation property loaded by `GetQuestWithDetailsAsync` — a point-in-time snapshot from the database. If two POST requests arrive concurrently and both pass the navigation-property check (both see `FollowUpQuest == null` at snapshot time), both proceed to call `CreateFollowUpQuestAsync`. The service's `HasFollowUpQuestAsync` check inside `CreateFollowUpQuestAsync` (lines 155-157) is a separate query without a transaction or row lock, so both concurrent calls can also pass it. The second `AddAsync` call then violates the unique index `IX_Quests_OriginalQuestId` in the database, surfacing as an unhandled `DbUpdateException` → HTTP 500.

Since the unique index does protect data integrity, the only runtime impact is an ugly 500 instead of a user-friendly error. However, the service comment at line 154 explicitly documents this check as the integrity guard — and it is insufficient without a transaction.

**Fix:** Wrap the uniqueness check and insert in a serializable transaction in `CreateFollowUpQuestAsync`, or catch the `DbUpdateException` for unique-constraint violations and convert it to an `InvalidOperationException` with a clear message:

```csharp
try
{
    await repository.AddAsync(followUp, token);
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Quests_OriginalQuestId") == true
                                    || ex.InnerException?.Message.Contains("unique") == true)
{
    throw new InvalidOperationException("A follow-up quest already exists for this quest.");
}
```

The controller should then catch `InvalidOperationException` from `CreateFollowUpQuestAsync` and return `TempData["Error"]` rather than letting the 500 propagate.

---

## Warnings

### WR-01: `FollowUpQuestViewModel.TotalPlayerCount` has no range validation

**File:** `EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs:25`

**Issue:** `ChallengeRating` correctly carries `[Range(1, 20)]`, but `TotalPlayerCount` has only a default value and no `[Required]` or `[Range]` attribute. A user can submit `TotalPlayerCount = 0` or a negative number, which would create a quest where the player limit is 0 (permanently "full") or causes unexpected behavior in the player-count comparisons throughout the codebase.

**Fix:**

```csharp
[Required]
[Range(1, 20, ErrorMessage = "Player count must be between 1 and 20.")]
public int TotalPlayerCount { get; set; } = 6;
```

---

### WR-02: `[MinLength]` on `IList<DateTime>` does not trigger model-state validation

**File:** `EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs:35-36`

**Issue:** `[MinLength(1, ...)]` is a `ValidationAttribute` designed for arrays and strings, not `IList<T>`. When applied to `IList<DateTime>`, ASP.NET Core's model binder does not invoke it through the standard validation pipeline — it is not equivalent to a collection-count check. The controller already applies a manual null/count guard at lines 720-733, but `ModelState.IsValid` at line 720 will not reflect this attribute's violation. The `[MinLength]` annotation on line 35 is misleading — it appears to be declarative validation but is effectively dead.

**Fix:** Remove the `[MinLength]` attribute (the manual check in the controller handles this correctly) or replace it with a custom `ValidationAttribute` that properly checks `IList<T>.Count`:

```csharp
// Remove the [MinLength] annotation; rely on the controller guard already in place.
public IList<DateTime> ProposedDates { get; set; } = [];
```

---

### WR-03: `CreateFollowUpQuestAsync` creates the quest with the original's title, then the controller overwrites it — duplicating work and risking a stale title in case of partial failure

**File:** `EuphoriaInn.Domain/Services/QuestService.cs:159-172` and `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:740-753`

**Issue:** `CreateFollowUpQuestAsync` writes `Title = $"{original.Title} - Part 2"` to the database. The controller immediately follows with `UpdateQuestPropertiesWithNotificationsAsync(newQuestId, viewModel.Title, ...)` to apply the DM's potentially edited title. This means two round-trips write the title field. More critically, if the second call fails (e.g., connection drop, exception), the follow-up quest exists in the database with the auto-generated "Part 2" title and no proposed dates — a partially initialised record that is now linked as a follow-up (unique FK), preventing any retry.

**Fix:** Have `CreateFollowUpQuestAsync` not set the title (or accept it as a parameter), and let the single `UpdateQuestPropertiesWithNotificationsAsync` call be the only write. Alternatively, merge both operations into one atomic service method `CreateFollowUpQuestWithDatesAsync(originalQuestId, title, description, cr, totalPlayerCount, dmSession, proposedDates)`.

---

## Info

### IN-01: Antiforgery token is sent as a request header in `Manage.cshtml` but the MVC controller uses `[ValidateAntiForgeryToken]` which checks the form body

**File:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml:600-606`

**Issue:** The `deleteQuest` and `removePlayerSignup` JavaScript functions in `Manage.cshtml` send the antiforgery token as an HTTP header (`'RequestVerificationToken': '@tokens.RequestToken'`), while `Details.cshtml` uses `formData.append('__RequestVerificationToken', ...)` in the request body. ASP.NET Core's `[ValidateAntiForgeryToken]` attribute accepts the token in both the form body (field `__RequestVerificationToken`) and the request header (`RequestVerificationToken`), so this actually works. However, the inconsistency between `Manage.cshtml` (header) and `Details.cshtml` (body) is a maintenance hazard — a developer refactoring one pattern may inadvertently break the other. Document or standardise the approach across views.

**Fix:** Standardise on the header approach (which works for both `DELETE` and `POST` fetch calls and does not require `FormData`) across all views, or add a comment explaining both approaches are valid.

---

_Reviewed: 2026-06-16_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
