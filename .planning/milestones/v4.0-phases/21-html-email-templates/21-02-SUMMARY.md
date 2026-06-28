---
phase: 21-html-email-templates
plan: 02
subsystem: email, ui
tags: [razor-components, htmlrenderer, email-templates, inline-css, blazor]

# Dependency graph
requires:
  - phase: 21-01
    provides: "IEmailRenderService interface in Domain; IEmailService.SendAsync; EF Core migration"
provides:
  - "_EmailLayout.razor: shared email HTML document wrapper with Cinzel font link, parchment background, PreviewText hidden span, ChildContent slot"
  - "QuestFinalized.razor: quest finalization email with Poster1.png background, CR badge top-left, gold divider, metadata rows, wax seal, all 8 parameters"
  - "QuestDateChanged.razor: session date changed notification with Poster6.png background, no CR badge, OldDate/NewDate params, 6 parameters"
  - "SessionReminder.razor: 24h session reminder with Poster1.png, CR badge, full QuestDescription (not truncated), wax seal; parameter contract locked per D-15"
affects:
  - 21-03-hangfire-email-jobs
  - 21-04-questservice-decoupling
  - phase-22-session-reminder-job

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Email Razor components use ChildContent composition via <_EmailLayout> — no @layout directive (HtmlRenderer does not support @layout)"
    - "All CSS inline style= attributes — no <style> blocks, no external stylesheet links (email client compatibility)"
    - "All image URLs absolute with URL-encoded spaces: Blanks%20w%20Shadow, Wax%20Seals (email clients cannot resolve relative paths)"
    - "Table-based layout — no flexbox/grid/position:absolute (stripped by email clients)"

key-files:
  created:
    - EuphoriaInn.Service/Components/Emails/_EmailLayout.razor
    - EuphoriaInn.Service/Components/Emails/QuestFinalized.razor
    - EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor
    - EuphoriaInn.Service/Components/Emails/SessionReminder.razor

key-decisions:
  - "No deviations — plan executed exactly as specified; all four components match UI-SPEC.md exactly"

patterns-established:
  - "Email component composition: wrap content in <_EmailLayout Subject=... PreviewText=...>...</_EmailLayout> — never @layout"
  - "Poster background via background-image on outer <table> with background-color fallback for Outlook"
  - "CR badge as nested <table><tr><td> with inline gradient style — simulates absolute positioning without CSS position"

requirements-completed: [EMAIL-01, EMAIL-02, EMAIL-03]

# Metrics
duration: 3min
completed: 2026-06-26
---

# Phase 21 Plan 02: Razor Email Components Summary

**Four D&D-themed inline-CSS Razor email components with parchment poster backgrounds, Cinzel headings, gold CR badges, and wax seal imagery — ready for HtmlRenderer consumption in Wave 3**

## Performance

- **Duration:** 3 min
- **Started:** 2026-06-26T11:04:14Z
- **Completed:** 2026-06-26T11:06:56Z
- **Tasks:** 2
- **Files modified:** 4 (all new)

## Accomplishments

- `_EmailLayout.razor` created as shared wrapper: outputs a complete `<!DOCTYPE html>` document with Cinzel Google Fonts link, parchment body background (`#F4E4BC`), optional hidden PreviewText span for Gmail preview line, and `ChildContent` slot
- `QuestFinalized.razor` created with Poster1.png background, CR badge (top-left nested table), quest title/description/metadata rows, gold divider, CTA button, wax seal (Crown Seal.png); all 8 parameters as `[Parameter, EditorRequired]`
- `QuestDateChanged.razor` created with Poster6.png background (shorter email), no CR badge, no wax seal, OldDate/NewDate parameters — matches UI-SPEC D-03 exactly
- `SessionReminder.razor` created with Poster1.png, CR badge, full `QuestDescription` (not truncated), wax seal; parameter interface locked per D-15 — Phase 22 can consume without modification

## Task Commits

Each task was committed atomically:

1. **Task 1: Create _EmailLayout.razor and QuestFinalized.razor** - `ff10459` (feat)
2. **Task 2: Create QuestDateChanged.razor and SessionReminder.razor** - `befce95` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `EuphoriaInn.Service/Components/Emails/_EmailLayout.razor` — HTML document wrapper with `Subject`, `PreviewText?`, `ChildContent` parameters
- `EuphoriaInn.Service/Components/Emails/QuestFinalized.razor` — Quest finalization email; 8 required params; Poster1.png; CR badge; wax seal
- `EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor` — Date changed email; 6 required params; Poster6.png; no CR badge or wax seal
- `EuphoriaInn.Service/Components/Emails/SessionReminder.razor` — 24h reminder email; 8 required params locked per D-15; full description; Poster1.png; CR badge; wax seal

## Decisions Made

None - followed plan as specified. All design tokens, copy strings, parameter names, and image URLs implemented exactly as specified in UI-SPEC.md and the plan interfaces block.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None. The Service project uses `Microsoft.NET.Sdk.Web` which supports `.razor` files natively — no project file changes needed.

## Threat Surface Scan

No new trust boundaries introduced. All four components use `@variable` Razor auto-escaping for user-supplied content (QuestTitle, QuestDescription, DmName, ConfirmedPlayerNames). `AppUrl` is server-configured (not user-supplied). No `@((MarkupString)...)` used anywhere — XSS threat T-21-04 and T-21-05 are mitigated as planned.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- All four `.razor` components are in `EuphoriaInn.Service/Components/Emails/` and build without error
- Wave 3 (Plan 03: Hangfire email jobs) can now implement `RazorEmailRenderService` and call `renderService.RenderAsync<QuestFinalized>(parameters)` and `renderService.RenderAsync<QuestDateChanged>(parameters)`
- Phase 22 session reminder job can call `renderService.RenderAsync<SessionReminder>(parameters)` with the D-15 parameter set — no component changes needed
- `dotnet build EuphoriaInn.Service` exits 0 with 6 pre-existing warnings (NU1510 + CS0618 from Plan 01)

## Self-Check

Verified:
- `_EmailLayout.razor` exists and contains `<!DOCTYPE html>`, `https://fonts.googleapis.com/css2?family=Cinzel`, `background-color:#F4E4BC`, `[Parameter, EditorRequired] public RenderFragment ChildContent`
- `QuestFinalized.razor` contains `<_EmailLayout`, `Blanks%20w%20Shadow/Poster1.png`, `linear-gradient(135deg,#8B4513,#A0522D)`, `Wax%20Seals/Crown%20Seal.png`
- `QuestDateChanged.razor` contains `<_EmailLayout`, `Blanks%20w%20Shadow/Poster6.png`, `[Parameter, EditorRequired] public DateTime OldDate`, `[Parameter, EditorRequired] public DateTime NewDate`; does NOT contain `Poster1.png` or CR badge gradient
- `SessionReminder.razor` contains `<_EmailLayout`, `Blanks%20w%20Shadow/Poster1.png`, `linear-gradient(135deg,#8B4513,#A0522D)`, `Wax%20Seals/Crown%20Seal.png`, `[Parameter, EditorRequired] public string QuestDescription`, `[Parameter, EditorRequired] public IList<string> ConfirmedPlayerNames`, `[Parameter, EditorRequired] public int ChallengeRating`
- No `@layout` directive in any component
- No `<style>` blocks in any component
- All URL-encoded spaces confirmed (Blanks%20w%20Shadow, Wax%20Seals)
- Build exits 0, commits `ff10459` and `befce95` exist

## Self-Check: PASSED

---
*Phase: 21-html-email-templates*
*Completed: 2026-06-26*
