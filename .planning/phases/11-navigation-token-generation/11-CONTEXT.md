# Phase 11: Navigation + Token Generation - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

DMs can open Omphalos from a navbar link (plain navigation) or from any quest page via a signed deep link. Clicking "Open Session Notes" generates a 5-minute HMAC-SHA256 token and redirects the browser to the correct Omphalos session. All UI elements are gated behind `IsConfigured` (integration enabled + URL set + secret set).

This phase depends on `IAdminSettingService` from Phase 10 (compile-time dependency) — already delivered.

</domain>

<decisions>
## Implementation Decisions

### Username in Token
- **D-01:** The `username` parameter in the signed token MUST use `currentUser.Name` (the domain `User.Name` display name property), NOT `User.Identity.Name`. Rationale: `UserEntity.UserName` is set to the email address (`UserName = email` in `IdentityService.CreateUserAsync`). Using the email would break cross-app account matching with Omphalos. `currentUser.Name` is what the codebase already uses for DM identity throughout (e.g., `isQuestDm = currentUser.Name.Equals(quest.DungeonMaster?.Name)`).
- **D-02:** Username is normalized to lowercase before inclusion in both the MAC message and the URL parameter (TOKEN-04 requirement).

### Token Format (LOCKED — from STATE.md)
- **D-03:** Canonical MAC message: `expiry={unix_ts}&questId={id}&questTitle={url_encoded_title}&username={lower}` — keys alphabetical order, HMAC-SHA256, lowercase hex signature, TTL 300 seconds. This is fixed by the cross-repo contract with Omphalos (Phase 12).

### Button Placement on Quest Pages
- **D-04:** Quest **Detail page** (`Details.cshtml`): "Open Session Notes" button goes inside the existing **"DM Controls" card** in the `col-lg-2` right sidebar, below the "Manage Quest" button. The card is already gated by `ViewBag.CanManage` — no new conditional needed for the card itself.
- **D-05:** Quest **Manage page** (`Manage.cshtml`): "Open Session Notes" button goes as a **new sidebar card** (following the "View Public Page" card pattern) in the `col-md-4` right sidebar, placed below the "View Public Page" card.
- **D-06:** Both buttons use `w-100` class (full-width) to match the "View Public Page" / "Manage Quest" button style.

### Navbar ViewComponent (LOCKED — from STATE.md)
- **D-07:** An `OmphalosNavItem` ViewComponent renders the "Open Omphalos" link in the DM navbar dropdown (`_Layout.cshtml`). The ViewComponent injects `IAdminSettingService` directly and calls `GetSettingsAsync()` — one DB hit per layout render (Scoped registration). This avoids the base-controller inheritance anti-pattern.
- **D-08:** The "Open Omphalos" navbar link is plain navigation — opens Omphalos base URL in a new tab (`target="_blank"`). No token is generated for the navbar link (NAV-02).
- **D-09:** The link appears only inside the "Dungeon Master" dropdown (DM-only section of `_Layout.cshtml`), so it is already limited to DM/Admin users without additional role checks in the ViewComponent.

### ViewBag for Quest Page Flag (LOCKED — from STATE.md)
- **D-10:** `QuestController.Details` and `QuestController.Manage` set a `ViewBag.ShowOmphalosButton` flag (bool). This follows the existing pattern (5 other ViewBag flags already on these actions). No new ViewModel wrapper needed.
- **D-11:** `ViewBag.ShowOmphalosButton` is `true` when `settings.IsConfigured && (isQuestDm || isAdmin)`. The controller fetches `IAdminSettingService.GetSettingsAsync()` once per action — consistent with how other services are already injected into `QuestController`.

### LaunchOmphalos Endpoint
- **D-12:** `QuestController.LaunchOmphalos(int id)` GET action: returns `NotFound()` when `!settings.IsConfigured`. Also decorated with `[Authorize(Policy = "DungeonMasterOnly")]` for defense in depth — non-DMs who navigate to the URL directly get 401/403 before the 404 check runs.
- **D-13:** The action calls `IIntegrationTokenService.GenerateSignedUrl(settings.OmphalosUrl, questId, questTitle, username, settings.OmphalosSharedSecret)` and returns `Redirect(signedUrl)`.

### Token Service
- **D-14:** `IIntegrationTokenService` lives in `EuphoriaInn.Domain/Interfaces/`. Implementation `IntegrationTokenService` in `EuphoriaInn.Domain/Services/`. Follows the existing service placement pattern.
- **D-15:** Service method signature: `string GenerateSignedUrl(string omphalosBaseUrl, int questId, string questTitle, string username, string sharedSecret)`. Returns the complete Omphalos SSO URL with all query parameters and signature appended.
- **D-16:** Internally computes `expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds()`, constructs canonical message per TOKEN-02, computes HMAC-SHA256, appends lowercase hex `sig` parameter.
- **D-17:** The SSO endpoint path appended to `omphalosBaseUrl` is `/api/sso/open-quest` (from REQUIREMENTS.md SSO-01).

### Test Coverage
- **D-18:** **Unit tests** for `IntegrationTokenService` in `EuphoriaInn.UnitTests` — verify deterministic HMAC output with fixed inputs (fixed expiry timestamp, known quest data, known secret). Tests: correct canonical message construction, correct hex signature, correct URL parameter encoding, correct endpoint path appending.
- **D-19:** **Integration tests** for `LaunchOmphalos` endpoint in `EuphoriaInn.IntegrationTests` — via `TestDatabase` pattern, covering: 404 when integration is disabled, 404 when URL is blank, redirect to correct Omphalos URL when `IsConfigured`, signed URL contains expected parameters.

### Claude's Discretion
- DI registration: `AddTransient<IIntegrationTokenService, IntegrationTokenService>()` in `ServiceExtensions.AddDomainServices()` (Transient appropriate — stateless pure computation)
- ViewComponent location: `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` + `Views/Shared/Components/OmphalosNavItem/Default.cshtml`
- Button icon: `fas fa-book-open` (or similar "session notes" icon) consistent with FontAwesome 6.4 icons used throughout
- Button color: `btn-warning` or `btn-info` to visually distinguish from primary action buttons — Claude decides based on visual fit

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 11 Requirements
- `.planning/REQUIREMENTS.md` §Navigation + Token Generation — NAV-01 through NAV-05, TOKEN-01 through TOKEN-05
- `.planning/REQUIREMENTS.md` §SSO Endpoint — SSO-01 (for `/api/sso/open-quest` endpoint path that the token URL targets)

### Architecture
- `.planning/PROJECT.md` — Constraints section (no framework changes, EF migrations, standalone app requirement)
- `.planning/STATE.md` — Milestone 3 decisions section (token format contract, ViewComponent decision, ViewBag decision)

### Phase 10 Foundation (compile-time dependency)
- `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` — interface Phase 11 depends on
- `EuphoriaInn.Domain/Models/IntegrationSettings.cs` — `IsConfigured` property, field definitions

### Existing Patterns to Follow
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — DM navbar dropdown (lines ~59–84); ViewComponent goes inside this `<ul class="dropdown-menu">`
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` — `col-lg-2` sidebar with "DM Controls" card (line ~572+)
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — `col-md-4` sidebar with "View Public Page" card (line ~475+)
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — existing ViewBag pattern and `IAdminSettingService` injection point
- `EuphoriaInn.Domain/Services/AdminSettingService.cs` — existing service pattern in Domain layer
- `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs` — integration test DB helper pattern

### Unit Test Pattern
- `EuphoriaInn.UnitTests/` — existing unit test project for pure logic tests

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IAdminSettingService.GetSettingsAsync()` — already delivered in Phase 10; returns `IntegrationSettings` with `IsConfigured` convenience property
- `IntegrationSettings.IsConfigured` — pre-built: `IsEnabled && !empty(OmphalosUrl) && !empty(OmphalosSharedSecret)`
- DM navbar dropdown in `_Layout.cshtml` — `<ul class="dropdown-menu">` block for DM-only items is the insertion point for the ViewComponent
- `ViewBag.CanManage` — already set on the Details action; "DM Controls" card is already gated by it
- `modern-card` / `modern-card-header` / `modern-card-body` CSS classes — use for the new "Open Session Notes" card on Manage page

### Established Patterns
- ViewBag flags on QuestController: 5 existing flags (`IsPlayerSignedUp`, `CanManage`, `IsDetailsPage`, `CurrentQuestId`, `IsAuthorized`) — `ShowOmphalosButton` follows this pattern
- Service constructor injection via primary constructor parameters (no stored fields) — `IntegrationTokenService(IAdminSettingService settings)` is NOT the right pattern; token service is stateless and takes inputs as method parameters, no field injection needed
- `[Authorize(Policy = "DungeonMasterOnly")]` attribute usage — see `QuestController` existing actions for pattern
- `System.Security.Cryptography.HMACSHA256` — BCL, no NuGet packages needed

### Integration Points
- `QuestController` — add `LaunchOmphalos(int id)` action + inject `IAdminSettingService`
- `QuestController.Details()` and `QuestController.Manage()` — add `ViewBag.ShowOmphalosButton` flag
- `_Layout.cshtml` DM dropdown — add `@await Component.InvokeAsync("OmphalosNavItem")`
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — register `IIntegrationTokenService`
- `EuphoriaInn.Service/Program.cs` — no changes needed (ViewComponent registration is automatic in ASP.NET Core MVC)

</code_context>

<specifics>
## Specific Ideas

- The "Open Omphalos" navbar link uses `target="_blank" rel="noopener noreferrer"` (opens in new tab, no referrer leak)
- Username confirmed as `currentUser.Name.ToLower()` — NOT email. The code `UserName = email` in IdentityService confirmed these are distinct fields in this app.
- Token URL construction: `{OmphalosUrl.TrimEnd('/')}/api/sso/open-quest?expiry={expiry}&questId={id}&questTitle={Uri.EscapeDataString(title)}&username={username.ToLower()}&sig={hmacHex}`
- The canonical MAC message uses `Uri.EscapeDataString` for questTitle (percent-encoding matching what SSO-01 expects on the Omphalos side)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 11-navigation-token-generation*
*Context gathered: 2026-06-18*
