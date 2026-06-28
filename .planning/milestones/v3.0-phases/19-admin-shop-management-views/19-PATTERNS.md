# Phase 19: Admin & Shop Management Views — Pattern Map

**Mapped:** 2026-06-25
**Files analyzed:** 15 (8 views + 7 CSS files)
**Analogs found:** 15 / 15

---

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|----------|------|-----------|----------------|---------------|
| `Views/Admin/Users.Mobile.cshtml` | view (list) | request-response | `Views/Admin/Users.cshtml` + `Views/Players/Index.Mobile.cshtml` | exact |
| `Views/Admin/Quests.Mobile.cshtml` | view (list) | request-response | `Views/Admin/Quests.cshtml` + `Views/Players/Index.Mobile.cshtml` | exact |
| `Views/Admin/EditUser.Mobile.cshtml` | view (form) | request-response | `Views/Admin/EditUser.cshtml` + `Views/Quest/Edit.Mobile.cshtml` | exact |
| `Views/Admin/ResetPassword.Mobile.cshtml` | view (form) | request-response | `Views/Admin/ResetPassword.cshtml` + `Views/Quest/Edit.Mobile.cshtml` | exact |
| `Views/ShopManagement/Index.Mobile.cshtml` | view (list + modals) | request-response | `Views/ShopManagement/Index.cshtml` + `Views/Shop/Index.Mobile.cshtml` | exact |
| `Views/ShopManagement/Create.Mobile.cshtml` | view (form) | request-response | `Views/ShopManagement/Create.cshtml` + `Views/Quest/Edit.Mobile.cshtml` | exact |
| `Views/ShopManagement/Edit.Mobile.cshtml` | view (form) | request-response | `Views/ShopManagement/Edit.cshtml` + `Views/ShopManagement/Create.cshtml` | exact |
| `Views/Shop/Details.Mobile.cshtml` | view (detail) | request-response | `Views/Shop/Details.cshtml` + `Views/Shop/Index.Mobile.cshtml` | exact |
| `wwwroot/css/admin-users.mobile.css` | CSS | static | `wwwroot/css/quest-edit.mobile.css` | role-match |
| `wwwroot/css/admin-quests.mobile.css` | CSS | static | `wwwroot/css/quest-edit.mobile.css` | role-match |
| `wwwroot/css/admin-form.mobile.css` | CSS | static | `wwwroot/css/quest-edit.mobile.css` | role-match |
| `wwwroot/css/shop-management-index.mobile.css` | CSS | static | `wwwroot/css/shop.mobile.css` | role-match |
| `wwwroot/css/shop-management-create.mobile.css` | CSS | static | `wwwroot/css/quest-edit.mobile.css` | role-match |
| `wwwroot/css/shop-management-edit.mobile.css` | CSS | static | `wwwroot/css/quest-edit.mobile.css` | role-match |
| `wwwroot/css/shop-details.mobile.css` | CSS | static | `wwwroot/css/shop.mobile.css` | role-match |

---

## Pattern Assignments

### `Views/Admin/Users.Mobile.cshtml` (view, list, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/Admin/Users.cshtml`
**Mobile list analog:** `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml`

**Model declaration + antiforgery setup** (`Users.cshtml` lines 1–8):
```razor
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.AdminViewModels
@model IEnumerable<UserManagementViewModel>

@{
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}
```
Note: Do NOT add `@inject` — `IAntiforgery` is globally injected via `_ViewImports.cshtml`.

**CSS injection pattern** (from `Quest/Edit.Mobile.cshtml` line 8–10):
```razor
@section Styles {
    <link href="~/css/admin-users.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Card-per-item loop pattern** (from `Players/Index.Mobile.cshtml` lines 18–29 — adapt for users):
```razor
@foreach (var userModel in Model)
{
    <div class="user-card-mobile mb-3">
        <div class="d-flex align-items-center mb-2">
            @* Role icon + name + badge *@
            <i class="fas fa-user text-muted me-2"></i>
            <strong class="parchment-text">@userModel.User.Name</strong>
            @* role badge — copy conditional from Users.cshtml lines 49–70 *@
        </div>
        <div class="d-flex flex-wrap gap-2">
            @* stacked action buttons — copy conditionals from Users.cshtml lines 73–130 *@
        </div>
    </div>
}
```

**Role badge conditionals** (`Users.cshtml` lines 49–69):
```razor
@if (userModel.IsAdmin)
{
    <span class="badge bg-danger">
        <i class="fas fa-shield-alt me-1"></i>Administrator
    </span>
}
else if (userModel.IsDungeonMaster)
{
    <span class="badge bg-warning">
        <i class="fas fa-crown me-1"></i>Dungeon Master
    </span>
}
else if (userModel.IsPlayer)
{
    <span class="badge bg-primary">
        <i class="fas fa-dice-d20 me-1"></i>Player
    </span>
}
```

**Promote/Demote form buttons** (`Users.cshtml` lines 73–118) — copy all four `<form asp-action="...">` blocks verbatim; they use tag helpers which work unchanged on mobile.

**Edit + Delete buttons** (`Users.cshtml` lines 121–130):
```razor
<a asp-action="EditUser" asp-route-userId="@userModel.User.Id" class="btn btn-sm btn-info">
    <i class="fas fa-edit me-1"></i>Edit
</a>
<button type="button" class="btn btn-danger btn-sm" onclick="deleteUser(@userModel.User.Id)">
    <i class="fas fa-trash"></i>Delete
</button>
```

**deleteUser() JS** (`Users.cshtml` lines 148–165) — move to `@section Scripts`:
```razor
@section Scripts {
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
}
```

**Empty state pattern** (`Users.cshtml` lines 140–144):
```razor
<div class="text-center py-5">
    <i class="fas fa-users fa-3x text-muted mb-3"></i>
    <p class="text-muted">No users found.</p>
</div>
```

---

### `Views/Admin/Quests.Mobile.cshtml` (view, list, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/Admin/Quests.cshtml`

**Model declaration + antiforgery setup** (`Quests.cshtml` lines 1–8):
```razor
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Domain.Models.QuestBoard
@model IEnumerable<Quest>

@{
    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
}
```

**Status badge logic** (`Quests.cshtml` lines 61–88) — copy the `@{ string statusBadge; ... }` block verbatim:
```razor
@{
    string statusBadge;
    string statusIcon;
    string statusText;

    if (quest.IsFinalized && quest.FinalizedDate.HasValue && quest.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date)
    {
        statusBadge = "bg-dark";
        statusIcon = "fas fa-flag-checkered";
        statusText = "Done";
    }
    else if (quest.IsFinalized)
    {
        statusBadge = "bg-primary";
        statusIcon = "fas fa-check-circle";
        statusText = "Finalized";
    }
    else
    {
        statusBadge = "bg-success";
        statusIcon = "fas fa-clock";
        statusText = "Open";
    }
}
<span class="badge @statusBadge">
    <i class="@statusIcon me-1"></i>@statusText
</span>
```

**DM name display** (`Quests.cshtml` line 52–53):
```razor
<small class="text-muted">@(quest.DungeonMaster?.Name ?? "Unknown")</small>
```

**Edit + Delete buttons** (`Quests.cshtml` lines 92–100):
```razor
<a asp-controller="Quest" asp-action="Edit" asp-route-id="@quest.Id" class="btn btn-sm btn-info">
    <i class="fas fa-edit me-1"></i>Edit
</a>
<button type="button" class="btn btn-danger btn-sm" onclick="deleteQuest(@quest.Id)">
    <i class="fas fa-trash"></i>Delete
</button>
```

**deleteQuest() JS** (`Quests.cshtml` lines 120–135) — move to `@section Scripts`:
```razor
@section Scripts {
    <script>
        function deleteQuest(id) {
            if (confirm("Are you sure you want to delete this quest? This action cannot be undone.")) {
                fetch(`/Admin/DeleteQuest/${id}`, {
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
}
```

---

### `Views/Admin/EditUser.Mobile.cshtml` (view, form, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/Admin/EditUser.cshtml`
**Mobile form analog:** `EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml`

**CSS injection:**
```razor
@section Styles {
    <link href="~/css/admin-form.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Model + ViewData** (`EditUser.cshtml` lines 1–6):
```razor
@using EuphoriaInn.Service.ViewModels.AdminViewModels
@model EditUserViewModel
@{
    ViewData["Title"] = "Edit User";
}
```

**Glass card container** (mirror `Quest/Edit.Mobile.cshtml` lines 12–23, but use `admin-form-card-mobile` class):
```razor
<div class="admin-form-card-mobile mb-3">
    <div class="mb-3">
        <h5 class="mb-0">
            <i class="fas fa-user-edit text-warning me-2"></i>Edit User
        </h5>
    </div>
    <form asp-action="EditUser" method="post">
        ...
    </form>
</div>
```

**Form fields** (`EditUser.cshtml` lines 22–73) — copy all form fields verbatim; add `col-12` to inputs for full-width. Key fields:
```razor
<input asp-for="Id" type="hidden" />

<div class="mb-3">
    <label asp-for="Name" class="form-label"></label>
    <input asp-for="Name" class="form-control" />
    <span asp-validation-for="Name" class="text-danger"></span>
</div>
<div class="mb-3">
    <label asp-for="Email" class="form-label"></label>
    <input asp-for="Email" class="form-control" type="email" />
    <span asp-validation-for="Email" class="text-danger"></span>
</div>
<div class="mb-3">
    <div class="form-check">
        <input asp-for="HasKey" class="form-check-input" type="checkbox" />
        <label asp-for="HasKey" class="form-check-label">
            <i class="fas fa-key text-success me-2"></i>User has a building key (can close the building)
        </label>
    </div>
</div>
```

**Reset Password link + button row** (`EditUser.cshtml` lines 51–73):
```razor
<a asp-action="ResetPassword" asp-route-userId="@Model.Id" class="btn btn-outline-danger">
    <i class="fas fa-key me-2"></i>Reset Password
</a>

<hr>

<div class="d-flex justify-content-between">
    <a asp-action="Users" class="btn btn-secondary">
        <i class="fas fa-arrow-left me-2"></i>Back to Users
    </a>
    <button type="submit" class="btn btn-success">
        <i class="fas fa-save me-2"></i>Save Changes
    </button>
</div>
```

---

### `Views/Admin/ResetPassword.Mobile.cshtml` (view, form, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/Admin/ResetPassword.cshtml`

**CSS injection** (same shared file as EditUser):
```razor
@section Styles {
    <link href="~/css/admin-form.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Model declaration** (`ResetPassword.cshtml` lines 1–3):
```razor
@using EuphoriaInn.Service.ViewModels.AdminViewModels
@model ResetPasswordViewModel
```

**Hidden fields + form inputs** (`ResetPassword.cshtml` lines 19–35):
```razor
<input asp-for="UserId" type="hidden" />
<input asp-for="UserName" type="hidden" />

<div class="mb-3">
    <label asp-for="NewPassword" class="form-label"></label>
    <input asp-for="NewPassword" class="form-control" />
    <span asp-validation-for="NewPassword" class="text-danger"></span>
</div>
<div class="mb-3">
    <label asp-for="ConfirmPassword" class="form-label"></label>
    <input asp-for="ConfirmPassword" class="form-control" />
    <span asp-validation-for="ConfirmPassword" class="text-danger"></span>
</div>
```

**Warning alert — keep verbatim** (D-06; `ResetPassword.cshtml` lines 37–40):
```razor
<div class="alert alert-warning">
    <i class="fas fa-exclamation-triangle me-2"></i>
    <strong>Warning:</strong> This will reset the password for @Model.UserName. The user will need to use the new password to log in.
</div>
```

**Button row** (`ResetPassword.cshtml` lines 43–52):
```razor
<div class="d-flex justify-content-between">
    <a asp-action="Users" class="btn btn-secondary">
        <i class="fas fa-arrow-left me-2"></i>Cancel
    </a>
    <button type="submit" class="btn btn-danger">
        <i class="fas fa-save me-2"></i>Reset Password
    </button>
</div>
```

---

### `Views/ShopManagement/Index.Mobile.cshtml` (view, list + modals, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/ShopManagement/Index.cshtml`
**Mobile list analog:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml`

**Model declaration + using directives** (`Index.cshtml` lines 1–5):
```razor
@using EuphoriaInn.Service.ViewModels.ShopViewModels
@using EuphoriaInn.Domain.Enums
@model ShopManagementIndexViewModel
```

**CSS injection:**
```razor
@section Styles {
    <link href="~/css/shop-management-index.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Flat list construction** (RESEARCH.md Pattern 4 — use in `@{ }` block at top):
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

**Add New Item + View Shop buttons** (`Index.cshtml` lines 17–27 — adapt to full-width mobile):
```razor
<a href="/ShopManagement/Create" class="btn btn-success w-100 mb-2">
    <i class="fas fa-plus me-2"></i>Add New Item
</a>
<a href="/Shop" class="btn btn-outline-primary w-100 mb-3">
    <i class="fas fa-store me-2"></i>View Shop
</a>
```

**Rarity badge display** (`Index.cshtml` line 62–64):
```razor
<span class="item-rarity rarity-@item.Rarity.ToString().ToLower()">
    @item.Rarity.ToString().Replace("VeryRare", "Very Rare")
</span>
```

**Status badge switch** (`Index.cshtml` lines 181–199) — copy the `@switch (item.Status)` block verbatim for status badge display in the card.

**Icon-only action buttons** (`Index.cshtml` lines 200–251 from MyItems section) — copy all action button conditionals; adapt to `btn-sm` icon-only style:
```razor
<div class="d-flex gap-1 mt-2 flex-wrap">
    <a href="/Shop/Details/@item.Id" class="btn btn-info btn-sm" title="View" aria-label="View item">
        <i class="fas fa-eye"></i>
    </a>
    <a href="/ShopManagement/Edit/@item.Id" class="btn btn-primary btn-sm" title="Edit" aria-label="Edit item">
        <i class="fas fa-edit"></i>
    </a>
    @if (item.Status == ItemStatus.Published)
    {
        <form method="post" action="/ShopManagement/Archive/@item.Id" style="display:inline;" onsubmit="return confirm('Archive this item?')">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-secondary btn-sm" title="Archive" aria-label="Archive item">
                <i class="fas fa-ban"></i>
            </button>
        </form>
    }
    else if (item.Status == ItemStatus.Archived)
    {
        <form method="post" action="/ShopManagement/Reopen/@item.Id" style="display:inline;" onsubmit="return confirm('Reopen this item?')">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-success btn-sm" title="Reopen" aria-label="Reopen item">
                <i class="fas fa-undo"></i>
            </button>
        </form>
    }
    @* Deny button — only for Draft/pending items *@
    @if (item.Status == ItemStatus.Draft)
    {
        <button type="button" class="btn btn-danger btn-sm" title="Deny" aria-label="Deny item"
                data-bs-toggle="modal" data-bs-target="#denyModal"
                data-item-id="@item.Id" data-item-name="@item.Name">
            <i class="fas fa-times"></i>
        </button>
    }
    @* AdminOnly: Publish for Draft items *@
    @if (item.Status == ItemStatus.Draft && (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
    {
        <form method="post" action="/ShopManagement/Publish/@item.Id" style="display:inline;" onsubmit="return confirm('Publish this item?')">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-success btn-sm" title="Publish" aria-label="Publish item">
                <i class="fas fa-check"></i>
            </button>
        </form>
    }
    @* Delete — Denied items or AdminOnly for others *@
    @if (item.Status == ItemStatus.Denied || (item.Status != ItemStatus.Draft && item.Status != ItemStatus.Denied && (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded))
    {
        <form method="post" action="/ShopManagement/Delete/@item.Id" style="display:inline;" onsubmit="return confirm('Permanently delete this item?')">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-danger btn-sm" title="Delete" aria-label="Delete item">
                <i class="fas fa-trash"></i>
            </button>
        </form>
    }
</div>
```

**Deny Item modal** (`Index.cshtml` lines 446–483) — copy verbatim into mobile view.

**Bulk Actions modal** (`Index.cshtml` lines 415–443) — copy verbatim into mobile view.

**`@section Scripts` with modal JS** (`Index.cshtml` lines 485–509) — copy verbatim:
```razor
@section Scripts {
    <script>
        function bulkAction(action) {
            alert(`Bulk ${action} functionality will be available in a future update.`);
        }
        document.addEventListener('DOMContentLoaded', function() {
            const denyModal = document.getElementById('denyModal');
            if (denyModal) {
                denyModal.addEventListener('show.bs.modal', function(event) {
                    const button = event.relatedTarget;
                    const itemId = button.getAttribute('data-item-id');
                    const itemName = button.getAttribute('data-item-name');
                    const form = document.getElementById('denyForm');
                    form.action = '/ShopManagement/Deny/' + itemId;
                    document.getElementById('denyItemName').textContent = itemName;
                    document.getElementById('denialReason').value = '';
                });
            }
        });
    </script>
}
```

**Empty state** (adapt from `Shop/Index.Mobile.cshtml` lines 217–223):
```razor
<div class="shop-empty-state">
    <i class="fas fa-store fa-3x mb-2"></i>
    <h5>No items found</h5>
    <p class="text-muted mb-0">No shop items have been created yet.</p>
</div>
```

---

### `Views/ShopManagement/Create.Mobile.cshtml` (view, form, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/ShopManagement/Create.cshtml`
**Mobile form analog:** `EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml`

**Model declaration** (`Create.cshtml` lines 1–4):
```razor
@using EuphoriaInn.Service.ViewModels.ShopViewModels
@model CreateShopItemViewModel
```

**CSS injection:**
```razor
@section Styles {
    <link href="~/css/shop-management-create.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**All form fields** (`Create.cshtml` lines 16–153) — copy all fields with `col-12` replacing multi-column `col-md-*` layouts. Price field: use plain `<input asp-for="Price" ...>` with NO `input-group` wrapper (omit `randomPriceBtn` and `calculatedPriceBtn`).

**Price field on mobile** (D-12 — simplified, no buttons):
```razor
<div class="mb-3">
    <label for="Price" class="form-label">Price (Gold Pieces) *</label>
    <input asp-for="Price" type="number" class="form-control" min="0" step="0.01" required />
    <span asp-validation-for="Price" class="text-danger"></span>
    <div class="form-text" id="price-suggestion">
        Select a rarity to see pricing suggestions.
    </div>
</div>
```

**Availability window** (`Create.cshtml` lines 109–136) — copy verbatim; `toggleAvailabilityWindow()` function is kept.

**Button row** (mobile uses `d-flex justify-content-between`, omit Preview button):
```razor
<hr>
<div class="d-flex justify-content-between">
    <a href="/ShopManagement" class="btn btn-secondary">
        <i class="fas fa-arrow-left me-2"></i>Cancel
    </a>
    <button type="submit" class="btn btn-primary">
        <i class="fas fa-save me-2"></i>Create Item
    </button>
</div>
```

**`@section Scripts`** (`Create.cshtml` lines 159–303) — copy with these modifications:
- Keep `pricingGuide`, `calculatedPrices`, `toggleAvailabilityWindow()`, `updatePriceSuggestion()`, `@@keyframes` CSS, rarity CSS classes
- In `updatePriceSuggestion()`: remove `randomBtn.disabled` and `calculatedBtn.disabled` lines (buttons don't exist on mobile)
- Remove `setRandomPrice()` and `setCalculatedPrice()` functions entirely
- Remove form validation `addEventListener` (server-side validation handles it)

**Stripped `updatePriceSuggestion()`** (RESEARCH.md Pattern 3):
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

**`@@keyframes` in `<style>` block** (`Create.cshtml` lines 282–294) — copy verbatim; `@@` escaping is already correct:
```razor
@@keyframes legendary-glow {
    0% { box-shadow: 0 0 5px rgba(220, 53, 69, 0.5); }
    100% { box-shadow: 0 0 15px rgba(255, 193, 7, 0.8); }
}
```

---

### `Views/ShopManagement/Edit.Mobile.cshtml` (view, form, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/ShopManagement/Edit.cshtml`

Same structure and decisions as Create (D-15). Additional element to include:

**Status info alert** — keep the `alert-info` block that shows current item status (functional context for DMs). Copy from desktop `Edit.cshtml` status alert verbatim.

**CSS injection:**
```razor
@section Styles {
    <link href="~/css/shop-management-edit.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

All other patterns are identical to `ShopManagement/Create.Mobile.cshtml` — same JS, same field layout, same pricing field simplification.

---

### `Views/Shop/Details.Mobile.cshtml` (view, detail, request-response)

**Desktop source:** `EuphoriaInn.Service/Views/Shop/Details.cshtml`
**Mobile analog:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml` (for toast pattern)

**IMPORTANT: Use `ShopItemDetailsViewModel`, not `ShopItemViewModel`** (RESEARCH.md Pitfall 3):
```razor
@using EuphoriaInn.Service.ViewModels.ShopViewModels
@using EuphoriaInn.Domain.Enums
@model ShopItemDetailsViewModel
```

**CSS injection:**
```razor
@section Styles {
    <link href="~/css/shop-details.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Non-modal path only** (D-18) — skip the `@if (isModal)` branch entirely. Do NOT include `ViewBag.IsModal` check. Content is inlined (no `@await Html.PartialAsync("_ShopItemDetailsContent", Model)`).

**Glass card structure** (adapt from `Shop/Details.cshtml` lines 28–48, non-modal path):
```razor
<div class="shop-details-card-mobile mb-3">
    <div class="mb-3 d-flex justify-content-between align-items-center">
        <h5 class="mb-0 parchment-text">@Model.Name</h5>
        <span class="badge bg-primary">
            <i class="fas fa-tag me-1"></i>@Model.Type
        </span>
    </div>

    <span class="item-rarity rarity-@Model.Rarity.ToString().ToLower()">
        @Model.Rarity.ToString().Replace("VeryRare", "Very Rare")
    </span>

    <p class="mt-2 parchment-text-muted">@Model.Description</p>

    <div class="mb-2">
        <strong class="parchment-text"><i class="fas fa-coins me-1"></i>@Model.Price gp</strong>
    </div>

    @* Purchase form — conditional on Published + in stock *@
    @if (Model.Status == ItemStatus.Published && Model.Quantity != 0)
    {
        <form method="post" asp-controller="Shop" asp-action="Purchase" asp-route-id="@Model.Id">
            @Html.AntiForgeryToken()
            <div class="mb-3">
                <label for="Quantity" class="form-label parchment-text">Quantity</label>
                <input type="number" name="Quantity" id="Quantity" class="form-control" value="1" min="1" />
            </div>
            <button type="submit" class="btn btn-warning w-100">
                <i class="fas fa-coins me-2"></i>Purchase
            </button>
        </form>
    }
    else if (Model.Quantity == 0)
    {
        <p class="text-muted fst-italic">Out of stock.</p>
    }
    else
    {
        <p class="text-muted fst-italic">This item is not currently available for purchase.</p>
    }

    <div class="mt-3">
        <a href="/Shop" class="btn btn-secondary w-100">
            <i class="fas fa-arrow-left me-2"></i>Back to Shop
        </a>
    </div>
</div>
```

**Toast container + Scripts** (`Details.cshtml` lines 52–112) — copy the toast container and `@section Scripts` toast initialization verbatim:
```razor
<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 1055;">
    @if (TempData["Success"] != null) { ... }
    @if (TempData["Error"] != null) { ... }
    @if (TempData["GoldReceived"] != null) { ... }
</div>

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const toastElements = document.querySelectorAll('.toast');
            toastElements.forEach(function(toastElement) {
                const toast = new bootstrap.Toast(toastElement);
                toast.show();
            });
        });
    </script>
}
```
(Copy all three `@if (TempData[...])` blocks exactly from `Details.cshtml` lines 53–98.)

---

## CSS File Pattern Assignments

All 7 CSS files follow the same structure. The canonical template is `quest-edit.mobile.css`.

### Template CSS structure (from `wwwroot/css/quest-edit.mobile.css` lines 1–44):
```css
/* {filename}.css — {ViewName} mobile view only */
/* No @media queries — exclusively loaded by {ViewPath}.Mobile.cshtml via _Layout.Mobile.cshtml */

/* Glass card container */
.{view-card-class} {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 16px;
}

/* Form labels and headings — parchment text */
.{view-card-class} .form-label,
.{view-card-class} h5 {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}

/* Help text and small meta — faded parchment */
.{view-card-class} .form-text,
.{view-card-class} small {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}

/* Badges — no text-shadow */
.{view-card-class} .badge {
    text-shadow: none !important;
}
```

### Per-file class names and additional rules:

| CSS File | Card class | Additional rules |
|----------|-----------|-----------------|
| `admin-users.mobile.css` | `.admin-users-card-mobile` | `.user-card-mobile` item sub-card (same glass values, padding: 12px); `.d-flex.flex-wrap.gap-2` button row |
| `admin-quests.mobile.css` | `.admin-quests-card-mobile` | `.quest-card-mobile` item sub-card (same glass values, padding: 12px) |
| `admin-form.mobile.css` | `.admin-form-card-mobile` | No extra sub-cards; shared by EditUser + ResetPassword |
| `shop-management-index.mobile.css` | `.shop-mgmt-index-card-mobile` | `.shop-mgmt-item-card` item sub-card; `.item-rarity` classes already in `shop.mobile.css` — reference those class names, do NOT redefine; icon button row spacing |
| `shop-management-create.mobile.css` | `.shop-mgmt-create-card-mobile` | Availability window section spacing |
| `shop-management-edit.mobile.css` | `.shop-mgmt-edit-card-mobile` | Status info alert spacing (`alert-info` top margin) |
| `shop-details.mobile.css` | `.shop-details-card-mobile` | Purchase form full-width button; parchment text helpers for item name/description |

**Rarity classes reuse note:** `rarity-common`, `rarity-uncommon`, `rarity-rare`, `rarity-veryrare`, `rarity-legendary` and `.item-rarity` are defined in `shop.mobile.css` (lines 1–22 per full file). These class names can be used in ShopManagement and Shop/Details views directly — do NOT redefine them in the new CSS files.

---

## Shared Patterns

### 1. Glass Card CSS Values
**Source:** `EuphoriaInn.Service/wwwroot/css/quest-edit.mobile.css` lines 5–12 and `wwwroot/css/shop.mobile.css` lines 12–22
**Apply to:** All 7 new CSS files
```css
background: rgba(255, 255, 255, 0.15);
backdrop-filter: blur(15px);
border: 1px solid rgba(255, 255, 255, 0.3);
border-radius: 12px;
box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
padding: 16px;
```

### 2. Parchment Text Values
**Source:** `EuphoriaInn.Service/wwwroot/css/quest-edit.mobile.css` lines 15–18
**Apply to:** All 7 new CSS files for form labels and headings
```css
color: #F4E4BC !important;
text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
```
Faded variant for help text / `small`:
```css
color: rgba(244, 228, 188, 0.7) !important;
text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
```

### 3. `@section Styles` CSS Injection
**Source:** `EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml` lines 8–10
**Apply to:** All 8 new view files
```razor
@section Styles {
    <link href="~/css/{filename}.css" asp-append-version="true" rel="stylesheet" />
}
```

### 4. Antiforgery Token for Fetch DELETE
**Source:** `EuphoriaInn.Service/Views/Admin/Users.cshtml` lines 1–8 and 148–165
**Apply to:** `Admin/Users.Mobile.cshtml`, `Admin/Quests.Mobile.cshtml`
- Place `var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);` in the top `@{ }` block
- Do NOT add `@inject IAntiforgery` — already globally available
- Pass `'RequestVerificationToken': '@tokens.RequestToken'` in fetch headers

### 5. `@Html.AntiForgeryToken()` for POST Forms
**Source:** `EuphoriaInn.Service/Views/ShopManagement/Index.cshtml` lines 79, 213, 229, etc.
**Apply to:** `ShopManagement/Index.Mobile.cshtml` (all action forms), `ShopManagement/Create.Mobile.cshtml`, `ShopManagement/Edit.Mobile.cshtml`, `Shop/Details.Mobile.cshtml`
```razor
<form method="post" action="...">
    @Html.AntiForgeryToken()
    ...
</form>
```

### 6. Authorization Guard in Razor View
**Source:** `EuphoriaInn.Service/Views/ShopManagement/Index.cshtml` lines 226–234
**Apply to:** `ShopManagement/Index.Mobile.cshtml`
```razor
@if (item.Status == ItemStatus.Draft && (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
```
Do NOT add `@inject IAuthorizationService` — already globally available.

### 7. Toast Container + Initialization
**Source:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml` lines 259–323 and `Views/Shop/Details.cshtml` lines 52–112
**Apply to:** `Shop/Details.Mobile.cshtml`
```razor
document.addEventListener('DOMContentLoaded', function() {
    const toastElements = document.querySelectorAll('.toast');
    toastElements.forEach(function(toastElement) {
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
    });
});
```

### 8. Button Row Layout
**Source:** `EuphoriaInn.Service/Views/Admin/EditUser.cshtml` lines 64–73
**Apply to:** All 4 form views (EditUser, ResetPassword, ShopMgmt Create, ShopMgmt Edit)
```razor
<hr>
<div class="d-flex justify-content-between">
    <a ... class="btn btn-secondary">  @* Cancel/Back — left *@
        <i class="fas fa-arrow-left me-2"></i>...
    </a>
    <button type="submit" class="btn btn-{color}">  @* Submit — right *@
        <i class="fas fa-save me-2"></i>...
    </button>
</div>
```

### 9. No Layout Setting in Mobile Views
**Source:** `EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml` (absence of `Layout =`)
**Apply to:** All 8 new view files
`_ViewStart.cshtml` handles layout selection. Individual views MUST NOT set `Layout`.

---

## No Analog Found

None — all files have close analogs in the codebase.

---

## Critical Anti-Patterns (from RESEARCH.md)

| Anti-Pattern | Risk | Where It Applies |
|---|---|---|
| `@inject IAntiforgery` in mobile view | Compile error (duplicate injection) | Users.Mobile, Quests.Mobile |
| `@inject IAuthorizationService` in mobile view | Compile error (duplicate injection) | ShopManagement/Index.Mobile |
| Setting `Layout =` in mobile view | Breaks layout selection | All 8 views |
| Calling `randomBtn.disabled` in stripped JS | JS null reference error | ShopMgmt Create.Mobile, Edit.Mobile |
| `@keyframes` without `@@` escape in Razor style block | Razor parse error | ShopMgmt Create.Mobile, Edit.Mobile |
| `@model ShopItemViewModel` in Details.Mobile | Loses `ShopItemDetailsViewModel` properties | Shop/Details.Mobile |
| Missing `.DistinctBy(i => i.Id)` on flat list | Duplicate cards for DM's own Draft items | ShopManagement/Index.Mobile |

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Views/`, `EuphoriaInn.Service/wwwroot/css/`
**Files scanned:** 14 (8 desktop source views + 3 existing mobile views + 2 existing CSS files + 1 RESEARCH.md)
**Pattern extraction date:** 2026-06-25
