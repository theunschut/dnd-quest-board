# Phase 10: Admin Settings - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Admins can configure Omphalos integration settings (URL, shared secret, enabled flag) from the Admin panel. Settings are persisted in the database via a key-value store and take effect immediately without a restart.

This phase delivers `IAdminSettingService` — the compile-time dependency that Phases 11 and 12 (Quest Board) depend on. Everything else in Milestone 3 is blocked until this service exists.

</domain>

<decisions>
## Implementation Decisions

### Entity Design
- **D-01:** Use a key-value store entity — `AdminSettingEntity` with columns `Key nvarchar(200) PK`, `Value nvarchar(max)`, `UpdatedAt datetime2`. **This overrides SETT-06**, which specified a typed single-row entity. Rationale: avoids a new EF migration per settings key in future milestones (extensible without schema changes).
- **D-02:** Keys in use for Phase 10: `OmphalosUrl`, `OmphalosSharedSecret`, `IsEnabled`. These match the fields from SETT-06 but stored as separate rows rather than named columns.
- **D-03:** The EF migration creates an `AdminSettings` table (or `IntegrationSettings` — planner to decide table name consistent with `AdminSettingEntity`). Single migration for Phase 10.

### IAdminSettingService Interface
- **D-04:** Service exposes a single method: `GetSettingsAsync()` returning a typed `IntegrationSettings` record containing `OmphalosUrl (string?)`, `OmphalosSharedSecret (string?)`, and `IsEnabled (bool)`.
- **D-05:** When no settings exist in the DB (fresh install / first run), `GetSettingsAsync()` returns a default record with `IsEnabled = false` and null URL/secret — never returns `null`. Phase 11 callers can safely use `settings.IsEnabled` without null checks.
- **D-06:** `IAdminSettingService` is registered as Scoped — secret is read from DB per-request so settings changes (including enable/disable) take effect immediately without app restart.
- **D-07:** The service name `IAdminSettingService` is intentionally generic (not `IIntegrationSettingService`) — the underlying key-value entity is designed to store any admin settings in future milestones without a new migration.

### Secret Handling
- **D-08:** SETT-04 requirement: saving the form with the secret field left blank MUST preserve the existing secret. Service implementation checks: if incoming secret value is null/empty, skip the `OmphalosSharedSecret` key upsert entirely.
- **D-09:** Secret field renders as `type="password"` (SETT-03). Standard password field behavior: empty on load (browser does not populate password inputs). The view must display a hint: "Leave blank to keep the existing value."

### Test Coverage
- **D-10:** Integration tests for the service + repository layer, following the Phase 5/6/7 pattern. SQLite in-memory via `TestDatabase` helper. Cover:
  - `GetSettingsAsync()` returns default when DB is empty
  - `GetSettingsAsync()` returns stored values after save
  - Blank secret on save preserves existing secret (SETT-04 behavior)
  - Upsert behavior (save twice, second overwrites first)

### Claude's Discretion
- Table name for `AdminSettingEntity` (e.g., `AdminSettings` vs `IntegrationSettings`) — pick one consistent with the entity name
- View layout follows the `modern-card` pattern per CLAUDE.md conventions
- Settings link placement in Admin navbar dropdown (after Quest Management with a divider, or at the end)
- `IntegrationSettings` record lives in `EuphoriaInn.Domain/Models/` alongside other domain models

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 10 Requirements
- `.planning/REQUIREMENTS.md` §Admin Settings — SETT-01 through SETT-08 (note: SETT-06 typed-column spec is overridden by D-01 key-value decision in this context)

### Architecture
- `.planning/PROJECT.md` — Constraints section (no framework changes, EF migrations required, Docker deployable)
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — DbContext for adding new DbSet
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — DI registration pattern; `AddRepositoryServices()` extension

### Existing Admin Patterns
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — existing admin controller pattern, `[Authorize(Policy = "AdminOnly")]` usage
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — admin navbar dropdown (lines ~31-50) where Settings link goes

### Integration Test Patterns
- `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs` — SQLite in-memory test DB helper
- `EuphoriaInn.IntegrationTests/` — existing integration test patterns from Phases 5/6/7

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.cs` — existing admin controller with `[Authorize(Policy = "AdminOnly")]` at class level; GET/POST pattern for admin pages
- `AdminOnly` policy — registered in `Program.cs`; already tested in existing handlers
- `modern-card` CSS classes — defined in site.css; use for Settings page view consistency

### Established Patterns
- Service interface in `EuphoriaInn.Domain/Interfaces/`, implementation in `EuphoriaInn.Domain/Services/`
- Repository interface in `EuphoriaInn.Repository/Interfaces/`, implementation in `EuphoriaInn.Repository/`
- DI registration in `ServiceExtensions.AddDomainServices()` and `ServiceExtensions.AddRepositoryServices()`
- AutoMapper `EntityProfile` in `EuphoriaInn.Domain/Automapper/EntityProfile.cs` — maps entity↔model

### Integration Points
- `_Layout.cshtml` admin dropdown — add Settings link after Quest Management
- `Program.cs` — `AddDomainServices()` / `AddRepositoryServices()` — where new service registrations go
- `QuestBoardContext.cs` — add `DbSet<AdminSettingEntity>` and entity configuration
- `EuphoriaInn.Repository/Migrations/` — EF migration for `AdminSettings` table

### No Existing Analogs
- No single-row config entity exists; no key-value store pattern exists — this is new
- `IAdminSettingService` / `AdminSettingService` / `AdminSettingEntity` / `AdminSettingRepository` — all net-new

</code_context>

<specifics>
## Specific Ideas

- Key-value store decision explicitly chosen for extensibility: future admin settings (e.g., email override, feature flags) can be added as new keys without EF migrations
- Phase 11 ViewComponent injects `IAdminSettingService` and calls `GetSettingsAsync()` once per layout render — the Scoped registration ensures this is one DB hit per request
- The `IntegrationSettings` record should expose a convenience property: `bool IsConfigured => IsEnabled && !string.IsNullOrWhiteSpace(OmphalosUrl)` — Phase 11 needs both conditions to show any UI elements

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 10-admin-settings*
*Context gathered: 2026-06-18*
