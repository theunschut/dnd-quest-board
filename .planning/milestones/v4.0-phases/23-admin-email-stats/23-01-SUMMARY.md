---
phase: 23-admin-email-stats
plan: "01"
subsystem: email-stats
status: complete
tags:
  - resend-api
  - httpclient
  - unit-tests
  - email-settings
dependency_graph:
  requires: []
  provides:
    - ResendStatsAggregator.Aggregate (pure static helper for last_event aggregation)
    - ResendEmailRecord / ResendEmailListResponse (DTOs for Resend API JSON)
    - ResendStatCounts (value type with Sent/Delivered/Bounced/Failed)
    - EmailSettings.ResendApiKey (config property, D-09)
    - Named "Resend" HttpClient registration (D-10)
  affects:
    - EuphoriaInn.Service/Services/ResendStatsAggregator.cs
    - EuphoriaInn.Domain/Models/EmailSettings.cs
    - EuphoriaInn.Service/appsettings.json
    - EuphoriaInn.Service/Program.cs
tech_stack:
  added: []
  patterns:
    - Pure static aggregation class for unit-testable business logic
    - Named HttpClient via IHttpClientFactory (Pitfall 4 avoided: no auth header at registration)
    - IOptions<EmailSettings> for Resend API key binding (no new DI registration)
    - TDD (RED commit then GREEN commit)
key_files:
  created:
    - EuphoriaInn.Service/Services/ResendStatsAggregator.cs
    - EuphoriaInn.UnitTests/Services/ResendStatsAggregatorTests.cs
  modified:
    - EuphoriaInn.Domain/Models/EmailSettings.cs
    - EuphoriaInn.Service/appsettings.json
    - EuphoriaInn.Service/Program.cs
decisions:
  - "Aggregation logic placed in a pure static class (ResendStatsAggregator) in EuphoriaInn.Service.Services — keeps AdminController thin in Plan 02"
  - "opened and clicked last_event values count as Delivered (Pitfall 5)"
  - "No Authorization header on named Resend HttpClient at registration — set per-request in Plan 02 controller (Pitfall 4, T-23-02)"
  - "appsettings.json ResendApiKey value is empty string — real key supplied via EmailSettings__ResendApiKey env var (Pitfall 2, T-23-01)"
metrics:
  duration: "~10m"
  completed: "2026-06-27"
  tasks_completed: 2
  files_changed: 5
---

# Phase 23 Plan 01: Config Foundation and ResendStatsAggregator Summary

**One-liner:** Pure static last_event aggregator with full unit coverage plus EmailSettings.ResendApiKey config property and named Resend HttpClient registration.

## What Was Built

Plan 01 establishes all the wiring Plan 02 (AdminController.EmailStats) depends on:

1. **ResendStatsAggregator** — a pure static class in `EuphoriaInn.Service.Services` that maps Resend `last_event` values to four stat counts (Sent, Delivered, Bounced, Failed) and excludes records older than a UTC cutoff.

2. **DTOs** — `ResendEmailRecord` and `ResendEmailListResponse` records with `[JsonPropertyName]` attributes for System.Text.Json deserialization of the Resend API response.

3. **EmailSettings.ResendApiKey** — new config property appended to the existing `EmailSettings` record; binds automatically via the existing `IOptions<EmailSettings>` registration in `AddDomainServices`.

4. **appsettings.json placeholder** — `"ResendApiKey": ""` added to the `EmailSettings` section; value intentionally empty so no secret is committed (real key supplied via `EmailSettings__ResendApiKey` env var at runtime).

5. **Named HttpClient "Resend"** — registered in `Program.cs` with `https://api.resend.com/` base address, 15-second timeout, and Accept: application/json header. No Authorization header at registration (set per-request in Plan 02).

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 (RED) | Failing unit tests for ResendStatsAggregator | 80d3c31 | ResendStatsAggregatorTests.cs |
| 1 (GREEN) | Implement ResendStatsAggregator | fd8975f | ResendStatsAggregator.cs |
| 2 | Add ResendApiKey config + named HttpClient | b257c4b | EmailSettings.cs, appsettings.json, Program.cs |

## Test Results

`dotnet test EuphoriaInn.UnitTests --filter "Category=EmailStats"` — **8/8 passed**

| Test | Result |
|------|--------|
| Aggregate_SentEvent_IncrementsSentOnly | Pass |
| Aggregate_DeliveredOpenedClicked_AllCountAsDelivered | Pass |
| Aggregate_BouncedEvent_IncrementsBouncedOnly | Pass |
| Aggregate_FailedEvent_IncrementsFailedOnly | Pass |
| Aggregate_UnknownEvents_AreIgnored | Pass |
| Aggregate_RecordOlderThanCutoff_IsExcluded | Pass |
| Aggregate_RecordNewerThanOrEqualToCutoff_IsIncluded | Pass |
| Aggregate_EmptySequence_ReturnsAllZeroCounts | Pass |

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None — this plan creates pure logic and config; no UI stubs.

## Threat Flags

No new threat surfaces beyond those documented in the plan's threat model. T-23-01 (empty appsettings.json placeholder) and T-23-02 (no auth header on named client) both mitigated as designed.

## Self-Check: PASSED
- `EuphoriaInn.Service/Services/ResendStatsAggregator.cs` — exists
- `EuphoriaInn.UnitTests/Services/ResendStatsAggregatorTests.cs` — exists
- `EuphoriaInn.Domain/Models/EmailSettings.cs` — modified (ResendApiKey present)
- `EuphoriaInn.Service/appsettings.json` — modified (empty placeholder present)
- `EuphoriaInn.Service/Program.cs` — modified (named Resend HttpClient registered)
- Commits 80d3c31, fd8975f, b257c4b — all present in git log
- `dotnet build` — 0 errors
- `dotnet test --filter Category=EmailStats` — 8/8 green
