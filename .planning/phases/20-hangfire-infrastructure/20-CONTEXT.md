# Phase 20: Hangfire Infrastructure - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Install Hangfire with SQL Server storage, expose an admin-only dashboard at `/hangfire`, and establish the `IServiceScopeFactory` pattern as the mandatory contract for all subsequent background jobs. This phase delivers infrastructure only — no email templates, no real reminder jobs, no stats. Those land in Phases 21–23.

</domain>

<decisions>
## Implementation Decisions

### Dashboard Authorization
- **D-01:** Use a custom `IDashboardAuthorizationFilter` implementation checking the `Admin` role — NOT `LocalRequestsOnlyAuthorizationFilter` (bypassed by the Docker reverse proxy).
- **D-02:** Non-admin users (unauthenticated or wrong role) hitting `/hangfire` are **redirected to the login page** (consistent with the rest of the app's auth failure behavior), not returned a raw 401.
- **D-03:** The dashboard registration in `Program.cs` must appear **after** `UseAuthentication` and `UseAuthorization` so the auth middleware stack is in place.

### Hangfire Storage
- **D-04:** SQL Server storage sharing the **existing database**. The `[HangFire]` schema is auto-created by Hangfire on startup — **no EF Core migration required**.

### Job Scope Pattern
- **D-05:** All Hangfire jobs **must** resolve scoped services via `IServiceScopeFactory` + `CreateAsyncScope()` inside the job method body. Scoped services (e.g., `DbContext`, `IEmailService`) must **never** be injected into the job class constructor — Hangfire activates jobs outside a DI scope.
- **D-06:** This pattern is established in Phase 20 via a smoke-test job; Phases 21–22 must follow it without exception.

### Smoke-Test Job
- **D-07:** A smoke-test job is **enqueued once on application startup** (fire-and-forget via `BackgroundJob.Enqueue`) to prove `IServiceScopeFactory` resolves correctly before real jobs are wired.
- **D-08:** The smoke-test job resolves **`IEmailService`** via `IServiceScopeFactory` — the same service real reminder jobs will use. It logs a message; no actual email is sent.
- **D-09:** The startup enqueue call is **removed in Phase 21** once real jobs exist and the pattern is proven. The smoke-test job class itself may also be removed at that point.

### Admin Navigation
- **D-10:** A **Hangfire Dashboard** link is added to the admin panel navigation (alongside User Management and Quest Management), visible to Admin role only. Claude decides the exact link label and placement.

### Testing
- **D-11:** Phase 20 adds **no new integration tests** for Hangfire dashboard access — the success criterion for registration order is satisfied by code review. The existing 134 integration tests must continue to pass.

### Claude's Discretion
- Exact nav link label and position in the admin layout (e.g., "Job Dashboard", "Background Jobs", or "Hangfire")
- Hangfire worker count and polling interval (use sensible defaults for a small self-hosted app)
- Whether to guard Hangfire startup behind `!app.Environment.IsEnvironment("Testing")` (consistent with the existing `ConfigureDatabase` guard)
- Class and file naming for the custom `IDashboardAuthorizationFilter` implementation

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs beyond the roadmap — all requirements are captured in decisions above and the roadmap phase definition.

### Project Planning
- `.planning/ROADMAP.md` §"Phase 20: Hangfire Infrastructure" — goal, requirements (JOBS-01, JOBS-02), and success criteria
- `.planning/REQUIREMENTS.md` §"Hangfire Infrastructure" — JOBS-01 and JOBS-02 requirement text

### Existing Code
- `EuphoriaInn.Service/Program.cs` — middleware registration order; Hangfire must be added after `UseAuthentication` + `UseAuthorization`
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — service registration pattern; Hangfire server registration goes in `AddDomainServices` or a new `AddHangfireServices` extension
- `EuphoriaInn.Service/Authorization/AdminHandler.cs` — existing Admin role check pattern; the custom `IDashboardAuthorizationFilter` must check the same role

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminHandler.cs` / `AdminRequirement.cs` — existing Admin role authorization logic; the custom `IDashboardAuthorizationFilter` should mirror this check (read the `ClaimsPrincipal` from the `DashboardContext` and check `IsInRole("Admin")`)
- `MobileDetectionMiddleware.cs` — example of a custom middleware registered in `Program.cs`; follow the same `UseMiddleware<T>()` pattern style

### Established Patterns
- Authorization: policies defined in `Program.cs` (`AddAuthorizationBuilder()`), handlers registered as scoped services
- Service registration: domain services registered via `AddDomainServices()` extension in `ServiceExtensions.cs`; Hangfire registration should follow the same extension method pattern
- Scoped service lifetime guard: `SeedShopDataAsync` shows the correct `CreateScope()` pattern for resolving scoped services outside a request context — the smoke-test job extends this same concept into a Hangfire job

### Integration Points
- `Program.cs` — Hangfire middleware (`UseHangfireDashboard`, `UseHangfireServer`) registered after `UseAuthorization`
- Admin panel navigation layout (likely `_Layout.cshtml` or a shared admin partial) — add the `/hangfire` link here, visible to Admin role only

</code_context>

<specifics>
## Specific Ideas

No specific references or "I want it like X" moments from discussion — open to standard Hangfire patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 20-hangfire-infrastructure*
*Context gathered: 2026-06-25*
