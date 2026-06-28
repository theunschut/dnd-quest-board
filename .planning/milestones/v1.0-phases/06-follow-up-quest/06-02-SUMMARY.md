---
phase: 06-follow-up-quest
plan: 02
subsystem: ui
tags: [aspnet-core, mvc, razor, autoMapper, ef-core, quest, follow-up]

# Dependency graph
requires:
  - phase: 06-follow-up-quest plan 01
    provides: OriginalQuestId FK, Quest navigation properties, IQuestService.CreateFollowUpQuestAsync, HasFollowUpQuestAsync

provides:
  - FollowUpQuestViewModel with OriginalQuestId, empty ProposedDates default, and UI-SPEC validation message
  - CreateFollowUp.cshtml two-column form view with info banner and Pre-Approved Players sidebar
  - QuestController.CreateFollowUp GET and POST actions with DM auth, D-11 guard, D-03/05 pre-fill, and form date persistence
  - Details.cshtml Continues in/from sidebar links via Model.Quest.FollowUpQuest / Model.Quest.OriginalQuest
  - Manage.cshtml Continues in/from sidebar links and Create Follow-Up Quest button (hidden when follow-up exists)
  - QuestRepository eager-loading of OriginalQuest and FollowUpQuest in ProjectWithoutCharacterImages and GetQuestWithManageDetailsAsync
  - EntityProfile shallow mapping fix: QuestEntity->Quest maps OriginalQuest/FollowUpQuest as Id+Title objects (no circular recursion)

affects: [future phases reading Quest navigation properties in views]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Shallow AutoMapper navigation mapping: map nav properties as new DomainModel { Id, Title } to break circular recursion"
    - "Service-layer quest creation then UpdateQuestPropertiesWithNotificationsAsync to persist form-submitted dates"
    - "FollowUpQuestViewModel separates follow-up creation form binding from QuestViewModel, carries OriginalQuestId for POST routing"

key-files:
  created:
    - EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs
    - EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml
  modified:
    - EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs
    - EuphoriaInn.Service/Views/Quest/Details.cshtml
    - EuphoriaInn.Service/Views/Quest/Manage.cshtml
    - EuphoriaInn.Repository/QuestRepository.cs
    - EuphoriaInn.Repository/Automapper/EntityProfile.cs

key-decisions:
  - "AutoMapper shallow navigation fix: changed OriginalQuest/FollowUpQuest from Ignore() to MapFrom(src => src.X == null ? null : new Quest { Id, Title }) to populate sidebar links without circular recursion"
  - "POST action saves form dates via UpdateQuestPropertiesWithNotificationsAsync after CreateFollowUpQuestAsync because the service creates the quest shell without dates and form dates must be persisted"
  - "D-11 guard in GET and POST uses original.FollowUpQuest != null (now reliable because AutoMapper shallow mapping is fixed and repository eager-loads the nav property)"

patterns-established:
  - "Shallow navigation mapping pattern: use MapFrom with inline new DomainModel { Id, Title } instead of Ignore() when circular recursion is the only concern"

requirements-completed: [FOLLOW-01, FOLLOW-02, FOLLOW-03, FOLLOW-04]

# Metrics
duration: 35min
completed: 2026-06-16
---

# Phase 06 Plan 02: Follow-Up Quest UI Wiring Summary

**Full follow-up quest creation UI: FollowUpQuestViewModel, CreateFollowUp.cshtml two-column form, GET/POST controller actions with D-11 guard, and Continues in/from sidebar links on Details and Manage views backed by shallow AutoMapper navigation mapping**

## Performance

- **Duration:** 35 min
- **Started:** 2026-06-16T21:10:00Z
- **Completed:** 2026-06-16T21:45:00Z
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments
- Created `FollowUpQuestViewModel` with OriginalQuestId, empty ProposedDates default (D-03), and custom MinLength error message matching UI-SPEC copy
- Created `CreateFollowUp.cshtml` with two-column layout, alert-info pre-fill banner, Pre-Approved Players sidebar, and locked button copy ("Save Follow-Up Quest" / "Back to Quest")
- Added QuestController `CreateFollowUp` GET (pre-fills form, populates sidebar players) and POST (validates dates, delegates to service, persists form dates, redirects to new quest Manage page)
- Added eager-loading of OriginalQuest/FollowUpQuest to `ProjectWithoutCharacterImages` and `GetQuestWithManageDetailsAsync`
- Fixed AutoMapper EntityProfile to map navigation properties shallowly (Id+Title) instead of ignoring them, enabling the Continues in/from links to render
- Added Continues in/from sidebar paragraphs with fa-scroll text-warning icon to both Details.cshtml and Manage.cshtml
- Added conditional "Create Follow-Up Quest" btn-primary button to Manage.cshtml finalized button row, hidden when `Model.FollowUpQuest != null`

## Task Commits

Each task was committed atomically:

1. **Task 1: Create FollowUpQuestViewModel and CreateFollowUp.cshtml** - `5562054` (feat)
2. **Task 2: Eager-load OriginalQuest/FollowUpQuest and fix AutoMapper** - `379831f` (feat)
3. **Task 3: CreateFollowUp controller actions and sidebar links** - `2a66d11` (feat)

## Files Created/Modified
- `EuphoriaInn.Service/ViewModels/QuestViewModels/FollowUpQuestViewModel.cs` - Follow-up creation form view model with OriginalQuestId, empty ProposedDates, and custom validation message
- `EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml` - Two-column creation form with info banner, dates section, Pre-Approved Players sidebar
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` - Added CreateFollowUp GET and POST actions with DungeonMasterOnly auth, DM ownership check, D-11 guard, and form date persistence
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` - Added Continues in/from sidebar paragraphs using Model.Quest.FollowUpQuest / Model.Quest.OriginalQuest
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` - Added Continues in/from sidebar paragraphs and conditional Create Follow-Up Quest button in finalized row
- `EuphoriaInn.Repository/QuestRepository.cs` - Added .Include(q => q.OriginalQuest) and .Include(q => q.FollowUpQuest) to ProjectWithoutCharacterImages and GetQuestWithManageDetailsAsync
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` - Changed QuestEntity->Quest navigation property mapping from Ignore() to shallow MapFrom returning new Quest { Id, Title }

## Decisions Made
- **AutoMapper shallow navigation mapping**: The plan's Task 2 added EF includes to the repository, but the Plan 01 decision to `Ignore()` OriginalQuest/FollowUpQuest in AutoMapper meant the included data would never reach the domain model. Fixed by mapping each nav property as `new Quest { Id = src.X.Id, Title = src.X.Title }` — provides just enough data for sidebar links without triggering circular recursion.
- **Form date persistence via UpdateQuestPropertiesWithNotificationsAsync**: `CreateFollowUpQuestAsync(int, CancellationToken)` creates the quest shell without dates (per D-03). The form's ProposedDates must be persisted separately. After the service returns `newQuestId`, the POST action calls `UpdateQuestPropertiesWithNotificationsAsync` with the viewmodel's dates. This preserves the clean service interface while ensuring form dates are saved.
- **D-11 guard uses `original.FollowUpQuest != null`**: Now reliable because the AutoMapper fix and repository includes are in place. The plan's context note mentioned using `HasFollowUpQuestAsync` as an alternative, but with the AutoMapper fix the nav property approach works correctly and avoids an extra database query in the GET action.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Fixed AutoMapper to shallowly map OriginalQuest/FollowUpQuest navigation properties**
- **Found during:** Task 2 (eager-loading of navigation properties)
- **Issue:** Plan 01 explicitly set `OriginalQuest` and `FollowUpQuest` to `Ignore()` in AutoMapper to prevent circular mapping recursion. Adding EF `.Include()` calls would load the entities but AutoMapper would discard them — the "Continues in/from" sidebar lines would never render
- **Fix:** Changed `QuestEntity -> Quest` mapping from `Ignore()` to `MapFrom(src => src.X == null ? null : new Quest { Id = src.X.Id, Title = src.X.Title })`. The `Quest -> QuestEntity` direction remains `Ignore()` since EF handles the FK (OriginalQuestId) directly
- **Files modified:** EuphoriaInn.Repository/Automapper/EntityProfile.cs
- **Verification:** Build succeeds; navigation properties now populated in mapped domain models
- **Committed in:** `379831f` (Task 2 commit)

**2. [Rule 2 - Missing Critical] Added form date persistence after CreateFollowUpQuestAsync**
- **Found during:** Task 3 (QuestController POST action)
- **Issue:** The plan's POST action spec calls `CreateFollowUpQuestAsync(id, token)` and immediately redirects to Manage. The service creates the quest with `ProposedDates = []` (D-03: always empty). The FollowUpQuestViewModel's ProposedDates submitted by the form would be silently discarded, creating a quest without dates despite FOLLOW-03 requiring at least one date
- **Fix:** After `var newQuestId = await questService.CreateFollowUpQuestAsync(id, token)`, added a call to `UpdateQuestPropertiesWithNotificationsAsync(newQuestId, viewModel.Title, ..., updateProposedDates: true, viewModel.ProposedDates, token)` to persist the form's data and dates onto the created quest
- **Files modified:** EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs
- **Verification:** Build succeeds; form dates are persisted to the new quest before redirect
- **Committed in:** `2a66d11` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 2 - Missing Critical)
**Impact on plan:** Both fixes necessary for correct functioning. AutoMapper fix ensures navigation property data reaches the views. Date persistence fix ensures the form's proposed dates are saved to the quest (required by FOLLOW-03 which mandates at least one date). No scope creep.

## Issues Encountered
- Plan 01's AutoMapper Ignore() decision (correct for preventing circular recursion) was incompatible with Plan 02's need to render navigation property data in views — required a shallow mapping approach as the bridge

## User Setup Required
None - no external service configuration required. No new migrations needed (Plan 01 already created the OriginalQuestId migration).

## Next Phase Readiness
- Full follow-up quest feature is end-to-end complete: DMs can create follow-up quests from finalized quests, form is pre-filled per D-01 through D-04, player import happens at service layer per D-07, and both Details/Manage pages show Continues in/from sidebar links
- FOLLOW-01 through FOLLOW-04 requirements are all satisfied across Plan 01 + Plan 02
- All five FOLLOW-0X requirements complete

---
*Phase: 06-follow-up-quest*
*Completed: 2026-06-16*
