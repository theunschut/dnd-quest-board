# Phase 14: Calendar - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 14-calendar
**Areas discussed:** Month navigation, Vote button style in _Calendar.Mobile.cshtml, Update Your Vote unification (CAL-05), Legend on mobile

---

## Month Navigation

| Option | Description | Selected |
|--------|-------------|----------|
| Full-width row | Single row [<] JUNE 2026 [>], flanking chevrons, full-width | ✓ |
| Same as desktop but full-width | Compact, mirrors desktop layout | |
| Bottom sticky bar | Prev/next pinned to bottom of screen | |

**User's choice:** Full-width row — then refined to single row with [<] JUNE 2026 [>] (chevrons flanking the month name in one row)

**Notes:** User confirmed single row, not three separate rows for each element.

---

## Vote Button Style in _Calendar.Mobile.cshtml

| Option | Description | Selected |
|--------|-------------|----------|
| Stacked full-width buttons | Three full-width rows per date: Yes / No / Maybe, each 44px | |
| Horizontal [Yes] [No] [Maybe] row | Three equal-width buttons side-by-side, icon + text, 44px | ✓ |
| Icon-only buttons (match desktop) | Scale desktop icons to 44px | |

**User's choice:** Horizontal [Yes] [No] [Maybe] row — equal-width, icon + short text, 44px height

**Notes:** User asked for a text mockup before deciding. After seeing both options, chose the horizontal row as more compact for multiple proposed dates.

---

## Update Your Vote Unification (CAL-05)

| Option | Description | Selected |
|--------|-------------|----------|
| Replace with _Calendar partial | Follow CAL-05; signed-up players see full per-date list with vote state | ✓ |
| Keep AJAX buttons for Update Your Vote | Simpler; AJAX update approach; _Calendar.Mobile.cshtml for Choose a Date only | |

**User's choice:** Replace with `_Calendar` partial — follow CAL-05 as specified. Unified code path; per-date vote visibility.

**Notes:** The AJAX buttons in the current "Update Your Vote" section use quest-level vote endpoints (`ChangeVoteToYes/{questId}`, etc.), whereas the calendar partial uses per-date radio buttons bound to the form model. The replacement switches the interaction from AJAX to form-based, but provides more per-date granularity.

---

## Legend on Mobile

| Option | Description | Selected |
|--------|-------------|----------|
| Omit the legend | Clean view; inline indicators are self-explanatory | ✓ |
| Small inline legend below nav | Compact row of indicators below month navigation | |
| Collapsible legend | Expandable legend button | |

**User's choice:** Omit the legend entirely.

**Notes:** The colored dot per quest entry and the warning icon inline are sufficient for the mobile agenda.

---

## Claude's Discretion

- Exact Bootstrap utility classes for day-label section header styling
- Whether the warning icon (no building key) appears on agenda quest entries
- Heading text inside the "Update Your Vote" wrapper section after replacement
- Empty-state message for months with no quests

## Deferred Ideas

None.
