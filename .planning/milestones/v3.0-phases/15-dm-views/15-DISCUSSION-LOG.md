# Phase 15: DM Views - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 15-dm-views
**Areas discussed:** Quest Create sidebar, Quest Manage vote breakdown, Quest Manage player selection JS, DM Profile quest history

---

## Quest Create Sidebar

| Option | Description | Selected |
|--------|-------------|----------|
| Omit entirely | Clean single-column form. Tips sidebar is non-essential on mobile. | ✓ |
| Keep below form | Bootstrap stacks it below by default — adds length to the page. | |
| Collapse as accordion | Collapsed toggle below form fields, expands on tap. | |

**User's choice:** Omit entirely
**Notes:** DMs creating quests know what to fill in; clean form preferred.

---

## Quest Create Date Row

| Option | Description | Selected |
|--------|-------------|----------|
| Keep input-group as-is | datetime-local triggers native touch pickers; Bootstrap input-group holds fine at 375px+. | ✓ |
| Stack input + Remove button | Safer for small screens, more vertical height per date row. | |
| Claude's discretion | Let Claude decide during implementation. | |

**User's choice:** Keep input-group as-is
**Notes:** Native pickers already handle mobile touch; no layout change needed.

---

## Quest Manage Vote Breakdown

| Option | Description | Selected |
|--------|-------------|----------|
| Condensed counts only | `3 Yes · 1 Maybe · 0 No` badges on date heading. Clean, fits one line. | ✓ |
| Stacked full lists | Yes/Maybe/No player names stacked full-width. Long page per date. | |
| Yes-count only | Only Yes count visible. Very compact but hides Maybe/No context. | |

**User's choice:** Condensed counts only
**Notes:** DM only needs the signal to pick a date; full name lists are desktop-only.

---

## Quest Manage Player Selection JS

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse desktop JS as-is | Same form structure, same `data-*` attributes — existing script works. | ✓ |
| Simplify to plain checkboxes | Remove count/highlight/suggest JS. Simpler but loses UX features. | |
| Claude's discretion | Let Claude keep or simplify based on mobile view structure. | |

**User's choice:** Reuse desktop JS as-is
**Notes:** Consistent behavior between desktop and mobile.

---

## DM Profile Quest History

| Option | Description | Selected |
|--------|-------------|----------|
| Card list per quest | Title + date + CR badge per row, tap-navigable. Matches Phase 13 pattern. | ✓ |
| table-responsive wrapper | Horizontal scroll enabled. Zero markup change but poor touch UX. | |
| Claude's discretion | Let Claude choose format. | |

**User's choice:** Card list per quest
**Notes:** User asked to verify if any post-completion changes (glass card additions, etc.) applied — confirmed glass card pattern is established in Phase 13/14 and should be used here too.

---

## Form Glass Cards

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — consistent glass card wrapping | All DM mobile sections wrapped in glass card containers with parchment text. | ✓ |
| No — plain forms | Default Bootstrap styling, no glass overlay. | |

**User's choice:** Glass card wrapping for all DM mobile sections
**Notes:** Consistent with Phase 13 (quests.mobile.css) and Phase 14 (calendar agenda fix).

---

## CSS File Structure

| Option | Description | Selected |
|--------|-------------|----------|
| Per-page CSS files | `dm-create.mobile.css`, `dm-manage.mobile.css`, `dm-profile.mobile.css` | ✓ |
| One shared dm.mobile.css | Single file for all three DM views. | |

**User's choice:** Per-page CSS files
**Notes:** Consistent with Phase 13 pattern.

---

## Claude's Discretion

- Whether to reuse `.quest-section-card-mobile` or define new equivalent classes in dm-specific CSS
- Exact vote-count badge styling in `dm-manage.mobile.css`
- Radio/checkbox touch target sizing in Quest Manage
- Whether DM profile photo and bio appear in one card or two on mobile
- Empty-state markup for DM with no quest history

## Deferred Ideas

None — discussion stayed within Phase 15 scope.
