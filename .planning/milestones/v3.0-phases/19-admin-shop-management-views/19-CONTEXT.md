# Phase 19: Admin & Shop Management Views - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `.Mobile.cshtml` view variants for 8 views across three controllers:

**Admin views:**
- `Admin/Users.cshtml` → `Admin/Users.Mobile.cshtml`
- `Admin/EditUser.cshtml` → `Admin/EditUser.Mobile.cshtml`
- `Admin/Quests.cshtml` → `Admin/Quests.Mobile.cshtml`
- `Admin/ResetPassword.cshtml` → `Admin/ResetPassword.Mobile.cshtml`

**ShopManagement views:**
- `ShopManagement/Index.cshtml` → `ShopManagement/Index.Mobile.cshtml`
- `ShopManagement/Create.cshtml` → `ShopManagement/Create.Mobile.cshtml`
- `ShopManagement/Edit.cshtml` → `ShopManagement/Edit.Mobile.cshtml`

**Shop view:**
- `Shop/Details.cshtml` → `Shop/Details.Mobile.cshtml`

Strictly additive — no controllers, ViewModels, repositories, or domain services are modified.

**Note for planner:** ADMIN-01, ADMIN-02, and SHOPMGMT-01 are referenced in ROADMAP.md Phase 19 but must be confirmed/added in `REQUIREMENTS.md`. The planner must verify or add these requirements before or alongside the plans.

</domain>

<decisions>
## Implementation Decisions

### Admin Users (Admin/Users.Mobile.cshtml)
- **D-01:** Card per user — glass card with name + role badge (Administrator/DM/Player), then all applicable action buttons stacked vertically below. Show Promote/Demote buttons only when applicable (same conditional logic as desktop). Edit and Delete always visible. Follows the established card pattern from Phases 13–18.
- **D-02:** The inline `deleteUser()` JS function (fetch DELETE + location.reload) is copied from the desktop view into `@section Scripts`. Antiforgery token pattern is preserved.

### Admin Quests (Admin/Quests.Mobile.cshtml)
- **D-03:** Card per quest — Title, DM name, Status badge (Open/Finalized/Done), then Edit and Delete buttons. Description column is omitted on mobile (too verbose; not needed for admin management actions).
- **D-04:** Same status badge logic (bg-dark/bg-primary/bg-success) from the desktop view is preserved. The inline `deleteQuest()` JS is copied into `@section Scripts`.

### Admin EditUser (Admin/EditUser.Mobile.cshtml)
- **D-05:** Single-column form following Phase 15/16 pattern. Glass card container. Fields: Name, Email, HasKey checkbox, Reset Password link, Save Changes button. No structural changes — already a simple form.

### Admin ResetPassword (Admin/ResetPassword.Mobile.cshtml)
- **D-06:** Single-column form. Glass card container. The `alert-warning` is kept — it is functional context (warns admin about the impact of the action). Follows Phase 18 D-02 pattern (keep functional alerts).

### ShopManagement Index (ShopManagement/Index.Mobile.cshtml)
- **D-07:** Single flat list of ALL items — no section separation (no "Pending Review" / "My Items" / "All Other Items" headers). All items from `Model.ItemsForReview`, `Model.MyItems`, and `Model.AllOtherItems` are rendered in one combined list ordered by status (Pending Review first, then alphabetical by name, or by Id descending).
- **D-08:** Each item card shows: Name + Rarity badge + Price (in gp) + Status badge + action buttons (icon-only: View, Edit, Archive/Reopen, Deny, Delete). Conditional logic for which actions appear is preserved from the desktop (authorization checks, status-based visibility).
- **D-09:** "Deny Item" modal is kept as Bootstrap modal — works on mobile; no controller changes are allowed. "Bulk Actions" modal stub is also kept (shows placeholder message). Both modals copy from the desktop view.
- **D-10:** "Add New Item" button is kept at the top of the mobile view as a full-width primary button. "View Shop" link also kept.

### ShopManagement Create (ShopManagement/Create.Mobile.cshtml)
- **D-11:** Single-column form. All fields from the desktop (Name, Type, Description, Rarity, Quantity, Price, ReferenceUrl, AvailableFrom/Until availability window) are included.
- **D-12:** The price field is **full-width with no input-group buttons**. The dice (random price) and calculator (Tasha's recommended price) buttons are **omitted on mobile** — DMs type the price manually. The rarity hint text (`price-suggestion` div) is still shown as helper text below the price field.
- **D-13:** The `toggleAvailabilityWindow()` / `updatePriceSuggestion()` JS functions are kept (minus the button-enable/disable calls), and the rarity-based CSS animations are kept via `@section Scripts`.
- **D-14:** Per-page CSS: `shop-management-create.mobile.css`.

### ShopManagement Edit (ShopManagement/Edit.Mobile.cshtml)
- **D-15:** Same structure and decisions as Create mobile view. Status info alert is kept (functional context — DMs need to know if item is awaiting approval). Pricing tool buttons are also omitted on mobile.
- **D-16:** Per-page CSS: `shop-management-edit.mobile.css`.

### Shop Details (Shop/Details.Mobile.cshtml)
- **D-17:** Content is **inlined** (not delegated to `_ShopItemDetailsContent` partial) and wrapped in the D&D glass card + parchment text treatment. Consistent with all prior mobile phases.
- **D-18:** The `ViewBag.IsModal` conditional in the desktop view is handled on mobile by using the non-modal path only (no modal variant for mobile). Standard single-column layout: item name + type badge in glass card header, then rarity + description + price + purchase button in card body.
- **D-19:** Toast notifications (TempData["Success"], TempData["Error"], TempData["GoldReceived"]) are kept — they are functional feedback on purchase actions.
- **D-20:** Per-page CSS: `shop-details.mobile.css`.

### CSS Architecture
- **D-21:** One CSS file per view, following Phase 18 D-20. Files:
  - `admin-users.mobile.css` — card layout, stacked action buttons for Users
  - `admin-quests.mobile.css` — card layout for quest items (shared structure with users CSS but separate file)
  - `admin-edituser.mobile.css` — form container for EditUser + ResetPassword (can be referenced by both views)
  - `shop-management-index.mobile.css` — flat item list, item card, icon-only action buttons
  - `shop-management-create.mobile.css` — form container, availability window section
  - `shop-management-edit.mobile.css` — same as create but status info alert spacing
  - `shop-details.mobile.css` — item detail card, rarity badge, purchase button

  **Note:** If the EditUser and ResetPassword views are structurally similar enough, they can share `admin-form.mobile.css`.

### Claude's Discretion
- Ordering of items in the flat ShopManagement Index list (by status priority, then name/id).
- Whether EditUser and ResetPassword share one CSS file or get individual files.
- Exact icon-only button sizing for the ShopManagement item cards (e.g., `btn-sm` with padding adjustments).
- Empty-state markup when the flat item list has no items.
- Whether the glass card on Users mobile shows the email address of the user below the name, or omits it (email is less useful at a glance in the admin list).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — ADMIN-01, ADMIN-02, SHOPMGMT-01 must be defined/confirmed by the planner. Confirm Phase 19 success criteria in ROADMAP.md.
- `.planning/ROADMAP.md` — Phase 19 goal and success criteria (3 criteria); read before implementing.

### Phase 12 Infrastructure (authoritative for mobile view hook-in)
- `.planning/phases/12-mobile-infrastructure/12-CONTEXT.md` — `@section Styles` / `@section Scripts` rendering pattern, mobile CSS baseline loading. MUST read.

### Phase 16 Shop Pattern (closest prior art for Shop/Details)
- `.planning/phases/16-account-browse/16-CONTEXT.md` — D-02 (glass card treatment for shop index), D-08 (per-page CSS), D-09 (mobile.css baseline). The Shop/Index.Mobile.cshtml from Phase 16 is the closest analog.
- `EuphoriaInn.Service/wwwroot/css/shop.mobile.css` — Phase 16 Shop Index CSS. Reference for item card patterns, rarity badge styling.

### Phase 18 Patterns (most recent — form card + alert handling)
- `.planning/phases/18-dm-editing-secondary-quest-views/18-CONTEXT.md` — D-02 (keep functional alerts), D-05 (glass card + parchment text), D-20 (one CSS file per view). Directly applicable.

### Desktop Views Being Mobilized
- `EuphoriaInn.Service/Views/Admin/Users.cshtml` — `UserManagementViewModel` bindings, PromoteToAdmin/PromoteToDM/DemoteFromAdmin/DemoteToPlayer form POSTs, `deleteUser()` inline JS, Antiforgery token.
- `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` — `EditUserViewModel` bindings, HasKey checkbox, Reset Password link.
- `EuphoriaInn.Service/Views/Admin/Quests.cshtml` — `IEnumerable<Quest>` model, status badge logic, `deleteQuest()` inline JS.
- `EuphoriaInn.Service/Views/Admin/ResetPassword.cshtml` — `ResetPasswordViewModel` bindings, warning alert.
- `EuphoriaInn.Service/Views/ShopManagement/Index.cshtml` — `ShopManagementIndexViewModel` (ItemsForReview, MyItems, AllOtherItems), all action forms, Deny/Bulk modals.
- `EuphoriaInn.Service/Views/ShopManagement/Create.cshtml` — `CreateShopItemViewModel` bindings, `toggleAvailabilityWindow()`, `updatePriceSuggestion()`, `setRandomPrice()`/`setCalculatedPrice()` (to be stripped from mobile), inline `<style>` rarity animations.
- `EuphoriaInn.Service/Views/ShopManagement/Edit.cshtml` — `EditShopItemViewModel` bindings, same pricing JS structure as Create.
- `EuphoriaInn.Service/Views/Shop/Details.cshtml` — `ShopItemViewModel` bindings, `ViewBag.IsModal` conditional (use non-modal path on mobile), `_ShopItemDetailsContent` partial reference (inline instead on mobile), toast TempData.

### Existing Mobile Shop CSS
- `EuphoriaInn.Service/wwwroot/css/shop.mobile.css` — Existing mobile shop index CSS from Phase 16. Reference for `.shop-item-card-mobile`, rarity CSS classes, and glass card values. `shop-details.mobile.css` and `shop-management-*.mobile.css` should follow the same glass card values.

### Mobile Layout Shell
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — Confirms `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` are declared.

### Existing Mobile CSS Baseline
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch targets, typography scale. Per-page CSS extends this; do not redefine baseline rules.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `shop.mobile.css` — Phase 16 Shop Index CSS with `.shop-item-card-mobile`, rarity badge classes (`rarity-common`, `rarity-rare`, etc.) — reuse these class names or reference values in new Shop/Details and ShopManagement CSS files.
- Glass card CSS pattern: `background: rgba(255, 255, 255, 0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 12px;` — established in Phase 13, used in all subsequent phases.
- Parchment text: `color: #F4E4BC !important; text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);`
- Bootstrap modal JS (Deny Item, Bulk Actions) — copy `@section Scripts` block from desktop Index verbatim into mobile view; Bootstrap 5 modals work on mobile.
- `deleteUser()` / `deleteQuest()` inline JS — copy verbatim from desktop into mobile `@section Scripts`. Antiforgery token pattern (`@Antiforgery.GetAndStoreTokens(ViewContext.HttpContext)`) is used in these views; copy the `@inject` and `@{ var tokens = ... }` block.

### Established Patterns
- `@section Styles { <link href="~/css/{file}.css" asp-append-version="true" rel="stylesheet" /> }` — per-page CSS injection.
- `onclick="window.location.href='@Url.Action(...)'"` — tap navigation for list rows (Phase 13 pattern).
- Single-column forms with `col-12` inputs — all fields full-width (Phase 15–18 pattern).
- `asp-action` / `asp-controller` / `asp-route-*` tag helpers — use consistently; do not switch to hardcoded URLs on mobile.
- No `@inject` for globally available services — `_ViewImports.cshtml` already injects `IAntiforgery`, `IAuthorizationService`.

### Integration Points
- No new controllers, services, or ViewModels — all views use existing model types.
- `_ViewStart.cshtml` handles layout selection — no individual mobile view sets `Layout`.
- `HttpContext.Items["IsMobile"]` set by middleware; `.Mobile.cshtml` files served automatically by Phase 12 expander.
- `(await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded` — used in ShopManagement Index for conditional delete/publish buttons; preserve on mobile.
- `ViewBag.IsModal` — used in `Shop/Details.cshtml`; mobile view uses non-modal path only (skip the `@if (isModal)` branch entirely).

</code_context>

<specifics>
## Specific Ideas

- Admin Users card row order: icon (fa-user / fa-shield-alt / fa-crown / fa-dice-d20) + name + role badge at top; action button row below. Use `d-flex flex-wrap gap-2` for the button row so buttons don't overflow on very narrow screens.
- Admin Quests card: `<i class="fas fa-dragon text-muted me-2"></i>@quest.Title` + `<span class="badge @statusBadge">` + `<small class="text-muted">@(quest.DungeonMaster?.Name ?? "Unknown")</small>` — matches established card content density from Phase 13.
- ShopManagement flat item card: rarity badge first (visual scan), then name in bold, then `@item.Price gp`, then status badge, then icon-only action buttons using `btn-sm`. Class `rarity-@item.Rarity.ToString().ToLower()` already defined in `shop.mobile.css`.
- Shop/Details mobile: item name in glass card header + type badge inline; rarity + description + purchase section in card body. Purchase button full-width with gold icon. Toast container stays at top of page.
- ShopManagement Create/Edit: `updatePriceSuggestion()` still fires on rarity change and updates the `#price-suggestion` hint text. `setRandomPrice()` and `setCalculatedPrice()` functions can be omitted (buttons removed). The `@@keyframes legendary-glow` animation CSS in `@section Scripts` `<style>` is kept for rarity badge animation.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within Phase 19 scope.

</deferred>

---

*Phase: 19-admin-shop-management-views*
*Context gathered: 2026-06-25*
