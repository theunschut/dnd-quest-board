# Phase 3: Code Quality & Dead Code - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17
**Phase:** 03-code-quality-dead-code
**Areas discussed:** QUAL-02 removal scope, QUAL-04 constant location, QUAL-05 file rename + folder reorganize

---

## QUAL-02: Dead method removal scope

| Option | Description | Selected |
|--------|-------------|----------|
| All 4 layers | Remove from IQuestService, QuestService, IQuestRepository, and QuestRepository | ✓ |
| Service layer only | Remove from IQuestService and QuestService only, per literal requirement | |

**User's choice:** All 4 layers
**Notes:** Confirmed the method is dead — no controller or external caller uses it. Keeping a dead repository method would be incomplete.

---

## QUAL-04: Named constant location

| Option | Description | Selected |
|--------|-------------|----------|
| Private const in QuestRepository | `const int DateMatchWindowMinutes = 30` in QuestRepository.cs | ✓ |
| Shared constants class | New static class for cross-cutting reuse | |

**User's choice:** Private const in QuestRepository
**Notes:** Implementation detail of date-matching logic used only in one file — no need for a shared constants class.

---

## QUAL-05: File rename + folder reorganization

| Option | Description | Selected |
|--------|-------------|----------|
| File rename only | Rename GuildMembersIndexViewModel.cs → CharactersIndexViewModel.cs | |
| Rename + check folder | Rename file and verify CharacterViewModels/ for any other mismatches | ✓ (initial) |

**Follow-up: Folder scope**

| Option | Description | Selected |
|--------|-------------|----------|
| Verify only | Check filenames match class names, fix mismatches | |
| Full reorganize | Move misplaced view models to correct folders even if controllers/views need updating | ✓ |

**User's choice:** Full reorganize — move misplaced files and update all references.
**Notes:** `GuildMembersViewModels/GuildMembersIndexViewModel.cs` is a separate correctly-named class (used by PlayersController) and must not be touched.

---

## Claude's Discretion

- Exact constant name for the 30-minute window
- Whether to add a comment alongside the constant
- Order of the 5 cleanup tasks within the plan

## Deferred Ideas

None — discussion stayed within phase scope.
