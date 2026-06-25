# Phase 18: DM Editing & Secondary Quest Views - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 18-dm-editing-secondary-quest-views
**Areas discussed:** Pre-Approved Players panel, QuestLog Details sidebar, CSS file grouping

---

## Pre-Approved Players panel

| Option | Description | Selected |
|--------|-------------|----------|
| Keep it — compact card below the form | Show a glass card section below the form listing the pre-approved player names (or empty-state if none). DMs get a confirmation of who carries over without navigating back. | ✓ |
| Omit it — same as tips sidebars | The DM already knows which players they selected on Manage. Keeps the mobile form lean. | |

**User's choice:** Keep it — compact glass card below the form
**Notes:** Unlike the tips sidebar (Phase 15 D-01, omitted), this panel shows functional context — which specific players carry over from the original quest. Worth keeping even on mobile.

---

## QuestLog Details sidebar

| Option | Description | Selected |
|--------|-------------|----------|
| Stacked below main content — two glass cards | Quick Actions card first (Back + Manage Quest), then Quest Statistics card. Consistent with phases 15–17 stacking pattern. | ✓ |
| Statistics inline in main card header | Move Building Access badge and Participants count into the main card header. Quick Actions as a simple button row. | |
| Simplified — just navigation buttons | Keep only navigation buttons (Back to Quest Log / Manage Quest). Drop the statistics panel on mobile. | |

**User's choice:** Stacked below main content — two glass cards
**Notes:** Building Access badge is specifically useful for this group (physical venue key access). Both panels are short enough to show fully on mobile without adding meaningful scroll depth.

---

## CSS file grouping

| Option | Description | Selected |
|--------|-------------|----------|
| One file per view — four files | quest-edit.mobile.css, quest-followup.mobile.css, dm-editprofile.mobile.css, quest-log-detail.mobile.css. Matches Phase 15 approach. | ✓ |
| Shared form CSS + detail CSS — two files | quest-forms.mobile.css shared by Edit and CreateFollowUp (+ EditProfile); quest-log-detail.mobile.css for the details page. | |
| Extend existing files where possible | Quest Edit reuses dm-create.mobile.css; CreateFollowUp and EditProfile get new files; QuestLog Details adds to quest-log.mobile.css. | |

**User's choice:** One file per view — four files
**Notes:** Views differ enough structurally (readonly dates vs datetime-local, form vs recap detail, photo upload) to justify separate files. Matches Phase 15 precedent.

---

## Claude's Discretion

- Exact CSS class names in new files
- Recap textarea `rows` attribute on mobile
- Building access badge placement within the Quest Statistics card
- Empty-state markup for pre-approved players

## Deferred Ideas

None.
