---
phase: 19-admin-shop-management-views
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 16
files_reviewed_list:
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
  - EuphoriaInn.Service/Views/Admin/Users.Mobile.cshtml
  - EuphoriaInn.Service/Views/Admin/Quests.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/admin-users.mobile.css
  - EuphoriaInn.Service/wwwroot/css/admin-quests.mobile.css
  - EuphoriaInn.Service/wwwroot/css/admin-form.mobile.css
  - EuphoriaInn.Service/Views/Admin/EditUser.Mobile.cshtml
  - EuphoriaInn.Service/Views/Admin/ResetPassword.Mobile.cshtml
  - EuphoriaInn.Service/Views/ShopManagement/Create.Mobile.cshtml
  - EuphoriaInn.Service/Views/ShopManagement/Edit.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/shop-management-create.mobile.css
  - EuphoriaInn.Service/wwwroot/css/shop-management-edit.mobile.css
  - EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css
  - EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css
findings:
  critical: 4
  warning: 5
  info: 3
  total: 12
status: issues_found
---

# Phase 19: Code Review Report

**Reviewed:** 2026-06-25T00:00:00Z
**Depth:** standard
**Files Reviewed:** 16
**Status:** issues_found

## Summary

Phase 19 delivers mobile views for Admin (Users, Quests, EditUser, ResetPassword) and ShopManagement (Create, Edit, Index) plus the Shop Details mobile page, accompanied by per-view CSS files and integration tests. The overall structure follows the established mobile pattern correctly. However, there are four blocker-level defects: a Razor `disabled` attribute that does not work as the developer intends (silently breaking the availability-window toggle on the Edit form), an XSS vector through unescaped `innerHTML` injection in the price-suggestion widgets, a missing `max` constraint on the purchase quantity input that allows a buyer to over-purchase stock, and a CSS global selector leak in `shop-details.mobile.css` that will corrupt parchment text styling site-wide. There are also five warnings around missing CSRF protection on role-mutation forms, missing `[Authorize]` guarding on the Deny button pathway, a dead bulk-actions modal with client-side placeholder code, a redundant unnecessary import in two views, and an inconsistency in how the "Done" quest-status threshold is computed vs. the rest of the application.

---

## Critical Issues

### CR-01: `disabled` Razor attribute evaluates incorrectly — availability dates always render enabled on Edit form

**File:** `EuphoriaInn.Service/Views/ShopManagement/Edit.Mobile.cshtml:131-136`

**Issue:** The two `<input>` elements for `AvailableFrom` and `AvailableUntil` use:

```razor
disabled="@(Model.AvailableFrom == null && Model.AvailableUntil == null)"
```

In Razor/HTML, the `disabled` boolean attribute is rendered as the _string_ `"False"` or `"True"`. Browsers treat _any_ non-empty string value for `disabled` as disabling the input — including the string `"False"`. This means when an item already has availability dates set (`AvailableFrom != null`), the expression evaluates to `false`, Razor renders `disabled="False"`, and the browser still treats the field as disabled. The DM cannot edit existing availability windows because the inputs are always disabled regardless of whether dates exist. A DM editing an item that already has an `AvailableFrom` set will see the dates displayed in the checkbox and enabled fields, but the inputs will be disabled and the dates will not submit.

**Fix:** Use the conditional `disabled` pattern that Razor recognises — emit the attribute only when needed:

```razor
@* AvailableFrom *@
<input asp-for="AvailableFrom" type="datetime-local" class="form-control"
       @(Model.AvailableFrom == null && Model.AvailableUntil == null ? "disabled" : "") />

@* AvailableUntil *@
<input asp-for="AvailableUntil" type="datetime-local" class="form-control"
       @(Model.AvailableFrom == null && Model.AvailableUntil == null ? "disabled" : "") />
```

Or use the `disabled` tag helper approach:
```razor
<input asp-for="AvailableFrom" type="datetime-local" class="form-control"
       disabled="@(Model.AvailableFrom == null && Model.AvailableUntil == null ? (bool?)true : null)" />
```

---

### CR-02: XSS via `innerHTML` injection in price-suggestion widget

**File:** `EuphoriaInn.Service/Views/ShopManagement/Create.Mobile.cshtml:171`
**File:** `EuphoriaInn.Service/Views/ShopManagement/Edit.Mobile.cshtml:210`

**Issue:** The `updatePriceSuggestion()` function writes user-controlled data into the DOM via `innerHTML`:

```js
suggestionDiv.innerHTML = `<i class="fas fa-coins me-1"></i>${guide.text}<br>
    <small class="text-muted">Tasha's recommended: ${calculatedPrice} gp</small>`;
```

While `guide.text` and `calculatedPrice` come from the hardcoded `pricingGuide`/`calculatedPrices` objects in the same script block and are not attacker-controlled in the current implementation, the `rarity` value feeding `pricingGuide[rarity]` comes from a `<select>` element whose `value` is constrained by server-rendered `<option>` elements. However, the guard `if (rarity && pricingGuide[rarity])` prevents the `innerHTML` write when rarity is not a known key, so the actual attack surface is limited.

The real problem is the pattern itself: both the Create and Edit views use `innerHTML` when `textContent` or `insertAdjacentHTML` with escaped content would be safer. If the `pricingGuide` object is ever extended to include user-supplied data (e.g., if a server-side rendered value were ever templated in), this becomes a direct XSS vector. Per the adversarial review standard, using `innerHTML` with dynamic string interpolation is a security defect that must be addressed before it can be exploited by a later change.

**Fix:** Replace the `innerHTML` assignment with `textContent` for the text portions and append the icon separately, or use a sanitised string construction:

```js
suggestionDiv.textContent = '';
const icon = document.createElement('i');
icon.className = 'fas fa-coins me-1';
suggestionDiv.appendChild(icon);
suggestionDiv.appendChild(document.createTextNode(guide.text));
const small = document.createElement('small');
small.className = 'text-muted';
small.textContent = `Tasha's recommended: ${calculatedPrice} gp`;
suggestionDiv.appendChild(document.createElement('br'));
suggestionDiv.appendChild(small);
```

---

### CR-03: Purchase quantity input has no `max` attribute — allows over-purchasing beyond available stock

**File:** `EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml:83`

**Issue:** The purchase form renders:

```html
<input type="number" name="Quantity" id="Quantity" class="form-control" value="1" min="1" />
```

There is no `max` attribute. A buyer can type any positive integer and submit a quantity exceeding the item's actual stock. The server-side `PurchaseItemAsync` should enforce the stock limit, but the absence of a client-side `max` creates a confusing user experience and also means the UI actively enables the mistake. More critically, if `Model.Quantity` is `-1` (unlimited stock), there is no upper bound at all — which is correct behaviour for unlimited items — but for limited-stock items (`Model.Quantity > 0`) the UI gives no feedback that the quantity is bounded, leading to server-side errors that appear as generic `TempData["Error"]` messages.

Additionally, the condition `Model.Quantity != 0` shows the form for `Model.Quantity == -1` (unlimited) and positive values, which is correct. However, the `Quantity` field accepts `0` as a submitted value because `min="1"` is a hint only — the form will still submit with `quantity=0` if JavaScript is disabled.

**Fix:** For limited-stock items, bind `max` to the model quantity:

```razor
@if (Model.Status == ItemStatus.Published && Model.Quantity != 0)
{
    var maxQty = Model.Quantity > 0 ? Model.Quantity.ToString() : "";
    <form method="post" asp-controller="Shop" asp-action="Purchase" asp-route-id="@Model.Id">
        @Html.AntiForgeryToken()
        <div class="mb-3">
            <label for="Quantity" class="form-label parchment-text">Quantity</label>
            <input type="number" name="Quantity" id="Quantity" class="form-control"
                   value="1" min="1" @(maxQty != "" ? $"max=\"{maxQty}\"" : "") />
        </div>
        <button type="submit" class="btn btn-primary w-100">
            <i class="fas fa-shopping-cart me-2"></i>Purchase Item
        </button>
    </form>
}
```

---

### CR-04: Global CSS selector leak — `.parchment-text` and `.parchment-text-muted` applied site-wide from a view-scoped stylesheet

**File:** `EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css:15-26`

**Issue:** The stylesheet is documented as "exclusively loaded by Shop/Details.Mobile.cshtml" but defines bare (non-scoped) class selectors:

```css
.parchment-text {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}

.parchment-text-muted {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}
```

These class names (`.parchment-text`, `.parchment-text-muted`) are used broadly across multiple mobile views (e.g., `Users.Mobile.cshtml`, `Quests.Mobile.cshtml`, `Index.Mobile.cshtml`). Because they are defined globally in a per-view file, if this stylesheet were ever loaded on a page that also includes elements with those class names but where a different visual treatment is intended, the `!important` declarations will silently win. More concretely, if the mobile layout ever loads both `shop-details.mobile.css` and another view's styles in the same rendering pipeline (e.g., via a shared partial), the `!important` declarations here will override the other view's parchment styling.

The scoped rule `EuphoriaInn.Service/wwwroot/css/admin-users.mobile.css` correctly scopes rules under `.admin-users-card-mobile .parchment-text` — this file does not follow that pattern.

**Fix:** Scope both selectors under the view's root class:

```css
.shop-details-card-mobile h5,
.shop-details-card-mobile .parchment-text {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}

.shop-details-card-mobile .parchment-text-muted,
.shop-details-card-mobile small {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}
```

---

## Warnings

### WR-01: Role-mutation forms in Users.Mobile.cshtml lack CSRF token inputs

**File:** `EuphoriaInn.Service/Views/Admin/Users.Mobile.cshtml:50-87`

**Issue:** The four role-management forms (PromoteToAdmin, PromoteToDM, DemoteFromAdmin, DemoteToPlayer) do not use `asp-antiforgery="true"` or `@Html.AntiForgeryToken()`, and do not include the `asp-controller` tag helper. The controller actions at `AdminController.cs:41,58,74,89` all carry `[ValidateAntiForgeryToken]`. The `asp-action` tag helpers on the forms will generate a hidden `__RequestVerificationToken` field automatically via the `FormTagHelper` when inside a form with `method="post"` — this is the ASP.NET Core default behaviour. However, this relies on the Tag Helper infrastructure being active. The forms also have no explicit `asp-controller` attribute, which means they will route to whatever controller is rendering the view; if the layout or routing ever changes, these could silently target the wrong controller.

Verify that `FormTagHelper` anti-forgery injection is confirmed active for this layout. Additionally, add explicit `asp-controller="Admin"` to each form to make routing unambiguous.

**Fix:**
```razor
<form asp-controller="Admin" asp-action="PromoteToAdmin" method="post" class="d-inline">
    <input type="hidden" name="userId" value="@userModel.User.Id" />
    <button type="submit" class="btn btn-sm btn-danger">...</button>
</form>
```

---

### WR-02: Deny button in Index.Mobile.cshtml is not authorization-gated — any DM can see and use it

**File:** `EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml:93-100`

**Issue:** The Deny button for Draft items is rendered for all users viewing the page:

```razor
@if (item.Status == ItemStatus.Draft)
{
    <button type="button" class="btn btn-danger btn-sm" ...
            data-bs-toggle="modal" data-bs-target="#denyModal"
            data-item-id="@item.Id" data-item-name="@item.Name">
        <i class="fas fa-times"></i>
    </button>
}
```

The Publish button at line 101 is guarded by `AdminOnly` policy, but the Deny button has no authorization check. If the `/ShopManagement/Deny/{id}` endpoint also enforces `AdminOnly`, then a non-admin DM who clicks Deny will get a 403 — but the button will still be visible to them, creating a confusing UI and potentially leaking that a Deny action exists. If the Deny endpoint is not `AdminOnly` (it is not shown in scope), any DM could deny any other DM's item. The desktop version of this view should be cross-referenced to confirm whether Deny is admin-only there too.

**Fix:** Wrap the Deny button in the same authorization check as Publish:

```razor
@if (item.Status == ItemStatus.Draft && (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
{
    <button type="button" class="btn btn-danger btn-sm" ...>
        <i class="fas fa-times"></i>
    </button>
}
```

---

### WR-03: Dead bulk-actions modal with non-functional JavaScript in Index.Mobile.cshtml

**File:** `EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml:133-162,207-210`

**Issue:** A `#bulkActionsModal` Bootstrap modal is present in the rendered HTML, but there is no button anywhere in the view that triggers it (`data-bs-target="#bulkActionsModal"` appears nowhere). The modal body explicitly says "Note: Bulk actions will be available in a future update." The `bulkAction(action)` JavaScript function calls `alert(...)` and does nothing else. This dead UI fragment adds HTML weight to every page load, leaks a feature intention to users, and the placeholder `alert()` calls would degrade user trust if the trigger were ever accidentally added. 

**Fix:** Remove the `#bulkActionsModal` element and the `bulkAction` JavaScript function entirely until the feature is implemented. If the modal must be retained for design sign-off, gate it with an HTML comment, not live DOM.

---

### WR-04: "Done" quest status threshold off-by-one vs. application convention

**File:** `EuphoriaInn.Service/Views/Admin/Quests.Mobile.cshtml:26`

**Issue:** The quest status logic marks a quest as "Done" when:

```csharp
quest.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date
```

This means a quest finalized today (same UTC date) shows as "Finalized" (primary badge), and only shows "Done" after one day has elapsed. The semantics of `AddDays(-1)` mean yesterday-or-earlier equals Done. This is a non-obvious threshold choice that differs from the typical convention (`< DateTime.UtcNow.Date`, i.e., any date strictly in the past). If the rest of the application uses `FinalizedDate < DateTime.UtcNow` to determine a quest is in the past (as seen in the home page quest filter), this view will show "Finalized" for a quest that the home board no longer displays, creating a status inconsistency visible only to admins.

**Fix:** Align with the application's existing "past quest" semantics (verify against the quest repository filter) and use:

```csharp
if (quest.IsFinalized && quest.FinalizedDate.HasValue && quest.FinalizedDate.Value < DateTime.UtcNow)
```

---

### WR-05: `ResetPassword.Mobile.cshtml` password fields render as plain text inputs

**File:** `EuphoriaInn.Service/Views/Admin/ResetPassword.Mobile.cshtml:27-33`

**Issue:** The `NewPassword` and `ConfirmPassword` fields use `asp-for` without specifying `type="password"`:

```razor
<input asp-for="NewPassword" class="form-control" />
<input asp-for="ConfirmPassword" class="form-control" />
```

The `asp-for` Tag Helper infers `type="password"` from the `[DataType(DataType.Password)]` attribute on the view model properties, so this should work correctly at runtime. However, the behavior is dependent on the Tag Helper correctly reading `DataType.Password`. If for any reason the Tag Helper resolution fails or the attribute is removed from the view model, the inputs silently degrade to `type="text"`, exposing passwords in plaintext in the browser. Explicitly declaring `type="password"` is defensive coding practice for any password input.

Compare to `Create.Mobile.cshtml` which explicitly sets `type="number"`, `type="url"`, etc. for all non-text inputs. Password inputs warrant the same explicitness.

**Fix:**
```razor
<input asp-for="NewPassword" class="form-control" type="password" />
<input asp-for="ConfirmPassword" class="form-control" type="password" />
```

---

## Info

### IN-01: Unused `@using EuphoriaInn.Domain.Interfaces` import in Admin mobile views

**File:** `EuphoriaInn.Service/Views/Admin/Users.Mobile.cshtml:1`
**File:** `EuphoriaInn.Service/Views/Admin/Quests.Mobile.cshtml:1`

**Issue:** Both views open with `@using EuphoriaInn.Domain.Interfaces`. The `IUserService` and `IQuestService` interfaces from that namespace are already injected globally via `_ViewImports.cshtml` (`@inject IUserService UserService`) and used in the layout. Neither mobile partial view uses any symbol from `EuphoriaInn.Domain.Interfaces` directly — the views only reference `UserManagementViewModel` (from `EuphoriaInn.Service.ViewModels.AdminViewModels`) and `Quest` (from `EuphoriaInn.Domain.Models.QuestBoard`). The `@using EuphoriaInn.Domain.Interfaces` directive is redundant dead code.

**Fix:** Remove the `@using EuphoriaInn.Domain.Interfaces` line from both files.

---

### IN-02: Integration test for ADMIN-01 (Admin Users) uses duplicate section comment label

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:793-796`

**Issue:** The comment block at line 793 reads:

```csharp
// Phase 19 — ADMIN-01: Admin Quests list renders mobile layout
```

But the test below it (`GetMobilePage_AdminQuests_ReturnsSuccessAndMobileLayout`) is the correct Quests test. The preceding section comment at line 768 also uses `ADMIN-01` for the Users test, creating two separate `ADMIN-01` labels. The UAT requirement IDs appear to be `ADMIN-01` for the Users list and `ADMIN-02` for ShopManagement — the Quests list test may belong under a distinct ID (e.g., `ADMIN-01b`). This is a documentation/traceability gap that makes it harder to cross-reference test results against phase requirements.

**Fix:** Normalise the comment to a distinct requirement ID, e.g.:

```csharp
// Phase 19 — ADMIN-01b: Admin Quests list renders mobile layout
```

---

### IN-03: Empty `shop-mgmt-empty-state` CSS class defined but never used in Index.Mobile.cshtml

**File:** `EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css:50-71`
**File:** `EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml:124-130`

**Issue:** The CSS file defines `.shop-mgmt-empty-state` with sub-selectors for `i`, `h5`, and `p`. The empty-state HTML in the view uses inline styles and Bootstrap classes rather than the `.shop-mgmt-empty-state` class:

```razor
<div class="text-center py-3">
    <i class="fas fa-box-open fa-3x mb-2" style="color: rgba(244, 228, 188, 0.7);"></i>
    <h5 class="parchment-text">No items yet</h5>
    <p class="text-muted">Add the first item to your shop.</p>
</div>
```

The `.shop-mgmt-empty-state` class is dead CSS — it is never applied in the template. This also means the empty-state icon color is set via an inline style (`style="color: rgba(244, 228, 188, 0.7);"`) instead of the CSS file's rule for `.shop-mgmt-empty-state i`, which is inconsistent with the rest of the project's approach of avoiding inline styles.

**Fix:** Either apply the class to the empty-state container:
```razor
<div class="shop-mgmt-empty-state py-3">
```
Or remove the dead CSS block from `shop-management-index.mobile.css`.

---

_Reviewed: 2026-06-25T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
