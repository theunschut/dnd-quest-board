# Phase 17: Character & Player Views - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for four pages: `GuildMembers/Details`, `GuildMembers/Create`, `GuildMembers/Edit`, and `Players/Index`. Also remove the email column from the **desktop** `Players/Index.cshtml` view (folded in from backlog — name-only for both mobile and desktop). Strictly additive on all other files — no controllers, ViewModels, repositories, or domain services modified.

**Views to create:**
- `GuildMembers/Details.Mobile.cshtml`
- `GuildMembers/Create.Mobile.cshtml`
- `GuildMembers/Edit.Mobile.cshtml`
- `Players/Index.Mobile.cshtml`

**Existing desktop view to modify:**
- `Players/Index.cshtml` — remove email column (Name-only rows for both DMs and Players sections)

**Note for planner:** CHAR-01, CHAR-02, CHAR-03, and PLAYER-01 are referenced in ROADMAP.md Phase 17 but not yet defined in `REQUIREMENTS.md`. The planner must add these requirements before or alongside the plans.

</domain>

<decisions>
## Implementation Decisions

### Character Detail Page (GuildMembers/Details.Mobile.cshtml)
- **D-01:** Vertical layout order: portrait → name + status badges → character information (level, class, sheet link, description, backstory) → owner actions (Edit / Retire-Reactivate / Delete).
- **D-02:** Portrait displays full-width at the top, capped at approximately 220px, centered in the glass card container. No profile picture placeholder falls back to the `fa-user` icon (same as desktop).
- **D-03:** All three owner action buttons are shown on mobile: Edit Character, Retire/Reactivate, and Delete. The desktop `confirm()` dialog on Delete is preserved — no additional guard needed.
- **D-04:** Glass card + parchment text treatment throughout (`rgba(255,255,255,0.15)` backdrop-blur, `#F4E4BC` text with text-shadow) — consistent with phases 13–16.

### Character Create & Edit Forms (GuildMembers/Create.Mobile.cshtml + Edit.Mobile.cshtml)
- **D-05:** Profile picture upload section is at the **top** of the form, full-width. Existing portrait thumbnail (Edit view) is centered above the file input.
- **D-06:** Class entries stack **vertically**: Class select (`col-12`, full-width) → Level input (`col-12`, full-width) → Remove button (`col-12`, full-width danger button). This replaces the `col-md-5/4/3` desktop row. Applies to both the static Razor-rendered entries and the JS-generated dynamic entries.
- **D-07:** The `@section Scripts` block (file size validation + add-class / remove-class JS) is copied verbatim from the desktop view, with the dynamic entry `innerHTML` template updated to use the stacked `col-12` layout instead of `col-md-5/4/3`.
- **D-08:** Glass card + parchment text treatment applied to the form card container. All form controls (`form-control`, `form-select`) inherit Bootstrap's full-width default and are 44px+ compliant from `mobile.css`.

### Players Page (Players/Index.Mobile.cshtml + Players/Index.cshtml desktop change)
- **D-09:** Mobile Players page shows **name only** — no email column. Each section (Dungeon Masters, Registered Players) is a flat list of tap-navigable name rows. DM names link to `DungeonMaster/Profile`; player names are plain text (no profile page for players).
- **D-10:** Sections stack **vertically**: Dungeon Masters section first, Registered Players section below — both in glass card containers with the parchment text treatment.
- **D-11:** The **desktop** `Players/Index.cshtml` email column is also removed in this phase. Both Name and Email table header columns become Name-only; the email `<td>` and its `mailto:` link block are removed from both the DM and Player table rows.

### CSS Architecture
- **D-12:** Three CSS files for this phase:
  - `character-detail.mobile.css` — portrait sizing, section spacing, action button layout for `Details.Mobile.cshtml`
  - `character-form.mobile.css` — shared by both `Create.Mobile.cshtml` and `Edit.Mobile.cshtml`; class-entry stacked layout, file input block, form container
  - `players.mobile.css` — section headers, name list rows for `Players/Index.Mobile.cshtml`
- **D-13:** Each `.Mobile.cshtml` loads its CSS via `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }`. Create and Edit both reference `character-form.mobile.css`.
- **D-14:** `mobile.css` baseline applies globally (44px touch targets, typography). Per-page CSS adds layout-specific rules only — no baseline redefinition.

### Claude's Discretion
- Exact pixel sizing of portrait `max-height` within the ~220px guidance.
- Whether the class information section on Details uses inline badges or a stacked list (class name + class level per badge vs. one row per class).
- Empty-state markup on Players page when no DMs or no Players are registered.
- Whether the sheet link on Details wraps in a truncated `text-overflow: ellipsis` or wraps naturally.
- Exact `border-radius` and padding values — follow the established glass card values from `quests.mobile.css`.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — CHAR-01, CHAR-02, CHAR-03, PLAYER-01 (must be added by planner; phase roadmap references these requirement IDs). Also confirm Phase 17 success criteria in ROADMAP.md.
- `.planning/ROADMAP.md` — Phase 17 goal and success criteria (3 criteria); read before implementing.

### Phase 12 Infrastructure (authoritative for mobile view hook-in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — `@section Styles` / `@section Scripts` rendering pattern, mobile CSS baseline loading. MUST read.

### Phase 13–16 Patterns (glass card CSS + established patterns)
- `.planning/phases/16-account-browse/16-CONTEXT.md` — D-02 (glass card treatment), D-06 (single-column list row), D-08 (per-page CSS pattern), D-09 (mobile.css baseline separation). Closest prior art.
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — Glass card CSS source: `.quest-section-card-mobile`, parchment `#F4E4BC` with text-shadow, `backdrop-filter: blur(15px)`. Reference when writing character CSS files.
- `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` — Phase 16 Guild Members Index CSS; reuse circular thumbnail and list row patterns if applicable to the Details portrait section.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` — Source for `CharacterViewModel` bindings, portrait URL pattern (`Url.Action("GetProfilePicture", new { id = Model.Id })`), class badge loop, backstory/description rendering, owner action forms (ToggleRetirement POST, Delete POST with confirm).
- `EuphoriaInn.Service/Views/GuildMembers/Create.cshtml` — Source for `CharacterViewModel` bindings, class-entry JS (add/remove), file size validation JS, `enctype="multipart/form-data"`, `@foreach DndClass` enum option loop.
- `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` — Identical structure to Create; source for `asp-action="Edit"` and cancel link to `Details`.
- `EuphoriaInn.Service/Views/Players/Index.cshtml` — Source for `GuildMembersIndexViewModel` bindings (`Model.DungeonMasters`, `Model.Players`), DM profile link pattern (`asp-controller="DungeonMaster" asp-action="Profile" asp-route-id="@dm.Id"`). **Also the desktop file that gets the email column removed in this phase.**

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale. Per-page CSS extends this; do not redefine baseline rules.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Url.Action("GetProfilePicture", new { id = Model.Id })` — Portrait image URL pattern (same as Guild Members Index mobile view and Details desktop view).
- Glass card CSS from `quests.mobile.css` — `.quest-section-card-mobile` rule set; copy the selector pattern, rename for character pages.
- Phase 16 list row pattern from `guild-members.mobile.css` — `d-flex align-items-center` row with circular thumbnail; can be adapted for Players page name rows.
- Desktop class-entry JS (add/remove) — copy verbatim; update `innerHTML` template to `col-12` stacked layout.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — per-page CSS injection.
- `onclick="window.location.href='@Url.Action(...)'"` — tap navigation for list rows (Phase 13 pattern).
- Glass card: `background: rgba(255, 255, 255, 0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 12px;`
- Parchment text: `color: #F4E4BC !important; text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);`
- No `@inject` for globally available services — `_ViewImports.cshtml` already handles `IAntiforgery`, `IAuthorizationService`, etc.
- `@foreach (DndClass dndClass in Enum.GetValues(typeof(DndClass)))` — class enum loop for select options; same pattern in both desktop Create and Edit; replicate in mobile views.

### Integration Points
- No new controllers, services, or ViewModels — all views share existing model types (`CharacterViewModel`, `GuildMembersIndexViewModel`).
- `_ViewStart.cshtml` handles layout selection — no individual mobile view sets `Layout`.
- `HttpContext.Items["IsMobile"]` set by middleware; `.Mobile.cshtml` files served automatically by Phase 12 expander.
- `Players/Index.cshtml` desktop change: remove two `<th>Email</th>` elements and two `<td>` email conditional blocks (one each in DM table and Player table). No ViewModel change — `dm.Email` and `player.Email` fields remain on the model, just not rendered.

</code_context>

<specifics>
## Specific Ideas

- Character detail portrait: `<img src="@Url.Action("GetProfilePicture", new { id = Model.Id })" class="character-portrait-mobile img-fluid" style="max-height: 220px; width: auto;" />` centered in the glass card at the top.
- Class badges on Details mobile: `<span class="badge bg-primary me-1 mb-1">@charClass.Class Level @charClass.ClassLevel</span>` — same badge pattern as desktop, naturally wraps on narrow screens.
- Players list row DM tap: `<div class="d-flex align-items-center p-3" onclick="window.location.href='@Url.Action("Profile", "DungeonMaster", new { id = dm.Id })'">` — consistent with Phase 13/16 tap-navigation pattern.
- Players list row player (no profile page): `<div class="d-flex align-items-center p-3"><span class="parchment-text">@player.Name</span></div>` — non-tappable row.
- Desktop Players/Index.cshtml change: remove `<th scope="col" class="w-50">Email</th>` from both table `<thead>` elements and the corresponding `<td>` email conditional block from each `<tbody>` `@foreach`.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 17 scope.

</deferred>

---

*Phase: 17-character-player-views*
*Context gathered: 2026-06-25*
