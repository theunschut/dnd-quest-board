# Architecture Research

**Domain:** Mobile-specific Razor views in ASP.NET Core 8 MVC using IViewLocationExpander
**Researched:** 2026-06-23
**Confidence:** MEDIUM

---

## Standard Architecture

### System Overview

```
HTTP Request (mobile User-Agent)
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│  MobileDetectionMiddleware                                  │
│  Reads User-Agent → HttpContext.Items["IsMobile"] = true    │
│  (runs before UseRouting)                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  MVC Pipeline (UseRouting → UseAuthorization)               │
│  Controller executes, returns ViewResult with ViewModel     │
│  No controller changes required                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  RazorViewEngine — view resolution                          │
│                                                             │
│  MobileViewLocationExpander.PopulateValues()                │
│    reads HttpContext.Items["IsMobile"]                      │
│    stores in context.Values["isMobile"]  ← cache key       │
│                                                             │
│  MobileViewLocationExpander.ExpandViewLocations()           │
│    for each location yields:                                │
│      1. /Views/{1}/{0}.Mobile.cshtml  ← checked first      │
│      2. /Views/{1}/{0}.cshtml         ← desktop fallback    │
│    for Shared yields:                                       │
│      1. /Views/Shared/{0}.Mobile.cshtml                     │
│      2. /Views/Shared/{0}.cshtml                            │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  _ViewStart.cshtml                                          │
│  Sets Layout = mobile or desktop based on HttpContext.Items │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
               View renders with shared ViewModel
         (same ViewModel type — no controller changes)
```

### Component Responsibilities

| Component | Responsibility | Location |
|-----------|----------------|----------|
| `MobileDetectionMiddleware` | Parse User-Agent, set `HttpContext.Items["IsMobile"]` | `EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs` |
| `MobileViewLocationExpander` | Inject `.Mobile.cshtml` paths into view resolution chain | `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs` |
| `_Layout.Mobile.cshtml` | Shared mobile HTML shell, slim navbar, mobile CSS | `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` |
| `_ViewStart.cshtml` | Conditionally sets `Layout` to mobile or desktop | `EuphoriaInn.Service/Views/_ViewStart.cshtml` (modified) |
| `*.Mobile.cshtml` views | Mobile-specific page content, opt-in per page | `EuphoriaInn.Service/Views/{Controller}/` alongside desktop views |
| `mobile.css` | Mobile-specific stylesheet | `EuphoriaInn.Service/wwwroot/css/mobile.css` (new) |

---

## Architectural Patterns

### Pattern 1: IViewLocationExpander — suffix injection with fallback

**What:** A class implementing `IViewLocationExpander` that prepends `.Mobile.cshtml` paths ahead of every default location. The Razor engine tries paths in yield order — the first file that exists wins. Because every mobile path is paired with its desktop fallback in the same yield block, missing `.Mobile.cshtml` files automatically fall through to the desktop view.

**When to use:** Any time you want optional per-page overrides without modifying controllers. This is the canonical ASP.NET Core mechanism for view location customisation.

**Two-method contract:**

- `PopulateValues` runs on every request. It writes values into `context.Values` which form the cache key. Two requests with identical `context.Values` skip `ExpandViewLocations` and reuse cached paths. **This is where mobile detection state must be written.**
- `ExpandViewLocations` runs only on cache miss (new unique key combination). It receives the default location list and returns an expanded list. Order determines priority.

**Complete implementation:**

```csharp
// EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs
using Microsoft.AspNetCore.Mvc.Razor;

namespace EuphoriaInn.Service.ViewExpanders;

public class MobileViewLocationExpander : IViewLocationExpander
{
    private const string IsMobileKey = "isMobile";

    // Step 1 — runs every request, determines cache key.
    // Read the flag set by middleware; store it so the cache splits
    // desktop (isMobile=false) and mobile (isMobile=true) path sets.
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var isMobile = context.ActionContext.HttpContext.Items["IsMobile"] is true;
        context.Values[IsMobileKey] = isMobile.ToString();
    }

    // Step 2 — runs only when cache misses (i.e. first desktop request
    // and first mobile request per view name).
    // For mobile requests, prepend .Mobile.cshtml ahead of each default path.
    // For desktop requests, return the original locations unchanged.
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
            // Yield the .Mobile.cshtml variant first, then the original.
            // The engine picks the first path that resolves to a real file.
            yield return location.Replace(".cshtml", ".Mobile.cshtml",
                StringComparison.OrdinalIgnoreCase);
            yield return location;
        }
    }
}
```

**Registration in Program.cs** — add before `builder.Build()`:

```csharp
// Program.cs — add alongside AddControllersWithViews()
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});
```

---

### Pattern 2: Mobile detection middleware — set once, read everywhere

**What:** A lightweight middleware that runs before routing and sets a typed flag in `HttpContext.Items`. Controllers, views, layout, and the expander all read from the same place. No action filter needed; no DI service needed.

**Why middleware, not inside the expander:** `PopulateValues` is called by the view engine, not the request pipeline. Doing User-Agent string parsing inside `PopulateValues` would duplicate work on every view resolution call (partials, layouts, the main view). Middleware runs exactly once per request. The expander reads the already-computed result.

**Why not an action filter:** Action filters run after model binding and authorize attributes. Middleware runs earlier and is unconditional — it applies to all routes consistently. The flag is also needed in `_ViewStart.cshtml` which runs outside the action filter lifecycle.

**Why not a third-party library:** The project constraint is no framework changes. The User-Agent `Mobi` keyword is specified in the W3C mobile browser spec and is present in all modern mobile browser UAs (iOS Safari, Chrome for Android, Samsung Internet). A six-keyword check is reliable enough for layout selection; it does not need to be precise device identification.

**Implementation:**

```csharp
// EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs
namespace EuphoriaInn.Service.Middleware;

public class MobileDetectionMiddleware(RequestDelegate next)
{
    // RFC 2616 compliant mobile UA keywords. Covers iOS, Android, Windows Phone,
    // and generic "Mobi" (W3C spec for mobile browsers).
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

**Registration in Program.cs** — add in the middleware pipeline, before `UseRouting`:

```csharp
app.UseStaticFiles();

// Must come before UseRouting so IsMobile is set before
// any view resolution begins.
app.UseMiddleware<MobileDetectionMiddleware>();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

---

### Pattern 3: Mobile layout — _ViewStart.cshtml conditional selection

**What:** `_ViewStart.cshtml` is executed before every full view (not partials). Set `Layout` conditionally based on `HttpContext.Items["IsMobile"]`. This is the authoritative place for layout selection — it means individual mobile views do **not** need to specify `@{ Layout = "..." }`.

**Updated _ViewStart.cshtml:**

```cshtml
@{
    var isMobile = Context.Items["IsMobile"] is true;
    Layout = isMobile
        ? "~/Views/Shared/_Layout.Mobile.cshtml"
        : "_Layout";
}
```

**_Layout.Mobile.cshtml structure:**

```cshtml
@using EuphoriaInn.Domain.Interfaces
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - D&D Quest Board</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;600&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="~/css/mobile.css" asp-append-version="true" />
    @await RenderSectionAsync("Styles", required: false)
</head>
<body class="mobile-layout">
    <nav class="navbar navbar-dark bg-dark">
        <div class="container-fluid">
            <a class="navbar-brand" asp-controller="Home" asp-action="Index">
                <i class="fas fa-dice-d20"></i> Quest Board
            </a>
            <button class="navbar-toggler" type="button"
                    data-bs-toggle="offcanvas" data-bs-target="#mobileNav">
                <span class="navbar-toggler-icon"></span>
            </button>
        </div>
    </nav>

    <!-- Off-canvas nav for mobile (replaces dropdown-heavy desktop nav) -->
    <div class="offcanvas offcanvas-end" id="mobileNav">
        <div class="offcanvas-header">
            <h5 class="offcanvas-title">Menu</h5>
            <button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>
        </div>
        <div class="offcanvas-body">
            @* Same nav logic as _Layout.cshtml — copy auth-conditional nav items *@
        </div>
    </div>

    <div class="container-fluid px-2 mt-2">
        @RenderBody()
    </div>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Key differences from `_Layout.cshtml`:
- Loads `mobile.css` only (not the five desktop stylesheets that include desktop-specific rules)
- Uses Bootstrap offcanvas for nav instead of collapse — offcanvas is a better mobile pattern for deep nav menus
- `container-fluid px-2` instead of `container mt-3` — full-width, tighter padding on small screens
- The five desktop CSS files (`site.css`, `calendar.css`, `quests.css`, `shop.css`, `guild-members.css`) are not loaded; `mobile.css` provides mobile-appropriate overrides

---

### Pattern 4: File naming convention — ViewName.Mobile.cshtml alongside desktop view

**Recommended convention:** Place mobile views in the same controller folder as their desktop counterpart, with the `.Mobile.cshtml` suffix:

```
Views/
├── Home/
│   ├── Index.cshtml             ← desktop (unchanged)
│   └── Index.Mobile.cshtml      ← mobile override (new)
├── Quest/
│   ├── Details.cshtml           ← desktop (unchanged)
│   ├── Details.Mobile.cshtml    ← mobile override (new)
│   ├── _QuestCard.cshtml        ← desktop partial (unchanged)
│   └── _QuestCard.Mobile.cshtml ← mobile partial (only if needed)
├── Calendar/
│   ├── Index.cshtml             ← desktop (unchanged)
│   └── Index.Mobile.cshtml      ← mobile override (new — agenda view)
└── Shared/
    ├── _Layout.cshtml           ← desktop layout (unchanged)
    └── _Layout.Mobile.cshtml    ← mobile layout (new)
```

**Why not a separate `/Views/Mobile/` folder:** A parallel folder structure means every path transform in `ExpandViewLocations` needs controller-subfolder logic. The suffix approach lets the expander do a simple string replace on the incoming location list — one line of code, no edge cases. It also keeps related views physically co-located.

---

### Pattern 5: Partial view strategy — only create mobile partials when they differ substantially

**How partials interact with the expander:** `ExpandViewLocations` is called for every view lookup, including partials rendered via `<partial name="..." />` and `@Html.PartialAsync(...)`. The expander will therefore look for `_QuestCard.Mobile.cshtml` before `_QuestCard.cshtml` on a mobile request. This is automatic — no extra code needed.

**Recommendation:**
- Partials that are purely structural scaffolding (e.g. `_QuestFormScripts.cshtml`) — no mobile variant needed. The scripts work the same.
- Partials that render complex layout (`_QuestCard.cshtml`, `_Calendar.cshtml`) — create a mobile variant only if the desktop markup is too wide or image-heavy to work on small screens. Favour CSS-only fixes over duplicate markup where possible.
- Partials rendered inside a mobile page view that is itself already a `.Mobile.cshtml` — those are only reached from the mobile view, so they can reference the desktop partial if the calling view already adapts the outer structure.

---

## Recommended Project Structure (new files only)

```
EuphoriaInn.Service/
├── Middleware/
│   └── MobileDetectionMiddleware.cs        ← NEW
├── ViewExpanders/
│   └── MobileViewLocationExpander.cs       ← NEW
├── Views/
│   ├── _ViewStart.cshtml                   ← MODIFIED (conditional layout only)
│   ├── Shared/
│   │   └── _Layout.Mobile.cshtml           ← NEW
│   ├── Home/
│   │   └── Index.Mobile.cshtml             ← NEW (phase 2)
│   ├── Calendar/
│   │   └── Index.Mobile.cshtml             ← NEW (phase 2, agenda view)
│   ├── Quest/
│   │   ├── Details.Mobile.cshtml           ← NEW (phase 3)
│   │   └── _QuestCard.Mobile.cshtml        ← NEW if needed
│   └── [other controllers]/
│       └── *.Mobile.cshtml                 ← NEW per phase
└── wwwroot/
    └── css/
        └── mobile.css                      ← NEW
```

**Files that must NOT be modified:** All existing `*.cshtml` views (except `_ViewStart.cshtml`), all controllers, all ViewModels, all repository/domain/service layer files.

---

## Data Flow

### Mobile request — full path

```
Browser sends GET /  (User-Agent: Mozilla/5.0 ... iPhone ...)
    │
    ▼
MobileDetectionMiddleware
    HttpContext.Items["IsMobile"] = true
    │
    ▼
HomeController.Index()
    returns View(viewModel)   ← no change to controller
    │
    ▼
RazorViewEngine begins view resolution for "Index"
    │
    ▼
MobileViewLocationExpander.PopulateValues()
    reads HttpContext.Items["IsMobile"] → true
    context.Values["isMobile"] = "True"   ← unique cache key
    │
    ▼
Cache miss for ("Index", isMobile=True)?
    YES → MobileViewLocationExpander.ExpandViewLocations()
        yields: /Views/Home/Index.Mobile.cshtml  ← checked first
        yields: /Views/Home/Index.cshtml
    │
    ▼
File system check: /Views/Home/Index.Mobile.cshtml exists?
    YES → render it
    NO  → fall through to /Views/Home/Index.cshtml
    │
    ▼
_ViewStart.cshtml executes
    Context.Items["IsMobile"] is true
    Layout = "~/Views/Shared/_Layout.Mobile.cshtml"
    │
    ▼
Response rendered with mobile layout + mobile view content
    ViewModel is identical to what desktop view would receive
```

### Desktop request — path

```
Browser sends GET /  (User-Agent: Mozilla/5.0 ... Chrome/Windows ...)
    │
    ▼
MobileDetectionMiddleware
    HttpContext.Items["IsMobile"] = false
    │
    ▼
MobileViewLocationExpander.PopulateValues()
    context.Values["isMobile"] = "False"   ← different cache key
    │
    ▼
MobileViewLocationExpander.ExpandViewLocations()
    isMobile is false → returns viewLocations unchanged
    │
    ▼
Normal resolution: /Views/Home/Index.cshtml
_ViewStart.cshtml → Layout = "_Layout"  (desktop layout)
```

---

## Integration Points

### Program.cs — complete registration diff

Three additions in order:

```csharp
// 1. Register expander (add alongside AddControllersWithViews)
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});

// ... all existing service registrations unchanged ...

var app = builder.Build();

// 2. Middleware pipeline — add after UseStaticFiles, before UseRouting
app.UseStaticFiles();
app.UseMiddleware<MobileDetectionMiddleware>();   // ← ADD THIS LINE
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

### _ViewStart.cshtml — single conditional

```cshtml
@{
    var isMobile = Context.Items["IsMobile"] is true;
    Layout = isMobile
        ? "~/Views/Shared/_Layout.Mobile.cshtml"
        : "_Layout";
}
```

This is the **only** modification to existing view infrastructure. The existing `_Layout.cshtml` is not touched.

---

## Build Order (respects dependency chain)

| Step | Deliverable | Depends On | Notes |
|------|------------|------------|-------|
| 1 | `MobileDetectionMiddleware` + Program.cs registration | Nothing | Foundation — all other components read from `HttpContext.Items["IsMobile"]` |
| 2 | `MobileViewLocationExpander` + Program.cs registration | Step 1 (HttpContext.Items must be set before expander reads it) | Core mechanism — enables `.Mobile.cshtml` fallback |
| 3 | `_Layout.Mobile.cshtml` + `mobile.css` | Step 2 | Shared shell; every mobile page needs this to render correctly |
| 4 | `_ViewStart.cshtml` update | Step 3 (layout file must exist before _ViewStart references it) | Activates mobile layout for all views |
| 5 | `Home/Index.Mobile.cshtml` | Steps 1–4 | First page; proves the full pipeline end-to-end before building more |
| 6 | `Calendar/Index.Mobile.cshtml` | Steps 1–4 | Highest complexity; agenda view is a significant departure from desktop grid |
| 7 | `Quest/Details.Mobile.cshtml` | Steps 1–4 | High-traffic page for players |
| 8+ | Remaining `*.Mobile.cshtml` views | Steps 1–4 | Each is independent; can be added in any order |

Steps 1–4 are a single atomic unit — do not split across phases. They produce no user-visible change until step 5 adds a first mobile view. This is intentional: the infrastructure is safe to ship before mobile views exist because all mobile requests silently fall through to desktop views.

---

## Anti-Patterns

### Anti-Pattern 1: Mobile detection inside ExpandViewLocations

**What people do:** Read `context.ActionContext.HttpContext.Request.Headers.UserAgent` inside `ExpandViewLocations` to decide whether to inject mobile paths.

**Why it's wrong:** `ExpandViewLocations` is only called on cache miss. On a cache hit the User-Agent is never read again — but the cached paths from the previous call (which may have been a desktop request) are reused. The expander cache then serves desktop paths to mobile users until the cache expires.

**Do this instead:** Always detect in `PopulateValues` and store in `context.Values`. The values dictionary IS the cache key — different values guarantee different cache entries and force `ExpandViewLocations` to run per device type.

---

### Anti-Pattern 2: Setting Layout inside each mobile view

**What people do:** Every `.Mobile.cshtml` begins with `@{ Layout = "~/Views/Shared/_Layout.Mobile.cshtml"; }`.

**Why it's wrong:** Brittle — if the layout path changes, every mobile view needs updating. Also requires all desktop views to explicitly set their layout, removing the convention provided by `_ViewStart.cshtml`.

**Do this instead:** Set layout in `_ViewStart.cshtml` based on `HttpContext.Items["IsMobile"]`. Mobile views inherit automatically.

---

### Anti-Pattern 3: Separate `/Views/Mobile/` folder hierarchy

**What people do:** Create `/Views/Mobile/Home/Index.cshtml` and modify `ExpandViewLocations` to prepend the `Mobile/` directory.

**Why it's wrong:** Requires path manipulation that must account for controller subdirectories, Area routes, and shared views. The expander logic becomes non-trivial. Co-location is also lost — desktop and mobile views for the same page are in different folders.

**Do this instead:** Use the `.Mobile.cshtml` suffix in the same controller folder. The expander is a one-line string replace. Co-location is preserved.

---

### Anti-Pattern 4: Reading HttpContext.Items in view code with inline conditionals

**What people do:** `@if (Context.Items["IsMobile"] is true)` blocks scattered throughout desktop views to conditionally render mobile markup inline.

**Why it's wrong:** Defeats the purpose of separate mobile views. Desktop views become bloated with device-conditional branches. Violates the constraint that desktop views must not be modified.

**Do this instead:** Create a `.Mobile.cshtml` file only when the layout changes substantially. If only a CSS tweak is needed, handle it in `mobile.css` via media queries (no view change at all).

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (small group) | Single middleware, single expander, per-page opt-in mobile views — appropriate |
| Medium | Add a cookie-based override (let users force desktop on mobile) — middleware reads cookie before User-Agent; sets `HttpContext.Items["IsMobile"]` to cookie value if present |
| Large | Add tablet as a third device class — add `DeviceType` string to `context.Values` and yield `.Tablet.cshtml` before `.cshtml` in the expander |

---

## Sources

- [IViewLocationExpander Interface — Microsoft Learn (ASP.NET Core 8)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razor.iviewlocationexpander?view=aspnetcore-8.0) — MEDIUM confidence (official docs)
- [IViewLocationExpander.ExpandViewLocations Method — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razor.iviewlocationexpander.expandviewlocations?view=aspnetcore-8.0) — MEDIUM confidence (official docs)
- [ViewLocationExpanderContext.ActionContext Property — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razor.viewlocationexpandercontext.actioncontext?view=aspnetcore-8.0) — MEDIUM confidence (official docs)
- [Layout in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/layout?view=aspnetcore-8.0) — MEDIUM confidence (official docs); confirmed _ViewStart.cshtml conditional Layout pattern
- [IViewLocationExpander.cs source — dotnet/aspnetcore on GitHub](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Razor/src/IViewLocationExpander.cs) — MEDIUM confidence (source of truth for interface contract)
- [Extending The Razor View Engine With View Location Expanders — Jack Histon](https://jackhiston.com/2017/10/24/extending-the-razor-view-engine-with-view-location-expanders/) — LOW confidence (community blog, pre-.NET 8; registration pattern confirmed against official docs)
- [Searched Locations For The Razor View Engine — Khalid Abuhakmeh](https://khalidabuhakmeh.com/searched-locations-razor-view-engine-aspdotnet) — LOW confidence (community); confirmed default format patterns `/Views/{1}/{0}.cshtml`

---
*Architecture research for: Mobile Razor views, ASP.NET Core 8 MVC*
*Researched: 2026-06-23*
