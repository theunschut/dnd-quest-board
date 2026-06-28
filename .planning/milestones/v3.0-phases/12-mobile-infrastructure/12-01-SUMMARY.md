---
phase: 12-mobile-infrastructure
plan: "01"
subsystem: mobile-detection
tags: [middleware, view-expander, mobile, infrastructure, tdd]
dependency_graph:
  requires: []
  provides:
    - EuphoriaInn.Service.Middleware.MobileDetectionMiddleware
    - EuphoriaInn.Service.ViewExpanders.MobileViewLocationExpander
    - HttpContext.Items["IsMobile"] (per-request bool flag)
    - RazorViewEngineOptions.ViewLocationExpanders (MobileViewLocationExpander registered)
  affects:
    - EuphoriaInn.Service/Program.cs (middleware pipeline + DI registration)
tech_stack:
  added: []
  patterns:
    - C# primary constructor middleware (matching TestAuthSelectorMiddleware pattern)
    - IViewLocationExpander with PopulateValues cache-key separation
    - ViewLocationExpanders.Add() via Configure<RazorViewEngineOptions>
key_files:
  created:
    - EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs
    - EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs
    - EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs
  modified:
    - EuphoriaInn.Service/Program.cs
decisions:
  - "MobileKeywords array: [Mobi, Android, iPhone, iPad, Windows Phone, BlackBerry] per CONTEXT.md D-06"
  - "IsMobile stored as boxed bool (not string) in HttpContext.Items — downstream is true pattern is null-safe"
  - "Detection in PopulateValues only (INFRA-03) — ExpandViewLocations reads context.Values[isMobile] exclusively"
  - "In .NET 10 ViewLocationExpanderContext.Values is null after construction; initialized in test setup to match RazorViewEngine runtime behavior"
metrics:
  duration: "9 minutes"
  completed: "2026-06-24"
  tasks_completed: 3
  tasks_total: 3
  files_created: 4
  files_modified: 1
---

# Phase 12 Plan 01: Mobile Detection Infrastructure Summary

**One-liner:** Per-request mobile UA detection middleware and IViewLocationExpander cache-key registrar — the atomic prerequisite for .Mobile.cshtml view resolution in Phases 13-16.

## What Was Built

Three files created, one modified:

1. `MobileDetectionMiddleware.cs` — C# primary constructor middleware that reads the User-Agent header, checks against `MobileKeywords` (case-insensitive), and stores a boxed bool in `HttpContext.Items["IsMobile"]`. Always calls `next(context)`.

2. `MobileViewLocationExpander.cs` — implements `IViewLocationExpander`. `PopulateValues` reads the boxed bool flag and writes `context.Values["isMobile"]` as a Razor view-path cache key. `ExpandViewLocations` yields the `.Mobile.cshtml` path before each `.cshtml` path when `Values["isMobile"] == "True"`, and returns input unchanged for desktop. `HttpContext` is never referenced in `ExpandViewLocations` — detection is confined to `PopulateValues` (INFRA-03).

3. `Program.cs` — three additions: three `using` directives, `Configure<RazorViewEngineOptions>` registration immediately after `AddControllersWithViews()`, and `UseMiddleware<MobileDetectionMiddleware>()` inserted between `UseStaticFiles()` and `UseRouting()`. `UseSession()` and single `AddSession()` are untouched.

4. Two test files: `MobileDetectionMiddlewareTests.cs` (8 tests) and `MobileViewLocationExpanderTests.cs` (7 tests), all green.

## Verification Results

| Command | Result |
|---------|--------|
| `dotnet test --filter "MobileDetectionMiddleware"` | Passed: 8, Failed: 0 |
| `dotnet test --filter "MobileViewLocationExpander"` | Passed: 7, Failed: 0 |
| `dotnet build EuphoriaInn.Service` | 0 errors, 0 warnings |
| `ExpandViewLocations` grep for HttpContext | No matches — INFRA-03 satisfied |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Initialize ViewLocationExpanderContext.Values in test setup**

- **Found during:** Task 2 TDD GREEN phase
- **Issue:** In .NET 10, `ViewLocationExpanderContext.Values` is null after the 6-arg constructor call. The real `RazorViewEngine` initializes this dictionary before invoking `PopulateValues`, but unit tests bypass the engine. All 7 expander tests failed with `NullReferenceException` at `context.Values[IsMobileKey] = isMobile.ToString()`.
- **Fix:** Added `ctx.Values = new Dictionary<string, string?>(StringComparer.Ordinal);` in `CreateExpanderContext` test helper. The implementation itself (`MobileViewLocationExpander.cs`) is unchanged — the production code is correct since the real engine always sets `Values` first.
- **Files modified:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs`
- **Commit:** 96937b9

## TDD Gate Compliance

| Gate | Commit | Status |
|------|--------|--------|
| RED — MobileDetectionMiddleware tests written (compile-fail) | e53cfc5 (test file) | Confirmed failing before implementation |
| GREEN — MobileDetectionMiddleware implementation | e53cfc5 | 8/8 tests passed |
| RED — MobileViewLocationExpander tests written (compile-fail) | 96937b9 (test file) | Confirmed failing before implementation |
| GREEN — MobileViewLocationExpander implementation | 96937b9 | 7/7 tests passed |

Both RED gates were verified before GREEN commits. No REFACTOR phase was needed — both implementations are clean as written.

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| e53cfc5 | feat | MobileDetectionMiddleware + 8 unit tests |
| 96937b9 | feat | MobileViewLocationExpander + 7 unit tests |
| 88ed63f | feat | Program.cs middleware and expander registration |

## Requirements Satisfied

| ID | Requirement | Status |
|----|-------------|--------|
| INFRA-01 | MobileDetectionMiddleware writes HttpContext.Items["IsMobile"] per request | Done — 8 tests green |
| INFRA-03 | Detection in PopulateValues; ExpandViewLocations reads only context.Values | Done — 7 tests green, grep confirms no HttpContext in ExpandViewLocations |
| INFRA-02 (backend half) | Expander registered in RazorViewEngineOptions.ViewLocationExpanders | Done — builds, registration confirmed in Program.cs |

## Self-Check: PASSED

| Item | Status |
|------|--------|
| EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs | FOUND |
| EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs | FOUND |
| EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs | FOUND |
| EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs | FOUND |
| Commit e53cfc5 | FOUND |
| Commit 96937b9 | FOUND |
| Commit 88ed63f | FOUND |
