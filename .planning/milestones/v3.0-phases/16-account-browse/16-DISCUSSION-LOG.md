# Phase 16: Account & Browse - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 16-account-browse
**Areas discussed:** Account page scope, Shop filter handling, Guild Members density

---

## Account Page Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Login + Register + Edit only | Match requirements (ACCT-01/02/03) exactly | |
| Include Profile view-only page | Common landing page after login — players see name, role, edit link | ✓ |

**User's choice:** Include Profile (view-only) page

| Option | Description | Selected |
|--------|-------------|----------|
| No — ChangePassword out of scope | Low-frequency, simple 3-field form, acceptable on mobile already | |
| Yes — include ChangePassword | Complete the full Account section; accessible from Edit page | ✓ |

**User's choice:** Include ChangePassword

| Option | Description | Selected |
|--------|-------------|----------|
| Glass card + parchment treatment | Consistent with Phases 13–15 — frosted glass, #F4E4BC parchment text | ✓ |
| Clean card, no parchment | Maximum legibility for utility/form pages | |

**User's choice:** Glass card + parchment (consistent with prior phases)

**Notes:** All five Account mobile views use the established aesthetic. No exceptions for "utility" pages.

---

## Shop Filter Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Collapse behind a Filter button | Bottom drawer/accordion; items visible immediately | ✓ |
| Stack vertically at the top | All controls above items; requires scroll to reach items | |
| You decide | Claude picks simplest approach meeting BROWSE-01 | |

**User's choice:** Collapse behind a Filter & Sort button

| Option | Description | Selected |
|--------|-------------|----------|
| Omit purchase history | Keep mobile view focused on browsing | ✓ |
| Collapsible section at bottom | Feature parity — My Purchases accordion below items | |
| You decide | Claude picks based on BROWSE-01 scope | |

**User's choice:** Omit purchase history on mobile

| Option | Description | Selected |
|--------|-------------|----------|
| 2-column grid | Visual shopping feel; Bootstrap 5 grid handles 375px+ comfortably | |
| Single-column list | More room for text; simpler CSS | |
| Other (free text) | User specified: 2-column is fine as long as item tap shows full-info popup like desktop | ✓ |

**User's choice:** 2-column grid + item tap opens the existing Bootstrap `#itemDetailsModal` (same as desktop)

**Notes:** User confirmed the item detail modal must work the same as desktop. The existing JS event listener and modal structure are reused verbatim — no separate mobile implementation.

---

## Guild Members Density

| Option | Description | Selected |
|--------|-------------|----------|
| 2-column card grid with image | Mirrors desktop character-grid; circular photo + name + role | |
| Single-column list row | Small circle thumbnail + name + class + role; one per row; easier to scan | ✓ |

**User's choice:** Single-column list rows

| Option | Description | Selected |
|--------|-------------|----------|
| Glass card + parchment | Consistent with all Phase 13–16 mobile views | ✓ |
| Simple dark card | Match desktop guild-members.css aesthetic | |

**User's choice:** Glass card + parchment treatment

**Notes:** My Characters and Other Characters sections each appear as a glass card section with list rows inside. Character tap navigates to Details page using the Phase 13 onclick tap pattern.

---

## Claude's Discretion

- Exact drawer implementation for Shop filters (offcanvas vs. Bootstrap collapse vs. custom toggle — choose simplest that avoids new JS).
- Empty-state markup when My Characters or Other Characters lists are empty.
- Circular thumbnail sizing in Guild Members list rows (target ~40×40px).
- Placement of "Create New Character" button on Guild Members mobile view.
- Whether ChangePassword needs a full `.Mobile.cshtml` or just CSS adjustments (it's a 3-field form).

## Deferred Ideas

None — discussion stayed within Phase 16 scope.
