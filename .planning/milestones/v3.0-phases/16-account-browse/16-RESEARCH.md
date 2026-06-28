# Phase 16: Account & Browse — Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core 8 MVC — Mobile View Variants (Razor .Mobile.cshtml), Bootstrap 5 Offcanvas, Glass Card CSS
**Confidence:** HIGH — all findings sourced directly from codebase; no external lookups required

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Seven mobile views in scope: `Login.Mobile.cshtml`, `Register.Mobile.cshtml`, `Edit.Mobile.cshtml`, `Profile.Mobile.cshtml`, `ChangePassword.Mobile.cshtml`, `Shop/Index.Mobile.cshtml`, `GuildMembers/Index.Mobile.cshtml`.
- **D-02:** All Account pages use glass card + parchment text treatment — `rgba(255,255,255,0.15)` backdrop-filter glass surface, `#F4E4BC` parchment text with text-shadow.
- **D-03:** Filter and sort controls collapse behind a **Filter & Sort button**. A bottom drawer or accordion reveals controls when tapped.
- **D-04:** Purchase History side panel is **omitted** on mobile.
- **D-05:** Items display in a **2-column grid**. Same Bootstrap `#itemDetailsModal` reused verbatim.
- **D-06:** Guild Members characters display as **single-column list rows** — small circular profile thumbnail left, name + class/role right.
- **D-07:** Section headers and list rows use glass card + parchment text treatment.
- **D-08:** Per-page CSS files: `account.mobile.css` (all five Account views), `shop.mobile.css`, `guild-members.mobile.css`.
- **D-09:** `mobile.css` baseline applies globally. Per-page CSS adds only page-specific rules.

### Claude's Discretion
- Exact drawer implementation for Shop filters (Bootstrap offcanvas vs. collapse vs. custom CSS — UI-SPEC chose Bootstrap offcanvas-bottom).
- Empty-state markup when My Characters list is empty.
- Whether `ChangePassword` needs only CSS-only adjustment or full mobile view — D-01 mandates it as a full mobile view.
- Circular thumbnail sizing in Guild Members list rows.
- Whether "Create New Character" button appears at top or bottom — UI-SPEC places it at top.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within Phase 16 scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ACCT-01 | Login page on mobile is a full-width single-column form with large input fields and a clearly tappable submit button | Login.cshtml model + bindings documented; desktop `col-md-6 col-lg-4` removed for full-width |
| ACCT-02 | Register page on mobile is a full-width single-column form | Register.cshtml model + bindings documented; desktop `col-md-6 col-lg-5` removed for full-width |
| ACCT-03 | User Profile edit page is usable on small screens | Covers Edit, Profile, ChangePassword views; all three ViewModels confirmed with exact property names |
| BROWSE-01 | Shop index on mobile displays items in single-column scrollable list (or 2-column grid); filter and sort controls are accessible | ShopIndexViewModel confirmed; full @functions block documented; modal JS lines 491–501 identified |
| BROWSE-02 | Guild Members directory on mobile displays character cards in single-column or 2-column layout | CharactersIndexViewModel confirmed; CharacterViewModel properties documented; GetProfilePicture route confirmed |
</phase_requirements>

---

## Summary

Phase 16 is a strictly additive view-only phase. Zero controllers, ViewModels, repositories, or domain services are modified. Seven `.Mobile.cshtml` views and three `.mobile.css` files are the complete deliverable.

All seven desktop views have been read in full. Every ViewModel, binding, `asp-for` target, `ViewData` key, and Razor helper used in the desktop views is documented below with exact property names. The planner should use this document as the single source of truth — no desktop view re-reading is needed during planning or execution.

The Shop view is the most complex: it carries three `@functions` (BuildTabUrl, BuildPageUrl, PageWindow), a floating merchant element, a toast container, a Bootstrap modal, and a `@section Scripts` block with four distinct JS behaviors. All of these must be preserved or consciously omitted in the mobile view. Purchase History side panel (lines 59–173) is omitted per D-04.

The Guild Members view is the second-most complex: the desktop character card grid is replaced with a flat list row pattern. The `GetProfilePicture` action exists on `GuildMembersController` and is confirmed reachable via `Url.Action("GetProfilePicture", new { id = character.Id })`. The character Details route is `Url.Action("Details", new { id = character.Id })` — no controller name needed (same controller).

**Primary recommendation:** Implement in three plans: (1) Account views + `account.mobile.css`, (2) Shop mobile view + `shop.mobile.css`, (3) Guild Members mobile view + `guild-members.mobile.css`. Each plan is self-contained and independently testable.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Mobile view selection | Frontend Server (SSR) | — | `MobileViewLocationExpander` resolves `.Mobile.cshtml` before `.cshtml` on mobile requests |
| CSS injection per view | Frontend Server (SSR) | CDN/Static | `@section Styles` pushes link tag; `_Layout.Mobile.cshtml` renders it in `<head>` |
| Form POST (Login, Register, Edit, ChangePassword) | API / Backend | — | All POST actions in `AccountController`; mobile views share same action routes |
| Shop filter/sort state | Frontend Server (SSR) | — | Query string parameters; `BuildTabUrl`/`BuildPageUrl` functions generate URLs server-side |
| Shop modal content fetch | Browser / Client | API / Backend | Modal event listener fetches `/Shop/Details?id=&isModal=true` via AJAX |
| Guild Members character list | Frontend Server (SSR) | — | Controller builds `CharactersIndexViewModel` with pre-sorted `MyCharacters`/`OtherCharacters` |
| Profile picture delivery | API / Backend | — | `GuildMembersController.GetProfilePicture(int id)` returns `FileContentResult` |

---

## Standard Stack

This phase introduces no new NuGet packages or npm packages. All tooling is already installed. [VERIFIED: codebase]

### Core (already in use)

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| Bootstrap | 5.3.0 (CDN) | Grid, offcanvas, modal, buttons, badges | `_Layout.Mobile.cshtml` line 8 |
| Font Awesome | 6.4.0 (CDN) | Icons | `_Layout.Mobile.cshtml` line 9 |
| Cinzel (Google Fonts) | — | D&D heading font | `_Layout.Mobile.cshtml` line 10 |
| ASP.NET Core Tag Helpers | .NET 8 | `asp-for`, `asp-action`, `asp-validation-*` | `_ViewImports.cshtml` |

### No New Packages Required [VERIFIED: codebase]

No `npm view` or `pip index` calls needed — this phase is CSS + Razor only.

---

## Package Legitimacy Audit

> Not applicable — this phase installs no external packages.

---

## Architecture Patterns

### System Architecture Diagram

```
Mobile Browser Request
        |
        v
MobileDetectionMiddleware → sets HttpContext.Items["IsMobile"] = true
        |
        v
MobileViewLocationExpander.PopulateValues() → adds "mobile" to cache key
        |
        v
MobileViewLocationExpander.ExpandViewLocations() → prepends "{view}.Mobile.cshtml" paths
        |
        v
Razor View Engine → resolves e.g. Account/Login.Mobile.cshtml (found) ✓
        |
        v
_ViewStart.cshtml → Layout = "_Layout.Mobile" (IsMobile == true)
        |
        v
_Layout.Mobile.cshtml
  → loads mobile.css (baseline, 44px touch targets)
  → renders @section Styles (per-page mobile CSS)
  → renders @RenderBody() → Login.Mobile.cshtml content
  → renders @section Scripts (per-page JS, e.g. Shop modal listener)
```

### Recommended Project Structure

```
EuphoriaInn.Service/
├── Views/
│   ├── Account/
│   │   ├── Login.Mobile.cshtml          (NEW — ACCT-01)
│   │   ├── Register.Mobile.cshtml       (NEW — ACCT-02)
│   │   ├── Edit.Mobile.cshtml           (NEW — ACCT-03)
│   │   ├── Profile.Mobile.cshtml        (NEW — ACCT-03)
│   │   └── ChangePassword.Mobile.cshtml (NEW — ACCT-03)
│   ├── Shop/
│   │   └── Index.Mobile.cshtml          (NEW — BROWSE-01)
│   └── GuildMembers/
│       └── Index.Mobile.cshtml          (NEW — BROWSE-02)
└── wwwroot/css/
    ├── account.mobile.css               (NEW — shared by all 5 Account views)
    ├── shop.mobile.css                  (NEW — Shop/Index only)
    └── guild-members.mobile.css         (NEW — GuildMembers/Index only)
```

### Pattern: Glass Card Container [VERIFIED: codebase — dm-create.mobile.css, dm-profile.mobile.css, home.mobile.css]

Every content section in a mobile view is wrapped in a glass card div. The CSS class name is page-specific (not reusing `.quest-section-card-mobile` from quests.mobile.css — each CSS file defines its own class to avoid cross-file coupling).

```css
/* Canonical glass card definition — replicate in each per-page CSS file */
.account-card-mobile {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 16px;
}
```

**Parchment text (inside glass cards):**
```css
.account-card-mobile .form-label,
.account-card-mobile h5,
.account-card-mobile h6 {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}
```

**Faded parchment (hints, secondary labels):**
```css
.account-card-mobile .form-text,
.account-card-mobile small {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}
```

**Badges — always suppress text-shadow:**
```css
.account-card-mobile .badge {
    text-shadow: none !important;
}
```

### Pattern: Section Styles Injection [VERIFIED: codebase — all existing .Mobile.cshtml files]

Every `.Mobile.cshtml` view starts with:
```razor
@section Styles {
    <link href="~/css/account.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

No `Layout` assignment in any mobile view — `_ViewStart.cshtml` handles layout selection. [VERIFIED: codebase — _ViewStart.cshtml]

### Pattern: Tap Navigation on List Rows [VERIFIED: codebase — Index.Mobile.cshtml (Home), Profile.Mobile.cshtml (DM)]

```razor
<div class="guild-member-row d-flex align-items-center p-3"
     onclick="window.location.href='@Url.Action("Details", new { id = character.Id })'">
```

No Bootstrap JS dependency — plain `onclick` attribute. Works reliably on iOS and Android.

### Pattern: No @inject in Mobile Views [VERIFIED: codebase — STATE.md decision log]

`_ViewImports.cshtml` globally injects `IAntiforgery`, `IAuthorizationService`, `IUserService`. Adding `@inject` in a `.Mobile.cshtml` view causes a duplicate injection compile error. Never add `@inject` directives in mobile views.

### Pattern: No @{} Wrapper in Foreach Bodies [VERIFIED: codebase — STATE.md decision log, Manage.Mobile.cshtml lines 100–105]

Declare C# variables directly in code mode inside `@foreach{}` bodies:
```razor
@foreach (var character in Model.MyCharacters)
{
    string roleBadge = character.Role == CharacterRole.Main ? "bg-success" : "bg-secondary";
    // ^ correct — no @{} wrapper needed
```

### Anti-Patterns to Avoid

- **Using `@{}`-wrapped variable declarations inside `@foreach`:** Razor returns to C# code mode after HTML content inside a code block — the `@{}` wrapper causes a compile error. Use direct C# assignment.
- **Adding `@inject` in any `.Mobile.cshtml` view:** Causes duplicate injection compile error — everything is globally available via `_ViewImports.cshtml`.
- **Setting `Layout` in a mobile view:** `_ViewStart.cshtml` handles layout selection. Individual view layout assignment breaks the expander contract.
- **Loading desktop CSS in mobile views:** `_Layout.Mobile.cshtml` deliberately does NOT load `site.css`, `shop.css`, `guild-members.css`, etc. Mobile views rely on `mobile.css` baseline + their own per-page CSS only.
- **Using `@media` queries in per-page mobile CSS files:** These files are loaded exclusively by mobile views — no media query needed. (See all existing `*.mobile.css` files, line 1 comment.)
- **Adding text-shadow to `.badge` elements inside glass cards:** Badges use colored backgrounds that become unreadable with text-shadow. Always add `.badge { text-shadow: none !important; }` targeting rule. [VERIFIED: codebase — all existing mobile CSS files]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Filter drawer / bottom sheet | Custom CSS + JS drawer | Bootstrap Offcanvas (`offcanvas-bottom`) | Already loaded via CDN in `_Layout.Mobile.cshtml`; no new JS required |
| Modal for Shop item details | New mobile modal | Reuse `#itemDetailsModal` verbatim from desktop | Modal JS listener reads `data-item-url` attribute — same attribute on mobile cards triggers same behavior |
| Pagination URL construction | Custom URL building | Copy `BuildTabUrl`, `BuildPageUrl`, `PageWindow` `@functions` verbatim | Functions already tested in production; query param names match controller binding |
| Touch targets | Custom CSS | `mobile.css` baseline already sets `min-height: 44px` on `.btn`, `input`, `select`, `textarea`, `.form-control`, `.form-select` | Don't redefine; baseline applies globally to all mobile views |
| Profile picture thumbnail | Inline `<img>` with data URL | `Url.Action("GetProfilePicture", new { id = character.Id })` | Controller method at `GuildMembersController.GetProfilePicture` returns `FileContentResult` |

---

## Desktop View Inventory (Exact Bindings)

### Account/Login.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model LoginViewModel`

**Using statements needed:**
```razor
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.AccountViewModels
```

**ViewModel properties:**
| Property | Type | Display Name | Validation |
|----------|------|-------------|-----------|
| `Email` | string | (auto from `[EmailAddress]`) | Required, EmailAddress |
| `Password` | string | (auto from `[DataType.Password]`) | Required |
| `RememberMe` | bool | "Remember me?" | None |

**Form action:** `asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]"` method POST

**Desktop structure to remove on mobile:** `<div class="row justify-content-center"><div class="col-md-6 col-lg-4">` — replace with single full-width card.

**Desktop already uses:** `d-grid gap-2` submit button, `btn-warning btn-lg` — carry these over unchanged.

**No returnUrl routing on Create Account link:** `asp-action="Register" asp-route-returnurl="@ViewData["ReturnUrl"]"` — preserve the returnUrl chaining.

---

### Account/Register.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model RegisterViewModel`

**ViewModel properties:**
| Property | Type | Display Name | Validation |
|----------|------|-------------|-----------|
| `Name` | string | "Name" | Required, StringLength(100) |
| `Email` | string | "Email" | Required, EmailAddress |
| `Password` | string | "Password" | Required, min 8 chars |
| `ConfirmPassword` | string | "Confirm password" | Compare("Password") |
| `IsDungeonMaster` | bool | "I want to be a Dungeon Master" | None |

**Note:** The desktop `Register.cshtml` does NOT render the `IsDungeonMaster` checkbox — it is present in the ViewModel but not exposed in the form. The mobile view should match this omission (do not add the checkbox). [VERIFIED: codebase — Register.cshtml read in full, no IsDungeonMaster field rendered]

**Password hint text:** `"Password must be at least 6 characters long."` — Note: the ViewModel annotation says `MinimumLength = 8` but the hint says 6. Preserve the desktop hint text verbatim: `"Password must be at least 6 characters long."`

**Form action:** `asp-action="Register" asp-route-returnurl="@ViewData["ReturnUrl"]"` method POST

**Desktop structure to remove:** `<div class="row justify-content-center"><div class="col-md-6 col-lg-5">` — replace with full-width card.

---

### Account/Edit.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model EditProfileViewModel`

**ViewModel properties:**
| Property | Type | Notes |
|----------|------|-------|
| `Id` | int | Hidden field — must be preserved: `<input asp-for="Id" type="hidden" />` |
| `Name` | string | Required |
| `Email` | string? | Optional, type="email" |
| `HasKey` | bool | Checkbox, Display Name: "Has Building Key" |
| `IsDungeonMaster` | bool | Not rendered in Edit form (admin-only concern) |

**Validation summary:** `asp-validation-summary="ModelOnly"` (not "All") — note difference from Login/Register which use "All".

**Form action:** `asp-action="Edit"` method POST (no returnUrl needed)

**Password Management section:** Desktop uses `btn-outline-primary` for Change Password link — UI-SPEC mandates replacing with `btn-warning` per CLAUDE.md button guidelines.

**Cancel link:** `asp-action="Profile"` (goes back to Profile page)

**Desktop structure to remove:** `<div class="row justify-content-center"><div class="col-md-6">` — full-width card.

---

### Account/Profile.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model ProfileViewModel`
**ProfileViewModel.User property type:** `EuphoriaInn.Domain.Models.User?`

**User model properties used:**
| Access | Value |
|--------|-------|
| `Model.User?.Name` | Display name |
| `Model.User?.Email` | Display email |
| `Model.User?.HasKey` | bool — building key status |

**ViewData keys:**
| Key | Type | Usage |
|-----|------|-------|
| `ViewData["IsAdmin"]` | `bool?` | Shows Admin badge with `fas fa-shield-alt text-danger` |
| `ViewData["IsDungeonMaster"]` | `bool?` | Shows DM badge with `fas fa-crown text-warning` |

**Success message:** `TempData["SuccessMessage"]` — rendered as `alert-success` with `fas fa-check-circle`. Only shown when non-null.

**Desktop layout to remove:** `<div class="row justify-content-center"><div class="col-md-6">` and `<div class="row mb-3"><label class="col-sm-3"><div class="col-sm-9">` label-value grid — replace with stacked single-column label-above-value pairs.

**No form on this page** — Profile is display-only. Buttons link to Edit (`asp-action="Edit"`) and Home (`asp-controller="Home" asp-action="Index"`).

---

### Account/ChangePassword.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model ChangePasswordViewModel`

**ViewModel properties:**
| Property | Type | Display Name | Validation |
|----------|------|-------------|-----------|
| `CurrentPassword` | string | "Current Password" | Required |
| `NewPassword` | string | "New Password" | Required, min 6 chars |
| `ConfirmPassword` | string | "Confirm New Password" | Compare("NewPassword") |

**Validation summary:** `asp-validation-summary="All"` — same as Login.

**Form action:** `asp-action="ChangePassword"` method POST

**Cancel link:** `asp-action="Profile"` → back to Profile page.

**Desktop submit button:** `btn-primary` (brown gradient via `mobile.css` override) with `fas fa-save` — preserve these classes.

**Desktop structure to remove:** `<div class="row justify-content-center"><div class="col-md-6">` — full-width card.

---

### Shop/Index.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model ShopIndexViewModel`

**Using statements needed:**
```razor
@using EuphoriaInn.Service.ViewModels.ShopViewModels
@using EuphoriaInn.Domain.Enums
@using Microsoft.AspNetCore.Http
```

**ShopIndexViewModel properties:**
| Property | Type | Notes |
|----------|------|-------|
| `Items` | `IList<ShopItemViewModel>` | The paginated page of items |
| `SelectedType` | `ItemType?` | null = All; Equipment, MagicItem, QuestItems |
| `SelectedRarities` | `IList<ItemRarity>` | Multi-select; Common, Uncommon, Rare, VeryRare, Legendary |
| `SelectedSort` | string? | null/"" = Default, "price_asc", "price_desc" |
| `SearchQuery` | string? | Free-text search |
| `CurrentPage` | int | Current pagination page (1-based) |
| `TotalPages` | int | Total pages |
| `TotalItems` | int | Total item count across all pages |
| `HasActiveSearch` | bool | computed: `!string.IsNullOrEmpty(SearchQuery)` |
| `HasActiveFilters` | bool | computed: rarities or sort or search active |
| `UserPurchases` | `IList<UserTransactionViewModel>` | OMIT on mobile (D-04) |

**ShopItemViewModel properties used in item cards:**
| Property | Notes |
|----------|-------|
| `Id` | Used in `Url.Action("Details", "Shop", new { id = item.Id, isModal = true })` |
| `Name` | Item name |
| `Description` | Used for short truncation (120 chars) |
| `Rarity` | `ItemRarity` enum; `.ToString().ToLower()` for CSS class; `.Replace("VeryRare", "Very Rare")` for display |
| `Price` | decimal — display as `@item.Price gp` |
| `Quantity` | int; -1 = unlimited, 0 = sold out, 1-5 = low |
| `Status` | `ItemStatus` enum; check `ItemStatus.Published` for buy button enabled state |

**@functions block (copy verbatim — lines 9–56):**

```csharp
@functions {
    string BuildTabUrl(ItemType? tabType, IList<ItemRarity> rarities, string? sort, string? search = null)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (tabType.HasValue)
            pairs.Add(new("type", tabType.Value.ToString()));
        foreach (var r in rarities)
            pairs.Add(new("rarity", r.ToString()));
        if (!string.IsNullOrEmpty(sort))
            pairs.Add(new("sort", sort));
        if (!string.IsNullOrEmpty(search))
            pairs.Add(new("search", search));
        // page intentionally omitted — tab switch resets to page 1
        return Url.Action("Index", "Shop") + Microsoft.AspNetCore.Http.QueryString.Create(pairs).Value;
    }

    string BuildPageUrl(int pageNumber)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (Model.SelectedType.HasValue)
            pairs.Add(new("type", Model.SelectedType.Value.ToString()));
        foreach (var r in Model.SelectedRarities)
            pairs.Add(new("rarity", r.ToString()));
        if (!string.IsNullOrEmpty(Model.SelectedSort))
            pairs.Add(new("sort", Model.SelectedSort));
        if (!string.IsNullOrEmpty(Model.SearchQuery))
            pairs.Add(new("search", Model.SearchQuery));
        if (pageNumber > 1)
            pairs.Add(new("page", pageNumber.ToString()));
        return Url.Action("Index", "Shop") + Microsoft.AspNetCore.Http.QueryString.Create(pairs).Value;
    }

    IEnumerable<int?> PageWindow(int current, int total)
    {
        var pages = new SortedSet<int> { 1, total };
        for (int i = Math.Max(1, current - 2); i <= Math.Min(total, current + 2); i++)
            pages.Add(i);

        int? prev = null;
        foreach (var p in pages)
        {
            if (prev.HasValue && p - prev.Value > 1)
                yield return null; // ellipsis
            yield return p;
            prev = p;
        }
    }
}
```

**Filter form query parameter names (for mobile filter drawer form):**
| HTML name attribute | Maps to | Notes |
|--------------------|---------|-------|
| `type` | `Model.SelectedType` | Single value, e.g. `"Equipment"`, `"MagicItem"`, `"QuestItems"` |
| `rarity` | `Model.SelectedRarities` | Multi-value; use multiple `<input name="rarity">` elements |
| `sort` | `Model.SelectedSort` | `""`, `"price_asc"`, `"price_desc"` |
| `search` | `Model.SearchQuery` | Free text |
| `page` | — | Always reset to `1` when applying filters |

**Category tab type names:** `ItemType.Equipment`, `ItemType.MagicItem`, `ItemType.QuestItems` — use `Enum.GetValues<ItemType>()` or hardcode these three values as per desktop.

**Rarity enum values:** `Enum.GetValues<ItemRarity>()` → Common, Uncommon, Rare, VeryRare, Legendary. In CSS class: `.rarity-@r.ToString().ToLower()`. In display: `.Replace("VeryRare", "Very Rare")`.

**Item card modal trigger attributes (copy exactly):**
```razor
data-bs-toggle="modal"
data-bs-target="#itemDetailsModal"
data-item-url="@Url.Action("Details", "Shop", new { id = item.Id, isModal = true })"
```

**#itemDetailsModal markup (lines 436–448 — copy verbatim):**
```html
<div class="modal fade" id="itemDetailsModal" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content bg-dark text-light">
            <div class="modal-header border-secondary">
                <h5 class="modal-title" id="itemDetailsTitle">Item Details</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="itemDetailsBody">
                <!-- Content will be loaded via AJAX -->
            </div>
        </div>
    </div>
</div>
```

**Modal JS event listener (lines 491–501 — copy verbatim into @section Scripts):**
```javascript
document.getElementById('itemDetailsModal').addEventListener('show.bs.modal', function (event) {
    const button = event.relatedTarget;
    const itemUrl = button.getAttribute('data-item-url');
    if (itemUrl) {
        fetch(itemUrl)
            .then(response => response.text())
            .then(html => document.getElementById('itemDetailsBody').innerHTML = html);
    }
});
```

**Toast container (lines 451–478):** Keep in mobile view — success/error toasts from Purchase POST redirects need to display. Copy toast container and toast initialization JS verbatim.

**Floating merchant element (lines 429–433):** OMIT on mobile — complex CSS + JS interaction, low value on small screens.

**Empty state variants:**
- `Model.HasActiveSearch == true` → "No items match your search" + Clear search link
- `Model.HasActiveFilters == true` → "No items match your filters" + Clear filters link
- Neither → "The shop is currently empty" — use simpler copy from UI-SPEC: "No items found" / "Try adjusting your filters or search query."

**Clear filters link condition:** `Model.SelectedRarities.Any() || Model.SelectedSort != null` — note: desktop checks only these two (not search) for the Clear link inside the filter form. The tab URL links handle search clearing separately.

**Pagination count formula:** `((Model.CurrentPage - 1) * 12 + 1)` to `Math.Min(Model.CurrentPage * 12, Model.TotalItems)` — hard-coded page size of 12.

---

### GuildMembers/Index.cshtml [VERIFIED: codebase — read in full]

**Model:** `@model CharactersIndexViewModel`

**Using statements needed:**
```razor
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Service.ViewModels.CharacterViewModels
```

**CharactersIndexViewModel properties:**
| Property | Type | Notes |
|----------|------|-------|
| `MyCharacters` | `IList<CharacterViewModel>` | Pre-sorted by controller: Main first, Active before Retired, then Name |
| `OtherCharacters` | `IList<CharacterViewModel>` | Pre-sorted: by OwnerName, Active before Retired, then Name |
| `CurrentUserId` | int | Not used in Index view — used in Details |

**CharacterViewModel properties used in Index:**
| Property | Type | Notes |
|----------|------|-------|
| `Id` | int | Used in `Url.Action("Details", new { id = character.Id })` and `Url.Action("GetProfilePicture", new { id = character.Id })` |
| `Name` | string | Character name |
| `Level` | int | Used in "Level @character.Level" |
| `Classes` | `List<CharacterClassViewModel>` | Joined: `string.Join(" / ", character.Classes.Select(c => $"{c.Class} {c.ClassLevel}"))` |
| `Status` | `CharacterStatus` enum | `CharacterStatus.Retired` → dim row with `opacity: 0.7` |
| `Role` | `CharacterRole` enum | `CharacterRole.Main` → show "Main" badge; in OtherCharacters: Role badge omitted (desktop also omits) |
| `ProfilePicture` | `byte[]?` | null = show placeholder; non-null = show `<img src="@Url.Action("GetProfilePicture", new { id = character.Id })" />` |
| `OwnerName` | string? | OtherCharacters only — shown as secondary meta |

**CharacterClassViewModel properties:**
| Property | Type | Notes |
|----------|------|-------|
| `Class` | `DndClass` enum | Rendered via `.ToString()` |
| `ClassLevel` | int | Rendered as integer |

**Route confirmation:** `Url.Action("Details", new { id = character.Id })` — no controller name needed; view is in GuildMembers folder → resolves to `GuildMembersController.Details`. [VERIFIED: codebase — GuildMembersController.cs line 48]

**GetProfilePicture route:** `Url.Action("GetProfilePicture", new { id = character.Id })` — method at `GuildMembersController` line 276, returns `FileContentResult`. [VERIFIED: codebase]

**Create character route:** `Url.Action("Create")` — resolves to `GuildMembersController.Create`. Desktop uses `href="@Url.Action("Create")"` on the button (line 14).

**Desktop card grid to replace:** `<div class="character-grid">` with `<div class="character-card">` inside — replaced entirely with flat list rows in mobile view.

---

## Common Pitfalls

### Pitfall 1: Duplicate @inject Directive
**What goes wrong:** Adding `@inject IAuthorizationService AuthorizationService` or `@inject IAntiforgery Antiforgery` in any `.Mobile.cshtml` causes a compile error: "A second type named 'X' has been registered."
**Why it happens:** `_ViewImports.cshtml` already injects all services globally.
**How to avoid:** Never add `@inject` in mobile views. All services are available without it.
**Warning signs:** If copying a desktop view's preamble section, look for `@inject` lines and delete them.

### Pitfall 2: @{} Wrapper Inside @foreach Body
**What goes wrong:** Razor compile error or silently wrong output.
**Why it happens:** Inside a `@foreach` C# code block, the parser is already in C# mode. An `@{}` block inside tries to start a new C# block, which is invalid syntax.
**How to avoid:** Declare variables directly: `string badge = condition ? "bg-success" : "bg-secondary";` — no `@{}` wrapper.
**Warning signs:** Any variable assignment line inside a `@foreach{}` body that begins with `@{`.

### Pitfall 3: Shop Modal Not Triggering on Mobile Item Cards
**What goes wrong:** Tapping item card does nothing; modal never opens.
**Why it happens:** The item card div needs both `data-bs-toggle="modal"` and `data-bs-target="#itemDetailsModal"` AND the `data-item-url` attribute. Missing any one of the three breaks the chain. The JS listener reads `event.relatedTarget` which is the element that triggered the modal — if it's the card div itself (not a button), `relatedTarget` is the card div. Confirm the `data-item-url` attribute is on the same element that has `data-bs-toggle="modal"`.
**How to avoid:** Copy the `data-*` attribute triple exactly from the desktop view. The "Buy" button in the mobile view is a separate button inside the card — it also needs the same three attributes and `onclick="event.stopPropagation()"`.
**Warning signs:** Test by tapping an item card after implementing — open browser DevTools Network tab and confirm a fetch to `/Shop/Details?id=X&isModal=true` fires.

### Pitfall 4: Filter Form Missing Hidden Fields Breaks Pagination
**What goes wrong:** Clicking page 2 resets all active filters (wrong URL built by BuildPageUrl).
**Why it happens:** `BuildPageUrl` reads from `Model.SelectedType`, `Model.SelectedRarities`, etc. — it does NOT read the form. But the offcanvas filter form must POST/GET correctly to set those model properties for the next page render.
**How to avoid:** The mobile filter form action must be `method="get" action="@Url.Action("Index", "Shop")"` and must include all the same hidden/input fields as the desktop filter form, including a `<input type="hidden" name="page" value="1" />` (resets page on filter apply). Pagination links are generated by `BuildPageUrl` which already includes all current model state.
**Warning signs:** After applying filters and clicking page 2, if the URL loses the `type=`, `rarity=`, or `sort=` parameters.

### Pitfall 5: Profile Picture img Tag Shows Broken Image
**What goes wrong:** `<img>` renders but shows broken image placeholder.
**Why it happens:** Using a non-null byte array as the src directly, instead of routing through the controller action.
**How to avoid:** Always use `Url.Action("GetProfilePicture", new { id = character.Id })` as the src. The controller returns `FileContentResult` with the correct MIME type.
**Warning signs:** Any `<img src="@character.ProfilePicture" />` pattern in code is wrong.

### Pitfall 6: Bootstrap Offcanvas for Shop Filters Collides with Nav Offcanvas
**What goes wrong:** Opening the filter drawer also opens the nav, or closing one closes both.
**Why it happens:** Both offcanvas elements share the same `data-bs-toggle="offcanvas"` mechanism. If IDs are not unique, Bootstrap may confuse them.
**How to avoid:** Use a distinct ID for the filter drawer, e.g. `id="shopFilterOffcanvas"` — different from the nav's `id="mobileNav"`. The trigger button's `data-bs-target="#shopFilterOffcanvas"` must match exactly.
**Warning signs:** Two offcanvas elements with the same ID on one page.

### Pitfall 7: Text-muted Override in mobile.css Breaks Parchment Context
**What goes wrong:** Inside a glass card, `.text-muted` renders as dark warm brown (`#2c1810`) instead of faded parchment — unreadable on the dark translucent background.
**Why it happens:** `mobile.css` line 40: `.text-muted { color: #2c1810 !important; }` — this applies globally including inside glass cards. On the notice board background this is correct; inside a glass card it is wrong.
**How to avoid:** Inside glass cards, override `.text-muted` to faded parchment: `rgba(244, 228, 188, 0.7)`. The per-page mobile CSS files should add this scoped override for elements inside glass card containers. Pattern established in `quests.mobile.css` (lines 27–31) and `dm-manage.mobile.css` (lines 44–48).
**Warning signs:** Any `class="text-muted"` inside a glass card div will render incorrectly without a scoped override.

---

## Code Examples

### Account Card Mobile (glass card form pattern)
```razor
@* Source: dm-create.mobile.css + Create.Mobile.cshtml pattern *@
<div class="account-card-mobile mb-3">
    <div class="mb-3">
        <h5 class="mb-0">
            <i class="fas fa-sign-in-alt text-warning me-2"></i>Log in
        </h5>
    </div>
    <div asp-validation-summary="All" class="text-danger mb-3"></div>
    <form asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]" method="post">
        <div class="mb-3">
            <label asp-for="Email" class="form-label"></label>
            <input asp-for="Email" class="form-control" />
            <span asp-validation-for="Email" class="text-danger"></span>
        </div>
        <div class="d-grid gap-2 mt-3">
            <button type="submit" class="btn btn-warning btn-lg">
                <i class="fas fa-sign-in-alt me-2"></i>Log in
            </button>
        </div>
    </form>
</div>
```

### Guild Members List Row Pattern
```razor
@* Source: CONTEXT.md specifics + UI-SPEC character list row *@
<div class="guild-member-row d-flex align-items-center p-3"
     onclick="window.location.href='@Url.Action("Details", new { id = character.Id })'">
    @if (character.ProfilePicture != null)
    {
        <img src="@Url.Action("GetProfilePicture", new { id = character.Id })"
             class="guild-member-thumbnail me-3"
             alt="@character.Name" />
    }
    else
    {
        <div class="guild-member-placeholder me-3">
            <i class="fas fa-user"></i>
        </div>
    }
    <div class="flex-grow-1">
        <div class="guild-member-name">@character.Name</div>
        <div class="guild-member-class">
            @string.Join(" / ", character.Classes.Select(c => $"{c.Class} {c.ClassLevel}"))
        </div>
        @if (character.Role == CharacterRole.Main)
        {
            <span class="badge bg-warning text-dark">
                <i class="fas fa-star me-1"></i>Main
            </span>
        }
    </div>
</div>
```

### Shop Filter Offcanvas Trigger
```razor
@* Source: UI-SPEC D-03, Bootstrap offcanvas-bottom pattern *@
@{
    bool hasActiveFilters = Model.SelectedRarities.Any()
        || Model.SelectedSort != null
        || !string.IsNullOrEmpty(Model.SearchQuery);
}
<button class="btn btn-warning w-100 mb-3"
        type="button"
        data-bs-toggle="offcanvas"
        data-bs-target="#shopFilterOffcanvas">
    <i class="fas fa-filter me-2"></i>Filter & Sort
    @if (hasActiveFilters)
    {
        <span class="badge bg-dark ms-1">Active</span>
    }
</button>

<div class="offcanvas offcanvas-bottom" id="shopFilterOffcanvas" tabindex="-1">
    <div class="offcanvas-header bg-dark text-white">
        <h5 class="offcanvas-title">Filter & Sort</h5>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="offcanvas"></button>
    </div>
    <div class="offcanvas-body">
        <form method="get" action="@Url.Action("Index", "Shop")">
            @if (Model.SelectedType != null)
            {
                <input type="hidden" name="type" value="@Model.SelectedType" />
            }
            <input type="hidden" name="page" value="1" />
            @* ... rarity checkboxes, sort select, search input ... *@
            <div class="d-grid gap-2 mt-3">
                <button type="submit" class="btn btn-warning">
                    <i class="fas fa-filter me-2"></i>Apply Filters
                </button>
            </div>
        </form>
    </div>
</div>
```

### Shop 2-Column Item Card
```razor
@* Source: UI-SPEC D-05 + desktop item card data-* attributes *@
<div class="shop-item-card-mobile"
     data-bs-toggle="modal"
     data-bs-target="#itemDetailsModal"
     data-item-url="@Url.Action("Details", "Shop", new { id = item.Id, isModal = true })">
    <div class="shop-item-name">@item.Name</div>
    <span class="item-rarity rarity-@item.Rarity.ToString().ToLower()">
        @item.Rarity.ToString().Replace("VeryRare", "Very Rare")
    </span>
    <div class="shop-item-price">
        <i class="fas fa-coins me-1"></i>@item.Price gp
    </div>
    @if (User.Identity?.IsAuthenticated == true)
    {
        <button type="button" class="btn btn-warning btn-sm w-100 mt-2"
                data-bs-toggle="modal"
                data-bs-target="#itemDetailsModal"
                data-item-url="@Url.Action("Details", "Shop", new { id = item.Id, isModal = true })"
                onclick="event.stopPropagation()"
                @(item.Status != EuphoriaInn.Domain.Enums.ItemStatus.Published || item.Quantity == 0 ? "disabled" : "")>
            <i class="fas fa-shopping-cart me-1"></i>Buy
        </button>
    }
    else
    {
        <a href="@Url.Action("Login", "Account")" class="btn btn-warning btn-sm w-100 mt-2"
           onclick="event.stopPropagation()">
            <i class="fas fa-lock me-1"></i>Login to Buy
        </a>
    }
</div>
```

---

## State of the Art

| Old Approach | Current Approach | Phase Established | Impact |
|--------------|------------------|-------------------|--------|
| No mobile views | `.Mobile.cshtml` variants auto-selected by view expander | Phase 12 | Zero desktop impact; additive only |
| Desktop `@media` queries | Per-page `.mobile.css` files loaded exclusively by mobile views | Phase 13 | No `@media` needed; simpler CSS |
| Global `mobile.css` for all mobile-specific rules | `mobile.css` = baseline only; per-page CSS adds specifics | Phase 13 | Avoids CSS bloat in baseline |
| `btn-outline-primary` (desktop) | `btn-warning` (filled, per CLAUDE.md) | Phase 13 | Consistent mobile button style |

**Deprecated/outdated:**
- Desktop `col-md-6`, `col-lg-4`, `col-sm-3 col-sm-9` grid wrappers — remove in all mobile views; use full-width single-column layout.
- Desktop `modern-card` / `modern-card-header` / `modern-card-body` Bootstrap card classes — replace with glass card CSS class pattern from per-page mobile CSS.
- Desktop `character-grid` CSS class — replace with flat list row structure in Guild Members mobile view.
- Purchase History panel (`purchase-history-panel`) — omit entirely on mobile per D-04.
- Floating merchant (`shop-merchant`) — omit on mobile (JavaScript + CSS complexity, low mobile value).

---

## Runtime State Inventory

> Not applicable — this is a greenfield additive phase (new files only, no renames or migrations).

---

## Environment Availability

> This phase is purely code/config changes (Razor views + CSS files). No external tools, databases, or services beyond the existing build chain are required.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Razor compilation | ✓ | 8.0 (per CLAUDE.md) | — |
| Bootstrap 5.3.0 | Offcanvas, modal, grid | ✓ | CDN in _Layout.Mobile.cshtml | — |
| Font Awesome 6.4.0 | Icons | ✓ | CDN in _Layout.Mobile.cshtml | — |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 + Microsoft.AspNetCore.Mvc.Testing 8.0.11 |
| Config file | `EuphoriaInn.IntegrationTests/` (no separate config; uses WebApplicationFactory) |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "Category=Mobile"` |
| Full suite command | `dotnet test EuphoriaInn.IntegrationTests` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | Notes |
|--------|----------|-----------|-------------------|-------|
| ACCT-01 | Login page serves `Login.Mobile.cshtml` on mobile UA | Integration (smoke) | `dotnet test --filter "ACCT01"` | GetWithUserAgentAsync("/Account/Login") |
| ACCT-02 | Register page serves `Register.Mobile.cshtml` on mobile UA | Integration (smoke) | `dotnet test --filter "ACCT02"` | GetWithUserAgentAsync("/Account/Register") |
| ACCT-03 | Edit/Profile/ChangePassword serve `.Mobile.cshtml` on mobile UA | Integration (smoke) | `dotnet test --filter "ACCT03"` | Requires authenticated request |
| BROWSE-01 | Shop index serves `Index.Mobile.cshtml` on mobile UA; contains filter button | Integration (smoke) | `dotnet test --filter "BROWSE01"` | Assert "Filter & Sort" text in response |
| BROWSE-02 | Guild Members serves `Index.Mobile.cshtml` on mobile UA | Integration (smoke) | `dotnet test --filter "BROWSE02"` | Requires authenticated request |

### Sampling Rate
- **Per task commit:** `dotnet test EuphoriaInn.IntegrationTests --filter "Category=Mobile" -x` (stop on first failure)
- **Per wave merge:** `dotnet test EuphoriaInn.IntegrationTests`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

The existing integration test infrastructure (`TestDatabase`, `TestDataHelper`, `GetWithUserAgentAsync`) covers mobile smoke tests. New test methods for ACCT-01 through BROWSE-02 must be added to the existing mobile test class (look for `MobileLayoutTests` or similar in `EuphoriaInn.IntegrationTests/`).

Pattern from prior phases (STATE.md):
- After creating `.Mobile.cshtml` files, run `dotnet build EuphoriaInn.IntegrationTests` before running tests — `WebApplicationFactory` uses compiled output.
- Tests for authenticated pages (Edit, Profile, ChangePassword, GuildMembers) require seeding a user and using an authenticated client.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | This phase creates views only; authentication logic is in AccountController (unchanged) |
| V3 Session Management | No | Session handled by existing middleware; views are passive |
| V4 Access Control | No | Authorization attributes remain on controllers; mobile views inherit same auth |
| V5 Input Validation | Yes — forms | `asp-validation-summary` + `asp-validation-for` + ViewModel data annotations handle server-side validation |
| V6 Cryptography | No | No cryptographic operations in views |

### Known Threat Patterns for Razor Views

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via model data in Razor | Tampering/Spoofing | Razor auto-encodes `@expression` output — do not use `@Html.Raw()` unless absolutely necessary |
| CSRF on form POSTs | Tampering | AntiForgery tokens auto-injected by `<form asp-action="...">` Tag Helper — do not use plain `<form action="">` |
| Open redirect via ReturnUrl | Tampering | ReturnUrl is passed through as-is to the form; AccountController validates it server-side — mobile view does not need additional validation |

---

## File Deliverable Checklist

| File | Type | Satisfies |
|------|------|-----------|
| `Views/Account/Login.Mobile.cshtml` | Razor view | ACCT-01 |
| `Views/Account/Register.Mobile.cshtml` | Razor view | ACCT-02 |
| `Views/Account/Edit.Mobile.cshtml` | Razor view | ACCT-03 |
| `Views/Account/Profile.Mobile.cshtml` | Razor view | ACCT-03 |
| `Views/Account/ChangePassword.Mobile.cshtml` | Razor view | ACCT-03 |
| `Views/Shop/Index.Mobile.cshtml` | Razor view | BROWSE-01 |
| `Views/GuildMembers/Index.Mobile.cshtml` | Razor view | BROWSE-02 |
| `wwwroot/css/account.mobile.css` | CSS | ACCT-01, 02, 03 |
| `wwwroot/css/shop.mobile.css` | CSS | BROWSE-01 |
| `wwwroot/css/guild-members.mobile.css` | CSS | BROWSE-02 |

**Total: 7 Razor views + 3 CSS files = 10 new files. Zero existing files modified.**

---

## Assumptions Log

> No assumptions — all findings were verified directly from the codebase.

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | All claims in this research were verified against live source files | — | — |

---

## Open Questions

1. **RegisterViewModel.IsDungeonMaster field**
   - What we know: The ViewModel has the field; the desktop view does not render it.
   - What's unclear: Is there a business reason the field was hidden (e.g., DM registration is admin-controlled)?
   - Recommendation: Do not add the `IsDungeonMaster` checkbox to the mobile Register view — match desktop behavior exactly.

2. **Shop pagination page size (12 items per page)**
   - What we know: Desktop pagination count formula uses hardcoded `12` (lines 423–424).
   - What's unclear: Whether 12 is configured server-side or hardcoded only in the view.
   - Recommendation: Copy the formula verbatim from desktop. If page size changes server-side, the formula will still work because `TotalItems` and `TotalPages` are model-driven.

---

## Sources

### Primary (HIGH confidence — codebase)
- `EuphoriaInn.Service/Views/Account/Login.cshtml` — ViewModel bindings, form action, desktop structure
- `EuphoriaInn.Service/Views/Account/Register.cshtml` — ViewModel bindings, IsDungeonMaster omission confirmed
- `EuphoriaInn.Service/Views/Account/Edit.cshtml` — ViewModel bindings, HasKey checkbox, hidden Id field
- `EuphoriaInn.Service/Views/Account/Profile.cshtml` — ProfileViewModel, ViewData keys, TempData["SuccessMessage"]
- `EuphoriaInn.Service/Views/Account/ChangePassword.cshtml` — ViewModel bindings, form action
- `EuphoriaInn.Service/Views/Shop/Index.cshtml` — @functions block (lines 9–56), modal markup (436–448), JS listener (491–501), filter form structure, empty state variants
- `EuphoriaInn.Service/Views/GuildMembers/Index.cshtml` — CharactersIndexViewModel, CharacterViewModel properties, profile picture URL pattern
- `EuphoriaInn.Service/ViewModels/AccountViewModels/*.cs` — all five ViewModels confirmed
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs` — ShopIndexViewModel properties
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopItemViewModel.cs` — ShopItemViewModel properties
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharactersIndexViewModel.cs` — two lists + CurrentUserId
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` — all properties including Classes list
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — 44px touch target rules, text-muted override
- `EuphoriaInn.Service/wwwroot/css/quests.mobile.css` — canonical glass card CSS, parchment text pattern
- `EuphoriaInn.Service/wwwroot/css/dm-profile.mobile.css` — section heading pattern, row divider pattern
- `EuphoriaInn.Service/wwwroot/css/dm-create.mobile.css` — form label parchment pattern, card padding
- `EuphoriaInn.Service/wwwroot/css/dm-manage.mobile.css` — text-muted scoped override in glass card
- `EuphoriaInn.Service/wwwroot/css/home.mobile.css` — badge text-shadow suppression pattern
- `EuphoriaInn.Service/Views/DungeonMaster/Profile.Mobile.cshtml` — section Styles injection pattern, dm-quest-history-item onclick pattern
- `EuphoriaInn.Service/Views/Quest/Create.Mobile.cshtml` — container-fluid + glass card pattern confirmed
- `EuphoriaInn.Service/Views/Quest/Manage.Mobile.cshtml` — no @inject confirmed, no @{} in foreach confirmed
- `EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml` — variable declaration in foreach body, onclick tap pattern
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — @section Styles/Scripts rendering confirmed, offcanvas nav ID confirmed as "mobileNav"
- `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` — GetProfilePicture action confirmed, Details route confirmed
- `.planning/STATE.md` — all accumulated "no @inject", "no @{} in foreach", "rebuild before test" decisions

---

## Metadata

**Confidence breakdown:**
- ViewModel bindings: HIGH — read directly from source files
- Glass card CSS values: HIGH — read from existing mobile CSS files
- Shop @functions block: HIGH — copied verbatim from source; no interpretation needed
- Modal JS block: HIGH — lines 491–501 identified precisely in source
- Route patterns: HIGH — confirmed from controller source

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (stable codebase; no external dependencies to expire)
