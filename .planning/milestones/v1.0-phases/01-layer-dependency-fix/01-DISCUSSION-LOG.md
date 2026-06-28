# Phase 1: Layer Dependency Fix - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-15
**Phase:** 01 — Layer Dependency Fix

---

## Areas Selected for Discussion

All four areas selected by user: Repo interface home, Entity construction in services, EntityProfile move, Removal scope.

---

## Area 1: Repository Interface Home

**Question:** Where should the repository interfaces (IBaseRepository, IQuestRepository, ICharacterRepository, etc.) live after Phase 1?

**Options presented:**
1. Move to Domain — Domain/Interfaces/ gets IBaseRepository + all specific repo contracts. Repository project implements them. Eliminates the namespace reference from Domain services.
2. Stay in Repository — Keep them where they are; only viable with a Contracts shim (adds complexity).

**Selected:** Move to Domain

**Notes:** Standard clean architecture; Domain defines contracts, Repository implements them.

---

## Area 2: Entity Construction in Services

**Context surfaced:** QuestService directly instantiates `ProposedDateEntity` in two private methods (`UpdateProposedDatesIntelligentlyAsync`, `UpdateProposedDatesWithNotificationTrackingAsync`). CharacterService instantiates `CharacterImageEntity` when updating profile images. These must move to avoid the ProjectReference.

**Question:** QuestService and CharacterService create entity objects directly. Where should that logic move?

**Options presented:**
1. Push to Repository methods — Add IQuestRepository.UpdateProposedDatesAsync and ICharacterRepository.UpdateProfileImageAsync. Repository owns entity construction internally.
2. Domain models only — Domain services pass only domain models; AutoMapper in Repository handles entity construction (adds DI complexity in Repository).

**Selected:** Push to Repository methods

**Notes:** Repository owns its entity graph; interface signatures use only primitive/domain model types.

---

## Area 3: EntityProfile Move

**Question:** Any preferences for how EntityProfile.cs is handled after the move?

**Options presented:**
1. Claude handles it — Standard move: new namespace EuphoriaInn.Repository.Automapper, update Program.cs using, delete from Domain.
2. Keep old namespace — Move the file but keep namespace EuphoriaInn.Domain.Automapper (misleading).

**Selected:** Claude handles it

---

## Area 4: Removal Scope

**Question:** How thoroughly should Phase 1 clean up Domain's relationship with Repository?

**Options presented:**
1. Minimum viable — Only remove what blocks the build; leave anything that compiles cleanly.
2. Full sweep — Audit every Domain file for EF-adjacent patterns and eliminate them (scope risk).

**Selected:** Minimum viable

---

## Summary of Decisions

| # | Decision |
|---|----------|
| D-01 | Repository interfaces move to EuphoriaInn.Domain/Interfaces/ |
| D-02 | Entity construction pushed to new targeted repository methods |
| D-03 | EntityProfile moves to EuphoriaInn.Repository/Automapper/ with correct namespace |
| D-04 | Minimum viable scope — only fix what blocks the build |
