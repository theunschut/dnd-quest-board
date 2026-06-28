---
phase: 3
phase_name: code-quality-dead-code
status: draft
created: 2026-04-17
design_system: none
shadcn: not applicable
---

# UI-SPEC — Phase 3: Code Quality & Dead Code

## Summary

Phase 3 contains zero UI changes. All five requirements (QUAL-01 through QUAL-05) are
pure backend/structural edits: deleting a dead configuration class, removing dead interface
methods, replacing a magic number with an enum cast, extracting a named constant, and
renaming a ViewModel file. No view, no stylesheet, no JavaScript, and no rendered page is
added or modified. No new visual or interaction contract is needed.

This UI-SPEC is recorded as a no-UI phase so the checker and auditor can confirm absence
of UI work is intentional rather than an oversight.

---

## Design System

| Field            | Value                                                              |
|------------------|--------------------------------------------------------------------|
| Tool             | none — Bootstrap 5.3.0 via CDN; no component registry             |
| shadcn gate      | not applicable — ASP.NET Core MVC Razor project, not React/Next.js |
| Registry         | not applicable                                                     |
| Registry Safety  | not applicable                                                     |

---

## Spacing

Not applicable — no UI elements added or modified in this phase.

Project baseline (Bootstrap 5 + custom CSS): 4 / 8 / 12 / 16 / 24 / 32 / 48 / 64 px.
These remain in force for all existing views; no overrides introduced.

---

## Typography

Not applicable — no UI elements added or modified in this phase.

Project baseline:
- Body: 16px, weight 400, line-height 1.5 (Bootstrap default)
- Headings (Cinzel, Google Fonts): 20 / 24 / 28 / 32px, weight 600, line-height 1.2
- Small/secondary text: 14px, weight 400

---

## Color

Not applicable — no UI elements added or modified in this phase.

Project baseline (Bootstrap 5 semantic tokens + custom):
- 60% dominant surface: #ffffff / Bootstrap `light` (page background, cards)
- 30% secondary: #212529 / Bootstrap `dark` (nav, footer, card headers)
- 10% accent: #ffc107 (Bootstrap `warning`) — reserved for: form focus ring, shop
  header border/glow, DM badge highlights, CTA hover states
- Destructive: #dc3545 (Bootstrap `danger`) — reserved for: delete actions, error alerts

---

## Copywriting

Not applicable — no new user-facing text, labels, empty states, or error messages are
introduced in this phase.

Existing copy across views is unchanged. No CTA labels, empty state messages, or
destructive confirmation flows are added or modified.

---

## Interaction Contracts

None. Phase 3 produces no new user interactions, no new form flows, and no new navigation
paths.

---

## Component Inventory

None. No new components are added or reused in this phase.

---

## States & Transitions

None. No UI state machine changes in this phase.

---

## Accessibility

Not applicable — no UI surface modified. Existing accessibility posture (Bootstrap 5
semantic HTML, ARIA from existing views) is unchanged.

---

## Source Traceability

| Decision         | Source                          | Notes                                                    |
|------------------|---------------------------------|----------------------------------------------------------|
| Zero UI changes  | CONTEXT.md — Phase Boundary     | "No behavior changes — purely structural and naming cleanup" |
| Zero UI changes  | RESEARCH.md — Standard Stack    | "No new libraries required. All work is pure C# edits and a file rename" |
| Zero UI changes  | REQUIREMENTS.md — QUAL-01..05   | All five requirements are backend/structural             |
| Baseline tokens  | site.css inspection             | Bootstrap 5 + #ffc107 accent + Cinzel font confirmed     |

---

## Executor Notes

- No view files require editing in this phase.
- The ViewModel file rename (QUAL-05) changes only the filename on disk; the class name,
  namespace, `@model` directive, and `using` in the view are already correct and must not
  be altered.
- `dotnet build` after each task is the design validation gate — there is no visual
  regression surface to check.
- UI-auditor: confirm no view files were modified. If any view diff appears, treat it as
  an unintended side-effect and flag for review.
