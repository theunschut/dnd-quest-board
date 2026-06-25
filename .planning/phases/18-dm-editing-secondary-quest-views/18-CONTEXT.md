# Phase 18: DM Editing & Secondary Quest Views - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for four pages:
- `Quest/Edit.cshtml` → `Quest/Edit.Mobile.cshtml`
- `Quest/CreateFollowUp.cshtml` → `Quest/CreateFollowUp.Mobile.cshtml`
- `DungeonMaster/EditProfile.cshtml` → `DungeonMaster/EditProfile.Mobile.cshtml`
- `QuestLog/Details.cshtml` → `QuestLog/Details.Mobile.cshtml`

Strictly additive — no controllers, ViewModels, repositories, or domain services are modified.

**Note for planner:** DMVIEW-04, DMVIEW-05, DMVIEW-06, and QLOG-01 are referenced in ROADMAP.md Phase 18 but not yet defined in `REQUIREMENTS.md`. The planner must add these requirements before or alongside the plans.

</domain>

<decisions>
## Implementation Decisions

### Quest Edit (Quest/Edit.Mobile.cshtml)
- **D-01:** Omit the "Quest Editing Tips" sidebar — follow Phase 15 D-01 pattern (no tips/decorative sidebars on mobile). Single-column form only.
- **D-02:** Keep the `alert-warning` ("Players have already signed up for this quest...") when `Model.HasExistingSignups` is true — it is functional context, not decorative, and directly affects DM behaviour.
- **D-03:** Existing dates rendered as readonly text + hidden field + Remove button, exactly as desktop. New dates added via `addProposedDate()` from `site.js`. This is the same readonly+hidden pattern as desktop; no change to model binding.
- **D-04:** Use `_QuestFormScripts` partial in `@section Scripts` (same as `Create.Mobile.cshtml`).
- **D-05:** Glass card container with parchment text, consistent with phases 13–17.
- **D-06:** Per-page CSS: `quest-edit.mobile.css`.

### CreateFollowUp (Quest/CreateFollowUp.Mobile.cshtml)
- **D-07:** Pre-Approved Players panel is **kept** on mobile as a compact glass card section **below the form** — shows the player names that carry over (or empty-state text if none). Unlike the tips sidebar, this is functional context the DM wants to confirm before saving.
- **D-08:** Dates use `datetime-local` inputs with the full inline JS block (`addDate`, `removeDate`, `renumberDates`) copied verbatim from the desktop view — identical to how Phase 15 D-05 copied the Manage inline script.
- **D-09:** The "This form is pre-filled from the original quest..." info alert is kept on mobile — brief and contextually useful for DMs.
- **D-10:** Glass card + parchment text, consistent with phases 13–17.
- **D-11:** Per-page CSS: `quest-followup.mobile.css`.

### DM EditProfile (DungeonMaster/EditProfile.Mobile.cshtml)
- **D-12:** Photo upload section at the **top** (full-width), bio textarea below — follows Phase 17 D-05 (photo/upload always at top of mobile forms).
- **D-13:** File validation JS (`DM_MAX_FILE_SIZE`, `DM_ALLOWED_TYPES`, `dmProfilePictureInput` change handler) copied verbatim from the desktop `EditProfile.cshtml` into `@section Scripts`.
- **D-14:** Glass card + parchment text, consistent with phases 13–17.
- **D-15:** Per-page CSS: `dm-editprofile.mobile.css`.

### QuestLog Details (QuestLog/Details.Mobile.cshtml)
- **D-16:** Layout order: main content card (quest info, description, adventurers, recap section) → Quick Actions glass card → Quest Statistics glass card. Both sidebar panels are stacked below the main content as separate glass cards — same pattern as phases 15–17.
- **D-17:** Both the Quick Actions and Quest Statistics panels are kept on mobile. The Building Access badge and participants count are meaningful to this group (physical venue key access) and are short enough to fit in a glass card without scrolling.
- **D-18:** `ViewBag.CanEditRecap` conditional preserved on mobile — DMs see the recap textarea form; all other users see the read-only display (or "No recap yet" message).
- **D-19:** Per-page CSS: `quest-log-detail.mobile.css`.

### CSS Architecture
- **D-20:** Four new CSS files, one per view: `quest-edit.mobile.css`, `quest-followup.mobile.css`, `dm-editprofile.mobile.css`, `quest-log-detail.mobile.css`. Matches Phase 15 approach (one file per view). Views differ enough in structure (readonly dates vs datetime-local, form vs recap detail) to justify separate files.

### Claude's Discretion
- Exact CSS class names in new files (can reuse `.quest-section-card-mobile` selector or define view-specific equivalents).
- `rows` attribute on the recap textarea — may reduce from desktop's `rows="10"` for better mobile UX; textarea remains scrollable.
- Building Access badge placement within the Quest Statistics card.
- Empty-state markup when `ViewBag.PreApprovedPlayers` is null or empty on CreateFollowUp.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — DMVIEW-04, DMVIEW-05, DMVIEW-06, QLOG-01 must be defined by the planner. Also confirm Phase 18 success criteria in ROADMAP.md.
- `.planning/ROADMAP.md` — Phase 18 goal and success criteria (4 criteria); read before implementing.

### Phase 12 Infrastructure (authoritative for mobile view hook-in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — `@section Styles` / `@section Scripts` rendering pattern, mobile CSS baseline loading. MUST read.

### Phase 15 DM View Patterns (closest prior art for DM form views)
- `.planning/phases/15-dm-views/15-CONTEXT.md` — D-01 (omit tips sidebar), D-02 (input-group works on mobile), D-05 (copy Manage inline script), D-09 (per-page CSS). Most directly applicable to Quest Edit and CreateFollowUp.
- `EuphoriaInn.Service/wwwroot/css/dm-create.mobile.css` — Glass card CSS for form views. Reference class names and glass card pattern.

### Phase 17 Character View Patterns (photo-at-top pattern for EditProfile)
- `.planning/phases/17-character-player-views/17-CONTEXT.md` — D-05 (photo/upload at top of form), D-06 (stacked col-12 layout for form sections), D-07 (copy JS template with col-12 updates).

### Closest Analog for Quest Edit / CreateFollowUp
- `EuphoriaInn.Service/Views/Quest/Create.Mobile.cshtml` — Model binding patterns, `_QuestFormScripts` usage, glass card structure, `addProposedDate` / `removeProposedDate` calls.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Quest/Edit.cshtml` — `EditQuestViewModel` bindings, readonly+hidden date pattern, `HasExistingSignups` warning, `_QuestFormScripts` partial.
- `EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml` — `FollowUpQuestViewModel` bindings, `datetime-local` inputs, full inline `addDate`/`removeDate`/`renumberDates` script, `ViewBag.PreApprovedPlayers` sidebar.
- `EuphoriaInn.Service/Views/DungeonMaster/EditProfile.cshtml` — `EditDMProfileViewModel` bindings, `enctype="multipart/form-data"`, photo upload with inline file validation JS, `GetDMProfilePicture` URL pattern.
- `EuphoriaInn.Service/Views/QuestLog/Details.cshtml` — `QuestLogDetailsViewModel` bindings, adventurers loop with `character-mini-avatar`, recap textarea conditional on `ViewBag.CanEditRecap`, Quest Statistics panel.

### Existing QuestLog Mobile CSS
- `EuphoriaInn.Service/wwwroot/css/quest-log.mobile.css` — Phase 13 QuestLog Index CSS. Source for `.quest-log-item` glass card pattern. `quest-log-detail.mobile.css` should follow the same glass card values.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale. Per-page CSS extends this; do not redefine baseline rules.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_QuestFormScripts.cshtml` — Form validation partial (checks for at least one datetime-local input, disables submit on submit). Used by `Create.Mobile.cshtml`; use the same way in `Edit.Mobile.cshtml`.
- `addProposedDate()` / `removeProposedDate(this)` — Functions defined in `site.js`; called directly in HTML. Quest Edit mobile uses these for the "Add Another Date" button and remove buttons on new dates.
- `CreateFollowUp.cshtml` inline script — `addDate`, `removeDate`, `renumberDates` with smart date pre-fill logic; copy verbatim into `CreateFollowUp.Mobile.cshtml @section Scripts`.
- `EditProfile.cshtml` inline script — File size/type validation; copy verbatim into `EditProfile.Mobile.cshtml @section Scripts`.
- Glass card CSS from `dm-create.mobile.css` — Reference `.dm-create-card-mobile` for the form card container pattern; create equivalent class in `quest-edit.mobile.css` and `quest-followup.mobile.css`.
- `Url.Action("GetDMProfilePicture", new { id = Model.DungeonMasterId })` — Profile photo URL for EditProfile (when `Model.ProfilePicture?.Length > 0`).

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — per-page CSS injection.
- `onclick="window.location.href='@Url.Action(...)'"`  — tap navigation for quest history and similar rows.
- Glass card: `background: rgba(255, 255, 255, 0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 12px;`
- Parchment text: `color: #F4E4BC !important; text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);`
- No `@inject` for globally available services — `_ViewImports.cshtml` already injects `IAntiforgery`, `IAuthorizationService`, etc.

### Integration Points
- No new controllers, services, or ViewModels — all four views use existing model types (`EditQuestViewModel`, `FollowUpQuestViewModel`, `EditDMProfileViewModel`, `QuestLogDetailsViewModel`).
- `_ViewStart.cshtml` handles layout selection — no individual mobile view sets `Layout`.
- `HttpContext.Items["IsMobile"]` set by middleware; `.Mobile.cshtml` files served automatically by the Phase 12 expander.
- `ViewBag.CanEditRecap` (QuestLog Details) — set by controller; mobile view uses same conditional as desktop.
- `ViewBag.PreApprovedPlayers` (CreateFollowUp) — set by controller; mobile view uses same `@foreach` loop as desktop sidebar.

</code_context>

<specifics>
## Specific Ideas

- Quest Edit existing date row: `<div class="mb-3 proposed-date-item"><label class="form-label small">Proposed Date @(i + 1)</label><div class="input-group"><input type="text" class="form-control" readonly value="@..." /><input type="hidden" name="Quest.ProposedDates[@i]" value="@..." /><button type="button" class="btn btn-danger" onclick="removeProposedDate(this)"><i class="fas fa-trash me-1"></i>Remove</button></div></div>` — identical to desktop, works on mobile.
- CreateFollowUp Pre-Approved Players glass card: rendered below `</form>`, using the `ViewBag.PreApprovedPlayers` dynamic list. If null or empty: show the "No players were selected..." message. If populated: `@foreach (var player in ...)` list with `<i class="fas fa-check-circle text-success me-2"></i>@player.Name`.
- QuestLog Details adventurers list: the `character-mini-avatar` inline image + `onerror` fallback placeholder works as-is on mobile; no special handling needed.
- QuestLog Details building access badge: reuse the same `bg-success`/`bg-danger` badge from desktop; renders cleanly in the glass card on narrow screens.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 18 scope.

</deferred>

---

*Phase: 18-dm-editing-secondary-quest-views*
*Context gathered: 2026-06-25*
