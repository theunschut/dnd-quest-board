# Phase 3: Code Quality & Dead Code - Research

**Researched:** 2026-04-17
**Domain:** C# dead code removal, magic number elimination, named constants, file/class naming alignment
**Confidence:** HIGH

## Summary

Phase 3 is five independent mechanical cleanups with no behavior changes. Every item is a precise, targeted edit to a known file at a known line. The codebase has been inspected directly — no speculation required.

`SecurityConfiguration.cs` is `internal` with zero callers and its `Security` section in `appsettings.json` is isolated (three keys, no cross-references). `UpdateQuestPropertiesAsync` (non-notification variant) exists in all four layers — both interfaces and both implementations — but no controller, test, or caller references it; the solution-wide grep confirms this. The `SignupRole == 1` literal is a single occurrence at `QuestRepository.cs:105`; the `SignupRole` enum already has `Spectator = 1` defined and the cast pattern is already established elsewhere. The `<= 30` literal in `IsSameDateTime` is at line 198 of the same file and is the only occurrence. The file rename for QUAL-05 is a pure filename change — the class name is already `CharactersIndexViewModel`, the controller already references `CharactersIndexViewModel`, and the view already uses `@model CharactersIndexViewModel` — nothing beyond the filename needs changing.

The only structural consideration is D-04: a full audit of `CharacterViewModels/` for any additional misplaced or misnamed files beyond the target rename. The audit result is: only two files exist in the folder (`GuildMembersIndexViewModel.cs` and `CharacterViewModel.cs`), `CharacterViewModel.cs` is correctly named, so D-04 produces no additional work beyond the QUAL-05 rename itself.

**Primary recommendation:** Execute the five cleanups in a single plan in dependency order — QUAL-01 (delete file+config) then QUAL-02 (remove dead method from all 4 layers atomically) then QUAL-03+QUAL-04 (edits within `QuestRepository.cs`) then QUAL-05 (file rename). Build-verify after QUAL-02 removal because it touches an interface.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01:** Remove `UpdateQuestPropertiesAsync` (non-notification variant) from **all 4 layers**: `IQuestService`, `QuestService`, `IQuestRepository`, and `QuestRepository`. Confirmed dead — no controller or external caller uses it. Leaving a dead method in any layer would be incomplete.

**D-02:** Extract the 30-minute window as a **private constant in `QuestRepository.cs`** (e.g., `private const int DateMatchWindowMinutes = 30;`). It is an implementation detail of the date-matching logic, used only in this file. A shared constants class would be over-engineering for a single-use value.

**D-03:** Rename `CharacterViewModels/GuildMembersIndexViewModel.cs` → `CharacterViewModels/CharactersIndexViewModel.cs` to match its class name (`CharactersIndexViewModel`). The `GuildMembersViewModels/GuildMembersIndexViewModel.cs` file is a separate, correctly named class — do not change it.

**D-04:** After the rename, do a **full reorganize pass** on `CharacterViewModels/`: move any misplaced view models to their correct folders. This may touch controllers or views that reference moved types. Scope: fix all filename/classname mismatches and folder placement issues found.

### Claude's Discretion

- Exact constant name for the 30-minute window (`DateMatchWindowMinutes`, `ProposedDateMatchToleranceMinutes`, etc.) — planner decides based on readability in context.
- Whether to add an explanatory comment alongside the constant or rely on the name alone.
- Order of the 5 cleanup tasks within the plan.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QUAL-01 | `SecurityConfiguration.cs` deleted; `Security` section removed from `appsettings.json` | File is `internal`, zero callers confirmed; `appsettings.json` Security block is 3 lines, self-contained |
| QUAL-02 | Dead `UpdateQuestPropertiesAsync` (non-notification variant) removed from `IQuestService` and `QuestService` | Verified dead across all 4 layers; no test or controller calls it; all 4 removal sites documented below |
| QUAL-03 | `SignupRole == 1` magic number replaced with `(SignupRole)playerSignup.SignupRole == SignupRole.Spectator` | Single occurrence at `QuestRepository.cs:105`; enum exists; cast pattern established |
| QUAL-04 | `IsSameDateTime` 30-minute window extracted as a named constant with explanatory comment | Single literal at `QuestRepository.cs:198`; constant belongs in same file per D-02 |
| QUAL-05 | `CharacterViewModels/GuildMembersIndexViewModel.cs` renamed to `CharactersIndexViewModel.cs` | Class name already `CharactersIndexViewModel`; controller + view already use class name correctly; pure filename rename |
</phase_requirements>

---

## Standard Stack

No new libraries required. All work is pure C# edits and a file rename within the existing solution.

| Tool | Version | Purpose |
|------|---------|---------|
| dotnet build | .NET 8 SDK | Verify no broken references after interface method removal |
| dotnet test | xUnit 2.5.3 | Regression check (30 unit + 53 integration tests, all passing) |

## Architecture Patterns

### Pattern 1: Removing a Method from Interface + All Implementations Atomically

For QUAL-02, the four files must be edited in a single commit to keep the solution buildable. The order of edits within a single task:
1. `IQuestRepository.cs` — remove `UpdateQuestPropertiesAsync` declaration
2. `QuestRepository.cs` — remove `UpdateQuestPropertiesAsync` implementation
3. `IQuestService.cs` — remove `UpdateQuestPropertiesAsync` declaration
4. `QuestService.cs` — remove `UpdateQuestPropertiesAsync` implementation

The `IQuestService` interface method delegates to `IQuestRepository`; both disappear together. Because `internal class QuestService` implements `IQuestService`, removing from the interface means removing the implementation too — no orphan method can compile. Build after this group of edits.

### Pattern 2: Private Named Constant for Magic Number

```csharp
// QuestRepository.cs — place near top of class body, before first method
// Tolerance window for treating two proposed dates as the same slot;
// accommodates minor timezone rounding when users resubmit dates.
private const int DateMatchWindowMinutes = 30;
```

Replace `<= 30` with `<= DateMatchWindowMinutes` at line 198. The constant is `private` because `IsSameDateTime` is a private static helper used only within `QuestRepository`.

### Pattern 3: Enum Cast to Replace Magic Number

Existing pattern already used in `EntityProfile.cs` and `PlayerSignupRepository.cs`:

```csharp
// Before (QuestRepository.cs:105):
if (playerSignup.SignupRole == 1)

// After:
if ((SignupRole)playerSignup.SignupRole == SignupRole.Spectator)
```

`PlayerSignupEntity.SignupRole` is stored as `int`; casting to the enum for comparison is the established project convention.

### Pattern 4: File Rename Without Class/Namespace Change

The file `CharacterViewModels/GuildMembersIndexViewModel.cs` already contains:
```csharp
namespace EuphoriaInn.Service.ViewModels.CharacterViewModels;
public class CharactersIndexViewModel { ... }
```

The controller (`GuildMembersController.cs`) references `CharactersIndexViewModel` by class name and imports `EuphoriaInn.Service.ViewModels.CharacterViewModels`. The view (`GuildMembers/Index.cshtml`) has `@model CharactersIndexViewModel` and `@using EuphoriaInn.Service.ViewModels.CharacterViewModels`. Neither references the filename. The rename is a filesystem operation only — no content edits needed.

### D-04 Audit Result: No Additional Work

`CharacterViewModels/` contains exactly two files:
- `GuildMembersIndexViewModel.cs` — class `CharactersIndexViewModel` — misnamed (QUAL-05 fixes this)
- `CharacterViewModel.cs` — class `CharacterViewModel` + `CharacterClassViewModel` + validation attributes — correctly named and correctly placed

No other files need to move or be renamed. D-04 is satisfied by QUAL-05 alone.

### Anti-Patterns to Avoid

- **Partial interface removal:** Removing `UpdateQuestPropertiesAsync` from `IQuestService` but leaving it in `IQuestRepository` (or vice versa) will break `QuestService` which implements `IQuestService` and calls `repository.UpdateQuestPropertiesAsync`. Remove from all 4 layers in one atomic operation.
- **Renaming the class name:** QUAL-05 renames the file only. The class name `CharactersIndexViewModel` is correct and already referenced throughout. Renaming it would require updating controller, view, and AutoMapper profile.
- **Touching `GuildMembersViewModels/GuildMembersIndexViewModel.cs`:** This is a different file, different class (`GuildMembersIndexViewModel`), used by `PlayersController` and `Views/Players/Index.cshtml`. Must not be changed.
- **Shared constants class for QUAL-04:** D-02 explicitly ruled this out. The constant stays private in `QuestRepository.cs`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Verify dead code | Manual grep alone | `dotnet build` after removal — compiler errors are the authoritative check |
| File rename tracking | Manual `using` audit | Trust the compiler — since filename != class name in C#, the rename has no code impact |

## Runtime State Inventory

No runtime state is affected by these changes. All five cleanups are source-only edits.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — no database columns reference class names or method names | None |
| Live service config | None — `SecurityConfiguration` values are in `appsettings.json` only, not in any external service config | None |
| OS-registered state | None | None |
| Secrets/env vars | `Security:PasswordIterations`, `Security:SaltSize`, `Security:HashSize` — in `appsettings.json` only; no env var overrides reference these keys | Remove from `appsettings.json` only |
| Build artifacts | None — no compiled artifacts embed these names persistently | None |

## Common Pitfalls

### Pitfall 1: Forgetting the `Security` section in `appsettings.json` after deleting `SecurityConfiguration.cs`

**What goes wrong:** The dead class is deleted but the three-key JSON block remains. Future developers or automated tooling may try to bind it.
**How to avoid:** Delete both in the same task. The Security block is lines 2-6 of `appsettings.json` — a self-contained top-level JSON key.

### Pitfall 2: Leaving `UpdateQuestPropertiesAsync` in `IQuestRepository` after removing from `IQuestService`

**What goes wrong:** `QuestService` calls `repository.UpdateQuestPropertiesAsync(...)` — if that method is gone from `IQuestRepository`, `QuestService` won't compile. If only `IQuestService` is cleaned, the implementation in `QuestService.cs` still calls the repository method and the interface removal is incomplete.
**How to avoid:** Remove all four occurrences in one atomic pass; run `dotnet build` immediately after.

### Pitfall 3: Treating D-04 as open-ended scope

**What goes wrong:** D-04 says "full reorganize pass" which sounds like significant work. In practice `CharacterViewModels/` has exactly two files, and only one has a filename mismatch. Misreading the scope wastes planning effort.
**How to avoid:** The audit is complete (done in this research). D-04 produces no additional work beyond the QUAL-05 rename.

### Pitfall 4: Renaming the class when renaming the file

**What goes wrong:** Some editors auto-rename the class when renaming the file. The class name `CharactersIndexViewModel` is already correct. Renaming it to match the old filename, or to some other name, breaks the controller and view.
**How to avoid:** Rename the file only. Verify the class name stays `CharactersIndexViewModel` after the rename. No `sed` or find-replace on the class name.

## Code Examples

### QUAL-01: Delete `SecurityConfiguration.cs`, remove `Security` block from `appsettings.json`

`appsettings.json` before (lines 1-7):
```json
{
  "Security": {
    "PasswordIterations": 100000,
    "SaltSize": 32,
    "HashSize": 32
  },
  "Logging": {
```

After (lines 1-3):
```json
{
  "Logging": {
```

### QUAL-02: Remove `UpdateQuestPropertiesAsync` from all 4 layers

**`IQuestService.cs` — remove line 24:**
```csharp
// DELETE this line:
Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);
```

**`QuestService.cs` — remove lines 105-108:**
```csharp
// DELETE this method:
public async Task UpdateQuestPropertiesAsync(int questId, ...)
{
    await repository.UpdateQuestPropertiesAsync(...);
}
```

**`IQuestRepository.cs` — remove line 28:**
```csharp
// DELETE this line:
Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);
```

**`QuestRepository.cs` — remove lines 136-156:**
```csharp
// DELETE this method (UpdateQuestPropertiesAsync, lines 136-156):
public async Task UpdateQuestPropertiesAsync(int questId, ...) { ... }
```

### QUAL-03: Replace magic number in `QuestRepository.cs:105`

```csharp
// Before:
if (playerSignup.SignupRole == 1)

// After:
if ((SignupRole)playerSignup.SignupRole == SignupRole.Spectator)
```

Requires adding `using EuphoriaInn.Domain.Enums;` if not already present — it is already present (`using EuphoriaInn.Domain.Enums;` is line 3 of `QuestRepository.cs`). No import change needed.

### QUAL-04: Extract named constant in `QuestRepository.cs:198`

Add constant near top of class body (before first method, after class declaration):
```csharp
// Tolerance window for treating two proposed dates as the same slot;
// accommodates minor timezone rounding when users resubmit dates.
private const int DateMatchWindowMinutes = 30;
```

Change `IsSameDateTime` method (line 198):
```csharp
// Before:
return Math.Abs((date1 - date2).TotalMinutes) <= 30;

// After:
return Math.Abs((date1 - date2).TotalMinutes) <= DateMatchWindowMinutes;
```

### QUAL-05: Rename file

Git rename (preserves history):
```bash
git mv "EuphoriaInn.Service/ViewModels/CharacterViewModels/GuildMembersIndexViewModel.cs" \
       "EuphoriaInn.Service/ViewModels/CharacterViewModels/CharactersIndexViewModel.cs"
```

No content changes inside the file.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj`, `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --no-build` |
| Full suite command | `dotnet test` |

### Phase Requirements -> Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QUAL-01 | `SecurityConfiguration` deleted, `appsettings.json` Security section gone | build smoke | `dotnet build` | N/A — build-only check |
| QUAL-02 | Dead method absent from all 4 layers; build succeeds | build smoke | `dotnet build` | N/A — build-only check |
| QUAL-03 | Spectator auto-approve uses enum, not literal 1 | existing unit test | `dotnet test EuphoriaInn.UnitTests --no-build` | Covered by `QuestFinalizeTests` (integration) and `QuestServiceTests` (unit) |
| QUAL-04 | Named constant present; `IsSameDateTime` uses it | build smoke | `dotnet build` | N/A — naming/readability, no behavioral change |
| QUAL-05 | File renamed; build succeeds; `GuildMembers/Index` still renders | build smoke + integration | `dotnet test EuphoriaInn.IntegrationTests --no-build` | `GuildMembersControllerIntegrationTests.cs` ✅ |

### Sampling Rate

- **Per task commit:** `dotnet build` — confirms no broken references
- **Per wave merge:** `dotnet test` (full suite: 30 unit + 53 integration)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

None — existing test infrastructure covers all phase requirements. No new test files needed. QUAL-03 behavioral correctness is covered by existing tests. QUAL-01/02/04/05 are structural — build success is the validation.

## Environment Availability

Step 2.6: SKIPPED — all five changes are code/config edits with no external tool dependencies beyond the .NET 8 SDK (already confirmed present: `dotnet build` and `dotnet test` both run successfully).

## Sources

### Primary (HIGH confidence)

Direct source code inspection of:
- `EuphoriaInn.Domain/Configuration/SecurityConfiguration.cs`
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs`
- `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs`
- `EuphoriaInn.Domain/Services/QuestService.cs`
- `EuphoriaInn.Repository/QuestRepository.cs`
- `EuphoriaInn.Domain/Enums/SignupRole.cs`
- `EuphoriaInn.Service/appsettings.json`
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/GuildMembersIndexViewModel.cs`
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs`
- `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs`
- `EuphoriaInn.Service/Views/GuildMembers/Index.cshtml`
- Solution-wide grep for `UpdateQuestPropertiesAsync` — confirmed no callers in controllers or tests
- `dotnet test` — 83 tests passing (30 unit, 53 integration)

### Secondary (MEDIUM confidence)

- `.planning/codebase/CONCERNS.md` — original concern catalogue corroborating dead-code classification
- `.planning/research/PITFALLS.md` — prior research pitfalls for this codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries; existing .NET 8 solution verified building and all tests passing
- Architecture: HIGH — all change sites identified by direct file inspection, exact line numbers documented
- Pitfalls: HIGH — verified by reading actual file contents and running solution-wide grep

**Research date:** 2026-04-17
**Valid until:** 2026-05-17 (stable codebase; no external API dependencies)
