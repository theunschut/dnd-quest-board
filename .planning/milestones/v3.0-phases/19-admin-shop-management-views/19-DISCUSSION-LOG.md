# Phase 19: Admin & Shop Management Views - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 19-admin-shop-management-views
**Areas discussed:** Admin list action buttons, ShopManagement Index sections, Shop/Details theming, ShopManagement Create/Edit pricing tools

---

## Admin list action buttons

### Users — action button layout

| Option | Description | Selected |
|--------|-------------|----------|
| Card per user — stacked buttons below | Glass card: name + role badge, then all applicable action buttons stacked vertically below. All actions immediately visible. | ✓ |
| Name row links to EditUser | Tappable name row opens EditUser; Promote/Demote/Delete added to EditUser as admin-only section. | |
| You decide | Claude picks the approach. | |

**User's choice:** Card per user — stacked buttons below
**Notes:** Follows established glass card pattern from Phases 13–18. All Promote/Demote/Edit/Delete buttons visible without extra taps.

---

### Quests — card structure

| Option | Description | Selected |
|--------|-------------|----------|
| Card per quest: Title — DM name — Status badge — Edit/Delete | No description. Clean and action-focused. | ✓ |
| Card per quest with short description excerpt | 50-char description on the card for admin context. | |

**User's choice:** Title, DM name, Status badge, Edit/Delete only — description omitted on mobile.

---

## ShopManagement Index — three sections

| Option | Description | Selected |
|--------|-------------|----------|
| All three sections: Pending Review, My Items, All Other Items | Full parity. | |
| Pending Review + My Items only | Omit All Other Items. | |
| My Items only | Minimal; review on desktop. | |
| Flat combined list with ALL items | No section separation. Single list of all items. | ✓ |

**User's choice:** Flat list with all items — no category separation. If not possible, fall back to all three sections.
**Notes:** User prefers simplified single-list view. Items from all three sections merged. Per-item info: Name + Rarity + Price + Status badge + action buttons.

---

## Shop/Details — theming approach

| Option | Description | Selected |
|--------|-------------|----------|
| Inline content with D&D glass card + parchment text | Consistent with all prior mobile phases. | ✓ |
| Reuse partial as-is inside glass card wrapper | Simpler. Minor visual inconsistency. | |
| You decide | Claude picks. | |

**User's choice:** Inline content with D&D glass card + parchment text treatment.

---

## ShopManagement Create/Edit — pricing tool buttons

| Option | Description | Selected |
|--------|-------------|----------|
| Keep in input-group — icon-only buttons | Stays as a row, smaller on mobile. | |
| Move buttons below price field as full-width buttons | Cleaner on mobile, more vertical space. | |
| Omit pricing tool buttons on mobile | DMs type price manually; rarity hint text still shown. | ✓ |

**User's choice:** Omit both pricing tool buttons (dice + hat) on mobile.
**Notes:** Price field full-width. Rarity hint text (`#price-suggestion`) still shows after rarity selection. `updatePriceSuggestion()` still fires but without enabling/disabling buttons.

---

## Claude's Discretion

- Ordering of items in the flat ShopManagement Index list
- Whether EditUser and ResetPassword share one CSS file
- Icon-only button sizing for ShopManagement item cards
- Empty-state markup when flat item list has no items
- Whether user email shows below name in Admin Users cards

## Deferred Ideas

None — discussion stayed within Phase 19 scope.
