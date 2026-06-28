# Phase 17: Character & Player Views - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 17-character-player-views
**Areas discussed:** Character detail layout, Dynamic class form on mobile, Players page — email visibility, CSS file naming strategy

---

## Character detail layout

| Option | Description | Selected |
|--------|-------------|----------|
| Portrait → name/badges → stats → backstory → actions | Leads with visual identity; actions at bottom | ✓ |
| Portrait → name/badges → actions → stats → backstory | Actions surfaced early for edit/retire workflows | |
| Stats → portrait → backstory → actions | Info-first layout | |

**User's choice:** Portrait → name/badges → stats → backstory → actions (recommended)

**Portrait size:**

| Option | Description | Selected |
|--------|-------------|----------|
| Full-width at top, capped ~220px, centered | Fills mobile width without being oversized | ✓ |
| Small circle/thumbnail inline with name | Matches Phase 16 Guild Members Index pattern | |
| You decide | Claude picks | |

**Delete button visibility:**

| Option | Description | Selected |
|--------|-------------|----------|
| Show all three actions including Delete | Feature parity; confirm dialog preserved | ✓ |
| Show Edit and Retire only — hide Delete on mobile | Prevents accidental deletion | |
| You decide | Claude picks | |

---

## Dynamic class form on mobile

| Option | Description | Selected |
|--------|-------------|----------|
| Stack vertically: Class select full-width, Level input full-width, Remove button below | All controls comfortably tappable | ✓ |
| Two columns: Class select ~70% + Level input ~30%, Remove button below | More compact but class names may be cramped | |
| Inline same as desktop with smaller col widths | Risky — 140px for class dropdown with long D&D names | |

**User's choice:** Stack vertically (recommended)

**Class JS:**

| Option | Description | Selected |
|--------|-------------|----------|
| Copy same @section Scripts block verbatim | Same logic, different HTML template string | ✓ |
| Extract JS to shared file | Cleaner long-term, adds new wwwroot JS file | |
| You decide | Claude picks | |

**Photo upload placement:**

| Option | Description | Selected |
|--------|-------------|----------|
| Top of form, full-width, thumbnail centered above file input | Natural first thing when creating character | ✓ |
| Below name/level/status fields, before classes | Character basics more important than photo | |
| You decide | Claude places naturally | |

**Notes:** User confirmed glass card treatment should follow the same pattern as phases 13–16 throughout.

---

## Players page — email visibility

| Option | Description | Selected |
|--------|-------------|----------|
| Show name only — no email on mobile | Clean layout; email is a desktop use case | ✓ (via custom) |
| Show email below name as a smaller line | Preserves feature parity | |
| Show email only if short enough, truncate otherwise | Inconsistent row heights | |

**User's choice (freeform):** "I have an issue on the backlog to remove the email entirely from this view. Also from the desktop version. So in this case name only, and remove the email column on the desktop version as well."

**Decision to fold desktop email removal into Phase 17:**

| Option | Description | Selected |
|--------|-------------|----------|
| Fold into Phase 17 — do both mobile + desktop email removal together | Small, contained change; low risk | ✓ |
| Defer — mobile name-only now, desktop email stays | Keeps phase strictly additive | |

**Players layout:**

| Option | Description | Selected |
|--------|-------------|----------|
| Stack vertically: DMs section first, Players section below | Direct adaptation of desktop 2-column | ✓ |
| Tabs: one tab for DMs, one for Players | Hides half the content; adds complexity | |
| You decide | Claude picks simplest | |

---

## CSS file naming strategy

| Option | Description | Selected |
|--------|-------------|----------|
| 3 files: character-form.mobile.css, character-detail.mobile.css, players.mobile.css | Create+Edit share one file; follows Phase 16 pattern | ✓ |
| 4 files: separate CSS for each view | Maximum isolation | |
| 1 file for all GuildMembers + players.mobile.css | Maximum reuse | |

**User's choice:** 3 files (recommended)

---

## Claude's Discretion

- Exact portrait `max-height` within the ~220px guidance
- Class information display style on Details (inline badges vs. stacked list)
- Empty-state markup when no DMs or Players registered
- Sheet link truncation vs. natural wrap
- Exact spacing/padding values — follow established glass card pattern from quests.mobile.css

## Deferred Ideas

None — discussion stayed within Phase 17 scope.
