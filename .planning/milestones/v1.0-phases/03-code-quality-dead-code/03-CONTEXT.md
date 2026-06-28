# Phase 3: Code Quality & Dead Code - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Five targeted mechanical cleanups:
1. Delete `SecurityConfiguration.cs` and its `Security` section in `appsettings.json`
2. Remove the dead `UpdateQuestPropertiesAsync` (non-notification variant) from all 4 layers
3. Replace `SignupRole == 1` magic number with named enum cast
4. Extract the 30-minute `IsSameDateTime` window as a private named constant
5. Rename `CharacterViewModels/GuildMembersIndexViewModel.cs` to match its class name, and verify/reorganize the `CharacterViewModels/` folder for filename/classname alignment

No behavior changes — purely structural and naming cleanup. All existing flows must work after this phase.

</domain>

<decisions>
## Implementation Decisions

### QUAL-02: Dead method removal scope

- **D-01:** Remove `UpdateQuestPropertiesAsync` (non-notification variant) from **all 4 layers**: `IQuestService`, `QuestService`, `IQuestRepository`, and `QuestRepository`. Confirmed dead — no controller or external caller uses it. Leaving a dead method in any layer would be incomplete.

### QUAL-04: Named constant location

- **D-02:** Extract the 30-minute window as a **private constant in `QuestRepository.cs`** (e.g., `private const int DateMatchWindowMinutes = 30;`). It is an implementation detail of the date-matching logic, used only in this file. A shared constants class would be over-engineering for a single-use value.

### QUAL-05: File rename and folder housekeeping

- **D-03:** Rename `CharacterViewModels/GuildMembersIndexViewModel.cs` → `CharacterViewModels/CharactersIndexViewModel.cs` to match its class name (`CharactersIndexViewModel`). The `GuildMembersViewModels/GuildMembersIndexViewModel.cs` file is a separate, correctly named class — do not change it.
- **D-04:** After the rename, do a **full reorganize pass** on `CharacterViewModels/`: move any misplaced view models to their correct folders. This may touch controllers or views that reference moved types. Scope: fix all filename/classname mismatches and folder placement issues found.

### Claude's Discretion

- Exact constant name for the 30-minute window (`DateMatchWindowMinutes`, `ProposedDateMatchToleranceMinutes`, etc.) — planner decides based on readability in context.
- Whether to add an explanatory comment alongside the constant or rely on the name alone.
- Order of the 5 cleanup tasks within the plan.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Files Being Changed

- `EuphoriaInn.Domain/Configuration/SecurityConfiguration.cs` — delete this file
- `EuphoriaInn.Service/appsettings.json` — remove `Security` section
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs` — remove `UpdateQuestPropertiesAsync`
- `EuphoriaInn.Domain/Services/QuestService.cs` — remove `UpdateQuestPropertiesAsync` implementation
- `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs` — remove `UpdateQuestPropertiesAsync`
- `EuphoriaInn.Repository/QuestRepository.cs` — remove `UpdateQuestPropertiesAsync` implementation; fix `SignupRole == 1` magic number; extract 30-minute constant; rename constant near `IsSameDateTime`
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/GuildMembersIndexViewModel.cs` — rename file to `CharactersIndexViewModel.cs`
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/` — full folder audit for misplaced/misnamed files

### Requirements

- `.planning/REQUIREMENTS.md` §Code Quality & Dead Code — QUAL-01 through QUAL-05

### Codebase Map

- `.planning/codebase/CONCERNS.md` — original concern catalogue for context on why these cleanups were identified

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `EuphoriaInn.Domain/Enums/SignupRole.cs` — `SignupRole` enum already exists with `Player` and `Spectator` values; used correctly in domain and service layers. The fix in `QuestRepository.cs:105` is: `(SignupRole)playerSignup.SignupRole == SignupRole.Spectator`.
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` — already in the folder; reference point for what "correctly placed" looks like.

### Established Patterns

- `SignupRole` is stored as `int` in the entity (`PlayerSignupEntity.SignupRole`); the enum cast pattern `(SignupRole)src.SignupRole` is already used in `EntityProfile.cs` and `PlayerSignupRepository.cs`.
- `SecurityConfiguration` is `internal` and has zero callers — safe to delete without any reference updates.

### Integration Points

- `QuestRepository.cs:104-105` — `SignupRole == 1` comment and comparison to fix
- `QuestRepository.cs:196-198` — `IsSameDateTime` method containing the `<= 30` literal to extract
- `EuphoriaInn.Service/ViewModels/GuildMembersViewModels/GuildMembersIndexViewModel.cs` — separate, correctly named class used by `PlayersController`; must NOT be touched

</code_context>

<specifics>
## Specific Ideas

- The `CharacterViewModels/` folder reorganization is a **full reorganize** — move misplaced view models to correct folders even if it means updating controller `using` directives and view `@model` references.
- All 4 layers of `UpdateQuestPropertiesAsync` removal should be done atomically to keep the codebase buildable throughout.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 03-code-quality-dead-code*
*Context gathered: 2026-04-17*
