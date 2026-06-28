# Phase 2: Email & Service Consolidation - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Move email dispatch (quest finalize and date-change notifications) fully into `QuestService`;
move remaining-quantity calculation into `ShopService`; refactor `EmailService` to use the typed
options pattern (`IOptions<EmailSettings>`); fix the `[Quest Board URL]` placeholder; fix stale
quest state used when building finalize email recipient list.

`QuestController` must not inject `IEmailService` after this phase.
`ShopController.Index` must contain no quantity-calculation logic after this phase.

</domain>

<decisions>
## Implementation Decisions

### ServiceResult pattern (CTRL-03)

- **D-01:** Introduce a **generic `ServiceResult<T>`** type in `EuphoriaInn.Domain`. This becomes
  the standard return type for service operations that need to communicate success/failure with an
  optional payload. Start with `ServiceResult<T>` (bool Success, T? Data, string? Error) rather
  than a minimal one-off type — it becomes the convention going forward.
- **D-02:** `UpdateQuestPropertiesWithNotificationsAsync` changes its return type from
  `IList<User>` to `ServiceResult<int>` (or similar) so the controller receives only the
  operation outcome, not a user list. The email dispatch moves inside the service.

### Application URL for email body (EMAIL-03)

- **D-03:** Add an `AppUrl` field to the existing `EmailSettings` config section in
  `appsettings.json`. The `EmailSettings` typed options record gains a string `AppUrl` property.
  `EmailService.SendQuestDateChangedEmailAsync` reads `AppUrl` from the injected options and
  substitutes it for `[Quest Board URL]`.
- **D-04:** Add `"AppUrl": ""` (empty default) to `appsettings.json` `EmailSettings` section.
  Docker deployments override via environment variable `EmailSettings__AppUrl`.

### Finalize email recipient list (EMAIL-04)

- **D-05:** Keep `FinalizeQuestAsync` returning void (no interface signature change). After
  `FinalizeQuestAsync` completes, re-fetch the quest from the repository to get post-save state
  before building the email recipient list. One additional DB round-trip is acceptable.

### EmailService typed options (EMAIL-01, EMAIL-02)

- **D-06:** Create an `EmailSettings` record (or class) in `EuphoriaInn.Domain` with all SMTP
  properties: `SmtpServer`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `FromEmail`, `FromName`,
  `AppUrl`.
- **D-07:** Register via `AddOptions<EmailSettings>().BindConfiguration("EmailSettings")` in
  `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` (or Domain's ServiceExtensions —
  planner decides based on where `EmailService` is registered).
- **D-08:** `EmailService` injects `IOptions<EmailSettings>` instead of `IConfiguration`. The
  duplicated SMTP setup block is extracted to a private helper method used by both send methods.

### QuestService injecting IEmailService (CTRL-01, CTRL-02)

- **D-09:** Claude's discretion on the constructor signature. Both `IEmailService` and
  `IQuestService` live in `EuphoriaInn.Domain` — the injection is architecturally clean and
  requires no layer boundary changes.

### ShopService remaining-quantity (CTRL-04)

- **D-10:** Move the remaining-quantity calculation from `ShopController.Index` into
  `ShopService`. The controller only maps and renders. Planner determines the exact method
  signature and whether `ShopItemViewModel` gains computed properties or a new service method
  returns enriched models.

### Claude's Discretion

- Constructor signature for `QuestService` adding `IEmailService` — planner decides.
- Whether `ServiceResult<T>` lives in its own file or is co-located with a related model.
- Exact `ServiceResult<T>` type parameter for the date-change update (could be `int` for
  affected player count, or `Unit`/`bool` if the count is not needed by the controller).
- Method name and return type details for the ShopService remaining-quantity extraction.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Files Being Changed
- `EuphoriaInn.Domain/Services/QuestService.cs` — gains IEmailService injection; FinalizeQuestAsync stays void; date-change method return type changes
- `EuphoriaInn.Domain/Services/EmailService.cs` — replace IConfiguration with IOptions<EmailSettings>; extract SMTP setup helper; fix AppUrl placeholder
- `EuphoriaInn.Domain/Interfaces/IEmailService.cs` — review if signature changes are needed
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs` — UpdateQuestPropertiesWithNotificationsAsync return type changes
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — remove IEmailService injection; slim finalize and date-change actions
- `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` — remove remaining-quantity calculation
- `EuphoriaInn.Domain/Services/ShopService.cs` — gains remaining-quantity calculation
- `EuphoriaInn.Service/appsettings.json` — add AppUrl to EmailSettings section
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — add AddOptions<EmailSettings> registration

### Requirements
- `.planning/REQUIREMENTS.md` §Controller Slimming — CTRL-01 through CTRL-04
- `.planning/REQUIREMENTS.md` §Email & Configuration — EMAIL-01 through EMAIL-04

### Codebase Map
- `.planning/codebase/ARCHITECTURE.md` — layer overview
- `.planning/codebase/CONCERNS.md` — concerns 21 (stale quest state), 22 (URL placeholder), and related

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EuphoriaInn.Domain/Services/EmailService.cs` — both send methods have identical SMTP setup blocks (lines ~14-27 and ~68-81); extract to private helper as part of EMAIL-02
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs` — `UpdateQuestPropertiesWithNotificationsAsync` currently returns `IList<User>`; return type changes to `ServiceResult<T>`

### Established Patterns
- Domain services use primary constructor injection — `QuestService` gains `IEmailService emailService` as an additional primary constructor parameter
- All typed options registration follows `AddOptions<T>().BindConfiguration(sectionName)` pattern (none yet — this is the first use in this codebase)
- `IConfiguration` is already available in Domain via `Microsoft.Extensions.Configuration` package — replacing it with `IOptions<T>` removes the generic config dependency

### Integration Points
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:15` — `IEmailService emailService` injection to remove
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:186` — date-change email dispatch block to move into service
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:640-660` — finalize email dispatch block to move into service (post re-fetch)
- `EuphoriaInn.Service/Controllers/Shop/ShopController.cs:35-44` — remaining-quantity calculation to move into ShopService

</code_context>

<specifics>
## Specific Ideas

- `ServiceResult<T>` is preferred over a minimal one-off type — it becomes the standard going forward.
- `AppUrl` belongs in `EmailSettings` (not a separate config section) for simplicity.
- Docker override: `EmailSettings__AppUrl=https://your-domain.com` as environment variable.

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-email-service-consolidation*
*Context gathered: 2026-04-17*
