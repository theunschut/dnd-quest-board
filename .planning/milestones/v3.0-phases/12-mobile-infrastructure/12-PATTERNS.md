# Phase 12: Mobile Infrastructure - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 6 (4 new, 2 modified)
**Analogs found:** 6 / 6

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs` | middleware | request-response | `EuphoriaInn.IntegrationTests/Helpers/TestAuthSelectorMiddleware.cs` | role-match |
| `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs` | utility | request-response | `EuphoriaInn.IntegrationTests/Helpers/TestAuthSelectorMiddleware.cs` | partial-match (no expander exists; middleware analog for DI/constructor style) |
| `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` | layout | request-response | `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | exact |
| `EuphoriaInn.Service/wwwroot/css/mobile.css` | config/static | — | `EuphoriaInn.Service/wwwroot/css/site.css` | role-match |
| `EuphoriaInn.Service/Views/_ViewStart.cshtml` *(modify)* | config | request-response | itself (current single-line) | self |
| `EuphoriaInn.Service/Program.cs` *(modify)* | config | — | itself (current registrations) | self |

---

## Pattern Assignments

### `EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs` (middleware, request-response)

**Analog:** `EuphoriaInn.IntegrationTests/Helpers/TestAuthSelectorMiddleware.cs`

**Namespace / file convention:** Middleware lives under `EuphoriaInn.Service.Middleware` namespace, file in `EuphoriaInn.Service/Middleware/`.

**Constructor pattern** — primary constructor with `RequestDelegate next` (lines 11-12 of analog):
```csharp
public class TestAuthSelectorMiddleware(RequestDelegate next, IWebHostEnvironment environment)
```
For `MobileDetectionMiddleware` the extra parameter is not needed; use:
```csharp
public class MobileDetectionMiddleware(RequestDelegate next)
```

**Core InvokeAsync pattern** (analog lines 14-30) — read from `HttpContext`, set state, call `await next(context)`:
```csharp
public async Task InvokeAsync(HttpContext context)
{
    // ... read context.Request.Headers ...
    // ... set context.Items["..."] or context.User ...
    await next(context);
}
```

**Complete implementation** (from RESEARCH.md Pattern 1 — authoritative):
```csharp
namespace EuphoriaInn.Service.Middleware;

public class MobileDetectionMiddleware(RequestDelegate next)
{
    private static readonly string[] MobileKeywords =
        ["Mobi", "Android", "iPhone", "iPad", "Windows Phone", "BlackBerry"];

    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var isMobile = MobileKeywords.Any(kw =>
            userAgent.Contains(kw, StringComparison.OrdinalIgnoreCase));

        context.Items["IsMobile"] = isMobile;

        await next(context);
    }
}
```

**Registration in Program.cs** — insert between `UseStaticFiles()` (line 88) and `UseRouting()` (line 90):
```csharp
app.UseStaticFiles();
app.UseMiddleware<MobileDetectionMiddleware>();   // NEW
app.UseRouting();
```
`UseSession()` stays at line 92 — do NOT move it.

---

### `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs` (utility, request-response)

**No existing expander analog** in the codebase — pattern is specified exclusively in RESEARCH.md Pattern 2. No `ViewExpanders/` directory exists yet; create it.

**Namespace:** `EuphoriaInn.Service.ViewExpanders`

**Import pattern** — single using for the Razor interface (no additional NuGet required):
```csharp
using Microsoft.AspNetCore.Mvc.Razor;

namespace EuphoriaInn.Service.ViewExpanders;
```

**Critical rule (INFRA-03):** Detection must be in `PopulateValues`, not `ExpandViewLocations`. `ExpandViewLocations` only executes on cache miss.

**Complete implementation** (from RESEARCH.md Pattern 2 — authoritative):
```csharp
using Microsoft.AspNetCore.Mvc.Razor;

namespace EuphoriaInn.Service.ViewExpanders;

public class MobileViewLocationExpander : IViewLocationExpander
{
    private const string IsMobileKey = "isMobile";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var isMobile = context.ActionContext.HttpContext.Items["IsMobile"] is true;
        context.Values[IsMobileKey] = isMobile.ToString();
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (!context.Values.TryGetValue(IsMobileKey, out var isMobileStr)
            || isMobileStr != "True")
        {
            return viewLocations;
        }

        return ExpandForMobile(viewLocations);
    }

    private static IEnumerable<string> ExpandForMobile(IEnumerable<string> viewLocations)
    {
        foreach (var location in viewLocations)
        {
            yield return location.Replace(".cshtml", ".Mobile.cshtml",
                StringComparison.OrdinalIgnoreCase);
            yield return location;
        }
    }
}
```

**Registration in Program.cs** — add immediately after `AddControllersWithViews()` at line 22:
```csharp
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});
```
Add `using Microsoft.AspNetCore.Mvc.Razor;` and `using EuphoriaInn.Service.ViewExpanders;` to Program.cs imports (lines 1-10).

---

### `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` (layout, request-response)

**Analog:** `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`

**Head / using pattern** (analog lines 1-17):
```cshtml
@using EuphoriaInn.Domain.Interfaces
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - D&D Quest Board</title>
    <!-- CDN links here -->
    <link rel="stylesheet" href="~/css/mobile.css" asp-append-version="true" />
</head>
```
Key differences from desktop:
- Load only `mobile.css` — NOT `site.css`, `calendar.css`, `quests.css`, `shop.css`, `guild-members.css`, `dm-profile.css` (D-03)
- Keep Bootstrap CDN, Font Awesome CDN, Google Fonts (Cinzel) CDN — same CDN links as desktop (D-04, RESEARCH.md Standard Stack)

**Auth injection pattern** (analog lines 1, implied from nav usage):
```cshtml
@using EuphoriaInn.Domain.Interfaces
@inject IAuthorizationService AuthorizationService
@inject IUserService UserService
```
The `@inject` directives are not visible at lines 1-17 of `_Layout.cshtml` because the file opens with `@using EuphoriaInn.Domain.Interfaces` and then `<!DOCTYPE html>` — the `@inject` directives needed for `AuthorizationService` and `UserService` must be deduced from nav usage in lines 28-143. Planner must verify by reading lines 1 exactly; copy whatever `@inject` lines appear before `<!DOCTYPE html>` verbatim.

**Body + nav shell pattern** (analog lines 18-26) — mobile variant:
```cshtml
<body class="d-flex flex-column min-vh-100 mobile-layout @ViewData["BodyClass"]">
    <header>
        <nav class="navbar navbar-dark bg-dark py-2">
            <div class="container-fluid px-3">
                <a class="navbar-brand" asp-controller="Home" asp-action="Index">
                    <i class="fas fa-dice-d20"></i> Quest Board
                </a>
                <button class="navbar-toggler" type="button"
                        data-bs-toggle="offcanvas" data-bs-target="#mobileNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
            </div>
        </nav>
    </header>
```
Changes vs desktop:
- Body adds `mobile-layout` class (needed for INFRA-05 test assertion)
- Brand text is "Quest Board" not "D&D Quest Board" (D-02)
- Toggler uses `data-bs-toggle="offcanvas"` instead of `data-bs-toggle="collapse"` (INFRA-04)

**Offcanvas nav pattern** (source: RESEARCH.md Code Examples — nav translation):
```cshtml
<div class="offcanvas offcanvas-start" id="mobileNav" tabindex="-1">
    <div class="offcanvas-header bg-dark text-white">
        <h5 class="offcanvas-title">
            <i class="fas fa-dice-d20 me-2"></i>Quest Board
        </h5>
        <button type="button" class="btn-close btn-close-white"
                data-bs-dismiss="offcanvas"></button>
    </div>
    <div class="offcanvas-body">
        <ul class="navbar-nav">
            @* Auth-conditional items — mirror _Layout.cshtml lines 28-143 *@
            @if (User.Identity?.IsAuthenticated == true)
            {
                @* Admin section (lines 31-50 of _Layout.cshtml) *@
                @if ((await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
                {
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Admin" asp-action="Users"
                           data-bs-dismiss="offcanvas">
                            <i class="fas fa-users-cog me-2"></i>User Management
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Admin" asp-action="Quests"
                           data-bs-dismiss="offcanvas">
                            <i class="fas fa-scroll me-2"></i>Quest Management
                        </a>
                    </li>
                }
                @* DM section (lines 53-78 of _Layout.cshtml) *@
                @if ((await AuthorizationService.AuthorizeAsync(User, "DungeonMasterOnly")).Succeeded)
                {
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="Quest" asp-action="Create"
                           data-bs-dismiss="offcanvas">
                            <i class="fas fa-scroll me-2"></i>Create Quest
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="ShopManagement" asp-action="Index"
                           data-bs-dismiss="offcanvas">
                            <i class="fas fa-coins me-2"></i>Manage Shop
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-controller="DungeonMaster" asp-action="EditProfile"
                           data-bs-dismiss="offcanvas">
                            <i class="fas fa-user-edit me-2"></i>Edit My Profile
                        </a>
                    </li>
                }
                @* Authenticated user items (lines 81-101 of _Layout.cshtml) *@
                <li class="nav-item">
                    <a class="nav-link" asp-controller="Shop" asp-action="Index"
                       data-bs-dismiss="offcanvas">
                        <i class="fas fa-store me-2"></i>Shop
                    </a>
                </li>
                @* ... Quest Log, Guild Members, Players same pattern ... *@
            }
            @* Calendar — available to all (line 106 of _Layout.cshtml) *@
            <li class="nav-item">
                <a class="nav-link" asp-controller="Calendar" asp-action="Index"
                   data-bs-dismiss="offcanvas">
                    <i class="fas fa-calendar-alt me-2"></i>Calendar
                </a>
            </li>
            @* User profile / login (lines 110-142 of _Layout.cshtml) *@
        </ul>
    </div>
</div>
```
Rule: add `data-bs-dismiss="offcanvas"` to every nav link so the drawer closes on navigation.

**RenderBody pattern** (analog line 149-151) — mobile variant uses full-width container:
```cshtml
<main class="container-fluid px-2 mt-2">
    @RenderBody()
</main>
```

**Scripts section** (analog line 166 — must be present in mobile layout):
```cshtml
@await RenderSectionAsync("Styles", required: false)
@await RenderSectionAsync("Scripts", required: false)
```
Both sections required (RESEARCH.md Pitfall 4). Desktop layout has `Scripts` only; mobile layout adds `Styles` too.

---

### `EuphoriaInn.Service/wwwroot/css/mobile.css` (static CSS, —)

**Analog:** `EuphoriaInn.Service/wwwroot/css/site.css` (style: plain CSS, no preprocessor, no `@media` needed here)

**CSS link tag pattern** (from `_Layout.cshtml` line 11):
```html
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```
Apply the same `asp-append-version="true"` to the `mobile.css` link in `_Layout.Mobile.cshtml`.

**Complete content** (from RESEARCH.md Pattern 5 — authoritative for INFRA-06):
```css
/* Touch target sizing — INFRA-06 */
.btn,
a.nav-link,
input,
select,
textarea,
.form-control,
.form-select {
    min-height: 44px;
}

/* Mobile typography scale */
body {
    font-size: 16px; /* prevents iOS auto-zoom on input focus */
    line-height: 1.5;
}

/* Mobile spacing overrides */
.container-fluid {
    padding-left: 0.5rem;
    padding-right: 0.5rem;
}

/* D&D theme baseline — D-04 */
.navbar-brand {
    font-family: 'Cinzel', serif;
    font-weight: 600;
}

.mobile-layout {
    background-color: #212529; /* matches Bootstrap bg-dark */
}

.mobile-layout .navbar {
    min-height: 56px;
}
```
No `@media` query — this file is ONLY loaded on mobile requests by `_Layout.Mobile.cshtml`.

---

### `EuphoriaInn.Service/Views/_ViewStart.cshtml` *(modify)*

**Current content** (lines 1-3):
```cshtml
@using EuphoriaInn.Domain.Interfaces
@{
    Layout = "_Layout";
}
```

**Modified content** (from RESEARCH.md Pattern 3 — authoritative):
```cshtml
@using EuphoriaInn.Domain.Interfaces
@{
    var isMobile = Context.Items["IsMobile"] is true;
    Layout = isMobile
        ? "~/Views/Shared/_Layout.Mobile.cshtml"
        : "_Layout";
}
```
The `is true` pattern safely handles `null` (Pitfall 2 — null-safe; does not throw on static file or health check requests).

---

### `EuphoriaInn.Service/Program.cs` *(modify)*

**Current middleware pipeline** (lines 87-94):
```csharp
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

**Current DI registrations** (lines 22-76) — `AddControllersWithViews()` is at line 22.

**Two additions required:**

1. After line 22 (`AddControllersWithViews()`), add expander registration:
```csharp
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});
```

2. Between `UseStaticFiles()` (line 88) and `UseRouting()` (line 90), add middleware:
```csharp
app.UseMiddleware<MobileDetectionMiddleware>();
```

3. Add two `using` statements to the import block (lines 1-10):
```csharp
using EuphoriaInn.Service.Middleware;
using EuphoriaInn.Service.ViewExpanders;
using Microsoft.AspNetCore.Mvc.Razor;
```

`UseSession()` at line 92 stays after `UseRouting()` — do NOT move (Pitfall 3).

---

## Shared Patterns

### Middleware Primary Constructor Style
**Source:** `EuphoriaInn.IntegrationTests/Helpers/TestAuthSelectorMiddleware.cs` line 11
**Apply to:** `MobileDetectionMiddleware.cs`
```csharp
public class TestAuthSelectorMiddleware(RequestDelegate next, IWebHostEnvironment environment)
```
Use C# primary constructor syntax (no explicit `private readonly` field or body constructor). This is the established pattern in the codebase.

### HttpContext.Items for Per-Request State
**Source:** `TestAuthSelectorMiddleware.cs` lines 21-27 — reads from `context.Request.Headers`; sets `context.User` for downstream.
**Apply to:** `MobileDetectionMiddleware.cs` — reads `context.Request.Headers.UserAgent`, sets `context.Items["IsMobile"]`.
Pattern: read input, set state, `await next(context)`.

### Auth-Conditional Razor Checks
**Source:** `_Layout.cshtml` lines 28-142
**Apply to:** `_Layout.Mobile.cshtml` offcanvas body nav
```cshtml
@if (User.Identity?.IsAuthenticated == true)
{
    @if ((await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)
    { ... }
    @if ((await AuthorizationService.AuthorizeAsync(User, "DungeonMasterOnly")).Succeeded)
    { ... }
}
```
Copy this structure verbatim into the offcanvas body. Same policy names, same guard pattern.

### CSS asp-append-version Cache Busting
**Source:** `_Layout.cshtml` lines 11-16
**Apply to:** `mobile.css` link tag in `_Layout.Mobile.cshtml`
```html
<link rel="stylesheet" href="~/css/mobile.css" asp-append-version="true" />
```

### Integration Test: HTML Content Assertion
**Source:** `EuphoriaInn.IntegrationTests/Controllers/HomeControllerIntegrationTests.cs` lines 29-37
**Apply to:** `MobileInfrastructureIntegrationTests.cs`
```csharp
var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);
var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
content.Should().Contain("...");
```

### Integration Test: Custom Request with Headers
**Source:** RESEARCH.md Validation Architecture — UA-based test pattern (no codebase analog exists yet)
**Apply to:** `MobileInfrastructureIntegrationTests.cs`
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "/");
request.Headers.Add("User-Agent",
    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1");
var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
```

### Unit Test Structure
**Source:** `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` lines 1-27
**Apply to:** `MobileDetectionMiddlewareTests.cs` and `MobileViewLocationExpanderTests.cs`
```csharp
namespace EuphoriaInn.UnitTests.Middleware;

public class MobileDetectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_IPhoneUserAgent_SetsMobileTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0 (iPhone; ...)";
        var middleware = new MobileDetectionMiddleware(ctx => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items["IsMobile"].Should().Be(true);
    }
}
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs` | utility | request-response | No `IViewLocationExpander` implementation exists in the codebase; pattern sourced entirely from RESEARCH.md Pattern 2 |

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/`, `EuphoriaInn.UnitTests/`, `EuphoriaInn.IntegrationTests/`
**Files scanned:** 14
**Key source files read:**
- `EuphoriaInn.Service/Program.cs` (lines 1-134)
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (lines 1-168, complete)
- `EuphoriaInn.Service/Views/_ViewStart.cshtml` (lines 1-3, complete)
- `EuphoriaInn.IntegrationTests/Helpers/TestAuthSelectorMiddleware.cs` (complete)
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` (complete)
- `EuphoriaInn.IntegrationTests/Controllers/HomeControllerIntegrationTests.cs` (complete)
- `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` (lines 1-50)
- `EuphoriaInn.Service/wwwroot/css/site.css` (lines 1-30)
**Pattern extraction date:** 2026-06-24
