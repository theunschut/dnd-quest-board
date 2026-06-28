# Phase 12: Mobile Infrastructure - Research

**Researched:** 2026-06-23
**Domain:** ASP.NET Core 8 MVC — IViewLocationExpander, middleware pipeline, Razor layout selection
**Confidence:** HIGH (implementation is fully specified in canonical research docs; codebase confirmed)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** The mobile offcanvas nav mirrors the desktop nav exactly — same auth-conditional sections (Admin panel, DM tools, player-facing items, auth/logout). Each section renders as list items (not nested dropdowns) in the offcanvas drawer. Same Razor auth-check patterns as `_Layout.cshtml`.
- **D-02:** The mobile navbar shows the d20 icon + "Quest Board" text as the brand — consistent with the desktop navbar brand.
- **D-03:** `_Layout.Mobile.cshtml` loads **only `mobile.css`** — the five desktop CSS files (`site.css`, `calendar.css`, `quests.css`, `shop.css`, `guild-members.css`, `dm-profile.css`) are NOT loaded.
- **D-04:** `mobile.css` Phase 12 baseline includes: 44px minimum height on interactive elements (buttons, form controls, nav links); mobile typography scale and spacing overrides; D&D theme baseline (Cinzel font reference and dark navbar palette).
- **D-05:** Phase 12 completes before any `.Mobile.cshtml` views are added. True infrastructure-first. No proof-of-concept views in Phase 12.
- **D-06:** `"iPad"` remains in `MobileKeywords` — tablets get the mobile layout.

### Claude's Discretion

- Exact `MobileKeywords` array entries beyond the agreed set (`["Mobi", "Android", "iPhone", "iPad", "Windows Phone", "BlackBerry"]` from the research). Use the research-documented set.
- The named sections in `_Layout.Mobile.cshtml` (`@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)`) — must be declared so views that push scripts/styles via `@section` don't error.
- Whether `_Layout.Mobile.cshtml` renders `@RenderBody()` inside `<main>` or `<div>` — use `<main class="container-fluid px-2 mt-2">` per the research architecture.
- `mobile.css` exact selectors and values for touch targets — use `min-height: 44px` on `.btn`, `a.nav-link`, `input`, `select`, `textarea`.

### Deferred Ideas (OUT OF SCOPE)

- **"Request desktop site" / cookie-based override** — already tracked in `.planning/REQUIREMENTS.md` Future Requirements as "Switch to desktop cookie override — user-controlled escape hatch." Defer to a post-Phase 16 phase.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INFRA-01 | `MobileDetectionMiddleware` detects mobile user agents per request and stores the result in `HttpContext.Items["IsMobile"]` | Middleware pattern fully specified in ARCHITECTURE.md Pattern 2; keyword set confirmed in CONTEXT.md D-06 |
| INFRA-02 | `MobileViewLocationExpander` registered; on mobile requests the view engine checks `ViewName.Mobile.cshtml` before `ViewName.cshtml`; desktop requests unaffected | Expander pattern fully specified in ARCHITECTURE.md Pattern 1; verified against `IViewLocationExpander` interface contract |
| INFRA-03 | Mobile detection logic lives in `PopulateValues` (not `ExpandViewLocations`) so view path cache correctly separates mobile and desktop entries | Critical cache-key requirement documented in ARCHITECTURE.md Anti-Pattern 1; confirmed from interface source |
| INFRA-04 | `_Layout.Mobile.cshtml` provides the mobile HTML shell with a Bootstrap offcanvas navigation drawer | Layout structure fully specified in ARCHITECTURE.md Pattern 3; offcanvas nav structure documented; desktop nav source in `_Layout.cshtml` lines 20-146 |
| INFRA-05 | `_ViewStart.cshtml` selects `_Layout.Mobile.cshtml` for mobile requests and `_Layout.cshtml` for desktop — no individual mobile view sets its layout explicitly | `_ViewStart.cshtml` conditional pattern documented in ARCHITECTURE.md Pattern 3; current file confirmed to be a one-liner at `Views/_ViewStart.cshtml:3` |
| INFRA-06 | `mobile.css` provides baseline touch target sizing (minimum 44px height), mobile typography scale, and spacing overrides | CSS selectors specified in CONTEXT.md Claude's Discretion section; D&D theme baseline (Cinzel + dark navbar) from D-04 |
</phase_requirements>

---

## Summary

Phase 12 wires the mobile detection pipeline for an ASP.NET Core 8 MVC application. The implementation is entirely within the Service layer — no controllers, ViewModels, repositories, or domain services change. Four new files are created and two existing files are modified.

The architecture has been fully researched and documented. The canonical reference is `.planning/research/ARCHITECTURE.md` which contains complete, copy-ready C# implementations for both the middleware and the expander, plus the exact `_ViewStart.cshtml` conditional and `_Layout.Mobile.cshtml` structure. The implementation decision (hand-rolled over Wangkanai.Responsive) is locked — zero new NuGet packages, zero middleware reorder risk.

The critical correctness requirement is that mobile detection writes to `context.Values` in `PopulateValues`, not in `ExpandViewLocations`. The latter only runs on cache miss — detection there would serve cached desktop paths to mobile users. This is INFRA-03 and must be verified by any implementation.

At Phase 12 completion, no user-visible change exists for desktop users and no new `.Mobile.cshtml` content views are present. Mobile requests will get the mobile layout shell wrapping the existing desktop view content — intentionally unstyled until Phases 13-16 add proper mobile views.

**Primary recommendation:** Follow ARCHITECTURE.md exactly. The five deliverables are: `MobileDetectionMiddleware.cs`, `MobileViewLocationExpander.cs`, `_Layout.Mobile.cshtml`, updated `_ViewStart.cshtml`, and `mobile.css`.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Mobile UA detection | Frontend Server (middleware) | — | Must run before routing so `HttpContext.Items["IsMobile"]` is available to all downstream components including view engine |
| View path routing (mobile vs desktop) | Frontend Server (Razor engine) | — | `IViewLocationExpander` is a Razor engine extension point; no controller involvement |
| Layout selection | Frontend Server (Razor) | — | `_ViewStart.cshtml` runs as part of view rendering pipeline |
| Mobile HTML shell | Frontend Server (Razor layout) | CDN (Bootstrap offcanvas JS) | Layout renders server-side; Bootstrap offcanvas JS delivered from CDN |
| Touch target CSS | CDN / Static | — | Static file served from `wwwroot/css/mobile.css` |
| Auth-conditional nav items | Frontend Server (Razor + Identity) | — | Same `IAuthorizationService` pattern as desktop layout |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.Mvc.Razor` | Built-in (.NET 8 SDK) | `IViewLocationExpander` interface; view engine integration | Already present via `Microsoft.NET.Sdk.Web`; no additional package needed [ASSUMED] |
| Bootstrap 5.3.0 | CDN | Offcanvas nav component for mobile drawer | Already loaded in `_Layout.cshtml`; offcanvas is the canonical Bootstrap mobile nav pattern [ASSUMED] |

No new NuGet packages. Zero new dependencies. The hand-rolled implementation uses only what is already in the project.

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Font Awesome 6.4.0 | CDN | Icons in mobile nav (d20, crown, scroll, etc.) | Already on CDN; mobile layout must load it for nav icons |
| Google Fonts (Cinzel) | CDN | D&D-theme typography in mobile layout | Already loaded in `_Layout.cshtml`; mobile layout loads it for brand consistency per D-04 |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled `IViewLocationExpander` | `Wangkanai.Responsive 7.14.0` | Wangkanai requires moving `UseSession()` before `UseRouting()` and risks overriding the project's 24-hour session timeout with a 10-second default if the install guide is followed literally. Hand-rolled is ~30 lines, zero dependencies, zero risk. |
| Middleware for UA detection | Detection inside `PopulateValues` | Detection inside `PopulateValues` would parse UA on every view lookup (partials, layouts, main views); middleware runs once per request. Also: detection inside `ExpandViewLocations` (not `PopulateValues`) breaks the cache. |

**No installation command needed** — zero new packages.

---

## Package Legitimacy Audit

> No external packages are being installed in this phase. All implementation uses ASP.NET Core primitives already present in the project.

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

---

## Architecture Patterns

### System Architecture Diagram

```
HTTP Request (any UA)
        |
        v
UseStaticFiles()          — static assets bypass middleware chain
        |
        v
MobileDetectionMiddleware  — reads User-Agent header once
        |                    sets HttpContext.Items["IsMobile"] = true/false
        v
UseRouting()
        |
        v
UseSession() → UseAuthentication() → UseAuthorization()
        |
        v
Controller.Action()       — NO CHANGES to any controller
        returns ViewResult(viewModel)
        |
        v
RazorViewEngine — view resolution
        |
        +-- MobileViewLocationExpander.PopulateValues()
        |       reads HttpContext.Items["IsMobile"]
        |       writes context.Values["isMobile"] = "True"/"False"
        |       (this IS the cache key — different values = different cache entries)
        |
        +-- cache hit?  YES → reuse cached path list
        |               NO  → MobileViewLocationExpander.ExpandViewLocations()
        |                       for mobile: yield ViewName.Mobile.cshtml THEN ViewName.cshtml
        |                       for desktop: return locations unchanged
        |
        v
_ViewStart.cshtml
        reads Context.Items["IsMobile"]
        Layout = mobile ? "~/Views/Shared/_Layout.Mobile.cshtml" : "_Layout"
        |
        v
View renders with shared ViewModel
(no ViewModel changes in this or any phase)
```

### Recommended Project Structure

```
EuphoriaInn.Service/
├── Middleware/
│   └── MobileDetectionMiddleware.cs      NEW
├── ViewExpanders/
│   └── MobileViewLocationExpander.cs     NEW
├── Views/
│   ├── _ViewStart.cshtml                 MODIFIED (2-line conditional)
│   └── Shared/
│       └── _Layout.Mobile.cshtml         NEW
└── wwwroot/
    └── css/
        └── mobile.css                    NEW
```

Files that MUST NOT be modified: all other `*.cshtml` views, all controllers, all ViewModels, all domain/repository/service code.

### Pattern 1: MobileDetectionMiddleware

**What:** Lightweight ASP.NET Core middleware that runs before routing; reads the `User-Agent` header once and writes a bool to `HttpContext.Items`.

**When to use:** Any time you need a per-request flag that all downstream pipeline stages (controller, views, expander) can read from a single source of truth.

```csharp
// Source: .planning/research/ARCHITECTURE.md Pattern 2
// EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs
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

**Registration in Program.cs:**

```csharp
// Source: .planning/research/ARCHITECTURE.md Integration Points
app.UseStaticFiles();
app.UseMiddleware<MobileDetectionMiddleware>();   // ADD — before UseRouting
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

NOTE: `UseSession()` does NOT need to move. It stays after `UseRouting()` as it currently is. This is the key difference from the Wangkanai approach.

### Pattern 2: MobileViewLocationExpander

**What:** Implements `IViewLocationExpander` to prepend `.Mobile.cshtml` paths into the view resolution chain. The cache key (via `PopulateValues`) ensures desktop and mobile path sets are cached separately.

**Critical rule:** Detection logic MUST be in `PopulateValues`, not `ExpandViewLocations`. `ExpandViewLocations` only runs on cache miss — putting detection there causes stale cached paths to be served to the wrong device type.

```csharp
// Source: .planning/research/ARCHITECTURE.md Pattern 1
// EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs
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

**Registration in Program.cs** (add after `AddControllersWithViews()`):

```csharp
// Source: .planning/research/ARCHITECTURE.md Integration Points
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});
```

### Pattern 3: _ViewStart.cshtml conditional

**Current file** (`Views/_ViewStart.cshtml`, line 3): `Layout = "_Layout";`

**Updated file:**

```cshtml
@using EuphoriaInn.Domain.Interfaces
@{
    var isMobile = Context.Items["IsMobile"] is true;
    Layout = isMobile
        ? "~/Views/Shared/_Layout.Mobile.cshtml"
        : "_Layout";
}
```

This is the ONLY modification to existing view infrastructure.

### Pattern 4: _Layout.Mobile.cshtml structure

**Key structural differences from `_Layout.cshtml`:**
- Loads only `mobile.css` (not `site.css`, `calendar.css`, `quests.css`, `shop.css`, `guild-members.css`, `dm-profile.css`)
- Bootstrap offcanvas for nav (`data-bs-toggle="offcanvas"`) instead of collapse
- `<main class="container-fluid px-2 mt-2">` wrapper for `@RenderBody()` (full-width, tighter padding)
- Same `@await RenderSectionAsync("Scripts", required: false)` section (must match existing views that use `@section Scripts`)
- Adds `@await RenderSectionAsync("Styles", required: false)` (not in desktop layout — new capability)
- Uses `@inject IAuthorizationService AuthorizationService` and `@inject IUserService UserService` — same as desktop layout
- Nav items: flatten dropdowns to flat `<li>` list items in offcanvas body (D-01)
- Brand: `<i class="fas fa-dice-d20"></i> Quest Board` (D-02)

**Nav content source:** Copy the auth-conditional block from `_Layout.cshtml` lines 28-143. Flatten all `<ul class="dropdown-menu">` dropdown items into direct `<li>` items in the offcanvas body.

### Pattern 5: mobile.css baseline (INFRA-06)

**File location:** `EuphoriaInn.Service/wwwroot/css/mobile.css`

**Required content:**

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

Note: No `@media` query is needed in `mobile.css` — this file is ONLY loaded on mobile requests (by `_Layout.Mobile.cshtml`), not on desktop.

### Anti-Patterns to Avoid

- **Detection in `ExpandViewLocations`:** Only runs on cache miss. Cached desktop paths will be served to mobile users. Always detect in `PopulateValues`.
- **Moving `UseSession()` before `UseRouting()`:** Not required by the hand-rolled approach. Only Wangkanai needs this. Leave `UseSession()` where it is (after `UseRouting()` in `Program.cs`).
- **Second `AddSession()` call:** The project already has `AddSession()` with 24-hour timeout (Program.cs line 58). Do not add another.
- **Setting Layout in each mobile view:** Brittle. `_ViewStart.cshtml` handles it for all views.
- **Parallel `/Views/Mobile/` folder hierarchy:** Complex expander logic required. Use `.Mobile.cshtml` suffix in the same controller folder.
- **Loading desktop CSS in `_Layout.Mobile.cshtml`:** D-03 is explicit — mobile layout loads ONLY `mobile.css`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| View cache key management | Custom cache invalidation logic | `IViewLocationExpander.PopulateValues()` writes to `context.Values` — this IS the cache key | The Razor engine manages the cache; `context.Values` dict is the full key; identical dicts hit the same cache entry |
| Offcanvas nav drawer | Custom slide-in JS panel | Bootstrap 5 offcanvas (already on CDN) | Already loaded; battle-tested; accessible; keyboard-navigable |
| UA string parsing beyond keyword match | Regex UA parser, device database | Simple `string.Contains` keyword array | A 6-keyword check is sufficient for layout selection; full device fingerprinting is over-engineering |
| Per-view layout detection | `@if (Context.Items["IsMobile"])` blocks in desktop views | `.Mobile.cshtml` file only where desktop markup is structurally incompatible | Desktop views must not be modified; CSS-only differences belong in `mobile.css` |

**Key insight:** The Razor view engine already provides all the extension points needed. `IViewLocationExpander` is the right seam — implementing it correctly is the entire task.

---

## Common Pitfalls

### Pitfall 1: Mobile detection in ExpandViewLocations (INFRA-03 violation)

**What goes wrong:** Mobile users get desktop view paths from cache after the first desktop user hits that route.

**Why it happens:** `ExpandViewLocations` only runs on cache miss. The first request (desktop) populates the cache with desktop paths. The second request (mobile) is a cache hit — `ExpandViewLocations` never runs, and the cached desktop paths are used.

**How to avoid:** All detection logic MUST be in `PopulateValues`. Write `context.Values["isMobile"] = isMobile.ToString()`. The values dict is the full cache key — distinct values guarantee distinct cache entries.

**Warning signs:** Mobile users see desktop views intermittently or only on first request; desktop users see mobile views after a mobile user has visited.

### Pitfall 2: _ViewStart.cshtml null reference on IsMobile

**What goes wrong:** `Context.Items["IsMobile"]` returns `null` on requests where middleware hasn't run (e.g., static file requests, health check).

**Why it happens:** Static files are served by `UseStaticFiles()` which is before the middleware. But `_ViewStart.cshtml` only runs on MVC view renders — static files never invoke `_ViewStart.cshtml`. Health check (`/health`) maps to `MapHealthChecks` and also does not execute the view engine.

**How to avoid:** The `is true` pattern (`Context.Items["IsMobile"] is true`) safely handles `null` — it evaluates to `false` rather than throwing. This is already in the ARCHITECTURE.md implementation.

**Warning signs:** `NullReferenceException` in `_ViewStart.cshtml` during startup or health check requests.

### Pitfall 3: UseSession timing — leaving it in the wrong place by accident

**What goes wrong:** Mistakenly moving `UseSession()` thinking it's required (it's not — that's only Wangkanai). Or accidentally duplicating the `AddSession()` call.

**Why it happens:** The research documents both Wangkanai and hand-rolled approaches; the Wangkanai path requires middleware reorder; implementer conflates the two.

**How to avoid:** The hand-rolled approach requires NO middleware reorder. `UseSession()` stays after `UseRouting()`. Only one line is added to the middleware pipeline: `app.UseMiddleware<MobileDetectionMiddleware>()` between `UseStaticFiles()` and `UseRouting()`.

**Warning signs:** Session-based auth stops working after Phase 12 (24-hour session replaced by 10-second default).

### Pitfall 4: Missing @section declarations in _Layout.Mobile.cshtml

**What goes wrong:** Views that use `@section Scripts { ... }` or `@section Styles { ... }` throw `InvalidOperationException: The following sections have been defined but have not been rendered`.

**Why it happens:** The mobile layout is a new file; if `@await RenderSectionAsync("Scripts", required: false)` is omitted, any view that pushes a `Scripts` section will crash at runtime on mobile.

**How to avoid:** Include both `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` in `_Layout.Mobile.cshtml`. The `required: false` makes them optional — views that don't use them work fine.

**Warning signs:** `InvalidOperationException` when navigating to any page that has a `@section Scripts` block on mobile.

### Pitfall 5: Cache is per view name + context.Values — expander registration order matters

**What goes wrong:** A second expander registered after `MobileViewLocationExpander` could interfere with path expansion.

**Why it happens:** Multiple expanders are applied in registration order; each receives the output of the previous. If a later expander strips `.Mobile.` from paths, mobile resolution breaks.

**How to avoid:** No other expanders are currently registered in `Program.cs` (confirmed from file inspection). Registration is safe. Document that future expanders must not modify `.Mobile.cshtml` paths.

---

## Code Examples

### Complete Program.cs diff (both additions)

```csharp
// Source: .planning/research/ARCHITECTURE.md Integration Points

// --- ADDITION 1: after builder.Services.AddControllersWithViews() ---
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});

// --- ADDITION 2: middleware pipeline, between UseStaticFiles and UseRouting ---
app.UseStaticFiles();
app.UseMiddleware<MobileDetectionMiddleware>();   // NEW LINE
app.UseRouting();
app.UseSession();        // stays here — no move required
app.UseAuthentication();
app.UseAuthorization();
```

### Nav item translation (Desktop dropdown → Mobile flat list)

Desktop `_Layout.cshtml` (DM dropdown, lines 55-78):
```cshtml
<li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle text-danger" ...>
        <i class="fas fa-crown me-1"></i>Dungeon Master
    </a>
    <ul class="dropdown-menu">
        <li><a class="dropdown-item" asp-controller="Quest" asp-action="Create">...</a></li>
        ...
    </ul>
</li>
```

Mobile `_Layout.Mobile.cshtml` offcanvas body equivalent:
```cshtml
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
```

Note: `data-bs-dismiss="offcanvas"` on each link closes the drawer when the user navigates. This is a Bootstrap offcanvas pattern and should be on all mobile nav links.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `DisplayModeProvider` (ASP.NET MVC 4 / System.Web) | `IViewLocationExpander` (ASP.NET Core 2+) | ASP.NET Core 1.0 | `DisplayModeProvider` not ported to ASP.NET Core; `IViewLocationExpander` is the canonical replacement |
| `Microsoft.AspNetCore.Mobile` NuGet | Hand-rolled expander (no package) | Deprecated pre-Core 2 | The package does not exist for .NET 8; implement directly |
| Per-view `@{ Layout = ... }` conditionals | `_ViewStart.cshtml` centralised layout selection | Convention since ASP.NET Core 1.0 | `_ViewStart.cshtml` is the authoritative place; individual views should not override layout |

**Deprecated/outdated:**
- `DisplayModeProvider`: Not available in ASP.NET Core — does not exist for this project.
- `Microsoft.AspNetCore.Mobile`: Deprecated and removed before ASP.NET Core 2.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Microsoft.AspNetCore.Mvc.Razor` namespace and `IViewLocationExpander` interface are available without any additional NuGet package (they are part of the web SDK) | Standard Stack | Low — confirmed from existing project compilation and research docs [ASSUMED — not independently verified via `npm view` equivalent this session] |
| A2 | Bootstrap 5 offcanvas component is available on the existing CDN link (`cdn.jsdelivr.net/npm/bootstrap@5.3.0`) | Architecture Patterns | Low — Bootstrap 5.3.0 offcanvas is a stable component that exists in this version |
| A3 | `data-bs-dismiss="offcanvas"` closes the offcanvas drawer on nav link click | Code Examples (nav items) | Low — this is documented Bootstrap 5 behavior |

**All claims in the Standard Stack, architecture patterns, middleware ordering, and expander implementation are MEDIUM or higher confidence** — sourced from the canonical `.planning/research/ARCHITECTURE.md` which was produced from official Microsoft docs and interface source code.

---

## Open Questions

1. **Nav items order in offcanvas drawer**
   - What we know: D-01 says mirror the desktop nav exactly; desktop nav has: Admin (if admin) → DM tools (if DM) → Shop, Quest Log, Guild Members, Players (auth users left) → Calendar, User dropdown / Login (right)
   - What's unclear: Should the right-side desktop items (Calendar, user profile/logout) appear after the left-side items in the offcanvas, or interleaved differently?
   - Recommendation: Place all items in a single flat list in offcanvas order: Admin → DM → Shop → Quest Log → Guild Members → Players → Calendar → Profile → Logout (or Login). This is a natural vertical reading order.

2. **`@inject` directives in `_Layout.Mobile.cshtml`**
   - What we know: `_Layout.cshtml` uses `@inject IAuthorizationService AuthorizationService` and `@inject IUserService UserService` (lines not shown but implied by the nav code). `_ViewStart.cshtml` uses `@using EuphoriaInn.Domain.Interfaces`.
   - What's unclear: The exact `@inject` directives at the top of `_Layout.cshtml` are not shown in the file read — only the `@using`.
   - Recommendation: Planner should verify the `@inject` lines in `_Layout.cshtml` (they appear between the `@using` and `<!DOCTYPE html>`) and copy them verbatim to `_Layout.Mobile.cshtml`.

---

## Environment Availability

> Step 2.6: External tool dependencies for this phase.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Compile and run | ✓ (confirmed — projects target net10.0, compatible) | net10.0 | — |
| SQL Server (Windows host) | Integration tests | ✓ (SQLite in-memory used for tests) | SQLite via TestDatabase | — |
| Bootstrap 5.3.0 CDN | Offcanvas nav | ✓ (already in `_Layout.cshtml`) | 5.3.0 | — |
| Font Awesome 6.4.0 CDN | Nav icons | ✓ (already in `_Layout.cshtml`) | 6.4.0 | — |
| Google Fonts (Cinzel) CDN | Mobile brand font | ✓ (already in `_Layout.cshtml`) | current | — |

**Missing dependencies with no fallback:** None.

---

## Validation Architecture

> `workflow.nyquist_validation` is `true` in `.planning/config.json` — this section is required.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit v3 (3.2.2) + FluentAssertions 8.9.0 |
| Integration test project | `EuphoriaInn.IntegrationTests/` (targets net10.0) |
| Unit test project | `EuphoriaInn.UnitTests/` (targets net10.0) |
| Test host | `WebApplicationFactory<Program>` via `WebApplicationFactoryBase` |
| Quick run command | `dotnet test EuphoriaInn.IntegrationTests --filter "Category=Mobile"` |
| Full suite command | `dotnet test` (from solution root) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | Mobile UA → `HttpContext.Items["IsMobile"] = true` | Unit | `dotnet test EuphoriaInn.UnitTests --filter "MobileDetection"` | ❌ Wave 0 |
| INFRA-01 | Desktop UA → `HttpContext.Items["IsMobile"] = false` | Unit | `dotnet test EuphoriaInn.UnitTests --filter "MobileDetection"` | ❌ Wave 0 |
| INFRA-02 | Mobile request → `Index.Mobile.cshtml` served (when exists) | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileViewResolution"` | ❌ Wave 0 |
| INFRA-02 | Mobile request for view without `.Mobile.cshtml` → desktop view served | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileViewResolution"` | ❌ Wave 0 |
| INFRA-02 | Desktop request → desktop view served, never `.Mobile.cshtml` | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileViewResolution"` | ❌ Wave 0 |
| INFRA-03 | `PopulateValues` writes `isMobile` to `context.Values` | Unit | `dotnet test EuphoriaInn.UnitTests --filter "MobileViewLocationExpander"` | ❌ Wave 0 |
| INFRA-04 | Mobile UA request returns HTML containing `offcanvas` element | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileLayout"` | ❌ Wave 0 |
| INFRA-04 | Desktop UA request does NOT contain `offcanvas` element | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileLayout"` | ❌ Wave 0 |
| INFRA-05 | Mobile request HTML contains `mobile-layout` class on body | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileLayout"` | ❌ Wave 0 |
| INFRA-05 | Desktop request HTML does NOT contain `mobile-layout` class | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileLayout"` | ❌ Wave 0 |
| INFRA-06 | `mobile.css` is linked in mobile response HTML | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "MobileLayout"` | ❌ Wave 0 |
| INFRA-06 | `mobile.css` file contains `min-height: 44px` selector | Unit/smoke | `dotnet test EuphoriaInn.UnitTests --filter "MobileCss"` | ❌ Wave 0 |

### Test Strategy Details

**INFRA-01 — `MobileDetectionMiddleware` unit tests:**

Test the middleware in isolation using a mock `HttpContext`. Key cases:
- iPhone UA → `isMobile = true`
- Android UA → `isMobile = true`
- iPad UA → `isMobile = true`
- Windows desktop Chrome UA → `isMobile = false`
- Empty UA → `isMobile = false`
- Case-insensitive match (`mobi` lowercase) → `isMobile = true`

```csharp
// Pattern: create DefaultHttpContext, set UserAgent header, invoke middleware, assert Items["IsMobile"]
var context = new DefaultHttpContext();
context.Request.Headers.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 ...)";
var middleware = new MobileDetectionMiddleware(ctx => Task.CompletedTask);
await middleware.InvokeAsync(context);
context.Items["IsMobile"].Should().Be(true);
```

**INFRA-02 + INFRA-04 + INFRA-05 — Integration tests using WebApplicationFactoryBase:**

Use the existing `WebApplicationFactoryBase` pattern with a custom `User-Agent` header. Key insight: the existing factory creates an `HttpClient`; override the `User-Agent` on each request.

```csharp
// Mobile UA smoke test pattern
var request = new HttpRequestMessage(HttpMethod.Get, "/");
request.Headers.Add("User-Agent",
    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1");
var response = await _client.SendAsync(request);
var html = await response.Content.ReadAsStringAsync();

// INFRA-04: offcanvas nav present on mobile
html.Should().Contain("offcanvas");
html.Should().Contain("mobileNav");  // the offcanvas id

// INFRA-05: mobile layout applied
html.Should().Contain("mobile-layout");  // body class

// INFRA-06: mobile.css linked
html.Should().Contain("mobile.css");

// Desktop parity: same route, desktop UA → no offcanvas
var desktopRequest = new HttpRequestMessage(HttpMethod.Get, "/");
desktopRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0");
var desktopResponse = await _client.SendAsync(desktopRequest);
var desktopHtml = await desktopResponse.Content.ReadAsStringAsync();
desktopHtml.Should().NotContain("mobile-layout");
desktopHtml.Should().NotContain("mobile.css");
```

**INFRA-03 — `MobileViewLocationExpander` unit test:**

Test `PopulateValues` writes to `context.Values` (not just reads); test `ExpandViewLocations` inserts `.Mobile.cshtml` paths only when `context.Values["isMobile"] == "True"`.

```csharp
var expander = new MobileViewLocationExpander();
var actionContext = new ActionContext(httpContextWithMobileFlag, ...);
var expanderContext = new ViewLocationExpanderContext(actionContext, "Index", "Home", "Home", false);
expander.PopulateValues(expanderContext);
expanderContext.Values["isMobile"].Should().Be("True");
```

**INFRA-06 — CSS content smoke test:**

Read the file content and assert the `min-height: 44px` rule is present. This can be a simple file-read test or a dedicated unit test.

```csharp
var cssPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "mobile.css");
var css = File.ReadAllText(cssPath);
css.Should().Contain("min-height: 44px");
```

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests --filter "Mobile"` (fast, no DB)
- **Per wave merge:** `dotnet test EuphoriaInn.IntegrationTests --filter "Mobile"` (requires app startup, ~10-30s)
- **Phase gate:** `dotnet test` (full suite — all unit + integration tests green)

### Wave 0 Gaps

All test files for this phase are new:

- [ ] `EuphoriaInn.UnitTests/Middleware/MobileDetectionMiddlewareTests.cs` — covers INFRA-01
- [ ] `EuphoriaInn.UnitTests/ViewExpanders/MobileViewLocationExpanderTests.cs` — covers INFRA-03
- [ ] `EuphoriaInn.IntegrationTests/Controllers/MobileInfrastructureIntegrationTests.cs` — covers INFRA-02, INFRA-04, INFRA-05, INFRA-06
- [ ] `EuphoriaInn.UnitTests/Css/MobileCssTests.cs` — covers INFRA-06 CSS content check (optional; can be folded into integration tests)

No framework install needed — xUnit v3 + FluentAssertions already configured.

---

## Security Domain

> `security_enforcement` is not explicitly set to `false` in config.json — this section is required.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | No new auth endpoints; existing Identity untouched |
| V3 Session Management | No | No session changes; `UseSession()` position unchanged |
| V4 Access Control | Partial | Mobile nav reproduces the same `AuthorizeAsync` checks as desktop nav — same policy names, same guards |
| V5 Input Validation | No | No user inputs processed in this phase |
| V6 Cryptography | No | No cryptographic operations |
| V13 API | No | No new API endpoints |

### Known Threat Patterns for this phase

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mobile nav bypasses auth checks | Elevation of Privilege | `_Layout.Mobile.cshtml` MUST use identical `AuthorizeAsync` checks to `_Layout.cshtml`; any nav item visible in the mobile layout must be equally protected by controller-level `[Authorize]` attributes |
| User-Agent spoofing to access mobile views | Spoofing | Not a security concern — mobile views serve the same data as desktop views; UA is only used for layout selection, not access control |

**Security note:** The mobile detection flag (`HttpContext.Items["IsMobile"]`) controls layout rendering only — it is not used for authorization decisions. All existing authorization policies remain unchanged. The mobile layout's nav items must mirror the auth-conditional checks of the desktop layout exactly (D-01).

---

## Sources

### Primary (HIGH confidence from canonical research docs)
- `.planning/research/ARCHITECTURE.md` — complete middleware, expander, `_ViewStart.cshtml`, and layout implementations; all code examples sourced here
- `.planning/research/STACK.md` — Wangkanai evaluation and rejection rationale; hand-rolled path confirmed
- `.planning/research/SUMMARY.md` — cross-cutting summary; phase build order confirmed

### Secondary (MEDIUM confidence — official docs cited in research)
- [IViewLocationExpander Interface — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razor.iviewlocationexpander?view=aspnetcore-8.0)
- [IViewLocationExpander.cs source — dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Razor/src/IViewLocationExpander.cs)
- [Layout in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/layout?view=aspnetcore-8.0)

### Tertiary (codebase inspection — confirmed this session)
- `EuphoriaInn.Service/Program.cs` — middleware pipeline order confirmed; `AddSession()` at line 58 with 24h timeout; `UseSession()` at line 92 after `UseRouting()`
- `EuphoriaInn.Service/Views/_ViewStart.cshtml` — confirmed single-line `Layout = "_Layout"` at line 3
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — confirmed desktop nav auth-conditional structure (lines 28-143); 6 CSS files loaded; `@await RenderSectionAsync("Scripts", required: false)` at line 166
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` — confirmed `WebApplicationFactory<Program>` pattern; `HttpClient` available for UA-based integration tests

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero new packages; uses existing SDK types only
- Architecture: HIGH — fully specified in canonical research with complete C# code
- Pitfalls: HIGH — INFRA-03 cache pitfall confirmed from interface source; other pitfalls confirmed from codebase inspection
- Tests: MEDIUM — test patterns are well-established (existing integration tests use same `WebApplicationFactoryBase` + `HttpClient` pattern); specific test file content is new

**Research date:** 2026-06-23
**Valid until:** 2027-01-01 (stable — ASP.NET Core 8 `IViewLocationExpander` interface has not changed since Core 2; Bootstrap 5.3.0 offcanvas API is stable)
