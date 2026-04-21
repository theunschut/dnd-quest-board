---
phase: 03-code-quality-dead-code
plan: 02
subsystem: api
tags: [csharp, enums, refactor, dead-code, viewmodels]

requires:
  - phase: 03-01
    provides: SecurityConfiguration removed, dead UpdateQuestPropertiesAsync removed

provides:
  - SignupRole.Spectator enum cast in QuestService.FinalizeQuestAsync and PlayerSignupService.UpdatePlayerDateVotesAsync
  - DateMatchWindowMinutes private const with explanatory comment in QuestService
  - CharactersIndexViewModel.cs filename matches its class name

affects: [phase-04-security, phase-05-new-features, phase-06-follow-up-quest]

tech-stack:
  added: []
  patterns:
    - "Enum cast comparison: (SignupRole)entity.SignupRole == SignupRole.Value instead of raw int literals"
    - "Named private const for magic numbers with explanatory comment at class body top"

key-files:
  created: []
  modified:
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Domain/Services/PlayerSignupService.cs
    - EuphoriaInn.Service/ViewModels/CharacterViewModels/CharactersIndexViewModel.cs (renamed from GuildMembersIndexViewModel.cs)

key-decisions:
  - "QUAL-03/04 applied to Domain services (QuestService, PlayerSignupService), not QuestRepository — worktree architecture moved finalize logic to Domain layer during Phase 01 refactor"
  - "DateMatchWindowMinutes const placed in QuestService (not a shared constants class) per D-02 decision: private-scoped, no public constants class"
  - "File rename only for QUAL-05 — no content changes; class name and namespace unchanged"

patterns-established:
  - "Enum cast comparison: cast the raw int to the enum type before comparing to enum member"
  - "Magic number extraction: private const at top of class body with comment explaining the domain rationale"

requirements-completed: [QUAL-03, QUAL-04, QUAL-05]

duration: 8min
completed: 2026-04-20
---

# Phase 03 Plan 02: Code Quality — Enum Cast, Named Constant, File Rename Summary

**SignupRole magic number replaced with typed enum cast in two Domain services; 30-minute IsSameDateTime tolerance extracted as DateMatchWindowMinutes private const; GuildMembersIndexViewModel.cs renamed to CharactersIndexViewModel.cs**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-04-20T05:55:00Z
- **Completed:** 2026-04-20T05:58:50Z
- **Tasks:** 2
- **Files modified:** 3 (2 edited, 1 renamed)

## Accomplishments

- Replaced `SignupRole == 1` magic number with `(SignupRole)x == SignupRole.Spectator` in `QuestService.FinalizeQuestAsync` and `PlayerSignupService.UpdatePlayerDateVotesAsync`
- Added `private const int DateMatchWindowMinutes = 30` with explanatory comment in `QuestService`; replaced the literal `30` in `IsSameDateTime` with the constant
- Renamed `CharacterViewModels/GuildMembersIndexViewModel.cs` to `CharactersIndexViewModel.cs` via git-tracked rename preserving history; class name and namespace unchanged

## Task Commits

1. **Task 1: Replace SignupRole magic number AND extract 30-minute constant** - `370d5f0` (refactor)
2. **Task 2: Rename GuildMembersIndexViewModel.cs to CharactersIndexViewModel.cs** - `55afb46` (refactor)

## Files Created/Modified

- `EuphoriaInn.Domain/Services/QuestService.cs` - Added `using EuphoriaInn.Domain.Enums`, `private const int DateMatchWindowMinutes = 30`, replaced `SignupRole == 1` and `<= 30` literal
- `EuphoriaInn.Domain/Services/PlayerSignupService.cs` - Added `using EuphoriaInn.Domain.Enums`, replaced `SignupRole == 1`
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharactersIndexViewModel.cs` - Renamed from GuildMembersIndexViewModel.cs (no content change)

## Decisions Made

The plan targeted `EuphoriaInn.Repository/QuestRepository.cs` for both QUAL-03 and QUAL-04, but the worktree's Phase 01 refactor had already moved the finalization logic and `IsSameDateTime` into `EuphoriaInn.Domain/Services/QuestService.cs`. The magic number also appeared in `PlayerSignupService.cs`. Applied all fixes to the correct current locations — same intent, different files. This is a deviation per Rule 1 (auto-fix — plan's file reference was stale relative to the worktree state).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Applied fixes to Domain services, not QuestRepository**
- **Found during:** Task 1 start (reading QuestRepository.cs in worktree)
- **Issue:** Plan targeted `EuphoriaInn.Repository/QuestRepository.cs` for `SignupRole == 1` and `IsSameDateTime`, but the worktree's Phase 01 refactor had moved `FinalizeQuestAsync` and `UpdateProposedDates*` into `EuphoriaInn.Domain/Services/QuestService.cs`. The `SignupRole == 1` comparison also appeared in `PlayerSignupService.cs`. QuestRepository.cs in the worktree contains no finalization logic.
- **Fix:** Applied QUAL-03 fix to both `QuestService.cs` (FinalizeQuestAsync) and `PlayerSignupService.cs` (UpdatePlayerDateVotesAsync). Applied QUAL-04 fix to `QuestService.cs` where `IsSameDateTime` lives.
- **Files modified:** EuphoriaInn.Domain/Services/QuestService.cs, EuphoriaInn.Domain/Services/PlayerSignupService.cs
- **Verification:** `grep -rn "SignupRole == 1" --include="*.cs" .` returns 0 matches; all 67 tests green
- **Committed in:** 370d5f0 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — stale file reference in plan)
**Impact on plan:** Fix applies the same intent to the correct current locations. No scope creep. All QUAL-03, QUAL-04, QUAL-05 requirements satisfied.

## Issues Encountered

None beyond the file location deviation documented above.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- QUAL-03, QUAL-04, QUAL-05 complete — Phase 03 fully done
- Phase 06 (follow-up quest) dependency on SignupRole enum fix is now satisfied
- Phase 04 (security) can proceed independently

---
*Phase: 03-code-quality-dead-code*
*Completed: 2026-04-20*
