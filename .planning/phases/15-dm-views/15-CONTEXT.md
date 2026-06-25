# Phase 15: DM Views - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for three DM-facing pages: `Quest/Create`, `Quest/Manage`, and `DungeonMaster/Profile`. Strictly additive — no controllers, ViewModels, repositories, or domain services are modified. The Phase 12 view expander auto-serves `.Mobile.cshtml` files on mobile requests.

</domain>

<decisions>
## Implementation Decisions

### Quest Create Form (DMVIEW-01)
- **D-01:** Omit the "Quest Creation Tips" sidebar entirely on mobile — single-column form only. Tips sidebar (`col-lg-4`) is non-essential; DMs creating quests know what to fill in. Keeps the mobile form clean.
- **D-02:** Keep the `input-group` layout for proposed date rows (datetime-local input + Remove button side-by-side). Native touch pickers already fire on iOS/Android for `datetime-local`; the Bootstrap `input-group` layout holds fine on 375px+ screens.
- **D-03:** Form container wrapped in a glass card (frosted glass surface, parchment text), consistent with Phase 13/14 mobile aesthetic.

### Quest Manage (DMVIEW-02)
- **D-04:** Vote results per proposed date: **condensed counts only** — e.g. "3 Yes · 1 Maybe · 0 No" shown as small badges on the date heading. The full per-date player name lists (3-column Yes/Maybe/No grid from desktop) are omitted — too wide for mobile and the DM only needs the signal to pick a date.
- **D-05:** Reuse the desktop inline JS as-is. `Manage.Mobile.cshtml` uses the same form structure (same `data-*` attributes, same input names) so the existing player-count tracker, yes-voter highlights, and date-change suggestions all work without modification.
- **D-06:** Form sections (proposed dates, player selection, finalize button) wrapped in glass card containers with parchment text, consistent with Phase 13/14 pattern.

### DM Profile (DMVIEW-03)
- **D-07:** Quest history rendered as a **card list per quest** — title (bold, tappable to Quest Details), date + CR badge on the row below it. Replaces `table.table-striped` which would overflow on mobile.
- **D-08:** Full glass card + parchment text treatment throughout: profile header (photo + name + edit button), bio card, and quest history section. Consistent with Phase 13/14 mobile aesthetic.

### CSS Architecture
- **D-09:** Per-page CSS files: `dm-create.mobile.css`, `dm-manage.mobile.css`, `dm-profile.mobile.css`. Each `.Mobile.cshtml` view loads its own file via `@section Styles`. Consistent with Phase 13 pattern (`home.mobile.css`, `quests.mobile.css`).
- **D-10:** `mobile.css` baseline (44px touch targets) applies to all form controls. Per-page CSS adds glass card containers and layout rules only.

### Claude's Discretion
- Whether to reuse the `.quest-section-card-mobile` CSS class from `quests.mobile.css` or define new equivalent classes in the dm-specific CSS files. Either is fine — keep class names predictable.
- Exact vote-count badge styling in `dm-manage.mobile.css` (color, size, layout within the date heading row).
- Radio button and checkbox touch target sizing in Quest Manage (min-height per INFRA-06, already enforced by `mobile.css` baseline).
- Whether the DM Profile photo and bio appear in one glass card or two separate cards on mobile.
- Empty-state markup when the DM has no quest history.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — DMVIEW-01, DMVIEW-02, DMVIEW-03 are the three requirements for this phase; all must be satisfied.
- `.planning/ROADMAP.md` — Phase 15 goal and success criteria (3 criteria); read before implementing to confirm scope.

### Phase 12 Infrastructure (authoritative for how mobile views hook in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — `@section Styles` / `@section Scripts` rendering pattern, mobile CSS baseline loading. MUST read.

### Phase 13 Patterns (per-page CSS + glass card CSS source)
- `.planning/phases/13-core-player-views/13-CONTEXT.md` — D-01 (per-page CSS file pattern), D-02 (mobile.css is baseline only). Also the source for `.quest-section-card-mobile` glass card class.
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — Glass card CSS: `.quest-section-card-mobile`, `.quest-header-card-mobile`, `.participant-list-mobile` — parchment `#F4E4BC` with text-shadow, `backdrop-filter: blur(15px)`. Reuse or reference when writing dm-specific CSS.

### Phase 14 Patterns (glass card in post-completion fix)
- `.planning/phases/14-calendar/14-CONTEXT.md` — D-14/D-15 (per-page calendar CSS). The `ab72a50` fix added `.agenda-card-mobile` with the same glass pattern; DM Views follow the same post-completion-quality standard.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Quest/Create.cshtml` — Desktop Quest Create form; source for model bindings, form field names, `_QuestFormScripts` partial usage, and proposed-dates section structure.
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — Desktop Quest Manage; source for the finalize form structure, `data-*` attributes on `.manage-date-option`, player checkbox names, and the full inline `<script>` block (player count, yes-voter highlights, date-change handler). MUST read before implementing mobile view.
- `EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml` — Desktop DM Profile; source for model bindings (`DMProfileViewModel`), profile photo URL pattern, bio section, and quest history loop.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale, spacing overrides. Per-page CSS files should follow the same patterns.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_QuestFormScripts.cshtml` — Partial providing add/remove proposed date JS. `Create.Mobile.cshtml` uses `@section Scripts { @{ await Html.RenderPartialAsync("_QuestFormScripts"); } }` — same as desktop.
- `Manage.cshtml` inline `<script>` — Player selection count, yes-voter highlights, date-change handler. Copy into `Manage.Mobile.cshtml` (same form structure, same `data-*` attributes, same input names).
- `.quest-section-card-mobile` in `quests.mobile.css` — Glass card CSS pattern. Extend or reuse for DM view cards.
- `@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery` — Required at top of `Manage.Mobile.cshtml` (same as desktop) for the finalize form's anti-forgery token.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — per-page CSS injection.
- `onclick="window.location.href='@Url.Action(...)'"`  — tap navigation (Phase 13 pattern; used for quest history card taps on Profile).
- Glass card: `background: rgba(255, 255, 255, 0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 12px;` — core glass card CSS.
- Parchment text: `color: #F4E4BC !important; text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);` — standard parchment text treatment.

### Integration Points
- No new controllers, services, or ViewModels for this phase — all three views share the existing model types (`QuestViewModel`, `Quest`, `DMProfileViewModel`).
- `_ViewStart.cshtml` handles layout selection — no individual mobile view should set `Layout`.
- `HttpContext.Items["IsMobile"]` set by middleware; `.Mobile.cshtml` files are served automatically by the expander.
- `ViewBag.IsAuthorized` and `ViewBag.IsAdmin` in `Manage.cshtml` — mobile view must replicate the same authorization guard at the top.

</code_context>

<specifics>
## Specific Ideas

- Vote count format on Manage: `3 Yes · 1 Maybe · 0 No` using small `<span class="badge">` elements in success/warning/danger colors, inline on the date label row.
- Quest history tap target on Profile: each quest row as a `<div onclick="window.location.href='@Url.Action("Details", "Quest", new { id = quest.Id })'">` or `<a>` block — same tap pattern as Phase 13 quest cards.
- DM Profile photo: `rounded-circle` class retained on mobile (same as desktop) but sized appropriately for mobile viewport width.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 15 scope.

</deferred>

---

*Phase: 15-dm-views*
*Context gathered: 2026-06-24*
