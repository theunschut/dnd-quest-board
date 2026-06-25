---
phase: 16-account-browse
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 11
files_reviewed_list:
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
  - EuphoriaInn.Service/Views/Account/ChangePassword.Mobile.cshtml
  - EuphoriaInn.Service/Views/Account/Edit.Mobile.cshtml
  - EuphoriaInn.Service/Views/Account/Login.Mobile.cshtml
  - EuphoriaInn.Service/Views/Account/Profile.Mobile.cshtml
  - EuphoriaInn.Service/Views/Account/Register.Mobile.cshtml
  - EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml
  - EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/account.mobile.css
  - EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css
  - EuphoriaInn.Service/wwwroot/css/shop.mobile.css
findings:
  critical: 0
  warning: 3
  info: 3
  total: 6
status: issues_found
---

# Phase 16: Code Review Report

**Reviewed:** 2026-06-25
**Depth:** standard
**Files Reviewed:** 11
**Status:** issues_found

## Summary

Phase 16 adds mobile Razor views for Account pages (Login, Register, Edit, Profile, ChangePassword), Shop index, and Guild Members index, along with three companion CSS files and integration test stubs. The views correctly follow project conventions: no `@Layout` assignment, no `@media` queries in mobile CSS, and no `@inject` directives (all globally injected via `_ViewImports.cshtml`). Form security is sound — password fields inherit `type="password"` from `[DataType(DataType.Password)]` annotations on ViewModels, antiforgery tokens are present via Tag Helpers on all POST forms, and the `onclick` URL patterns in the Guild Members view are safe.

Two logic bugs require fixing: the "Clear Filters" button in the shop offcanvas is not shown when only a search query is active (condition omits `SearchQuery`), and `event.relatedTarget` in the modal script is unchecked before use, which can throw a TypeError if the modal is opened programmatically. One content inconsistency exists in the Profile mobile view versus desktop. Additionally, `ChangePassword.Mobile.cshtml` has no integration test coverage, and the `@functions` URL helpers have an unguarded nullable `Url.Action` concatenation.

## Warnings

### WR-01: "Clear Filters" Button Hidden When Only Search Is Active

**File:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml:171`

**Issue:** The trigger badge for active filters (`hasActiveFilters`, line 60) includes `SearchQuery` in its condition, but the "Clear Filters" button rendering condition at line 171 does not. When a user enters a search term without selecting any rarity or sort, the "Filter & amp; Sort" button correctly shows "Active", but the offcanvas offers no way to clear the search — the "Clear Filters" link is absent.

```razor
// Line 60 — hasActiveFilters includes SearchQuery (correct):
bool hasActiveFilters = Model.SelectedRarities.Any() || Model.SelectedSort != null || !string.IsNullOrEmpty(Model.SearchQuery);

// Line 171 — Clear Filters button omits SearchQuery (bug):
@if (Model.SelectedRarities.Any() || Model.SelectedSort != null)
```

**Fix:** Add the `SearchQuery` check to match `hasActiveFilters`:
```razor
@if (Model.SelectedRarities.Any() || Model.SelectedSort != null || !string.IsNullOrEmpty(Model.SearchQuery))
{
    <a href="@Url.Action("Index", "Shop")" class="btn btn-outline-secondary">Clear Filters</a>
}
```

---

### WR-02: `event.relatedTarget` Used Without Null Guard in Modal Script

**File:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml:302-303`

**Issue:** Bootstrap 5 sets `event.relatedTarget` to `null` when a modal is triggered programmatically (e.g., via `bootstrap.Modal.show()`). Line 303 calls `button.getAttribute('data-item-url')` directly on that value. If `button` is null, a TypeError is thrown at runtime, breaking the modal loading flow silently and leaving `#itemDetailsBody` empty.

```javascript
// Current — crashes if button is null:
const button = event.relatedTarget;
const itemUrl = button.getAttribute('data-item-url');
```

**Fix:** Guard with an early return or optional chaining:
```javascript
const button = event.relatedTarget;
if (!button) return;
const itemUrl = button.getAttribute('data-item-url');
if (itemUrl) {
    fetch(itemUrl)
        .then(response => response.text())
        .then(html => document.getElementById('itemDetailsBody').innerHTML = html)
        .catch(() => {}); // silently ignore fetch errors
}
```

Note: The same pattern exists in `EuphoriaInn.Service/Views/Shop/Index.cshtml:493-494` — both should be fixed together.

---

### WR-03: DM Account Type Label Differs Between Mobile and Desktop Profile Views

**File:** `EuphoriaInn.Service/Views/Account/Profile.Mobile.cshtml:37`

**Issue:** When a Dungeon Master views their own profile, the mobile view renders "Dungeon Master" but the desktop view renders "Dungeon Master & Player". A DM is also a player, and the desktop view explicitly communicates this. The mobile view silently drops the "& Player" part, which can mislead DMs who check their account type on mobile.

Mobile (`Profile.Mobile.cshtml:37`):
```html
<span class="account-field-value"><i class="fas fa-crown text-warning me-2"></i>Dungeon Master</span>
```

Desktop (`Profile.cshtml:47`):
```html
<p class="form-control-plaintext">
    <i class="fas fa-crown text-warning me-2"></i>
    Dungeon Master & Player
</p>
```

**Fix:** Update the mobile view to match the desktop label:
```html
<span class="account-field-value"><i class="fas fa-crown text-warning me-2"></i>Dungeon Master &amp; Player</span>
```

---

## Info

### IN-01: No Integration Test for `ChangePassword.Mobile.cshtml`

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs`

**Issue:** Phase 16 added `ChangePassword.Mobile.cshtml` but no corresponding ACCT-03 (ChangePassword) test case was added to `MobileViewsTests.cs`. The other four Account mobile views (Login ACCT-01, Register ACCT-02, Edit ACCT-03, Profile ACCT-03) each have at least one smoke test. ChangePassword is the only account page left without any mobile rendering assertion (`account-card-mobile` presence and `account.mobile.css` link).

**Fix:** Add a test following the same pattern as `MobileAccountEdit_MobileUserAgent_RendersGlassCardForm`:
```csharp
[Fact]
public async Task MobileAccountChangePassword_MobileUserAgent_RendersGlassCardForm()
{
    var (authClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
        _factory, "acct_chpw16", "acct_chpw16@test.com");

    var request = new HttpRequestMessage(HttpMethod.Get, "/Account/ChangePassword");
    request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
    request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
    var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
    var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    html.Should().Contain("account-card-mobile");
    html.Should().Contain("account.mobile.css");
}
```

---

### IN-02: `Url.Action` Return Value Concatenated Without Null Check in `@functions`

**File:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml:19, 35`

**Issue:** `Url.Action("Index", "Shop")` returns `string?` in ASP.NET Core. Both `BuildTabUrl` and `BuildPageUrl` concatenate its result directly with the query string without guarding against null. In practice this never triggers because the route must exist for the page to render, but the compiler sees an unsafe concatenation and the pattern is technically incorrect.

```csharp
return Url.Action("Index", "Shop") + Microsoft.AspNetCore.Http.QueryString.Create(pairs).Value;
```

**Fix:** Use the null-coalescing fallback:
```csharp
return (Url.Action("Index", "Shop") ?? "/Shop") + Microsoft.AspNetCore.Http.QueryString.Create(pairs).Value;
```

---

### IN-03: Redundant `@using` Declarations in All Five Account Mobile Views

**File:** `EuphoriaInn.Service/Views/Account/ChangePassword.Mobile.cshtml:1-2`, `Edit.Mobile.cshtml:1-2`, `Login.Mobile.cshtml:1-2`, `Profile.Mobile.cshtml:1-2`, `Register.Mobile.cshtml:1-2`

**Issue:** All five account mobile views declare both `@using EuphoriaInn.Domain.Interfaces` and `@using EuphoriaInn.Service.ViewModels.AccountViewModels` at the top. Both of these namespaces are already imported globally in `_ViewImports.cshtml` (lines 3 and 7). The declarations are harmless but add noise. The desktop views (`Login.cshtml`, `Profile.cshtml`, etc.) have the same redundancy, so this is a project-wide pattern.

**Fix:** No action required if the project convention is to mirror desktop view headers. If the convention is to remove what is already in `_ViewImports`, remove the two `@using` lines from each mobile view. Consistency with the existing desktop views is the priority.

---

_Reviewed: 2026-06-25_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
