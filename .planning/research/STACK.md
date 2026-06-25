# Stack Research

**Domain:** Mobile-specific Razor views — ASP.NET Core 8 MVC (Milestone 3)
**Researched:** 2026-06-23
**Confidence:** MEDIUM (NuGet registry verified; middleware ordering confirmed from official install guide; limited ASP.NET Core 8-specific community examples for IViewLocationExpander + mobile)

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| `Wangkanai.Responsive` | 7.14.0 | Registers an `IViewLocationExpander` that resolves `.Mobile.cshtml` views; ships with `UseResponsive()` middleware | Only dedicated library that handles both UA detection and view resolution in one `AddResponsive()` call; targets net8.0+; 188K downloads; actively maintained (released 2025-07-17); pulls in Detection as a transitive dep |
| `Wangkanai.Detection` | 8.20.0 | UA parsing backend used by Wangkanai.Responsive; exposes `IDetectionService` for injecting device type into controllers or Razor views | Pulled in transitively — install separately only if you need `IDetectionService` injected outside the view expander; 6.1M downloads; released 2025-07-17 |
| `IViewLocationExpander` | built-in (ASP.NET Core 8) | Interface that lets you prepend `.Mobile.cshtml` paths before falling back to the standard `.cshtml` path | Stable ASP.NET Core primitive unchanged from v2 through v8; no extra package required — used internally by Wangkanai.Responsive and available if you implement it yourself |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.Text.RegularExpressions` | built-in .NET 8 | User-agent string matching in a hand-rolled expander | Use only for the zero-dependency path (see Alternatives Considered) |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Browser DevTools device emulation | Test `.Mobile.cshtml` path resolution locally without a real phone | Toggle UA to iPhone/Android; hard-refresh clears the Razor view-location cache |
| `curl -A "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"` | CLI smoke test in CI to verify mobile layout renders without a browser | Returns the HTML; grep for a mobile-specific landmark element to confirm the right view was selected |

---

## Installation

```bash
# Adds Wangkanai.Responsive + its transitive dependency Wangkanai.Detection
dotnet add EuphoriaInn.Service/EuphoriaInn.Service.csproj package Wangkanai.Responsive --version 7.14.0
```

The `IViewLocationExpander` interface itself needs no package — it lives in `Microsoft.AspNetCore.Mvc.Razor`, already present via the web SDK.

---

## Integration with Existing Program.cs

The project already registers `AddSession()` with a 24-hour idle timeout and calls `UseSession()` after `UseRouting()`. Wangkanai.Responsive uses session to store per-user device-preference overrides.

**Critical changes required:**

1. `UseSession()` must move to BEFORE `UseRouting()` (Wangkanai requires session already active when the responsive middleware runs).
2. `UseResponsive()` must be inserted BEFORE `UseRouting()`.
3. **Do NOT add a second `AddSession()` call** — the existing one is sufficient; Wangkanai.Responsive will find it on the DI container.

```csharp
// BEFORE (current Program.cs layout):
app.UseRouting();
app.UseSession();           // currently after routing
app.UseAuthentication();
app.UseAuthorization();

// AFTER:
app.UseSession();           // moved before routing
app.UseResponsive();        // new — MUST be before UseRouting
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

Service registration — one addition after `AddControllersWithViews()`:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddResponsive();   // new — internally calls AddDetection()
```

`AddResponsive()` calls `AddDetection()` internally. No separate `AddDetection()` call is needed.

---

## View Naming Convention

Wangkanai.Responsive uses a suffix convention — device type is inserted before `.cshtml`:

| File | Served to |
|------|-----------|
| `Index.cshtml` | Desktop (and any device without a more-specific variant) |
| `Index.Mobile.cshtml` | Smartphones (iPhone, Android phone, any UA containing `Mobi`) |
| `Index.Tablet.cshtml` | Tablets (optional; omit to share desktop layout with tablets) |

The expander checks the most-specific suffix first, then falls back to the plain `.cshtml`. **All existing `.cshtml` files continue to work unchanged** — the fallback guarantees this. Creating a `.Mobile.cshtml` file is opt-in per view.

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| `Wangkanai.Responsive` (NuGet) | Hand-rolled `IViewLocationExpander` + regex UA match | Zero external dependencies; only need phone vs. desktop (no tablet split, no user-preference override). ~30 lines of C#. Read `context.ActionContext.HttpContext.Request.Headers["User-Agent"]` in `PopulateValues`, set `context.Values["mobile"] = "1"`, then prepend `{0}.Mobile.cshtml` paths in `ExpandViewLocations`. Register via `services.Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new MobileViewLocationExpander()))`. No middleware change needed. |
| `Wangkanai.Responsive` (NuGet) | Single Bootstrap 5 responsive layout with CSS show/hide | Correct choice if pages need only to hide/show elements. Not sufficient here: the calendar requires a structurally different markup (agenda list vs. 7-column grid), and the quest board removes decorative poster images — these require separate view files, not CSS |
| `Wangkanai.Responsive` (NuGet) | `DisplayModeProvider` (ASP.NET MVC 4 era) | Not available in ASP.NET Core — this API was not ported from `System.Web.Mvc` |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `51Degrees.onPremise` or `DeviceDetector.NET` | Full device-fingerprint databases with 30–100 MB model files; vastly over-engineered for a binary phone/desktop split | `Wangkanai.Detection` (UA string parsing is sufficient) |
| A full PWA library (Blazor Mobile Bindings, MAUI hybrid) | Requires a framework change away from ASP.NET Core MVC; incompatible with the project constraint | Stay on MVC; add `.Mobile.cshtml` views |
| `Microsoft.AspNetCore.Mobile` NuGet | Deprecated and removed before ASP.NET Core 2; does not exist for .NET 8 | `Wangkanai.Responsive` or a hand-rolled `IViewLocationExpander` |
| Second `AddSession()` call in Program.cs | The project already registers session with a 24-hour idle timeout; adding a second call (as shown in Wangkanai's basic install example with `IdleTimeout = 10s`) would override the existing timeout to 10 seconds, breaking session-based auth | Reuse the existing `AddSession()` registration — do not follow Wangkanai's install example literally |
| `UseDetection()` middleware (without `UseResponsive()`) | Detection middleware alone populates `IDetectionService` but does NOT register the `IViewLocationExpander` — views won't switch | Use `UseResponsive()` which does both |

---

## Stack Patterns by Variant

**If you want full Wangkanai integration (recommended):**
- Install `Wangkanai.Responsive 7.14.0`
- Move `UseSession()` before `UseRouting()` in Program.cs
- Add `UseResponsive()` before `UseRouting()`
- Add `services.AddResponsive()` after `AddControllersWithViews()`
- Name mobile views `{Action}.Mobile.cshtml` alongside the existing `{Action}.cshtml`
- Optionally inject `IDetectionService` into controllers that need to branch on device type in C# logic

**If you want zero external dependencies:**
- Implement `IViewLocationExpander` manually (~30 lines)
- `PopulateValues`: `context.Values["mobile"] = Regex.IsMatch(ua, @"Mobi|Android|iPhone|iPad", RegexOptions.IgnoreCase) ? "1" : "0"`
- `ExpandViewLocations`: when `context.Values["mobile"] == "1"`, yield `"/Views/{1}/{0}.Mobile.cshtml"`, `"/Views/Shared/{0}.Mobile.cshtml"` before each standard path
- Register: `services.Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new MobileViewLocationExpander()))`
- No middleware change needed; no session dependency; no NuGet package

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| `Wangkanai.Responsive 7.14.0` | net8.0, net9.0, net10.0 | Requires `Wangkanai.Detection >= 8.20.0` as transitive dep |
| `Wangkanai.Detection 8.20.0` | net8.0, net9.0, net10.0 | No conflicts with Identity, EF Core, AutoMapper, or any existing dependency |
| Both packages | `Microsoft.AspNetCore.Mvc 8.x` | No known conflicts |

---

## Sources

- [NuGet Gallery — Wangkanai.Detection 8.20.0](https://www.nuget.org/packages/Wangkanai.Detection/) — version 8.20.0 and net8.0 target framework verified (MEDIUM confidence)
- [NuGet Gallery — Wangkanai.Responsive 7.14.0](https://www.nuget.org/packages/Wangkanai.Responsive/) — version 7.14.0, net8.0 target, dependency on Detection >= 8.20.0 verified (MEDIUM confidence)
- [Wangkanai Responsive INSTALL.md](https://github.com/wangkanai/wangkanai/blob/main/Responsive/INSTALL.md) — AddResponsive, UseResponsive, middleware order (UseSession before UseResponsive before UseRouting), view suffix convention (.Mobile.cshtml) (MEDIUM confidence — official repo install guide)
- [IViewLocationExpander source — dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Razor/src/IViewLocationExpander.cs) — interface signature and cache-key semantics (MEDIUM confidence — official source)
- [Jack Histon — IViewLocationExpander walkthrough](https://jackhiston.com/2017/10/24/extending-the-razor-view-engine-with-view-location-expanders/) — PopulateValues/ExpandViewLocations pattern; stable through ASP.NET Core 8 (MEDIUM confidence — community blog, interface unchanged)
- [IHttpContextAccessor — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.ihttpcontextaccessor?view=aspnetcore-8.0) — HttpContext access pattern in view engine components (MEDIUM confidence)

---

*Stack research for: Mobile-specific Razor views in ASP.NET Core 8 MVC*
*Researched: 2026-06-23*
