# Phase 16: Account & Browse - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for seven Account and Browse pages: `Account/Login`, `Account/Register`, `Account/Edit`, `Account/Profile`, `Account/ChangePassword`, `Shop/Index`, and `GuildMembers/Index`. Strictly additive — no controllers, ViewModels, repositories, or domain services are modified. The Phase 12 view expander auto-serves `.Mobile.cshtml` files on mobile requests.

</domain>

<decisions>
## Implementation Decisions

### Account Pages — Scope
- **D-01:** Seven mobile views in scope: `Login.Mobile.cshtml`, `Register.Mobile.cshtml`, `Edit.Mobile.cshtml`, `Profile.Mobile.cshtml`, `ChangePassword.Mobile.cshtml`, `Shop/Index.Mobile.cshtml`, `GuildMembers/Index.Mobile.cshtml`.
- **D-02:** All Account pages (`Login`, `Register`, `Edit`, `Profile`, `ChangePassword`) use the glass card + parchment text treatment — consistent with Phases 13–15 aesthetic (`rgba(255,255,255,0.15)` backdrop-filter glass surface, `#F4E4BC` parchment text with text-shadow).

### Shop Index — Layout & Filters
- **D-03:** Filter and sort controls (item type tabs, rarity checkboxes, sort dropdown, search input) collapse behind a **Filter & Sort button** on mobile. A bottom drawer or accordion reveals controls when tapped. Items are immediately visible without scrolling past controls.
- **D-04:** The Purchase History side panel is **omitted** on mobile. Shop mobile view focuses on browsing and buying only.
- **D-05:** Items display in a **2-column grid** on mobile (icon/image + name + price + Buy button). Tapping an item opens the **same Bootstrap `#itemDetailsModal`** already in the desktop view (reuse JS event listener verbatim — no separate mobile implementation).

### Guild Members — Layout
- **D-06:** Within each section (My Characters / Other Characters), characters display as **single-column list rows** — small circular profile thumbnail on the left, name + class/role on the right. One character per row.
- **D-07:** Section headers and list rows use the **glass card + parchment text** treatment, consistent with all other Phase 13–16 mobile views.

### CSS Architecture
- **D-08:** Per-page CSS files following the Phase 15 pattern: `account.mobile.css` (shared for all five Account views), `shop.mobile.css`, `guild-members.mobile.css`. Each `.Mobile.cshtml` view loads its file via `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }`.
- **D-09:** The `mobile.css` baseline (44px touch targets, typography scale) applies globally. Per-page CSS adds glass card containers, filter drawer, grid layout, and list row rules only.

### Claude's Discretion
- Exact drawer implementation for Shop filters (Bootstrap offcanvas vs. collapse vs. custom CSS — choose simplest that doesn't require new JS).
- Empty-state markup when My Characters list is empty.
- Whether `ChangePassword` needs only a CSS-only adjustment (it's a simple 3-field form) or a full mobile view.
- Circular thumbnail sizing in Guild Members list rows.
- Whether the "Create New Character" button appears at the top (above My Characters) or bottom of the mobile view.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — ACCT-01, ACCT-02, ACCT-03, BROWSE-01, BROWSE-02 are the five requirements for this phase; all must be satisfied.
- `.planning/ROADMAP.md` — Phase 16 goal and success criteria (4 criteria); read before implementing to confirm scope.

### Phase 12 Infrastructure (authoritative for how mobile views hook in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — `@section Styles` / `@section Scripts` rendering pattern, mobile CSS baseline loading. MUST read.

### Phase 13 Patterns (glass card CSS source + tap navigation)
- `.planning/phases/13-core-player-views/13-CONTEXT.md` — D-01 (per-page CSS pattern), D-02 (mobile.css baseline only), tap navigation via `onclick="window.location.href='@Url.Action(...)'"`
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — Glass card CSS: `.quest-section-card-mobile`, parchment `#F4E4BC` with text-shadow, `backdrop-filter: blur(15px)`. Reference when writing account/shop/guild-members CSS.

### Phase 15 Patterns (per-page CSS structure)
- `.planning/phases/15-dm-views/15-CONTEXT.md` — D-09 (per-page CSS file naming), D-10 (mobile.css baseline separation). Follow same pattern.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Account/Login.cshtml` — Source for `LoginViewModel` bindings, form action, return URL routing, validation summary.
- `EuphoriaInn.Service/Views/Account/Register.cshtml` — Source for `RegisterViewModel` bindings, form structure, placeholder text.
- `EuphoriaInn.Service/Views/Account/Edit.cshtml` — Source for `EditProfileViewModel` bindings, HasKey checkbox, Change Password link routing.
- `EuphoriaInn.Service/Views/Account/Profile.cshtml` — Source for `ProfileViewModel` bindings, `ViewData["IsAdmin"]` / `ViewData["IsDungeonMaster"]` flags, account type display, Edit/ChangePassword links.
- `EuphoriaInn.Service/Views/Account/ChangePassword.cshtml` — Source for form fields and validation.
- `EuphoriaInn.Service/Views/Shop/Index.cshtml` — Source for item grid structure, `data-bs-toggle="modal" data-bs-target="#itemDetailsModal"` item tap pattern, modal JS event listener (lines ~491–520), filter/sort form structure, pagination helper functions. MUST read before implementing — the modal JS must be included in the mobile view.
- `EuphoriaInn.Service/Views/GuildMembers/Index.cshtml` — Source for `CharactersIndexViewModel` bindings, `Model.MyCharacters` / `Model.OtherCharacters` loops, character profile picture URL (`Url.Action("GetProfilePicture", new { id = character.Id })`), CharacterStatus.Retired and CharacterRole.Main badge logic.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale, spacing overrides. Per-page CSS files extend this; do not redefine baseline rules.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `#itemDetailsModal` Bootstrap modal in `Shop/Index.cshtml` — the same modal and JS event listener (populate modal body from `data-*` attributes on item cards) is reused verbatim in `Shop/Index.Mobile.cshtml`. No separate mobile modal implementation needed.
- `.quest-section-card-mobile` in `quests.mobile.css` — Glass card CSS pattern. Reference directly when writing `account.mobile.css`, `shop.mobile.css`, `guild-members.mobile.css`.
- `Url.Action("GetProfilePicture", new { id = character.Id })` — Profile picture URL pattern for circular thumbnails in Guild Members list rows.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — per-page CSS injection (all `.Mobile.cshtml` views).
- `onclick="window.location.href='@Url.Action(...)'"`  — tap navigation for list rows (Phase 13 pattern).
- Glass card: `background: rgba(255, 255, 255, 0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 12px;` — core glass card CSS.
- Parchment text: `color: #F4E4BC !important; text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);` — standard parchment text treatment.
- No `@inject` for globally available services — `_ViewImports.cshtml` already injects `IAntiforgery`, `IAuthorizationService`, etc.
- No `@{} ` wrapper for C# variable declarations inside `@foreach` bodies — use direct C# code mode assignment (Phase 13 pattern).

### Integration Points
- No new controllers, services, or ViewModels for this phase — all views share existing model types (`LoginViewModel`, `RegisterViewModel`, `EditProfileViewModel`, `ProfileViewModel`, `ShopIndexViewModel`, `CharactersIndexViewModel`).
- `_ViewStart.cshtml` handles layout selection — no individual mobile view sets `Layout`.
- `HttpContext.Items["IsMobile"]` set by middleware; `.Mobile.cshtml` files served automatically by the Phase 12 expander.
- Shop filter/sort state (`SelectedType`, `SelectedRarities`, `SelectedSort`, `SearchQuery`) must be preserved in filter form on mobile — reuse the same hidden field pattern from the desktop view's `BuildTabUrl` / `BuildPageUrl` functions.

</code_context>

<specifics>
## Specific Ideas

- Shop item tap: each 2-column grid card includes `data-bs-toggle="modal" data-bs-target="#itemDetailsModal"` plus the same `data-*` attributes from desktop (item name, description, price, rarity) so the existing JS event listener populates the modal without modification.
- Guild Members list row: `<div class="d-flex align-items-center p-3" onclick="window.location.href='@Url.Action("Details", new { id = character.Id })'">` — small `img` circle on left (40×40px, `border-radius: 50%`), then name + class + role badge.
- Account pages: since Login and Register use `col-md-6 col-lg-4` / `col-md-6 col-lg-5` on desktop, the mobile views should use `col-12` or no column wrapper to ensure true full-width layout on phones.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 16 scope.

</deferred>

---

*Phase: 16-account-browse*
*Context gathered: 2026-06-24*
