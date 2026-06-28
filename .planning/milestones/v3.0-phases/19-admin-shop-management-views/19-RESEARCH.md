# Phase 19: Admin & Shop Management Views — Research

**Researched:** 2026-06-25
**Domain:** ASP.NET Core 8 MVC Razor mobile view variants — admin and shop management pages
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Admin Users (Admin/Users.Mobile.cshtml) — D-01, D-02:**
- Card per user: glass card with name + role badge (Administrator/DM/Player), then all applicable action buttons stacked vertically below
- Show Promote/Demote buttons only when applicable (same conditional logic as desktop); Edit and Delete always visible
- The inline `deleteUser()` JS function (fetch DELETE + location.reload) is copied from the desktop view into `@section Scripts`
- Antiforgery token pattern is preserved

**Admin Quests (Admin/Quests.Mobile.cshtml) — D-03, D-04:**
- Card per quest: Title, DM name, Status badge (Open/Finalized/Done), then Edit and Delete buttons
- Description column omitted on mobile
- Same status badge logic (bg-dark/bg-primary/bg-success) preserved
- The inline `deleteQuest()` JS is copied into `@section Scripts`

**Admin EditUser (Admin/EditUser.Mobile.cshtml) — D-05:**
- Single-column form following Phase 15/16 pattern; glass card container
- Fields: Name, Email, HasKey checkbox, Reset Password link, Save Changes button

**Admin ResetPassword (Admin/ResetPassword.Mobile.cshtml) — D-06:**
- Single-column form; glass card container; `alert-warning` kept (functional context)

**ShopManagement Index (ShopManagement/Index.Mobile.cshtml) — D-07, D-08, D-09, D-10:**
- Single flat list of ALL items — no section separation; ordered by status (Pending Review first, then alphabetical)
- Each item card: Name + Rarity badge + Price (gp) + Status badge + icon-only action buttons (View, Edit, Archive/Reopen, Deny, Delete)
- Conditional logic for which actions appear preserved from desktop (authorization checks, status-based visibility)
- "Deny Item" modal and "Bulk Actions" modal stub kept as Bootstrap modals — copy from desktop
- "Add New Item" full-width primary button at top; "View Shop" link also kept

**ShopManagement Create (ShopManagement/Create.Mobile.cshtml) — D-11, D-12, D-13, D-14:**
- Single-column form; all fields included (Name, Type, Description, Rarity, Quantity, Price, ReferenceUrl, AvailableFrom/Until)
- Price field full-width with NO input-group buttons — dice/calculator buttons omitted; manual price entry only
- `price-suggestion` hint div kept; `toggleAvailabilityWindow()` / `updatePriceSuggestion()` JS kept; rarity CSS animations kept
- Per-page CSS: `shop-management-create.mobile.css`

**ShopManagement Edit (ShopManagement/Edit.Mobile.cshtml) — D-15, D-16:**
- Same structure and decisions as Create; `alert-info` status info alert kept; pricing tool buttons omitted
- Per-page CSS: `shop-management-edit.mobile.css`

**Shop Details (Shop/Details.Mobile.cshtml) — D-17, D-18, D-19, D-20:**
- Content inlined (NOT delegated to `_ShopItemDetailsContent` partial); non-modal path only
- Glass card + parchment text; single-column: name + type badge header, rarity + description + price + purchase button in body
- Toast notifications (TempData["Success"], TempData["Error"], TempData["GoldReceived"]) kept
- Per-page CSS: `shop-details.mobile.css`

**CSS Architecture — D-21:**
- Seven CSS files (one per view, except EditUser+ResetPassword share `admin-form.mobile.css`):
  - `admin-users.mobile.css`, `admin-quests.mobile.css`, `admin-form.mobile.css`
  - `shop-management-index.mobile.css`, `shop-management-create.mobile.css`, `shop-management-edit.mobile.css`
  - `shop-details.mobile.css`

### Claude's Discretion
- Ordering of items in the flat ShopManagement Index list (by status priority, then name/id)
- Whether EditUser and ResetPassword share one CSS file or get individual files
- Exact icon-only button sizing for the ShopManagement item cards (e.g., `btn-sm` with padding adjustments)
- Empty-state markup when the flat item list has no items
- Whether the glass card on Users mobile shows the email address below the name (or omits it — UI-SPEC chose to omit)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within Phase 19 scope.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ADMIN-01 | Admin user list and edit pages are usable on mobile without horizontal scrolling | 8 mobile views confirmed; glass card pattern eliminates table overflow; AdminController requires `[Authorize(Policy = "AdminOnly")]` — integration tests need Admin-role user |
| ADMIN-02 | Shop Management index, create, and edit pages are fully functional on mobile | ShopManagementController pattern verified; form fields + JS confirmed; flat item list with authorization guards preserved |
| SHOPMGMT-01 | Shop item detail page renders in a single-column layout with no overflow | Shop/Details model is `ShopItemDetailsViewModel` (extends `ShopItemViewModel`); content inlined from partial; purchase + sell-to-shop forms both included in mobile view |

**Note:** ADMIN-01, ADMIN-02, and SHOPMGMT-01 are NOT currently defined in REQUIREMENTS.md. The planner MUST add these three requirements to REQUIREMENTS.md in Plan 01 (Wave 1 — before implementation plans).

</phase_requirements>

---

## Summary

Phase 19 creates eight `.Mobile.cshtml` view files and seven CSS files for Admin, ShopManagement, and Shop/Details pages. Every view is strictly additive — no controllers, ViewModels, repositories, or domain services are modified. The phase follows the same architectural pattern established in Phases 13–18: a per-view glass card container with parchment text, `@section Styles` CSS injection, and `@section Scripts` for inline JS.

The primary complexity in this phase is the ShopManagement/Index mobile view: it merges three desktop table sections (`ItemsForReview`, `MyItems`, `AllOtherItems`) into a single flat list, preserves complex conditional authorization guards for action buttons, and carries over both the Deny Item modal (with AJAX form submission) and the Bulk Actions stub modal. The Admin/Users view has moderate complexity due to the inline DELETE fetch JS and the role-based conditional buttons. The four form views (EditUser, ResetPassword, ShopMgmt Create, ShopMgmt Edit) and the Shop/Details view are straightforward single-column conversions.

Integration tests follow the established Nyquist pattern: one test per requirement checking that the mobile CSS file is linked and the primary glass card CSS class is present in the HTML. Tests require Admin-role authentication for Admin controller pages; ShopManagement tests require DungeonMaster role; Shop/Details tests require any authenticated user.

**Primary recommendation:** Structure Phase 19 as: Wave 1 = add requirements to REQUIREMENTS.md + Wave 2 = admin views + Wave 3 = ShopManagement views + Shop/Details + Wave 4 = integration tests. This matches the established phase pattern (separate test plan at the end).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Admin user management UI | Frontend Server (SSR) | — | `.Mobile.cshtml` views rendered server-side; AdminController already handles business logic |
| Admin quest management UI | Frontend Server (SSR) | — | Same as above; no new controller needed |
| Shop management forms | Frontend Server (SSR) | — | All ShopManagementController endpoints unchanged; only view layer added |
| Shop item detail display | Frontend Server (SSR) | — | ShopController.Details already handles modal vs full-page path; mobile takes non-modal path |
| Delete fetch (user/quest) | Browser / Client | — | Inline `deleteUser()`/`deleteQuest()` JS uses `fetch()` DELETE; antiforgery token passed in header |
| Authorization guards on buttons | Frontend Server (SSR) | API / Backend | `AuthorizationService.AuthorizeAsync` called in Razor view for AdminOnly policy; enforcement is server-side |
| CSS/styling | CDN / Static | — | Static files served from `wwwroot/css/`; `asp-append-version` for cache busting |

---

## Standard Stack

### Core (no new packages — all already present)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core 8 MVC | 8.x | Server-side rendering, controller actions, view engine | Project constraint — no framework changes |
| Razor / `.cshtml` views | ASP.NET Core built-in | Template rendering for mobile variants | Same engine as all prior phases |
| Bootstrap 5.x | CDN in `_Layout.Mobile.cshtml` | Glass card containers, badge classes, modals, toast | Already loaded; no install |
| FontAwesome 6 | CDN in `_Layout.Mobile.cshtml` | Icons (`fas` prefix) | Already loaded; CLAUDE.md enforces `me-2` spacing |

**No new NuGet packages required.** [VERIFIED: codebase inspection]

### Supporting (existing CSS patterns)

| Asset | Source | Purpose |
|-------|--------|---------|
| `mobile.css` | `wwwroot/css/mobile.css` | 44px touch targets, body font (16px), `.btn` min-height — do NOT redefine |
| `shop.mobile.css` | `wwwroot/css/shop.mobile.css` | Rarity badge classes (`.rarity-common` … `.rarity-legendary`) and `.item-rarity` — reuse class names |
| Glass card values | All prior phase CSS | `background: rgba(255,255,255,0.15); backdrop-filter: blur(15px); border: 1px solid rgba(255,255,255,0.3); border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.2); padding: 16px;` |

---

## Package Legitimacy Audit

No external packages are installed in this phase. Registry audit not required.

---

## Architecture Patterns

### System Architecture Diagram

```
Mobile Browser (iPhone UA)
        |
        v
MobileDetectionMiddleware  →  sets HttpContext.Items["IsMobile"] = true
        |
        v
MobileViewLocationExpander  →  prepends ViewName.Mobile.cshtml to search path
        |
        v
AdminController / ShopManagementController / ShopController
  (unchanged — same actions, same ViewModels)
        |
        v
Views/Admin/Users.Mobile.cshtml          → admin-users.mobile.css
Views/Admin/EditUser.Mobile.cshtml       → admin-form.mobile.css
Views/Admin/Quests.Mobile.cshtml         → admin-quests.mobile.css
Views/Admin/ResetPassword.Mobile.cshtml  → admin-form.mobile.css (shared)
Views/ShopManagement/Index.Mobile.cshtml → shop-management-index.mobile.css
Views/ShopManagement/Create.Mobile.cshtml→ shop-management-create.mobile.css
Views/ShopManagement/Edit.Mobile.cshtml  → shop-management-edit.mobile.css
Views/Shop/Details.Mobile.cshtml        → shop-details.mobile.css
        |
        v
_Layout.Mobile.cshtml renders @RenderBody() + @RenderSectionAsync("Styles") + @RenderSectionAsync("Scripts")
        |
        v
Mobile browser receives glass card HTML + per-page CSS
```

### Recommended Project Structure

New files this phase:

```
EuphoriaInn.Service/
├── Views/
│   ├── Admin/
│   │   ├── Users.Mobile.cshtml          (glass card list, deleteUser() JS)
│   │   ├── EditUser.Mobile.cshtml       (single-column form)
│   │   ├── Quests.Mobile.cshtml         (glass card list, deleteQuest() JS)
│   │   └── ResetPassword.Mobile.cshtml  (single-column form)
│   ├── ShopManagement/
│   │   ├── Index.Mobile.cshtml          (flat item list, Deny/Bulk modals)
│   │   ├── Create.Mobile.cshtml         (single-column form, pricing JS)
│   │   └── Edit.Mobile.cshtml           (single-column form, status alert)
│   └── Shop/
│       └── Details.Mobile.cshtml        (inlined content, toast notifications)
└── wwwroot/css/
    ├── admin-users.mobile.css
    ├── admin-quests.mobile.css
    ├── admin-form.mobile.css            (shared by EditUser + ResetPassword)
    ├── shop-management-index.mobile.css
    ├── shop-management-create.mobile.css
    ├── shop-management-edit.mobile.css
    └── shop-details.mobile.css
```

### Pattern 1: Glass Card Mobile View (established Phases 13–18)

**What:** Every mobile view wraps its content in a glass card div. The CSS class is unique per view (e.g., `.admin-users-card-mobile`). Per-page CSS is injected via `@section Styles`.

**When to use:** All 8 views in this phase.

**Example (from Phase 18 — quest-edit pattern):** [VERIFIED: codebase]
```razor
@section Styles {
    <link href="~/css/admin-users.mobile.css" asp-append-version="true" rel="stylesheet" />
}

<div class="admin-users-card-mobile mb-3">
    <div class="mb-3">
        <h5 class="mb-0">
            <i class="fas fa-users-cog text-danger me-2"></i>User Management
        </h5>
    </div>
    @foreach (var userModel in Model)
    {
        <div class="user-card-mobile mb-3">
            ...
        </div>
    }
</div>
```

### Pattern 2: Antiforgery Token for Inline DELETE Fetch

**What:** Views that use inline `fetch()` DELETE (Users and Quests) need the antiforgery token. The `IAntiforgery` service is already globally injected via `_ViewImports.cshtml` — do NOT add `@inject` in the mobile view.

**Example (from desktop Users.cshtml — copy verbatim):** [VERIFIED: codebase]
```razor
@{
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}
<script>
    function deleteUser(id) {
        if (confirm("Are you sure you want to delete this user? This action cannot be undone.")) {
            fetch(`/Admin/DeleteUser/${id}`, {
                method: "DELETE",
                headers: {
                    'RequestVerificationToken': '@tokens.RequestToken'
                }
            }).then(res => {
                if (res.ok) { location.reload(); } else { alert("Delete failed."); }
            });
        }
    }
</script>
```

Move this to `@section Scripts` in the mobile view. The `var tokens = ...` block goes at the top of the Razor file in `@{ ... }`.

### Pattern 3: Authorization Guard in Razor View

**What:** ShopManagement/Index uses `(await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded` to conditionally show Publish and Delete buttons. `AuthorizationService` is globally available via `_ViewImports.cshtml` — do NOT `@inject` it.

**Example (from desktop ShopManagement/Index.cshtml — copy verbatim for mobile):** [VERIFIED: codebase]
```razor
@if (item.Status == ItemStatus.Draft && (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
{
    <form method="post" action="/ShopManagement/Publish/@item.Id" ...>
        @Html.AntiForgeryToken()
        <button type="submit" class="btn btn-success btn-sm btn-action" title="Publish" aria-label="Publish item">
            <i class="fas fa-check"></i>
        </button>
    </form>
}
```

### Pattern 4: ShopManagement Index — Flat List Source

**What:** The mobile Index view draws from three ViewModel collections. The flat list ordered by status priority (Pending Review first, then alphabetical by name) is:

```razor
@{
    var allItems = Model.ItemsForReview
        .Concat(Model.MyItems)
        .Concat(Model.AllOtherItems)
        .OrderBy(i => i.Status == ItemStatus.Draft ? 0 : 1)
        .ThenBy(i => i.Name)
        .DistinctBy(i => i.Id)
        .ToList();
}
```

Note: `ItemsForReview` contains `Draft` status items (awaiting DM votes). `MyItems` and `AllOtherItems` may also contain Draft items if the current user is a DM. `DistinctBy(i => i.Id)` prevents duplicates if an item appears in multiple collections.

### Pattern 5: Shop Details — Inline Content (not partial)

**What:** Per D-17, the mobile Details view does NOT use `@await Html.PartialAsync("_ShopItemDetailsContent", Model)`. Content is inlined. The model type in the controller is `ShopItemDetailsViewModel` (extends `ShopItemViewModel`) — the mobile view should declare `@model ShopItemDetailsViewModel`. [VERIFIED: codebase — `ShopController.Details` maps to `ShopItemDetailsViewModel`]

The inlined content on mobile is a simplified version:
- Type icon + rarity badge + description + availability info + price
- Purchase form (quantity input + submit button) — conditional on `Model.Status == ItemStatus.Published && Model.Quantity != 0`
- "Out of stock" / "Under review" messages for other states
- Toast container for TempData

**The "Sell to Shop" section in the partial is intentionally omitted on mobile** — the UI-SPEC (D-17, D-18) only specifies purchase button in card body. The partial's sell-to-shop form is not mentioned in any CONTEXT.md decision, so omit it on mobile for simplicity.

### Anti-Patterns to Avoid

- **Using `@inject` in mobile views:** `IAntiforgery`, `IAuthorizationService`, and `IUserService` are already injected globally by `_ViewImports.cshtml`. Re-injecting causes a compile error (duplicate injection). [VERIFIED: STATE.md plan note — Plan 02, Plan 15-03]
- **Setting `Layout` in a mobile view:** `_ViewStart.cshtml` handles layout selection. Individual views must NOT set Layout. [VERIFIED: STATE.md plan note]
- **Adding `@media` queries to per-page CSS:** These files are exclusively loaded by the mobile layout; device targeting is done at the layout-selection layer. [VERIFIED: 12-CONTEXT.md D-03]
- **Using `@{} wrapper` inside `@foreach`:** Razor returns to C# code mode after HTML inside a foreach body; do not wrap variable declarations in `@{}`. [VERIFIED: STATE.md — Phase 13 Plan 02]
- **Re-including `@using` for already-imported namespaces:** `_ViewImports.cshtml` imports common namespaces. Check ViewImports before adding `@using`.
- **Calling `setRandomPrice()` / `setCalculatedPrice()` on mobile ShopMgmt:** These functions (and their triggering buttons) are removed per D-12. The `updatePriceSuggestion()` function is kept but must NOT reference `randomPriceBtn` or `calculatedPriceBtn` (they don't exist on mobile). Strip those `randomBtn.disabled` / `calculatedBtn.disabled` lines from the copied JS.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role badges | Custom badge HTML | Bootstrap `badge` classes (`bg-danger`, `bg-primary`, `bg-secondary`) | Already defined in Bootstrap 5; consistent with desktop |
| Antiforgery for fetch DELETE | Custom token management | `Antiforgery.GetAndStoreTokens(ViewContext.HttpContext).RequestToken` | Already globally injected via `_ViewImports.cshtml` |
| Authorization check in view | Custom auth logic | `(await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded` | Pattern established in ShopManagement/Index.cshtml desktop |
| Rarity CSS colors | Custom rarity styles | `.rarity-common`, `.rarity-uncommon`, `.rarity-rare`, `.rarity-veryrare`, `.rarity-legendary` in `shop.mobile.css` | Already defined; reuse class names |
| Toast initialization | Custom JS | Bootstrap 5 `new bootstrap.Toast(el)` + `toast.show()` pattern from Shop/Index.Mobile.cshtml | Copy verbatim from Phase 16 mobile shop view |
| Bootstrap modal trigger | Custom modal JS | `data-bs-toggle="modal" data-bs-target="#denyModal"` + `show.bs.modal` event listener | Copy from desktop ShopManagement/Index.cshtml |

---

## Common Pitfalls

### Pitfall 1: `updatePriceSuggestion()` references removed buttons

**What goes wrong:** The desktop Create/Edit JS calls `randomBtn.disabled = false/true` and `calculatedBtn.disabled = false/true`. On mobile these buttons don't exist, so `document.getElementById('randomPriceBtn')` returns `null` and calling `.disabled` throws a JS error.

**Why it happens:** Direct copy-paste of the desktop `updatePriceSuggestion()` function without removing the button-enable/disable calls.

**How to avoid:** When copying `updatePriceSuggestion()` into the mobile view's `@section Scripts`, remove all lines that reference `randomBtn` and `calculatedBtn`. Keep only the `suggestionDiv.innerHTML = ...` and `suggestionDiv.className = ...` lines.

**Warning signs:** JS console error "Cannot set properties of null (setting 'disabled')" when changing Rarity on mobile.

### Pitfall 2: DistinctBy required on ShopManagement flat list

**What goes wrong:** An item from a DM's own "My Items" that is also in "Pending Review" (`ItemsForReview`) will appear twice in the concatenated list.

**Why it happens:** `ItemsForReview` contains ALL draft items needing review (which may include the current DM's own items). `MyItems` contains all items owned by the current DM (which may be in draft status).

**How to avoid:** Use `.DistinctBy(i => i.Id)` after `.Concat().OrderBy()` chain. Requires `using System.Linq`.

### Pitfall 3: Shop/Details model mismatch

**What goes wrong:** Declaring `@model ShopItemViewModel` in Details.Mobile.cshtml when the controller passes `ShopItemDetailsViewModel`. The view compiles but loses access to `DmVotes` and `RecentTransactions`. More critically, if the controller changes to use AutoMapper it may pass a different type.

**Why it happens:** The partial `_ShopItemDetailsContent.cshtml` uses `@model ShopItemViewModel` (the base class), which can accept the derived `ShopItemDetailsViewModel`. When inlining the content, the same declaration appears natural — but the correct declaration is the full type.

**How to avoid:** Declare `@model ShopItemDetailsViewModel` in Details.Mobile.cshtml to match what `ShopController.Details` actually passes. `ShopItemDetailsViewModel` extends `ShopItemViewModel`, so all base properties are still accessible.

### Pitfall 4: Admin controller requires AdminOnly policy — tests need Admin role

**What goes wrong:** Integration tests for `/Admin/Users`, `/Admin/EditUser`, `/Admin/Quests`, `/Admin/ResetPassword` return 403 Forbidden if the test user is not in the Admin role.

**Why it happens:** `AdminController` has `[Authorize(Policy = "AdminOnly")]` at the class level.

**How to avoid:** In integration test stubs, use `AuthenticationHelper.CreateAuthenticatedAdminClientAsync(...)` (the dedicated helper that creates users with `["Admin"]` role) or pass `roles: new[] { "Admin" }` to `CreateAuthenticatedClientWithUserAsync`.

### Pitfall 5: `@Html.AntiForgeryToken()` vs antiforgery token pattern

**What goes wrong:** ShopManagement/Index desktop uses `@Html.AntiForgeryToken()` inside each action form. This works correctly — it renders a hidden input. On mobile, continue using this same pattern for all POST forms. Do NOT confuse this with the `deleteUser()` / `deleteQuest()` fetch-based DELETE, which requires the `Antiforgery.GetAndStoreTokens` pattern (token goes in HTTP header, not form body).

**How to avoid:** Keep both patterns intact as copied from the desktop view. `@Html.AntiForgeryToken()` inside `<form>` elements; `tokens.RequestToken` in fetch headers.

### Pitfall 6: Rarity animation `@@keyframes` in Razor

**What goes wrong:** CSS `@keyframes` inside a Razor `<style>` block inside `@section Scripts` must be escaped as `@@keyframes` to prevent Razor from interpreting the `@` as a C# expression.

**Why it happens:** Razor parses `@` as the start of a C# expression. Inside a `<script>` or `<style>` block within a section, this still applies.

**How to avoid:** Copy the `@@keyframes legendary-glow` pattern verbatim from the desktop Create/Edit view — it already uses `@@` correctly.

---

## Code Examples

### Glass card CSS — established pattern [VERIFIED: codebase]

```css
/* admin-users.mobile.css — Admin/Users.Mobile.cshtml only */
/* No @media queries — exclusively loaded by Admin/Users.Mobile.cshtml via _Layout.Mobile.cshtml */

.admin-users-card-mobile {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 16px;
}

.admin-users-card-mobile .form-label,
.admin-users-card-mobile h5 {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}

.admin-users-card-mobile .form-text,
.admin-users-card-mobile small {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}

.admin-users-card-mobile .badge {
    text-shadow: none !important;
}
```

### Icon-only button row — ShopManagement item card [VERIFIED: UI-SPEC + CONTEXT.md]

```html
<div class="d-flex gap-1 mt-2">
    <a href="/Shop/Details/@item.Id" class="btn btn-info btn-sm" title="View item" aria-label="View item">
        <i class="fas fa-eye"></i>
    </a>
    <a href="/ShopManagement/Edit/@item.Id" class="btn btn-primary btn-sm" title="Edit item" aria-label="Edit item">
        <i class="fas fa-edit"></i>
    </a>
    @if (item.Status == ItemStatus.Published)
    {
        <form method="post" action="/ShopManagement/Archive/@item.Id" style="display: inline;"
              onsubmit="return confirm('Are you sure you want to archive this item?')">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-secondary btn-sm" title="Archive item" aria-label="Archive item">
                <i class="fas fa-ban"></i>
            </button>
        </form>
    }
    ...
</div>
```

### updatePriceSuggestion — mobile version (stripped of button-enable calls) [VERIFIED: codebase]

```javascript
function updatePriceSuggestion() {
    const rarity = document.getElementById('Rarity').value;
    const suggestionDiv = document.getElementById('price-suggestion');

    if (rarity && pricingGuide[rarity]) {
        const guide = pricingGuide[rarity];
        const calculatedPrice = calculatedPrices[rarity];
        suggestionDiv.innerHTML = `<i class="fas fa-coins me-1"></i>${guide.text}<br><small class="text-muted">Tasha's recommended: ${calculatedPrice} gp</small>`;
        suggestionDiv.className = 'form-text text-info';
    } else {
        suggestionDiv.innerHTML = 'Select a rarity to see pricing suggestions.';
        suggestionDiv.className = 'form-text';
    }
}
```

---

## State of the Art

No state-of-the-art considerations — this phase uses only existing project patterns (no new libraries, no new architectural approaches).

| Phase Pattern | Established In | Applies Here |
|---------------|----------------|--------------|
| Glass card + parchment text | Phase 13 | All 8 views |
| `@section Styles` CSS injection | Phase 12 | All 8 views |
| No `@inject` for globally available services | Phase 13 Plan 03 | All 8 views |
| Inline JS copied verbatim into `@section Scripts` | Phase 13 Plan 03 | Users, Quests, ShopMgmt Index, ShopMgmt Create/Edit, Shop Details |
| One CSS file per view | Phase 18 D-20 | All 7 CSS files (EditUser+ResetPassword share admin-form.mobile.css) |
| Integration tests in final Wave | Phases 16–18 | Phase 19 final plan |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | The "Sell to Shop" section from `_ShopItemDetailsContent.cshtml` is omitted on mobile (not mentioned in any CONTEXT.md decision) | Architecture Patterns — Pattern 5 | If users expect sell-to-shop on mobile, feature is missing. Planner/user can add it as Claude's discretion. |
| A2 | `DistinctBy(i => i.Id)` is needed to deduplicate items in flat ShopManagement list | Common Pitfalls + Pattern 4 | If ViewModel collections guarantee no overlap, the call is harmless but unnecessary |

---

## Open Questions

1. **Should Shop/Details mobile also include the "Sell to Shop" form?**
   - What we know: D-17 specifies "rarity + description + price + purchase button in card body" — no mention of sell-to-shop
   - What's unclear: Whether mobile users would ever want to sell items from the details page
   - Recommendation: Omit for now (keeps view simple); can be added as a follow-up if requested

2. **ShopManagement Index: should Denied items with denial reason show the `alert-danger` denial reason block?**
   - What we know: D-07/D-08 specify card shows Name + Rarity + Price + Status + action buttons; denial reason block is in the desktop `MyItems` section
   - What's unclear: Whether the denial reason is important enough for mobile view
   - Recommendation: Omit the denial reason in the icon-only card layout (too verbose); status badge "Denied" is sufficient context for admin action

---

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — this phase creates only `.cshtml` view files and `.css` static files within the existing ASP.NET Core project; no new tools, services, CLIs, runtimes, or databases are required).

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit + FluentAssertions (existing) |
| Config file | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -q` |
| Full suite command | `dotnet test EuphoriaInn.IntegrationTests -q` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ADMIN-01 | Admin/Users mobile returns 200 + links `admin-users.mobile.css` + contains `admin-users-card-mobile` | integration | `dotnet test --filter "GetMobilePage_AdminUsers"` | ❌ Wave N |
| ADMIN-01 | Admin/EditUser mobile returns 200 + links `admin-form.mobile.css` | integration | `dotnet test --filter "GetMobilePage_AdminEditUser"` | ❌ Wave N |
| ADMIN-01 | Admin/Quests mobile returns 200 + links `admin-quests.mobile.css` | integration | `dotnet test --filter "GetMobilePage_AdminQuests"` | ❌ Wave N |
| ADMIN-02 | ShopManagement/Index mobile returns 200 + links `shop-management-index.mobile.css` | integration | `dotnet test --filter "GetMobilePage_ShopManagementIndex"` | ❌ Wave N |
| ADMIN-02 | ShopManagement/Create mobile returns 200 + links `shop-management-create.mobile.css` | integration | `dotnet test --filter "GetMobilePage_ShopManagementCreate"` | ❌ Wave N |
| ADMIN-02 | ShopManagement/Edit mobile returns 200 + links `shop-management-edit.mobile.css` | integration | `dotnet test --filter "GetMobilePage_ShopManagementEdit"` | ❌ Wave N |
| SHOPMGMT-01 | Shop/Details mobile returns 200 + links `shop-details.mobile.css` + contains `shop-details-card-mobile` | integration | `dotnet test --filter "GetMobilePage_ShopDetails"` | ❌ Wave N |

All tests follow the pattern established in `MobileViewsTests.cs`: `GetWithUserAgentAsync` using the iPhone user agent string, check `html.Should().Contain("css-filename.css")` and `html.Should().Contain("primary-card-class")`.

Admin tests require Admin-role user (`CreateAuthenticatedAdminClientAsync` or `roles: new[] { "Admin" }`).
ShopManagement tests require DungeonMaster-role user (`roles: new[] { "DungeonMaster" }`).
ShopManagement/Edit test requires a seeded ShopItem (for the route param).
Shop/Details test requires any authenticated user and a seeded Published ShopItem.

### Sampling Rate

- **Per plan commit:** `dotnet build EuphoriaInn.Service --no-restore -q` (verify compilation)
- **Per wave merge:** `dotnet test EuphoriaInn.IntegrationTests -q` (full integration test suite)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — append Phase 19 test stubs (7 test methods for ADMIN-01, ADMIN-02, SHOPMGMT-01)
- [ ] `REQUIREMENTS.md` — add ADMIN-01, ADMIN-02, SHOPMGMT-01 requirement entries

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes (indirect) | All Admin routes behind `[Authorize(Policy = "AdminOnly")]` — enforced by controller, not view |
| V3 Session Management | no | No new session state; mobile views are additive |
| V4 Access Control | yes | `AdminOnly` policy on AdminController; `DungeonMasterOnly` implied for ShopManagement; authorization checks in Razor for conditional buttons |
| V5 Input Validation | yes (indirect) | No new form inputs that bypass server validation; all forms POST to existing validated controller actions |
| V6 Cryptography | no | No new cryptographic operations |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Cross-site request forgery on POST forms | Tampering | `@Html.AntiForgeryToken()` in all POST forms; `[ValidateAntiForgeryToken]` on controller actions (already present on desktop) |
| CSRF on DELETE fetch (deleteUser/deleteQuest) | Tampering | `RequestVerificationToken` header from `Antiforgery.GetAndStoreTokens(ViewContext.HttpContext).RequestToken` — copy verbatim from desktop |
| Authorization bypass via mobile view | Elevation of Privilege | No new controller actions — authorization enforced at controller/service layer; mobile views are purely additive |
| XSS via item descriptions in ShopManagement | Tampering | Razor auto-encodes `@item.Description` by default; no `Html.Raw()` calls planned |

No new attack surface beyond existing desktop views.

---

## Sources

### Primary (HIGH confidence — verified from codebase)

- `EuphoriaInn.Service/Views/Admin/Users.cshtml` — model binding, conditional buttons, deleteUser() JS, antiforgery pattern
- `EuphoriaInn.Service/Views/Admin/Quests.cshtml` — model binding, status badge logic, deleteQuest() JS
- `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` — EditUserViewModel bindings, HasKey checkbox, Reset Password link
- `EuphoriaInn.Service/Views/Admin/ResetPassword.cshtml` — ResetPasswordViewModel bindings, alert-warning
- `EuphoriaInn.Service/Views/ShopManagement/Index.cshtml` — ShopManagementIndexViewModel, all action forms, Deny/Bulk modals, AuthorizationService guards
- `EuphoriaInn.Service/Views/ShopManagement/Create.cshtml` — CreateShopItemViewModel, pricing JS, availability window, @@keyframes
- `EuphoriaInn.Service/Views/ShopManagement/Edit.cshtml` — EditShopItemViewModel, status alert, same pricing JS
- `EuphoriaInn.Service/Views/Shop/Details.cshtml` — ShopItemViewModel model declaration, ViewBag.IsModal, toast pattern
- `EuphoriaInn.Service/Views/Shared/_ShopItemDetailsContent.cshtml` — inlined content reference; purchase form; sell-to-shop form
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopItemDetailsViewModel.cs` — model type confirmation (extends ShopItemViewModel)
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — AdminOnly policy, action names
- `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` — Details action, ViewBag.IsModal, model mapping
- `EuphoriaInn.Service/wwwroot/css/shop.mobile.css` — rarity badge classes, glass card values
- `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml` — toast pattern reference, modal AJAX loading pattern
- `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — test structure, auth helper patterns, established assertions
- `.planning/phases/19-admin-shop-management-views/19-CONTEXT.md` — all implementation decisions D-01 through D-21
- `.planning/phases/19-admin-shop-management-views/19-UI-SPEC.md` — CSS values, component inventory, interaction contract
- `.planning/phases/18-dm-editing-secondary-quest-views/18-01-PLAN.md` — plan structure template
- `.planning/STATE.md` — anti-pattern catalogue (no @inject, no Layout=, @@keyframes escaping)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all technology is already in use
- Architecture patterns: HIGH — 7 prior phases provide direct templates; all desktop source views read and verified
- Pitfalls: HIGH — sourced from STATE.md accumulated decisions and codebase inspection
- Test structure: HIGH — MobileViewsTests.cs pattern well-established; test helper methods verified

**Research date:** 2026-06-25
**Valid until:** 2026-07-25 (stable stack — no framework upgrades expected within milestone)
