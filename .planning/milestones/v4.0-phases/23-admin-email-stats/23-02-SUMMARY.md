---
phase: 23-admin-email-stats
plan: "02"
subsystem: email-stats
status: complete
tags:
  - admin
  - resend-api
  - razor-view
  - integration-tests
  - memorycache
dependency_graph:
  requires:
    - 23-01 (ResendStatsAggregator.Aggregate, ResendEmailRecord, ResendEmailListResponse, ResendStatCounts, EmailSettings.ResendApiKey, named Resend HttpClient)
  provides:
    - AdminController.EmailStats GET action (cache, force-refresh, missing-key guard, api-error guard, Resend pagination)
    - EmailStatsViewModel (Sent/Delivered/Bounced/Failed/AsOf/IsMissingKey/IsApiError + factory methods)
    - Views/Admin/EmailStats.cshtml (modern-card page with 4 stat cards and alert banners)
    - Admin dropdown nav link to /Admin/EmailStats
    - Integration tests for /Admin/EmailStats authorization (unauthenticated + non-admin)
  affects:
    - EuphoriaInn.Service/Controllers/Admin/AdminController.cs
    - EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs
    - EuphoriaInn.Service/Views/Admin/EmailStats.cshtml
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
    - EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs
tech_stack:
  added: []
  patterns:
    - IMemoryCache with 5-minute AbsoluteExpirationRelativeToNow
    - Per-request Bearer token via AuthenticationHeaderValue (Pitfall 4 mitigated)
    - Resend API pagination with after cursor (Pitfall 1 respected)
    - Modern-card UI shell (CLAUDE.md mandatory pattern)
    - Static factory methods on ViewModel (MissingKey/ApiError)
key_files:
  created:
    - EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs
    - EuphoriaInn.Service/Views/Admin/EmailStats.cshtml
  modified:
    - EuphoriaInn.Service/Controllers/Admin/AdminController.cs
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
    - EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs
decisions:
  - "Per-request Bearer token (AuthenticationHeaderValue) set inside GetResendStatsAsync — no header at HttpClient registration (carries T-23-02 forward from Plan 01 as designed)"
  - "force=true returns View(viewModel) directly — no redirect, per D-08/Pitfall 6"
  - "GetResendStatsAsync wraps entire body in try/catch returning (new EmailStatsViewModel(), true) on any failure — exception text and apiKey never written to response (T-23-05)"
  - "ResendStatsAggregator.Aggregate called once after pagination loop completes — no inline last_event switch in AdminController"
  - "Mobile test failures (4 tests) are pre-existing since Hangfire IBackgroundJobClient was added to AdminController in an earlier phase — out of scope"
metrics:
  duration: "~12m"
  completed: "2026-06-27"
  tasks_completed: 3
  files_changed: 5
---

# Phase 23 Plan 02: EmailStats Controller, View, and Auth Tests Summary

**One-liner:** Admin EmailStats page with 4 Resend stat cards, 5-minute IMemoryCache, graceful degraded-state banners, and integration-test authorization coverage under the existing AdminOnly policy.

## What Was Built

Plan 02 wires the Plan 01 foundation (ResendStatsAggregator, named Resend HttpClient, EmailSettings.ResendApiKey) into a complete user-facing feature:

1. **EmailStatsViewModel** — lightweight ViewModel in `EuphoriaInn.Service.ViewModels.AdminViewModels` with auto-properties `Sent`, `Delivered`, `Bounced`, `Failed`, `AsOf`, `IsMissingKey`, `IsApiError`, and two static factories (`MissingKey()`, `ApiError()`) for the degraded states.

2. **AdminController.EmailStats action** — `[HttpGet]` action added to the existing `[Authorize(Policy = "AdminOnly")]` controller. Extended the primary constructor with `IHttpClientFactory`, `IOptions<EmailSettings>`, and `IMemoryCache`. Logic:
   - Reads `ResendApiKey` via `IOptions<EmailSettings>` — returns `MissingKey()` view when blank (D-05)
   - 5-minute `IMemoryCache` on key `"resend-email-stats"` — serves cached stats on cache hit (D-07)
   - `?force=true` clears the cache before fetching (D-08)
   - Delegates fetch to `GetResendStatsAsync` — returns `ApiError()` view on any failure (D-06)
   - On success, caches the ViewModel and returns the view

3. **GetResendStatsAsync private helper** — full Resend API pagination with `after` cursor (Pitfall 1), per-request `Authorization: Bearer {apiKey}` header (Pitfall 4), early break when `CreatedAt < cutoff` or page size < 100, delegates final aggregation to `ResendStatsAggregator.Aggregate` (no duplicate logic). Entire body wrapped in `try/catch` returning `(new EmailStatsViewModel(), true)` — no exception escapes, no secret leaked (T-23-05).

4. **EmailStats.cshtml** — Razor view on `EmailStatsViewModel` using the mandatory `modern-card` shell. Three render branches: success (4 stat cards with icon/color/label per UI-SPEC), missing-key (`alert-warning`), API error (`alert-danger`). Refresh button (`btn btn-primary`) renders in all three states to allow retry (D-08). Last-updated freshness line below stat cards.

5. **Admin dropdown nav link** — `<li>` with `asp-action="EmailStats"` inserted in `_Layout.cshtml` after the Background Jobs item (D-02).

6. **Integration tests** — Two `[Fact]` tests appended to `AdminControllerIntegrationTests`:
   - `EmailStats_WhenNotAuthenticated_ShouldRedirectToLogin` — asserts redirect/unauthorized for anonymous GET
   - `EmailStats_WhenNotAdmin_ShouldReturnForbidden` — asserts forbidden/redirect for Player-role GET
   — Proves the existing class-level `"AdminOnly"` policy covers the new action (D-11, STATS-01 criterion 4).

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | EmailStatsViewModel + EmailStats action | 7953e19 | EmailStatsViewModel.cs, AdminController.cs |
| 2 | EmailStats Razor view + nav link | 0ee0e85 | EmailStats.cshtml, _Layout.cshtml |
| 3 | Authorization integration tests | a8d15da | AdminControllerIntegrationTests.cs |

## Test Results

`dotnet test EuphoriaInn.IntegrationTests --filter "AdminController"` — **8/8 passed** (includes 2 new EmailStats auth cases)

| Test | Result |
|------|--------|
| Index_WhenNotAuthenticated_ShouldRedirectToLogin | Pass |
| Index_WhenNotAdmin_ShouldReturnForbidden | Pass |
| ManageUsers_WhenNotAuthenticated_ShouldRedirectToLogin | Pass |
| AdminActions_WhenNotAuthenticated_ShouldRedirectToLogin (3 data) | Pass |
| EmailStats_WhenNotAuthenticated_ShouldRedirectToLogin | Pass |
| EmailStats_WhenNotAdmin_ShouldReturnForbidden | Pass |

`dotnet build EuphoriaInn.Service` — **0 errors** (9 pre-existing warnings, all NU1510/CS9113, out of scope)

## Pre-Existing Failures (Out of Scope)

4 tests in `MobileViewsTests` fail with `Unable to resolve service for type 'Hangfire.IBackgroundJobClient'` when attempting to activate `AdminController`. These failures pre-date this plan — they were already failing before Plan 02 changes (verified by reverting commits and re-running). The Mobile tests hit Admin routes that require `IBackgroundJobClient`, which the Mobile test WebApplicationFactory does not register. Out of scope for this plan.

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — all four stat values are wired directly from the Resend API response via `ResendStatsAggregator.Aggregate`. No placeholder data.

## Threat Flags

No new threat surfaces beyond the plan's threat model. T-23-04 (AdminOnly policy), T-23-05 (exception catch suppression), T-23-06 (5-min cache), T-23-07 (bool force param) all mitigated as designed.

## Self-Check: PASSED

- `EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs` — exists
- `EuphoriaInn.Service/Views/Admin/EmailStats.cshtml` — exists
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — modified (EmailStats action + GetResendStatsAsync present)
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — modified (asp-action="EmailStats" link present)
- `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — modified (EmailStats x2 present)
- Commits 7953e19, 0ee0e85, a8d15da — all present in git log
- `dotnet build EuphoriaInn.Service` — 0 errors
- `dotnet test --filter "AdminController"` — 8/8 green
