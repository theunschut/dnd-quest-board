---
phase: 12-mobile-infrastructure
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 10
files_reviewed_list:
  - EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs
  - EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs
  - EuphoriaInn.Service/Program.cs
  - EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml
  - EuphoriaInn.Service/Views/_ViewStart.cshtml
  - EuphoriaInn.Service/wwwroot/css/mobile.css
  - EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs
  - EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs
  - EuphoriaInn.IntegrationTests/Mobile/MobileCssTests.cs
findings:
  critical: 1
  warning: 2
  info: 2
  total: 5
status: issues_found
---

# Phase 12: Code Review Report

**Reviewed:** 2026-06-24T00:00:00Z
**Depth:** standard
**Files Reviewed:** 10
**Status:** issues_found

## Summary

This phase introduces the mobile detection and routing infrastructure: a middleware that sets `IsMobile` on `HttpContext.Items`, a `IViewLocationExpander` that prepends `.Mobile.cshtml` paths for mobile users, a `_ViewStart.cshtml` that selects the layout, and a `_Layout.Mobile.cshtml` shell. The approach is clean and follows standard ASP.NET Core extension patterns well.

One critical finding: a hardcoded AutoMapper license key JWT is embedded in `Program.cs` in plain text. Two warnings cover a potential null-dereference in `_Layout.Mobile.cshtml` and a missing `aria-label` on the offcanvas toggle that causes an accessibility/contract gap. Two info items cover a fragile string comparison in the view expander and the absence of a missing-User-Agent test.

## Critical Issues

### CR-01: Hardcoded AutoMapper license key in Program.cs

**File:** `EuphoriaInn.Service/Program.cs:80`
**Issue:** A full AutoMapper JWT license key is hardcoded as a string literal in `Program.cs`. This credential is committed to version control and will appear in git history permanently. If the repository is ever made public or the token is reused, it can be extracted and abused to activate AutoMapper on unauthorized machines under the account that issued the token.
**Fix:** Move the key to configuration and read it at startup. Remove the literal from source immediately and rotate the token if the repository is or ever was public.

```csharp
// appsettings.json (and override via environment variable in Docker)
{
  "AutoMapper": {
    "LicenseKey": ""   // set via AUTOMAPPER__LICENSEKEY env var in production
  }
}

// Program.cs
builder.Services.AddAutoMapper(config =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        config.LicenseKey = licenseKey;

    config.AddProfile<ViewModelProfile>();
    config.AddProfile<EntityProfile>();
});
```

## Warnings

### WR-01: Null-dereference risk on GetUserAsync in _Layout.Mobile.cshtml

**File:** `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml:118-123`
**Issue:** `currentUser` is used without a null guard after `await UserService.GetUserAsync(User)`. The desktop layout `_Layout.cshtml` likely has the same guard pattern — the mobile layout must match. If `GetUserAsync` returns `null` (e.g., the Identity cookie is present but the user has been deleted from the database), accessing `currentUser.Name` will throw a `NullReferenceException` at render time, producing a 500 for the affected user.

```csharp
// Current (lines 118-123)
var currentUser = await UserService.GetUserAsync(User);
// ... then used as @currentUser.Name with no null check
```

**Fix:**

```cshtml
@{
    var currentUser = await UserService.GetUserAsync(User);
}
@if (currentUser != null)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="Account" asp-action="Profile"
           data-bs-dismiss="offcanvas">
            <i class="fas fa-user-cog me-2"></i>@currentUser.Name
        </a>
    </li>
    ...
}
```

Alternatively fall back to a display name from the `User` claims if `currentUser` is null.

### WR-02: Offcanvas toggle button missing aria-label (accessibility contract gap)

**File:** `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml:20-23`
**Issue:** The hamburger `<button>` that opens the offcanvas navigation has no `aria-label` or `aria-controls` attribute. Bootstrap's offcanvas requires `aria-controls` pointing to the offcanvas element's `id` for screen readers to announce the button correctly. Without it, assistive technology users on mobile cannot navigate the menu, and any future accessibility audit will flag this.

```html
<!-- Current -->
<button class="navbar-toggler" type="button"
        data-bs-toggle="offcanvas" data-bs-target="#mobileNav">
    <span class="navbar-toggler-icon"></span>
</button>
```

**Fix:**

```html
<button class="navbar-toggler" type="button"
        data-bs-toggle="offcanvas" data-bs-target="#mobileNav"
        aria-controls="mobileNav" aria-label="Open navigation menu">
    <span class="navbar-toggler-icon"></span>
</button>
```

## Info

### IN-01: String comparison in ExpandViewLocations relies on bool.ToString() casing

**File:** `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs:20`
**Issue:** `isMobileStr != "True"` is a case-sensitive comparison against the exact string `"True"`, which is what `bool.ToString()` produces on all platforms. This works correctly in practice, but the dependency on `bool.ToString()` output formatting is subtle. The test at `MobileViewLocationExpanderTests.cs:39` correctly asserts `"True"` (capital T), so the system is consistent. Consider making the intent explicit via a constant or an explicit `bool.Parse` round-trip.

**Fix:** Define a constant or parse back to bool for clarity:

```csharp
private const string MobileTrue = "True"; // same as bool.ToString() output

// or, in ExpandViewLocations:
if (!context.Values.TryGetValue(IsMobileKey, out var isMobileStr)
    || !bool.TryParse(isMobileStr, out var isMobile)
    || !isMobile)
{
    return viewLocations;
}
```

### IN-02: No test for missing / null User-Agent header

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs`
**Issue:** The middleware tests cover an empty string User-Agent (`InvokeAsync_EmptyUserAgent_SetsMobileFalse`) but not the case where the `User-Agent` header is entirely absent. `context.Request.Headers.UserAgent.ToString()` on a `DefaultHttpContext` with no User-Agent header returns an empty string, so the current implementation handles it correctly, but the test gap means a future refactor could silently break the absent-header path.

**Fix:** Add a test:

```csharp
[Fact]
public async Task InvokeAsync_NoUserAgentHeader_SetsMobileFalse()
{
    var context = new DefaultHttpContext(); // no User-Agent set at all
    var middleware = CreateMiddleware();

    await middleware.InvokeAsync(context);

    context.Items["IsMobile"].Should().Be(false);
}
```

---

_Reviewed: 2026-06-24T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
