# Phase 13: Core Player Views - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 13-core-player-views
**Areas discussed:** CSS architecture, Quest card content, Signup indicator style, Voting interaction pattern

---

## CSS Architecture

| Option | Description | Selected |
|--------|-------------|----------|
| Expand mobile.css | All mobile styles in one file; simple, predictable, grows over time | |
| @section Styles per view | Each .Mobile.cshtml pushes a `<style>` block inline | |
| Per-page CSS files | New `{page}.mobile.css` files loaded via `@section Styles` | ✓ |

**User's choice:** Per-page CSS files — consistent with the desktop pattern (desktop uses `quests.css`, `site.css`, etc.)
**Notes:** User confirmed this keeps the project consistent. Mobile files will be `home.mobile.css`, `quests.mobile.css`, `quest-log.mobile.css`.

---

## Quest Card Content

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal — required fields only | Title, CR, DM name, status badge. Tap to read description. | ✓ |
| Truncated description | 1–2 lines of description below meta fields | |

**User's choice:** Minimal required fields only
**Notes:** "tapping the card will (just like the desktop page) open the details page which holds the description"

---

## Signup Indicator Style

| Option | Description | Selected |
|--------|-------------|----------|
| Green badge / chip | Pill badge at top-right of card: "✓ Signed up" in green | ✓ |
| Colored left border | Thick left border on card row when signed up | |
| Icon row at card bottom | Checkmark + text line at bottom of card | |

**User's choice:** Green badge / chip

Follow-up — Status badge style:

| Option | Description | Selected |
|--------|-------------|----------|
| Colored badge | Open = green, Finalized = blue/gold, Done = gray | ✓ |
| Plain text label | Status as plain text next to DM name | |

**User's choice:** Colored badge — colors at Claude's discretion

---

## Voting Interaction Pattern

| Option | Description | Selected |
|--------|-------------|----------|
| Same AJAX pattern | Mobile view reuses existing JS voting logic, no page refresh | ✓ |
| Standard form posts | Form submit per vote, causes full page reload | |

**User's choice:** Same AJAX pattern as desktop
**Notes:** Mobile view is a simplified layout but uses identical interaction pattern.

---

## Claude's Discretion

- Exact Bootstrap color tokens for status badges
- Exact CSS values in per-page mobile CSS files
- Whether Quest Log mobile shows CR badge
- Empty-state markup
- Description placement within Quest Details mobile (above voting section)

## Deferred Ideas

None.
