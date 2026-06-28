---
phase: 21-html-email-templates
plan: 03
subsystem: email, background-jobs, domain-services
tags: [hangfire, iservicescopefactory, htmlrenderer, email-pipeline, quest-service, dispatcher-pattern]

# Dependency graph
requires:
  - phase: 21-01
    provides: "IEmailRenderService interface; IEmailService.SendAsync; IQuestRepository.SetFinalizedEmailSentForDateAsync; FinalizedEmailSentForDate migration"
  - phase: 21-02
    provides: "QuestFinalized.razor; QuestDateChanged.razor — Razor components with exact parameter names"
provides:
  - "RazorEmailRenderService: IEmailRenderService implementation using HtmlRenderer.Dispatcher.InvokeAsync"
  - "QuestFinalizedEmailJob: Hangfire job with dedup guard, per-recipient rendering, SetFinalizedEmailSentForDateAsync persistence"
  - "QuestDateChangedEmailJob: Hangfire job with per-recipient rendering, oldDate/newDate params"
  - "IQuestEmailDispatcher: Domain interface decoupling QuestService from Service-layer job types"
  - "HangfireQuestEmailDispatcher: Service-layer dispatcher implementing IBackgroundJobClient.Enqueue<T>"
  - "QuestService decoupled from IEmailService — uses IQuestEmailDispatcher instead"
  - "SmokeTestJob removed per D-07"
  - "Program.cs registers IEmailRenderService and IQuestEmailDispatcher"
affects:
  - 21-04-session-reminder-job
  - phase-22-session-reminder

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IServiceScopeFactory + CreateAsyncScope() inside Hangfire job body — no scoped services in constructor"
    - "IQuestEmailDispatcher: Domain-owned dispatcher interface to avoid Domain→Service circular reference"
    - "HtmlRenderer.Dispatcher.InvokeAsync wrapping RenderComponentAsync — required by HtmlRenderer thread model"
    - "Dedup guard: FinalizedEmailSentForDate?.Date == finalizedDate.Date before send loop"
    - "Post-send persistence: SetFinalizedEmailSentForDateAsync called after all recipients sent"
    - "Pre-update quest fetch: GetQuestWithDetailsAsync before UpdateQuestPropertiesWithNotificationsAsync to capture old proposed dates for oldDate param"

key-files:
  created:
    - EuphoriaInn.Service/Services/RazorEmailRenderService.cs
    - EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs
    - EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs
    - EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs
    - EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs
  modified:
    - EuphoriaInn.Domain/Services/QuestService.cs
    - EuphoriaInn.Service/Program.cs
    - EuphoriaInn.UnitTests/Services/QuestServiceTests.cs
  deleted:
    - EuphoriaInn.Service/Jobs/SmokeTestJob.cs

key-decisions:
  - "IQuestEmailDispatcher dispatcher pattern chosen over static BackgroundJob.Enqueue<T> — keeps Domain free of Hangfire and Service references; clean test injection point"
  - "Pre-update quest fetch for oldDate — GetQuestWithDetailsAsync called before UpdateQuestPropertiesWithNotificationsAsync to get current proposed dates as oldDate; first new proposed date from parameter becomes newDate"
  - "Unit tests updated to mock IQuestEmailDispatcher instead of IEmailService — tests now verify dispatcher receives correct email arrays rather than individual email sends"

patterns-established:
  - "Dispatcher pattern for Domain→Service job dispatch: define interface in Domain, implement in Service, inject at Service DI registration"
  - "Hangfire job scope: IServiceScopeFactory + CreateAsyncScope() — IQuestRepository, IEmailRenderService, IEmailService, IOptions<EmailSettings> all resolved via scope"

requirements-completed: [EMAIL-01, EMAIL-02]

# Metrics
duration: 7min
completed: 2026-06-26
---

# Phase 21 Plan 03: Integration — Render Service, Hangfire Jobs, QuestService Wiring Summary

**RazorEmailRenderService, QuestFinalizedEmailJob, QuestDateChangedEmailJob wired into full pipeline; QuestService decoupled from IEmailService via IQuestEmailDispatcher; SmokeTestJob removed**

## Performance

- **Duration:** 7 min
- **Started:** 2026-06-26T11:10:54Z
- **Completed:** 2026-06-26T11:17:25Z
- **Tasks:** 2
- **Files modified:** 8 (5 created, 3 modified, 1 deleted)

## Accomplishments

- `RazorEmailRenderService` created: implements `IEmailRenderService` using `HtmlRenderer.Dispatcher.InvokeAsync` — the only correct pattern for HtmlRenderer's thread model
- `QuestFinalizedEmailJob` created: `IServiceScopeFactory` pattern, resolves all scoped services inside job body, dedup guard on `FinalizedEmailSentForDate?.Date == finalizedDate.Date`, renders `QuestFinalized.razor` per recipient using `nameof()` for all parameter keys, persists `SetFinalizedEmailSentForDateAsync` after all sends succeed
- `QuestDateChangedEmailJob` created: `IServiceScopeFactory` pattern, renders `QuestDateChanged.razor` per recipient with `DateTime oldDate/newDate`
- `IQuestEmailDispatcher` interface created in Domain — avoids Domain → Service circular dependency
- `HangfireQuestEmailDispatcher` created in Service — wraps `IBackgroundJobClient.Enqueue<T>` calls for both job types
- `QuestService` decoupled from `IEmailService`: constructor now takes `IQuestEmailDispatcher`; `FinalizeQuestAsync` enqueues `QuestFinalizedEmailJob` with recipient arrays; `UpdateQuestPropertiesWithNotificationsAsync` fetches old proposed dates before update, then enqueues `QuestDateChangedEmailJob`
- `SmokeTestJob.cs` deleted per D-07
- `Program.cs` updated: removes `SmokeTestJob` enqueue, adds `AddScoped<IEmailRenderService, RazorEmailRenderService>()` and `AddScoped<IQuestEmailDispatcher, HangfireQuestEmailDispatcher>()`
- All 27 unit tests updated and passing: `QuestServiceTests` now mocks `IQuestEmailDispatcher` and asserts `EnqueueFinalizedEmail`/`EnqueueDateChangedEmail` receive correct email arrays

## Task Commits

Each task was committed atomically:

1. **Task 1: Create RazorEmailRenderService and Hangfire email jobs** - `cd84922` (feat)
2. **Task 2: Decouple QuestService; wire Hangfire dispatcher; remove SmokeTestJob** - `239daad` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `EuphoriaInn.Service/Services/RazorEmailRenderService.cs` — NEW: HtmlRenderer-based IEmailRenderService implementation
- `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` — NEW: Hangfire job with IServiceScopeFactory, dedup guard, per-recipient render+send, dedup persistence
- `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` — NEW: Hangfire job with IServiceScopeFactory, per-recipient render+send with oldDate/newDate
- `EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs` — NEW: Domain dispatcher interface with EnqueueFinalizedEmail and EnqueueDateChangedEmail
- `EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs` — NEW: Service-layer implementation using IBackgroundJobClient
- `EuphoriaInn.Domain/Services/QuestService.cs` — replaced IEmailService with IQuestEmailDispatcher; updated FinalizeQuestAsync and UpdateQuestPropertiesWithNotificationsAsync
- `EuphoriaInn.Service/Program.cs` — removed SmokeTestJob enqueue; added IEmailRenderService and IQuestEmailDispatcher registrations
- `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` — updated to mock IQuestEmailDispatcher; 4 tests updated
- `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` — DELETED per D-07

## Decisions Made

- **IQuestEmailDispatcher pattern chosen** over static `BackgroundJob.Enqueue<T>` or injecting `IBackgroundJobClient` directly into Domain. The static approach would work at runtime but makes Domain depend on Hangfire.Core at compile time. The dispatcher interface keeps Domain free of both Hangfire and Service-layer job references, and provides a clean injection point for unit tests.

- **Pre-update quest fetch for oldDate** — The `UpdateQuestPropertiesWithNotificationsAsync` repository method does not return the removed dates; it returns affected players. To provide `oldDate`/`newDate` for the email template, the service fetches the quest's current proposed dates BEFORE calling the repository method. The first proposed date (ordered by date) becomes `oldDate`; the first date from the new `proposedDates` parameter becomes `newDate`. This runs one extra DB query but avoids changing the repository interface.

- **Unit tests updated** — The existing 27 unit tests mocked `IEmailService.SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync`. Since QuestService no longer calls these, the tests now mock `IQuestEmailDispatcher` and assert `EnqueueFinalizedEmail`/`EnqueueDateChangedEmail` receive the correct arrays. Semantic coverage is preserved (same scenarios, same correctness assertions).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Unit tests failed to compile after QuestService constructor change**
- **Found during:** Task 2 (QuestService decoupling)
- **Issue:** `QuestServiceTests.cs` passed `IEmailService` as argument 3 to `QuestService` constructor, but the constructor was changed to expect `IQuestEmailDispatcher`. Compile error: `CS1503: cannot convert from IEmailService to IQuestEmailDispatcher`.
- **Fix:** Updated `QuestServiceTests.cs` to mock `IQuestEmailDispatcher` instead of `IEmailService`. Test assertions updated from `_emailService.Received(N).SendQuestFinalizedEmailAsync(...)` to `_dispatcher.Received(1).EnqueueFinalizedEmail(... emails array ...)`. All 4 affected tests updated; 27 total tests continue to pass.
- **Files modified:** `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs`
- **Committed in:** `239daad` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — compile error in unit tests)
**Impact on plan:** Tests were updated to match the new dispatcher pattern. Semantic coverage preserved.

## Issues Encountered

None beyond the unit test compile error (auto-fixed above).

## Threat Surface Scan

No new trust boundaries introduced beyond what was specified in the plan's threat model. `IQuestEmailDispatcher.EnqueueFinalizedEmail` and `EnqueueDateChangedEmail` are called only from `QuestService.FinalizeQuestAsync` and `UpdateQuestPropertiesWithNotificationsAsync` respectively — both gated behind DungeonMasterOnly authorization. Job arguments are server-generated primitives (T-21-07: accepted). No new HTTP-accessible endpoints added.

## Known Stubs

None — all pipeline steps are wired end-to-end:
- `QuestService.FinalizeQuestAsync` → `IQuestEmailDispatcher.EnqueueFinalizedEmail` → `HangfireQuestEmailDispatcher` → `IBackgroundJobClient.Enqueue<QuestFinalizedEmailJob>` → scope → `IEmailRenderService.RenderAsync<QuestFinalized>` → `IEmailService.SendAsync` → `IQuestRepository.SetFinalizedEmailSentForDateAsync`
- `QuestService.UpdateQuestPropertiesWithNotificationsAsync` → `IQuestEmailDispatcher.EnqueueDateChangedEmail` → `HangfireQuestEmailDispatcher` → `IBackgroundJobClient.Enqueue<QuestDateChangedEmailJob>` → scope → `IEmailRenderService.RenderAsync<QuestDateChanged>` → `IEmailService.SendAsync`

## User Setup Required

None — no external service configuration required for this plan. Existing `EmailSettings` (AppUrl, SMTP credentials) cover all runtime needs.

## Next Phase Readiness

- All Wave 3 integration work is complete
- Wave 4 (Plan 04: session reminder job) can use `IQuestEmailDispatcher` pattern or implement its own Hangfire job directly
- `SessionReminder.razor` is ready for Phase 22 to consume (D-15 parameter contract locked in Plan 02)
- No blockers

## Self-Check

Verified:
- `EuphoriaInn.Service/Services/RazorEmailRenderService.cs` exists with `new HtmlRenderer(serviceProvider, loggerFactory)` and `Dispatcher.InvokeAsync`
- `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` exists with `FinalizedEmailSentForDate?.Date == finalizedDate.Date`, `SetFinalizedEmailSentForDateAsync`, `RenderAsync<QuestFinalized>`
- `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` exists with `RenderAsync<QuestDateChanged>`, `DateTime oldDate`, `DateTime newDate`
- `EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs` exists with `EnqueueFinalizedEmail` and `EnqueueDateChangedEmail`
- `EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs` exists with `IBackgroundJobClient` reference
- `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` does NOT exist
- `EuphoriaInn.Service/Program.cs` does NOT contain `SmokeTestJob`; DOES contain `AddScoped<IEmailRenderService, RazorEmailRenderService>()` and `AddScoped<IQuestEmailDispatcher, HangfireQuestEmailDispatcher>()`
- `EuphoriaInn.Domain/Services/QuestService.cs` does NOT contain `IEmailService emailService` or `SendQuestFinalizedEmailAsync` or `SendQuestDateChangedEmailAsync`
- Commits `cd84922` and `239daad` exist
- `dotnet build EuphoriaInn.Service` exits 0 with 0 errors
- `dotnet test EuphoriaInn.UnitTests` exits 0: 27/27 passed

## Self-Check: PASSED

---
*Phase: 21-html-email-templates*
*Completed: 2026-06-26*
