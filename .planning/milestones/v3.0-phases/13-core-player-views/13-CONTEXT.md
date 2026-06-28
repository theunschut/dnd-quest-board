# Phase 13: Core Player Views - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for three player-facing views: Quest Board (`Home/Index.Mobile.cshtml`), Quest Details (`Quest/Details.Mobile.cshtml`), and Quest Log (`QuestLog/Index.Mobile.cshtml`). Strictly additive — no controllers, ViewModels, repositories, or domain services are modified. The Phase 12 view expander auto-serves `.Mobile.cshtml` files on mobile requests.

</domain>

<decisions>
## Implementation Decisions

### CSS Architecture
- **D-01:** Per-page mobile CSS files, consistent with the desktop pattern (desktop uses `quests.css`, `site.css`, etc.). Mobile equivalents: `home.mobile.css`, `quests.mobile.css`, `quest-log.mobile.css`. Each `.Mobile.cshtml` view loads its own CSS via `@section Styles { <link href="~/css/{page}.mobile.css" asp-append-version="true" rel="stylesheet" /> }`. `_Layout.Mobile.cshtml` already declares `@await RenderSectionAsync("Styles", required: false)`.
- **D-02:** `mobile.css` remains the baseline only (44px touch targets, typography, spacing, D&D theme). Page-specific rules go in the per-page CSS files, not `mobile.css`.

### Quest Card Content (Quest Board — HOME-01 to HOME-04)
- **D-03:** Mobile quest cards show required fields only: title, CR, DM name, and status badge. No description on the card. Users tap to read the description on the Details page.
- **D-04:** Each card is tap-navigable: tapping a DM's own quest goes to Quest Manage; tapping any other quest goes to Quest Details. Same logic as the desktop `onclick` — use `ViewBag.CurrentUserName` vs `quest.DungeonMaster?.Name` check.
- **D-05:** Status is displayed as a colored badge: Open = green, Finalized = blue/gold, past/done = gray. Colors at Claude's discretion within that palette.
- **D-06:** If the signed-in player is already signed up for a quest, show a green pill badge ("✓ Signed up") at the top-right of the card. No wax seal imagery.

### Quest Details (QVIEW-01 and QVIEW-02)
- **D-07:** Voting buttons (Yes / No / Maybe) use the same AJAX interaction pattern as the desktop view. The mobile view reuses the existing JS voting logic — no page refresh on vote.
- **D-08:** The participant list renders as a stacked single-column list (player name + character name + role per row) instead of the horizontal table used on desktop.

### Quest Log (QVIEW-03)
- **D-09:** Quest Log mobile view shows a vertical scrollable list. Each entry: title, date (finalized date), DM name. No description on the list view — the desktop truncated-description pattern is NOT carried into the mobile list. Entries are tap-navigable to the Quest Log Details page.

### Claude's Discretion
- Exact Bootstrap utility classes and color tokens for status badges (green/blue-gold/gray) — follow Bootstrap 5 contextual colors (`success`, `primary`, `secondary`).
- Exact CSS values in per-page CSS files — use `mobile.css` patterns as a guide (44px min-height, 16px font-size).
- Whether the Quest Log mobile view shows a CR badge alongside the title — not required by QVIEW-03 but natural given the card pattern.
- Empty-state markup when no quests exist on the Quest Board or Quest Log.
- Whether the Quest Details mobile view shows the quest description above the voting section or below — place it above (most important context before acting).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — HOME-01 through HOME-04, QVIEW-01 through QVIEW-03 are the seven requirements for this phase; all must be satisfied.
- `.planning/ROADMAP.md` — Phase 13 success criteria (5 criteria); read before implementing to confirm scope.

### Phase 12 Infrastructure (authoritative for how mobile views hook in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — D-03 (only `mobile.css` loaded), D-04 (mobile CSS baseline), `@section Styles` and `@section Scripts` rendering pattern. MUST read.
- `.planning/phases/12-mobile-infrastructure/12-PATTERNS.md` — Implementation patterns from Phase 12; read for view registration and CSS loading patterns.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Home/Index.cshtml` — Quest Board desktop view; source for model bindings, ViewBag fields (`CurrentUserId`, `CurrentUserName`), and quest card data structure.
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` — Quest Details desktop view; source for voting JS functions (`changeVoteToYes`, etc.), Antiforgery token injection, and participant list data structure.
- `EuphoriaInn.Service/Views/QuestLog/Index.cshtml` — Quest Log desktop view; source for model bindings and quest log data structure.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Mobile layout shell; confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — Touch targets (44px min-height), typography scale, spacing overrides, D&D theme baseline. New per-page CSS files should follow the same selector and value patterns.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Desktop `Home/Index.cshtml` — provides the complete Razor data-access pattern: `ViewBag.CurrentUserId` (int?), `ViewBag.CurrentUserName` (string), `quest.PlayerSignups.Any(ps => ps.Player.Id == currentUserId.Value)` for signup check, `Url.Action("Manage"/"Details", "Quest", new { id = quest.Id })` for navigation.
- Desktop `Quest/Details.cshtml` — provides AJAX voting JS (`changeVoteToYes`, `changeVoteToNo`, `changeVoteToMaybe`) and the Antiforgery token injection pattern (`@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery`). Mobile view should reference the same JS or extract it to a shared file.
- Desktop `QuestLog/Index.cshtml` — `Url.Action("Details", "QuestLog", new { id = quest.Id })` for tap navigation.
- `mobile.css` — `min-height: 44px` pattern; apply to vote buttons via per-page CSS.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — how individual views push additional CSS into the layout's `<head>`. Desktop views like `Details.cshtml` already use this pattern.
- `onclick="window.location.href='@Url.Action(...)'"`  — desktop tap/click navigation pattern; reuse in mobile card list.
- Bootstrap 5 badge: `<span class="badge bg-success">✓ Signed up</span>` — ready-made pill badge component.
- `table-responsive` wrapper — NOT needed on mobile; replaced by stacked single-column list per QVIEW-02.

### Integration Points
- `HttpContext.Items["IsMobile"]` — set by `MobileDetectionMiddleware`; `.Mobile.cshtml` files are served automatically by the expander, so views themselves don't need to check this flag.
- `_ViewStart.cshtml` already handles layout selection — no individual mobile view should set `Layout`.
- No new controllers, services, or ViewModels — all three views share the existing model types (`IEnumerable<Quest>`, `PlayerSignup`, `QuestLogIndexViewModel`).

</code_context>

<specifics>
## Specific Ideas

- Status badge color palette: `bg-success` (Open), `bg-primary` (Finalized — date confirmed), `bg-secondary` (past/archived). Claude picks final mapping based on Bootstrap 5 contextual colors.
- "✓ Signed up" badge: `badge bg-success` positioned at top-right of the card using Bootstrap flex utilities or absolute positioning within the card.
- Vote buttons: three full-width (or near-full-width) stacked buttons — Yes / No / Maybe — each `min-height: 44px` per INFRA-06 already in `mobile.css`. Mobile-specific layout rule goes in `quests.mobile.css`.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 13 scope.

</deferred>

---

*Phase: 13-core-player-views*
*Context gathered: 2026-06-24*
