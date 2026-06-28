---
phase: 21-html-email-templates
plan: 01
subsystem: database, email, api
tags: [efcore, migration, emailservice, iemailrenderservice, blazor-components, domain-interfaces]

# Dependency graph
requires: []
provides:
  - "DateTime? FinalizedEmailSentForDate property on QuestEntity and Quest domain model"
  - "EF Core migration AddFinalizedEmailSentForDate (datetime2, nullable: true, auto-applied on startup)"
  - "IQuestRepository.SetFinalizedEmailSentForDateAsync — dedup persistence hook for Wave 3 jobs"
  - "IEmailRenderService interface in Domain with generic RenderAsync<TComponent> where TComponent : IComponent"
  - "IEmailService.SendAsync(string toEmail, string subject, string htmlBody) — generic HTML email send method"
  - "EmailService.SendAsync implementation with IsBodyHtml=true and CreateSmtpClient() pattern"
affects:
  - 21-02-razor-components
  - 21-03-hangfire-email-jobs
  - 21-04-questservice-decoupling

# Tech tracking
tech-stack:
  added:
    - "FrameworkReference: Microsoft.AspNetCore.App added to EuphoriaInn.Domain.csproj (enables IComponent reference)"
  patterns:
    - "IEmailRenderService.RenderAsync<TComponent> generic interface pattern for component-to-HTML rendering"
    - "SetFinalizedEmailSentForDateAsync follows FindAsync/assign/SaveChangesAsync repository pattern (same as UpdateQuestRecapAsync)"
    - "[Obsolete] attribute on typed email methods — kept for backward compatibility, flagged for removal"

key-files:
  created:
    - EuphoriaInn.Domain/Interfaces/IEmailRenderService.cs
    - EuphoriaInn.Repository/Migrations/20260626105734_AddFinalizedEmailSentForDate.cs
    - EuphoriaInn.Repository/Migrations/20260626105734_AddFinalizedEmailSentForDate.Designer.cs
  modified:
    - EuphoriaInn.Repository/Entities/QuestEntity.cs
    - EuphoriaInn.Domain/Models/QuestBoard/Quest.cs
    - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
    - EuphoriaInn.Repository/QuestRepository.cs
    - EuphoriaInn.Domain/Interfaces/IEmailService.cs
    - EuphoriaInn.Domain/Services/EmailService.cs
    - EuphoriaInn.Domain/EuphoriaInn.Domain.csproj
    - EuphoriaInn.Repository/Migrations/QuestBoardContextModelSnapshot.cs

key-decisions:
  - "Added FrameworkReference to Microsoft.AspNetCore.App in Domain.csproj to allow IComponent constraint in IEmailRenderService — lightest fix that keeps the interface in Domain as planned"
  - "Legacy typed email methods marked [Obsolete] and kept to avoid breaking 27 existing unit tests that reference IEmailService"

patterns-established:
  - "IEmailRenderService: generic RenderAsync<TComponent> constrainted to IComponent — Wave 3 RazorEmailRenderService implements this"
  - "Repository dedup setter pattern: FindAsync([id]) / assign / SaveChangesAsync — same as UpdateQuestRecapAsync"

requirements-completed: [EMAIL-01, EMAIL-02]

# Metrics
duration: 5min
completed: 2026-06-26
---

# Phase 21 Plan 01: Foundation — Data Model, Interfaces, and Email Plumbing Summary

**EF Core migration for FinalizedEmailSentForDate dedup column, IEmailRenderService contract in Domain, and IEmailService.SendAsync with HTML body support**

## Performance

- **Duration:** 5 min
- **Started:** 2026-06-26T10:56:43Z
- **Completed:** 2026-06-26T11:00:59Z
- **Tasks:** 2
- **Files modified:** 9 (including 2 generated migration files)

## Accomplishments

- EF Core migration `AddFinalizedEmailSentForDate` created (datetime2, nullable, auto-applied on startup) — dedup guard for Wave 3 QuestFinalizedEmailJob
- `IEmailRenderService` interface created in Domain with generic `RenderAsync<TComponent>` constrained to `IComponent` — contract Wave 2 Razor components and Wave 3 RazorEmailRenderService reference
- `IEmailService.SendAsync` added as the primary send method with `IsBodyHtml=true` — Wave 3 jobs call this instead of the deprecated typed methods
- `IQuestRepository.SetFinalizedEmailSentForDateAsync` added — dedup persistence hook for Wave 3 jobs
- All 27 existing unit tests continue to pass; existing typed methods retained as deprecated wrappers

## Task Commits

Each task was committed atomically:

1. **Task 1: Add FinalizedEmailSentForDate to entity, domain model, repository, and migration** - `4a5deb6` (feat)
2. **Task 2: Add IEmailRenderService to Domain; add SendAsync to IEmailService and EmailService** - `a29cb05` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `EuphoriaInn.Repository/Entities/QuestEntity.cs` — added `DateTime? FinalizedEmailSentForDate` after `IsFinalized`
- `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` — added `DateTime? FinalizedEmailSentForDate` after `IsFinalized`
- `EuphoriaInn.Domain/Interfaces/IQuestRepository.cs` — added `SetFinalizedEmailSentForDateAsync` after `UpdateQuestRecapAsync`
- `EuphoriaInn.Repository/QuestRepository.cs` — implemented `SetFinalizedEmailSentForDateAsync` using FindAsync/assign/SaveChangesAsync
- `EuphoriaInn.Repository/Migrations/20260626105734_AddFinalizedEmailSentForDate.cs` — generated migration (datetime2, nullable: true)
- `EuphoriaInn.Domain/Interfaces/IEmailRenderService.cs` — NEW: generic `RenderAsync<TComponent>` interface
- `EuphoriaInn.Domain/Interfaces/IEmailService.cs` — added `SendAsync` as first method; marked legacy methods `[Obsolete]`
- `EuphoriaInn.Domain/Services/EmailService.cs` — implemented `SendAsync` with `IsBodyHtml=true`
- `EuphoriaInn.Domain/EuphoriaInn.Domain.csproj` — added `FrameworkReference` to `Microsoft.AspNetCore.App`

## Decisions Made

- `FrameworkReference` added to Domain.csproj rather than moving `IEmailRenderService` to Service project. The plan explicitly places the interface in Domain; this is the lightest fix that satisfies the constraint.
- Legacy typed email methods retained as `[Obsolete]` wrappers. They protect 27 existing unit tests from compile failures until QuestService decoupling (Plan 03) updates those tests.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added FrameworkReference to Domain.csproj for IComponent type resolution**
- **Found during:** Task 2 (IEmailRenderService creation)
- **Issue:** `EuphoriaInn.Domain` uses `Microsoft.NET.Sdk` (not Web SDK) and has no access to `Microsoft.AspNetCore.Components.IComponent`. The plan's `IEmailRenderService` interface uses `IComponent` as a generic constraint, causing CS0234 build error.
- **Fix:** Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `EuphoriaInn.Domain.csproj` — the standard pattern for non-Web-SDK projects that need ASP.NET Core framework types.
- **Files modified:** `EuphoriaInn.Domain/EuphoriaInn.Domain.csproj`
- **Verification:** `dotnet build` exits 0 with 0 errors; all 27 unit tests pass
- **Committed in:** `a29cb05` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 3 — blocking build error)
**Impact on plan:** Required fix for Domain project to reference ASP.NET Core framework types. No scope change, no architectural concern.

## Issues Encountered

The FrameworkReference addition introduces NU1510 "package will not be pruned" warnings for packages that are now redundant via the framework reference (e.g., `Microsoft.AspNetCore.Identity`, `Microsoft.Extensions.Configuration.Binder`). These are informational warnings only — they do not affect compilation or runtime. Removing the explicit package references would be a separate cleanup task (out of scope for this plan).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All Wave 1 contracts are in place: `IEmailRenderService`, `IEmailService.SendAsync`, `SetFinalizedEmailSentForDateAsync`, EF Core migration
- Wave 2 (Plan 02: Razor email components) can reference `IEmailRenderService` from Domain
- Wave 3 (Plan 03: Hangfire jobs) can implement `IEmailRenderService` in Service and call `IEmailService.SendAsync`
- No blockers. Migration will auto-apply on next startup.

---
*Phase: 21-html-email-templates*
*Completed: 2026-06-26*
