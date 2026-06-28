---
phase: 12-mobile-infrastructure
plan: "02"
subsystem: mobile-layout-shell
tags: [mobile, layout, razor, offcanvas, integration-tests, view-start]
dependency_graph:
  requires:
    - EuphoriaInn.Service.Middleware.MobileDetectionMiddleware (plan 01)
    - EuphoriaInn.Service.ViewExpanders.MobileViewLocationExpander (plan 01)
    - HttpContext.Items["IsMobile"] (plan 01)
  provides:
    - EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml (mobile HTML shell)
    - _ViewStart.cshtml conditional layout selection
    - MobileLayoutTests (INFRA-02/04/05 integration tests)
  affects:
    - EuphoriaInn.Service/Views/_ViewStart.cshtml (layout routing logic)
tech_stack:
  added: []
  patterns:
    - Bootstrap 5 offcanvas nav (data-bs-toggle="offcanvas")
    - Razor layout selection via Context.Items["IsMobile"] is true (null-safe)
    - HttpRequestMessage per-request User-Agent header for integration tests
    - IClassFixture<WebApplicationFactoryBase> integration test pattern
key_files:
  created:
    - EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml
    - EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs
  modified:
    - EuphoriaInn.Service/Views/_ViewStart.cshtml
decisions:
  - "D-01: Mobile offcanvas nav mirrors desktop auth-conditional sections using identical AuthorizeAsync policy names (AdminOnly/DungeonMasterOnly)"
  - "D-02: Mobile brand is 'Quest Board' (not 'D&D Quest Board') beside fa-dice-d20"
  - "D-03: _Layout.Mobile.cshtml loads only mobile.css — no site.css/calendar.css/quests.css/shop.css/guild-members.css/dm-profile.css"
  - "Null-safe is true pattern in _ViewStart.cshtml — evaluates to false when IsMobile is null (static files, health checks)"
  - "No @inject in _Layout.Mobile.cshtml — AuthorizationService/UserService come from _ViewImports.cshtml lines 14-15"
  - "data-bs-dismiss=offcanvas on every nav link so drawer closes on navigation"
metrics:
  duration: "3 minutes"
  completed: "2026-06-24"
  tasks_completed: 3
  tasks_total: 3
  files_created: 2
  files_modified: 1
---

# Phase 12 Plan 02: Mobile Layout Shell Summary

**One-liner:** Bootstrap offcanvas mobile HTML shell with auth-mirrored nav and conditional `_ViewStart.cshtml` — the request-path wiring that makes Plan 01's mobile detection pipeline visible to browsers.

## What Was Built

Three changes (two files created, one modified):

1. `_Layout.Mobile.cshtml` — full Razor layout for mobile requests:
   - Dark D&D-themed top navbar with offcanvas toggler (`data-bs-toggle="offcanvas"`)
   - Offcanvas drawer with `id="mobileNav"` containing auth-conditional nav items
   - Mirrors all six desktop nav sections (Admin, DM, player, Calendar, profile/logout, login) as flat list items — no nested dropdowns (D-01)
   - Identical `AuthorizeAsync(User, "AdminOnly")` and `AuthorizeAsync(User, "DungeonMasterOnly")` policy checks (T-12-04 mitigation)
   - `<body class="d-flex flex-column min-vh-100 mobile-layout ...">` — `mobile-layout` class enables INFRA-05 assertions
   - Loads only `~/css/mobile.css` — no desktop CSS files (D-03)
   - No `@inject` directives — `AuthorizationService` and `UserService` already in scope via `_ViewImports.cshtml`
   - Declares `@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)` (prevents InvalidOperationException from views that define `@section Scripts`)

2. `_ViewStart.cshtml` (modified) — conditional layout selector:
   - `var isMobile = Context.Items["IsMobile"] is true;` — null-safe; `false` on static file/health check requests
   - `Layout = isMobile ? "~/Views/Shared/_Layout.Mobile.cshtml" : "_Layout";`
   - No individual view overrides layout

3. `MobileLayoutTests.cs` — 4 integration tests (INFRA-02/04/05):
   - `MobileLayoutOffcanvas_MobileUserAgent_RendersOffcanvasNav`: iPhone UA → response contains "offcanvas" and "mobileNav"
   - `MobileLayout_MobileUserAgent_RendersBodyWithMobileLayoutClass`: iPhone UA → response contains "mobile-layout"
   - `DesktopLayoutParity_DesktopUserAgent_HasNoMobileLayout`: Chrome UA → response has no "mobile-layout"/"offcanvas", has "D&D Quest Board" desktop brand
   - `MobileViewResolution_DesktopUserAgent_ServesDesktopView`: both UAs return 200 with correct layout (fallback path — no .Mobile.cshtml content views in Phase 12)

## Verification Results

| Command | Result |
|---------|--------|
| `dotnet build EuphoriaInn.Service` | 0 errors, 0 warnings |
| `dotnet test --filter "MobileLayoutOffcanvas\|DesktopLayoutParity\|MobileViewResolution"` | Passed: 3, Failed: 0 |
| `dotnet test --filter "MobileLayout"` | Passed: 4, Failed: 0 |
| grep for desktop CSS in `_Layout.Mobile.cshtml` | 0 matches — D-03 satisfied |
| grep for `@inject` in `_Layout.Mobile.cshtml` | 0 matches — injection via _ViewImports |
| Policy names match desktop `AdminOnly`/`DungeonMasterOnly` | Confirmed — T-12-04 mitigated |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Integration test assertion used HTML-encoded entity instead of literal string**

- **Found during:** Task 3 integration test run
- **Issue:** `DesktopLayoutParity` test asserted `html.Should().Contain("D&amp;D Quest Board")` but the raw HTTP response body contains the literal characters `D&D Quest Board` (the `&` is not HTML-encoded in the anchor text as served)
- **Fix:** Changed assertion to `html.Should().Contain("D&D Quest Board")`
- **Files modified:** `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs`
- **Commit:** 206b9b8

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| 769dfe1 | feat | `_Layout.Mobile.cshtml` — mobile HTML shell with offcanvas nav |
| c1b5d1c | feat | `_ViewStart.cshtml` — conditional mobile/desktop layout selector |
| 206b9b8 | feat | `MobileLayoutTests.cs` — INFRA-02/04/05 integration tests (4 tests green) |

## Requirements Satisfied

| ID | Requirement | Status |
|----|-------------|--------|
| INFRA-02 (fallback half) | Mobile UA with no .Mobile.cshtml content view returns 200 with mobile shell | Done — integration test green |
| INFRA-04 | Mobile UA response contains offcanvas nav element with id="mobileNav" | Done — integration test green |
| INFRA-05 | Mobile body has mobile-layout class; desktop has none; _ViewStart drives selection | Done — integration test green |

## Known Stubs

None. The mobile shell has no stub data — it renders real auth state from `AuthorizationService` and `UserService` (injected via `_ViewImports.cshtml`). Content views are deliberately absent in Phase 12 (D-05).

## Threat Flags

None. All threats from the plan's threat model are addressed:
- T-12-04 (nav elevation): mitigated — `AdminOnly`/`DungeonMasterOnly` policy checks present in `_Layout.Mobile.cshtml` at the same locations as desktop
- T-12-05 (UA spoofing): accepted — layout change only, no security boundary crossed
- T-12-06 (IsMobile disclosure): accepted — null-safe `is true` prevents exception path

## Self-Check: PASSED

| Item | Status |
|------|--------|
| EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml | FOUND |
| EuphoriaInn.Service/Views/_ViewStart.cshtml (modified) | FOUND |
| EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs | FOUND |
| Commit 769dfe1 | FOUND |
| Commit c1b5d1c | FOUND |
| Commit 206b9b8 | FOUND |
