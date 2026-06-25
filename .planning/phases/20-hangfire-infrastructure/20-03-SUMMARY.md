---
phase: 20-hangfire-infrastructure
plan: "03"
subsystem: infra
tags: [hangfire, program-cs, service-registration, integration-tests]

dependency_graph:
  requires:
    - 20-01 (AdminDashboardAuthFilter, SmokeTestJob)
    - 20-02 (nav link wiring)
  provides:
    - Program.cs with full Hangfire wiring (service registration, dashboard middleware, smoke-test enqueue)
    - All 134 integration tests passing with Hangfire present
  affects:
    - EuphoriaInn.Service/Program.cs

tech_stack:
  added: []
  patterns:
    - "Hangfire Testing guard: both AddHangfire+AddHangfireServer AND UseHangfireDashboard wrapped in !IsEnvironment(Testing)"
    - "Hangfire SqlServerStorage: DefaultConnection, 5-min timeouts, DisableGlobalLocks=true"
    - "Hangfire dashboard placement: after UseAuthentication+UseAuthorization, before MapControllerRoute"

key_files:
  modified:
    - EuphoriaInn.Service/Program.cs

decisions:
  - "UseHangfireDashboard must also be guarded by !IsEnvironment('Testing') — Hangfire calls ThrowIfNotConfigured() inside UseHangfireDashboard, which fails if AddHangfire was never called"
  - "BackgroundJob.Enqueue<SmokeTestJob> placed inside existing Testing guard block alongside ConfigureDatabase and SeedShopDataAsync"

metrics:
  duration: "~3 minutes"
  completed: "2026-06-25"
  tasks_completed: 2
  tasks_total: 2
  files_changed: 1

status: complete
---

# Phase 20 Plan 03: Hangfire Program.cs Wiring Summary

**Complete Hangfire integration into Program.cs: SQL Server storage registration, Admin-only dashboard middleware, and smoke-test job enqueue — all guarded for Testing environment; all 134 integration tests pass**

## Performance

- **Duration:** ~3 minutes
- **Started:** 2026-06-25T20:43:16Z
- **Completed:** 2026-06-25T20:46:00Z
- **Tasks:** 2 of 2
- **Files modified:** 1

## Accomplishments

- Added three using directives (`Hangfire`, `Hangfire.SqlServer`, `EuphoriaInn.Service.Jobs`) to Program.cs
- Registered Hangfire services (`AddHangfire` with SQL Server storage using DefaultConnection, `AddHangfireServer` with WorkerCount=2) inside `!IsEnvironment("Testing")` guard
- Mounted `UseHangfireDashboard("/hangfire")` with `AdminDashboardAuthFilter` after `UseAuthentication`/`UseAuthorization` and before `MapControllerRoute`
- Added `BackgroundJob.Enqueue<SmokeTestJob>` inside the existing Testing guard startup block
- Verified all 134 integration tests pass — zero regressions from Hangfire changes

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Hangfire using directives and service registrations** — `a4e1c9e` (feat)
2. **Auto-fix [Rule 1 - Bug]: Guard UseHangfireDashboard in Testing block** — `611bcd8` (fix)
3. **Task 2: Regression gate — 134/134 integration tests passing** — verified via `dotnet test` (no new files to commit)

## Files Created/Modified

- `EuphoriaInn.Service/Program.cs` — Hangfire using directives added, AddHangfire+AddHangfireServer registered (both Testing-guarded), UseHangfireDashboard mounted after UseAuthorization and before MapControllerRoute (both Testing-guarded), BackgroundJob.Enqueue<SmokeTestJob> added to startup Testing guard block

## Decisions Made

- **UseHangfireDashboard also needs the Testing guard:** Hangfire's `UseHangfireDashboard` internally calls `ThrowIfNotConfigured()` which checks that `AddHangfire` was previously called. Since `AddHangfire` is skipped in Testing, `UseHangfireDashboard` must also be skipped. This was not explicitly stated in the plan but is a correctness requirement driven by Hangfire's own validation.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] UseHangfireDashboard also requires Testing guard**
- **Found during:** Task 2 (regression gate)
- **Issue:** The plan's acceptance criteria specified that `AddHangfire` and `AddHangfireServer` be wrapped in `!IsEnvironment("Testing")` but did not mention `UseHangfireDashboard`. Hangfire's middleware calls `ThrowIfNotConfigured()` at startup which requires `AddHangfire` to have been called. With `AddHangfire` skipped in Testing, `UseHangfireDashboard` threw `InvalidOperationException: Unable to find the required services` — causing all 134 tests to fail.
- **Fix:** Wrapped `UseHangfireDashboard` in `if (!app.Environment.IsEnvironment("Testing"))` guard block
- **Files modified:** `EuphoriaInn.Service/Program.cs`
- **Commit:** `611bcd8`

## Verification

Full Plan 03 acceptance criteria verified:

- [x] `using Hangfire;` present in Program.cs
- [x] `using Hangfire.SqlServer;` present in Program.cs
- [x] `using EuphoriaInn.Service.Jobs;` present in Program.cs
- [x] `AddHangfire(config => config` inside `!IsEnvironment("Testing")` block
- [x] `AddHangfireServer(options =>` inside the same `!IsEnvironment("Testing")` block
- [x] `UseSqlServerStorage(` present
- [x] `builder.Configuration.GetConnectionString("DefaultConnection")` present
- [x] `DisableGlobalLocks = true` present
- [x] `UseHangfireDashboard("/hangfire"` present
- [x] `Authorization = new[] { new AdminDashboardAuthFilter() }` present
- [x] `app.UseHangfireDashboard` appears AFTER `app.UseAuthorization();` (line ordering confirmed)
- [x] `app.UseHangfireDashboard` appears BEFORE `app.MapControllerRoute` (line ordering confirmed)
- [x] `BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None));` present
- [x] `BackgroundJob.Enqueue` is inside `!IsEnvironment("Testing")` block
- [x] `dotnet build EuphoriaInn.Service/EuphoriaInn.Service.csproj` exits with 0 errors, 0 warnings
- [x] `dotnet test EuphoriaInn.IntegrationTests` → `Passed: 134, Failed: 0`

Full phase verification (Plans 01+02+03):
- [x] `EuphoriaInn.Service/Authorization/AdminDashboardAuthFilter.cs` exists (Plan 01)
- [x] `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` exists (Plan 01)
- [x] `_Layout.cshtml` contains `href="/hangfire"` inside AdminOnly block (Plan 02)
- [x] Program.cs has complete Hangfire wiring (Plan 03)

## Known Stubs

- **SmokeTestJob** is explicitly a stub/smoke-test. The job comment in Program.cs says "REMOVE THIS in Phase 21 once real jobs exist." This is intentional — Phase 21 will replace it with the actual reminder job.

## Threat Flags

None. All threat mitigations from the plan's threat model were implemented:
- T-20-05: `UseHangfireDashboard` placed after `UseAuthentication` and `UseAuthorization` ✓
- T-20-06: `UseHangfireDashboard` placed before `MapControllerRoute` ✓
- T-20-07: Both `AddHangfire`, `AddHangfireServer`, and `UseHangfireDashboard` guarded by `!IsEnvironment("Testing")` ✓
- T-20-08: Job argument visibility — accepted (Admin-only dashboard) ✓

## Self-Check: PASSED

- [x] `EuphoriaInn.Service/Program.cs` modified — FOUND (contains all required Hangfire wiring)
- [x] Commit `a4e1c9e` exists — FOUND
- [x] Commit `611bcd8` exists — FOUND
- [x] 134 integration tests pass — VERIFIED

---
*Phase: 20-hangfire-infrastructure*
*Completed: 2026-06-25*
