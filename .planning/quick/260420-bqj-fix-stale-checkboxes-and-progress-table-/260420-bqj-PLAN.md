---
phase: quick
plan: 260420-bqj
type: execute
wave: 1
depends_on: []
files_modified:
  - .planning/ROADMAP.md
  - .planning/PROJECT.md
autonomous: true
requirements: []
must_haves:
  truths:
    - "All completed phase checkboxes in ROADMAP.md are checked [x]"
    - "Bogus plan lists in Phases 4-8 are replaced with a TBD placeholder"
    - "Progress table reflects actual completion (3 phases done)"
    - "PROJECT.md Architecture Refactor items for Phase 2 are checked [x]"
  artifacts:
    - path: ".planning/ROADMAP.md"
      provides: "Accurate progress tracking"
    - path: ".planning/PROJECT.md"
      provides: "Accurate requirement completion status"
  key_links: []
---

<objective>
Fix stale checkboxes and the progress table in ROADMAP.md and PROJECT.md so they accurately reflect that Phases 1, 2, and 3 are complete.

Purpose: State corruption causes confusion when resuming planning — stale `[ ]` checkboxes and an outdated progress table make it look like earlier work is incomplete.
Output: ROADMAP.md and PROJECT.md with all completed items marked `[x]`, placeholder plan lists removed from Phases 4-8, and the progress table updated.
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/ROADMAP.md
@.planning/PROJECT.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix ROADMAP.md checkboxes, plan lists, and progress table</name>
  <files>.planning/ROADMAP.md</files>
  <action>
Apply these targeted changes to .planning/ROADMAP.md:

1. Phase header checkboxes (lines 15-17) — mark all three complete:
   - `[ ] **Phase 1: Layer Dependency Fix**` → `[x] **Phase 1: Layer Dependency Fix**`
   - `[ ] **Phase 2: Email & Service Consolidation**` → `[x] **Phase 2: Email & Service Consolidation**`
   - `[ ] **Phase 3: Code Quality & Dead Code**` → `[x] **Phase 3: Code Quality & Dead Code**`

2. Phase 1 plan list — mark 01-02-PLAN.md complete:
   - `- [ ] 01-02-PLAN.md` → `- [x] 01-02-PLAN.md`

3. Phase 3 plan list — mark 03-02-PLAN.md complete:
   - `- [ ] 03-02-PLAN.md` → `- [x] 03-02-PLAN.md`

4. Phases 4-8 plan lists — replace the copy-pasted Phase 1 entries with a TBD line.
   For each of the five phases (4 through 8), replace:
   ```
   Plans:
   - [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
   - [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference
   ```
   with:
   ```
   Plans:
   - TBD (phase not yet planned)
   ```

5. Progress table — replace the entire table with accurate data:
   ```
   | Phase | Plans Complete | Status | Completed |
   |-------|----------------|--------|-----------|
   | 1. Layer Dependency Fix | 2/2 | Complete | 2026-04-20 |
   | 2. Email & Service Consolidation | 3/3 | Complete | 2026-04-20 |
   | 3. Code Quality & Dead Code | 2/2 | Complete | 2026-04-20 |
   | 4. Security Hardening | 0/? | Not started | - |
   | 5. Shop Filter & Sort | 0/? | Not started | - |
   | 6. Follow-Up Quest | 0/? | Not started | - |
   | 7. DM Profile Page | 0/? | Not started | - |
   | 8. Profile Picture Avatar Crop | 0/? | Not started | - |
   ```
  </action>
  <verify>
    <automated>grep -c "\[x\]" .planning/ROADMAP.md</automated>
  </verify>
  <done>
    - Phase headers 1, 2, 3 are `[x]`
    - 01-02-PLAN.md and 03-02-PLAN.md entries are `[x]`
    - Phases 4-8 have no bogus 01-01/01-02 plan entries
    - Progress table shows Phases 1-3 as Complete with 2026-04-20
  </done>
</task>

<task type="auto">
  <name>Task 2: Fix PROJECT.md Architecture Refactor checkboxes</name>
  <files>.planning/PROJECT.md</files>
  <action>
Mark two Architecture Refactor items as complete (they were delivered in Phase 2: Email & Service Consolidation):

1. Line 30:
   `- [ ] Business logic (email sending, finalize logic, shop transactions) must live in services, not controllers`
   → `- [x] Business logic (email sending, finalize logic, shop transactions) must live in services, not controllers — Validated in Phase 02: email-service-consolidation`

2. Line 31:
   `- [ ] Controllers reduced to: validate input → call service → return view/redirect`
   → `- [x] Controllers reduced to: validate input → call service → return view/redirect — Validated in Phase 02: email-service-consolidation`
  </action>
  <verify>
    <automated>grep -c "\[x\]" .planning/PROJECT.md</automated>
  </verify>
  <done>
    Both Architecture Refactor lines are `[x]` with a phase reference note matching the pattern used for the other validated items in the file.
  </done>
</task>

</tasks>

<verification>
After both tasks complete:
- grep "\[ \]" .planning/ROADMAP.md should return only Phase 4-8 header lines and Security/Feature items — no plan list entries for completed phases
- grep "\[ \]" .planning/PROJECT.md should return only Security and New Feature items, not Architecture Refactor items
- Progress table rows for Phases 1-3 show "Complete" and "2026-04-20"
</verification>

<success_criteria>
- ROADMAP.md: Phases 1-3 fully checked, no phantom plan entries in Phases 4-8, accurate progress table
- PROJECT.md: Architecture Refactor section fully checked for completed items
- No other content changed in either file
</success_criteria>

<output>
No SUMMARY.md required for quick tasks. Changes are self-evident from git diff.
</output>
