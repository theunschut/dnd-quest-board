# Phase 5: Shop Filter & Sort - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-20
**Phase:** 05-shop-filter-sort
**Areas discussed:** Rarity filter UI, Type + rarity stacking, Sort control, Empty state

---

## Rarity Filter UI

| Option | Description | Selected |
|--------|-------------|----------|
| Compact row above grid | Checkboxes above item grid, styled like category tabs, shared Apply button | ✓ |
| Collapsible filter panel | Toggle bar that expands/collapses filter controls | |
| Inline with category tabs | Rarity checkboxes in same bar as category tabs | |

**User's choice:** Compact row above grid
**Notes:** Recommended option accepted.

---

## Type + Rarity + Sort Stacking

| Option | Description | Selected |
|--------|-------------|----------|
| Combine — stack all params | ?type=Equipment&rarity=Rare&sort=price_asc; category links preserve rarity+sort | ✓ |
| Independent — separate concerns | Type tabs reset rarity/sort; simpler but changes existing tab behavior | |

**User's choice:** Combine — stack all params
**Notes:** Recommended option accepted.

---

## Sort Control

| Option | Description | Selected |
|--------|-------------|----------|
| Select dropdown + shared submit | Sort select inside filter form, one Apply button for both | ✓ |
| Clickable sort links | <a href> sort links carrying current rarity state | |

**User's choice:** Select dropdown + shared submit
**Notes:** Recommended option accepted.

---

## Empty State

| Option | Description | Selected |
|--------|-------------|----------|
| Message + clear-filters link | "No items match your filters" + Clear filters link | ✓ |
| Message only | Message with no action link | |

**User's choice:** Message + clear-filters link
**Notes:** Recommended option accepted.

---

## Claude's Discretion

- Exact HTML structure and CSS class names for the filter row.
- Service method vs. controller-level filtering approach.
- URL parameter names (rarity, sort).
- Default sort order definition.

## Deferred Ideas

None.
