# Project Research Summary

**Project:** D&D Quest Board — Milestone 3: Mobile Version
**Domain:** Mobile-specific Razor views in ASP.NET Core 8 MVC
**Researched:** 2026-06-23
**Confidence:** MEDIUM

## Executive Summary

Milestone 3 delivers a mobile-optimised experience for the D&D Quest Board by adding `.Mobile.cshtml` view variants alongside existing desktop views, using ASP.NET Core's built-in `IViewLocationExpander` mechanism for transparent device-based view routing. The approach requires no controller changes, no ViewModel changes, and no changes to the repository or domain layers. A lightweight middleware detects the User-Agent once per request, stores the result in `HttpContext.Items`, and the view expander reads that flag to prepend `.Mobile.cshtml` paths before falling back to the desktop views. All existing flows remain intact by design — a missing `.Mobile.cshtml` silently falls through to the desktop view.

The recommended implementation uses a hand-rolled `IViewLocationExpander` (~30 lines, zero new dependencies) rather than the `Wangkanai.Responsive` NuGet package. The hand-rolled path is lower-risk: no transitive dependencies, no middleware reordering required. Wangkanai requires moving `UseSession()` before `UseRouting()` in `Program.cs` and must not copy-paste its example `AddSession()` call, which would override the project's 24-hour session timeout with a 10-second default.

The highest-complexity mobile views are the Calendar (agenda list vs. 7-column grid — structurally different markup) and the Quest Board index (removal of decorative poster images). The infrastructure block (middleware + expander + mobile layout + `_ViewStart.cshtml` update) must be built as an atomic unit before any individual mobile views are added; once in place, each `.Mobile.cshtml` is independent and deliverable incrementally.

## Key Findings

### Recommended Stack

**Core components (hand-rolled, zero new NuGet dependencies):**
- `MobileDetectionMiddleware` — ~20 lines; parses User-Agent once per request; keywords: Mobi, Android, iPhone, iPad, Windows Phone, BlackBerry; sets `HttpContext.Items["IsMobile"]`
- `MobileViewLocationExpander` — implements `IViewLocationExpander` (built-in ASP.NET Core 8); prepends `.Mobile.cshtml` paths; cache-key written in `PopulateValues`, path expansion in `ExpandViewLocations`
- Bootstrap 5 offcanvas (already on CDN) — replaces desktop collapse nav with a slide-in drawer on mobile

**Optional NuGet alternative:**
- `Wangkanai.Responsive 7.14.0` + `Wangkanai.Detection 8.20.0` (transitive) — packages UA detection and view expander together; adds tablet detection and user-preference cookies; requires middleware reorder; NOT recommended due to session-timeout trap

**Stay as-is:**
- All existing NuGet packages, CDN libraries, Bootstrap 5, jQuery, FontAwesome — no changes

### Expected Features (Milestone 3 Scope)

**Must have (table stakes):**
- Mobile layout shell (`_Layout.Mobile.cshtml`) with Bootstrap offcanvas nav
- Quest Board index mobile view — card list, no poster images, tap-friendly
- Calendar mobile view — agenda/list replacing 7-column grid (highest complexity)
- Quest Details mobile view — touch-friendly voting and signup UI
- `mobile.css` — touch target sizing (44px min), mobile typography scale

**Should have:**
- Quest Create / Edit mobile views (DMs create quests from phone)
- DM Manage quest mobile view
- Account pages mobile views (login, register, profile)
- Guild Members mobile view

**Defer:**
- Tablet-specific views (`.Tablet.cshtml`)
- PWA manifest and service worker
- Push notifications
- "Force desktop" cookie override

### Architecture Approach

Strictly additive to the Service layer only — no controllers, ViewModels, repositories, or domain services are modified.

**Build order is fixed:**
1. `MobileDetectionMiddleware` — UA parsing, sets `HttpContext.Items["IsMobile"]`
2. `MobileViewLocationExpander` — registered in `Program.cs` via `RazorViewEngineOptions`
3. `_Layout.Mobile.cshtml` — mobile HTML shell; Bootstrap offcanvas nav; loads `mobile.css`
4. `_ViewStart.cshtml` (modified) — single conditional sets Layout based on `HttpContext.Items["IsMobile"]`
5. `mobile.css` — mobile overrides for typography, spacing, touch targets
6. Per-page `*.Mobile.cshtml` — opt-in; created only where desktop markup is structurally incompatible

**Key implementation details:**
- Detection **must** live in `PopulateValues`, not `ExpandViewLocations` — the latter only runs on cache miss; putting detection in the wrong method serves cached desktop paths to mobile users
- Partial views participate in `ExpandViewLocations` automatically — `_QuestCard.Mobile.cshtml` will be checked before `_QuestCard.cshtml` on mobile requests with no extra code
- View suffix `.Mobile.cshtml` alongside desktop view (e.g., `Views/Quest/Details.Mobile.cshtml` next to `Views/Quest/Details.cshtml`) — NOT a separate `/Views/Mobile/` folder hierarchy

### Critical Pitfalls

1. **Mobile detection inside `ExpandViewLocations`** — only called on cache miss; cached desktop paths are then served to mobile users. Always detect in `PopulateValues` and write to `context.Values`.
2. **Adding a second `AddSession()` call when following Wangkanai's install guide** — overrides the project's 24-hour session timeout with 10 seconds, breaking auth. Reuse the existing `AddSession()`.
3. **Using `UseDetection()` without `UseResponsive()`** — Detection alone does not register the `IViewLocationExpander`; views will not switch.
4. **Setting Layout in each individual mobile view** — brittle. Set once in `_ViewStart.cshtml`; mobile views inherit automatically.
5. **Creating a `/Views/Mobile/` parallel folder hierarchy** — complex expander path manipulation required. Use `.Mobile.cshtml` suffix in the same controller folder; expander is a one-line string replace.

## Implications for Roadmap

### Phase 10: Mobile Infrastructure Foundation
**Rationale:** All mobile views depend on the middleware + expander + layout + `_ViewStart` being in place as an atomic unit. Safe to ship with no `.Mobile.cshtml` files — all requests fall through to desktop views unchanged.
**Delivers:** Mobile detection pipeline, view routing, mobile layout shell, `mobile.css` baseline; zero user-visible change for desktop users
**Implements:** `MobileDetectionMiddleware`, `MobileViewLocationExpander`, `_Layout.Mobile.cshtml`, `_ViewStart.cshtml` update, `mobile.css`
**Avoids:** Detection-in-ExpandViewLocations pitfall; second AddSession() pitfall; Layout-in-each-view pitfall

### Phase 11: Core Player-Facing Mobile Views
**Rationale:** Players are the primary mobile users. Quest board index and Quest Details are the highest-traffic routes for this persona.
**Delivers:** `Home/Index.Mobile.cshtml` (card list, no poster images), `Quest/Details.Mobile.cshtml` (touch voting UI)
**Uses:** Bootstrap 5 card grid, mobile.css touch target rules

### Phase 12: Calendar Mobile View
**Rationale:** Highest-complexity mobile view — desktop 7-column grid is structurally unusable on a phone. Isolated to contain risk.
**Delivers:** `Calendar/Index.Mobile.cshtml` (agenda list), mobile calendar CSS
**Research flag:** Calendar is the highest-complexity mobile adaptation. The phase plan should include a spike on the agenda layout before committing to markup.

### Phase 13: DM Mobile Views
**Rationale:** DMs create and manage quests — lower mobile urgency than player-facing views, but forms must work on touch.
**Delivers:** `Quest/Create.Mobile.cshtml`, `Quest/Manage.Mobile.cshtml`, DM directory mobile views

### Phase 14: Account and Secondary Pages
**Rationale:** Simplest pages; Bootstrap's responsive utilities may handle these without separate `.Mobile.cshtml` files. Audit each page — only create view files where desktop markup is structurally incompatible.
**Delivers:** Mobile-ready Account pages (CSS or dedicated views), Shop index, Guild Members
**Decision point:** CSS-only first; create `.Mobile.cshtml` only where needed

### Phase Ordering Rationale

- Phase 10 is mandatory first — prerequisite for all others; can ship without breaking anything
- Phases 11–14 reflect player-facing priority over DM-facing over administrative
- Calendar (Phase 12) isolated from bulk player views (Phase 11) — failed calendar approach should not block quest board delivery
- Phase 14 deferred — lowest risk pages, likely CSS-only

### Research Flags

Needs research during planning:
- **Phase 10 (infrastructure):** Confirm hand-rolled vs. Wangkanai final decision before starting
- **Phase 12 (Calendar):** Agenda view markup needs a spike; `CalendarViewModel` may need a reshaping helper

Standard patterns (skip research):
- **Phase 11:** Bootstrap card list; existing ViewModel sufficient
- **Phase 13:** Same pattern as Phase 11
- **Phase 14:** CSS media query audit; no new architecture decisions

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | Versions verified from NuGet registry; middleware ordering from official Wangkanai install guide; limited .NET 8 community examples |
| Architecture | MEDIUM | IViewLocationExpander interface confirmed from official source; PopulateValues cache-key semantics confirmed |
| Pitfalls | MEDIUM | Critical pitfalls evidenced from source and official docs; Wangkanai-specific pitfalls from official install guide |
| Features | MEDIUM | Scope inferred from project brief; no dedicated Milestone 3 feature research file (FEATURES.md is stale Milestone 2) |

**Overall confidence:** MEDIUM

### Gaps to Address

- **FEATURES.md is stale (Milestone 2):** No dedicated Milestone 3 feature research file. Roadmapper should derive feature scope from project brief and architecture research.
- **Calendar agenda layout design:** No specific markup pattern researched. Spike warranted before Phase 12 plan.
- **Hand-rolled vs. Wangkanai final decision:** Resolve in Phase 10 planning. Recommendation: hand-rolled.

## Sources

### Primary (MEDIUM confidence)
- NuGet Gallery — Wangkanai.Responsive 7.14.0 / Detection 8.20.0 (version, net8.0 target)
- Wangkanai Responsive INSTALL.md (middleware order, view suffix convention)
- IViewLocationExpander source — dotnet/aspnetcore (interface contract, cache-key semantics)
- IViewLocationExpander Interface — Microsoft Learn
- Layout in ASP.NET Core — Microsoft Learn

### Secondary (MEDIUM confidence)
- Jack Histon — IViewLocationExpander walkthrough (PopulateValues/ExpandViewLocations pattern; confirmed stable through .NET 8)
- Khalid Abuhakmeh — Razor View Engine searched locations (default format patterns)

---
*Research completed: 2026-06-23*
*Ready for roadmap: yes*
