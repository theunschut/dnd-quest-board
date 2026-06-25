# Phase 11: Navigation + Token Generation — Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core MVC ViewComponents, HMAC-SHA256 token generation, QuestController integration
**Confidence:** HIGH — all findings verified against live codebase; no external library lookups required (BCL only)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** `username` MUST use `currentUser.Name`, NOT `User.Identity.Name` (UserName = email in this app)
- **D-02:** Username normalized to lowercase before inclusion in MAC message and URL parameter
- **D-03:** Canonical MAC message: `expiry={unix_ts}&questId={id}&questTitle={url_encoded_title}&username={lower}` — alphabetical key order, HMAC-SHA256, lowercase hex signature, TTL 300 seconds
- **D-04:** Details page: "Open Session Notes" button inside existing "DM Controls" card, below "Manage Quest" button, gated by existing `ViewBag.CanManage`
- **D-05:** Manage page: "Open Session Notes" as a new sidebar card following "View Public Page" pattern, placed after it
- **D-06:** Both buttons use `w-100` class
- **D-07:** `OmphalosNavItem` ViewComponent injects `IAdminSettingService` directly, one DB hit per layout render
- **D-08:** Navbar link is plain navigation to Omphalos base URL in new tab — no token
- **D-09:** Navbar link inside DM dropdown only — no additional role check needed in ViewComponent
- **D-10:** `ViewBag.ShowOmphalosButton` (bool) set on `Details` and `Manage` actions
- **D-11:** `ViewBag.ShowOmphalosButton = settings.IsConfigured && (isQuestDm || isAdmin)`
- **D-12:** `LaunchOmphalos` returns `NotFound()` when `!settings.IsConfigured`; decorated with `[Authorize(Policy = "DungeonMasterOnly")]`
- **D-13:** Action calls `IIntegrationTokenService.GenerateSignedUrl(...)` then returns `Redirect(signedUrl)`
- **D-14:** `IIntegrationTokenService` in `EuphoriaInn.Domain/Interfaces/`; `IntegrationTokenService` in `EuphoriaInn.Domain/Services/`
- **D-15:** Method signature: `string GenerateSignedUrl(string omphalosBaseUrl, int questId, string questTitle, string username, string sharedSecret)`
- **D-16:** `expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds()`; canonical message per TOKEN-02; lowercase hex sig
- **D-17:** SSO endpoint path: `/api/sso/open-quest`
- **D-18:** Unit tests for `IntegrationTokenService` in `EuphoriaInn.UnitTests`
- **D-19:** Integration tests for `LaunchOmphalos` in `EuphoriaInn.IntegrationTests`

### Claude's Discretion

- DI registration lifetime: `AddTransient` (stateless pure computation — no repository, no state)
- ViewComponent file locations: `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` + `Views/Shared/Components/OmphalosNavItem/Default.cshtml`
- Button icon: `fas fa-book-open` (or similar — Claude decides based on visual fit with FontAwesome 6.4)
- Button color: `btn-warning` or `btn-info` to distinguish from primary action buttons

### Deferred Ideas (OUT OF SCOPE)

None.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| NAV-01 | `_Layout.cshtml` renders `OmphalosNavItem` ViewComponent in DM dropdown; shows link only when integration enabled and URL configured | ViewComponent pattern confirmed — no existing ViewComponents in codebase; ASP.NET Core MVC auto-discovers them by convention |
| NAV-02 | "Open Omphalos" navbar link opens base URL in new tab, no SSO token | Plain `<a>` tag with `target="_blank" rel="noopener noreferrer"` |
| NAV-03 | Quest Detail page shows "Open Session Notes" when integration enabled and user is DM/Admin | `ViewBag.ShowOmphalosButton` set on `Details` GET action; button inside existing "DM Controls" card (line 575) |
| NAV-04 | Quest Manage page shows "Open Session Notes" under same conditions | `ViewBag.ShowOmphalosButton` set on `Manage` GET action; new card after "View Public Page" card (line 476) |
| NAV-05 | When integration disabled/URL missing, no Omphalos UI appears | Driven by `IsConfigured` — no URL/secret/disabled all return `false` |
| TOKEN-01 | `IIntegrationTokenService` in Domain layer generates signed redirect URL | BCL `HMACSHA256` — zero new NuGet packages |
| TOKEN-02 | HMAC canonical message: alphabetical query string per D-03 | `Uri.EscapeDataString` for questTitle; fixed key order locked by cross-repo contract |
| TOKEN-03 | Tokens expire after 300 seconds | `DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds()` |
| TOKEN-04 | Username lowercase in both MAC and URL parameter | `.ToLower()` on `currentUser.Name` in controller before passing to service |
| TOKEN-05 | `QuestController.LaunchOmphalos(int id)` GET generates URL, returns `Redirect(signedUrl)` or 404 | Follows existing controller action pattern; `IAdminSettingService` already injected |
</phase_requirements>

---

## 1. Phase Summary

This phase wires up the Omphalos deep-link UX in the Quest Board. It builds three things:

**1. OmphalosNavItem ViewComponent** — A new ViewComponent (the first in this codebase) that injects `IAdminSettingService`, checks `IsConfigured`, and renders an "Open Omphalos" `<a>` link in the DM navbar dropdown. Inserted into `_Layout.cshtml` at line 82 (after the "Edit My Profile" `<li>`, before closing `</ul>` of the DM dropdown).

**2. Quest-page Omphalos buttons** — `ViewBag.ShowOmphalosButton` added to `QuestController.Details` and `QuestController.Manage`. Details view gets a button inside the existing "DM Controls" card body. Manage view gets a new "Open Session Notes" card (mirroring the "View Public Page" card pattern).

**3. IntegrationTokenService + LaunchOmphalos endpoint** — A pure-computation domain service that computes HMAC-SHA256 over a fixed canonical message and builds the redirect URL. A new `GET Quest/LaunchOmphalos/{id}` controller action that fetches settings, checks `IsConfigured`, and issues a `302 Redirect` to the signed Omphalos URL.

No new NuGet packages. No EF migrations. No `Program.cs` changes (ViewComponents are auto-discovered in ASP.NET Core MVC).

---

## 2. Existing Patterns

### 2.1 Service Interface Pattern (replicate for IIntegrationTokenService)

**File:** `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` (lines 1–9)
**File:** `EuphoriaInn.Domain/Services/AdminSettingService.cs` (lines 1–36)

```csharp
// IAdminSettingService.cs — public interface, namespace EuphoriaInn.Domain.Interfaces
public interface IAdminSettingService
{
    Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default);
    Task SaveSettingsAsync(string? url, string? secret, bool isEnabled, CancellationToken token = default);
}

// AdminSettingService.cs — internal class, primary constructor injection
internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
```

`IntegrationTokenService` is SIMPLER than this: it has NO repository dependency. It is a stateless pure function. No constructor parameters.

```csharp
// Pattern to follow:
internal class IntegrationTokenService : IIntegrationTokenService
{
    public string GenerateSignedUrl(
        string omphalosBaseUrl, int questId, string questTitle,
        string username, string sharedSecret) { ... }
}
```

### 2.2 ServiceExtensions DI Registration

**File:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (lines 1–27)

Current tail of `AddDomainServices()`:
```csharp
services.AddScoped<IDungeonMasterProfileService, DungeonMasterProfileService>();
services.AddScoped<IAdminSettingService, AdminSettingService>();
// INSERT HERE:
services.AddTransient<IIntegrationTokenService, IntegrationTokenService>();
```

`AddTransient` is correct for a stateless computation service (no shared state, no DB context, no request lifetime concerns).

### 2.3 QuestController Constructor and ViewBag Pattern

**File:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` (lines 13–19, 238–265, 622–644)

Current constructor (line 13–19):
```csharp
public class QuestController(
    IUserService userService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService
    ) : Controller
```

`IAdminSettingService` and `IIntegrationTokenService` must be added as additional primary constructor parameters.

Existing ViewBag flags on `Details` GET (lines 238–265):
- `ViewBag.IsPlayerSignedUp` (line 238)
- `ViewBag.UserCharacters` (line 239)
- `ViewBag.CanManage` (line 244) — already sets `isQuestDm` and `isAdmin` locals (lines 242–244)
- `ViewBag.CalendarMonths` (line 262)
- `ViewBag.IsDetailsPage` (line 263)
- `ViewBag.CurrentQuestId` (line 264)
- `ViewBag.CurrentUserId` (line 265)

`ViewBag.ShowOmphalosButton` goes after line 244, reusing the `isQuestDm` and `isAdmin` locals already in scope.

Existing ViewBag flags on `Manage` GET (lines 622–644):
- `ViewBag.IsAuthorized` (line 640) — sets `isQuestDm` and `isAdmin` locals (lines 638–639)
- `ViewBag.IsAdmin` (line 641)

`ViewBag.ShowOmphalosButton` goes after line 641, reusing `isQuestDm` and `isAdmin` already in scope.

**CRITICAL NOTE on `isQuestDm` comparison:** The Details action uses `currentUser?.Name == quest.DungeonMaster?.Name` (line 242, `==` operator). The Manage action uses `.Equals(..., OrdinalIgnoreCase)` (line 638). For `ShowOmphalosButton`, follow the Manage pattern (`OrdinalIgnoreCase`) since this is the more robust comparison.

### 2.4 Existing `[Authorize]` Attribute Pattern on QuestController

DungeonMasterOnly actions (existing examples from QuestController):
- Line 22: `[Authorize(Policy = "DungeonMasterOnly")]` on `Create` GET
- Line 36: `[Authorize(Policy = "DungeonMasterOnly")]` on `Create` POST
- Line 565: `[Authorize(Policy = "DungeonMasterOnly")]` on `Finalize`
- Line 621: `[Authorize(Policy = "DungeonMasterOnly")]` on `Manage` GET

`LaunchOmphalos` follows this exact pattern:
```csharp
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> LaunchOmphalos(int id, CancellationToken token = default)
```

### 2.5 IntegrationSettings Model

**File:** `EuphoriaInn.Domain/Models/IntegrationSettings.cs` (lines 1–16)

```csharp
public record IntegrationSettings
{
    public string? OmphalosUrl { get; init; }
    [JsonIgnore]
    public string? OmphalosSharedSecret { get; init; }
    public bool IsEnabled { get; init; }

    public bool IsConfigured => IsEnabled
        && !string.IsNullOrWhiteSpace(OmphalosUrl)
        && !string.IsNullOrWhiteSpace(OmphalosSharedSecret);
}
```

`IsConfigured` is the single gate: check this before showing any Omphalos UI or generating a token. No need to check individual fields separately.

---

## 3. Integration Points

### 3.1 `_Layout.cshtml` DM Dropdown Insertion

**File:** `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`

The DM dropdown `<ul class="dropdown-menu">` runs from line 65 to line 83. Current last `<li>` is "Edit My Profile" (lines 77–80). The closing `</ul>` is at line 82.

**Exact insertion point:** After line 80 (closing `</li>` of "Edit My Profile"), before line 82 (closing `</ul>`):

```razor
@* Lines 77–80 — existing "Edit My Profile" item *@
<li>
    <a class="dropdown-item" asp-controller="DungeonMaster" asp-action="EditProfile">
        <i class="fas fa-user-edit me-2"></i>Edit My Profile
    </a>
</li>
@* INSERT HERE — line 81 *@
<li><hr class="dropdown-divider"></li>
@await Component.InvokeAsync("OmphalosNavItem")
@* line 82 — existing closing </ul> *@
</ul>
```

Note: `_Layout.cshtml` already uses `@using EuphoriaInn.Domain.Interfaces` (line 1) and calls `await AuthorizationService.AuthorizeAsync(User, "DungeonMasterOnly")` at line 59. The ViewComponent does not need role checks (D-09) but will use `IAdminSettingService` internally.

### 3.2 `Details.cshtml` DM Controls Card

**File:** `EuphoriaInn.Service/Views/Quest/Details.cshtml`

The "DM Controls" card body is at lines 572–586:
```razor
<div class="col-lg-2 col-md-3">
    @if ((bool)ViewBag.CanManage)
    {
        <div class="card modern-card mb-3">
            <div class="card-header modern-card-header">
                <h5>DM Controls</h5>
            </div>
            <div class="card-body modern-card-body">
                <a href="@Url.Action("Manage", "Quest", new { id = Model.Quest?.Id })" class="btn btn-primary w-100">
                    <i class="fas fa-cog me-2"></i>
                    Manage Quest
                </a>
                @* INSERT HERE — after the Manage Quest anchor, still inside card-body *@
            </div>
        </div>
    }
```

The "Open Session Notes" button goes INSIDE the `modern-card-body` div, after the "Manage Quest" `<a>` anchor. An additional conditional on `ViewBag.ShowOmphalosButton` wraps it:

```razor
@if ((bool)(ViewBag.ShowOmphalosButton ?? false))
{
    <a href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Quest?.Id })"
       class="btn btn-warning w-100 mt-2">
        <i class="fas fa-book-open me-2"></i>
        Open Session Notes
    </a>
}
```

The outer `@if ((bool)ViewBag.CanManage)` already gates the entire DM Controls card — `ShowOmphalosButton` adds a second inner gate so the button only appears when integration is also configured.

### 3.3 `Manage.cshtml` New Sidebar Card

**File:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml`

The `col-md-4` sidebar starts at line 475. "View Public Page" card is lines 476–486. "Quest Summary" card starts at line 488.

**Exact insertion point:** After line 486 (closing `</div>` of "View Public Page" card), before line 488 ("Quest Summary" card). The new card mirrors the "View Public Page" pattern:

```razor
@* After line 486 *@
@if ((bool)(ViewBag.ShowOmphalosButton ?? false))
{
    <div class="card modern-card mb-3">
        <div class="card-header modern-card-header">
            <h5>Session Notes</h5>
        </div>
        <div class="card-body modern-card-body">
            <a href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Id })"
               class="btn btn-warning w-100">
                <i class="fas fa-book-open me-2"></i>
                Open Session Notes
            </a>
        </div>
    </div>
}
@* line 488 — existing "Quest Summary" card *@
```

Note: `Manage.cshtml` model is `Quest` directly (not `PlayerSignup`), so `Model.Id` — not `Model.Quest?.Id`.

### 3.4 ViewComponent File Locations (No Prior Examples — First in Codebase)

ASP.NET Core MVC auto-discovers ViewComponents by convention. No registration in `Program.cs` needed. [VERIFIED: ASP.NET Core conventions — class name ends in `ViewComponent` OR decorated with `[ViewComponent]`, placed anywhere in assembly]

Required files:
- `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` — ViewComponent class
- `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` — view template

ViewComponent class shape:
```csharp
using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Components;

public class OmphalosNavItemViewComponent(IAdminSettingService adminSettingService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await adminSettingService.GetSettingsAsync();
        if (!settings.IsConfigured)
            return Content(string.Empty);   // render nothing
        return View(settings);              // passes OmphalosUrl to Default.cshtml
    }
}
```

Default.cshtml shape:
```razor
@model EuphoriaInn.Domain.Models.IntegrationSettings
<li>
    <a class="dropdown-item" href="@Model.OmphalosUrl" target="_blank" rel="noopener noreferrer">
        <i class="fas fa-external-link-alt me-2"></i>Open Omphalos
    </a>
</li>
```

### 3.5 `LaunchOmphalos` Action in QuestController

New action — no existing analogue in file. Insert after the last `[HttpGet]` action (`CreateFollowUp` ends at line 795). Signature:

```csharp
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> LaunchOmphalos(int id, CancellationToken token = default)
{
    var settings = await adminSettingService.GetSettingsAsync(token);
    if (!settings.IsConfigured)
        return NotFound();

    var quest = await questService.GetQuestWithDetailsAsync(id, token);
    if (quest == null)
        return NotFound();

    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null)
        return Challenge();

    var signedUrl = integrationTokenService.GenerateSignedUrl(
        settings.OmphalosUrl!,
        quest.Id,
        quest.Title,
        currentUser.Name.ToLower(),
        settings.OmphalosSharedSecret!);

    return Redirect(signedUrl);
}
```

The constructor must be extended:
```csharp
public class QuestController(
    IUserService userService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService,
    IAdminSettingService adminSettingService,        // NEW
    IIntegrationTokenService integrationTokenService // NEW
    ) : Controller
```

---

## 4. Implementation Risks

### Risk 1: ViewComponent `Content(string.Empty)` vs. `View()`
When `IsConfigured` is false, `return Content(string.Empty)` renders nothing to the layout. This is the correct pattern for conditional navbar items. Returning an empty View would require a view file to be present. `Content(string.Empty)` needs no view file for the "empty" path. [ASSUMED — standard ASP.NET Core ViewComponent behavior, not tested against a running instance]

### Risk 2: `Details.cshtml` ViewBag Cast Safety
The existing code casts `ViewBag.CanManage` directly: `@if ((bool)ViewBag.CanManage)`. If `ShowOmphalosButton` is ever not set (e.g., in a route that bypasses the controller), `(bool)ViewBag.ShowOmphalosButton` would throw. Use `(bool)(ViewBag.ShowOmphalosButton ?? false)` defensively. The existing `CanManage` cast would also throw if not set — but it always is because the Details action always sets it. `ShowOmphalosButton` must always be set before any view that reads it.

### Risk 3: `Details` Action Has Two Overloads
`QuestController.Details` has a GET (line 209) and a POST (line 277). The `ViewBag.ShowOmphalosButton` must only be set on the GET. The POST redirects to GET, so this is fine — but the planner must target the GET specifically.

### Risk 4: `isQuestDm` Comparison Inconsistency
The `Details` GET uses `currentUser?.Name == quest.DungeonMaster?.Name` (null-propagating `==`). The `Manage` GET uses `currentUser.Name.Equals(..., OrdinalIgnoreCase)`. For `ShowOmphalosButton`, use `OrdinalIgnoreCase` consistently (matching the Manage pattern). The `Details` action already has the `isQuestDm` local (line 242) — re-read it carefully; for ShowOmphalosButton, prefer the safe comparison.

### Risk 5: `currentUser` Null Guard in Details Before ShowOmphalosButton
In the `Details` GET, `currentUser` can be null (unauthenticated visitor). `ShowOmphalosButton` requires `isQuestDm || isAdmin`, both of which require `currentUser != null`. Guard: `settings.IsConfigured && currentUser != null && (isQuestDm || isAdmin)`. The existing `isAdmin` computation at line 243 (`await userService.IsInRoleAsync(User, "Admin")`) is called unconditionally — so the existing locals are always available.

### Risk 6: Token URL Construction — `OmphalosUrl` Trailing Slash
`settings.OmphalosUrl` may or may not have a trailing slash. The `GenerateSignedUrl` implementation must use `TrimEnd('/')` before appending `/api/sso/open-quest`. The CONTEXT.md specifies this explicitly: `{OmphalosUrl.TrimEnd('/')}/api/sso/open-quest?...`.

### Risk 7: `Uri.EscapeDataString` vs. `HttpUtility.UrlEncode`
The TOKEN-02 canonical message uses `Uri.EscapeDataString` for questTitle (percent-encoding). This is the correct BCL method — it encodes spaces as `%20` not `+`. The Omphalos SSO-01 side expects percent-encoding. Do not use `HttpUtility.UrlEncode` (encodes spaces as `+`).

### Risk 8: HMAC Byte Encoding
`HMACSHA256.ComputeHash` returns `byte[]`. Convert to lowercase hex with `Convert.ToHexString(hash).ToLower()` (BCL, .NET 5+). Do not use `BitConverter.ToString().Replace("-", "")` — it produces uppercase and is noisier.

### Risk 9: Integration Test — AdminSettings Seeding Pattern
The `LaunchOmphalos` integration tests need to seed `AdminSettingEntity` rows in the test database. The `AdminSettingServiceTests` (lines 1–75) directly instantiate `AdminSettingRepository` over a `TestDatabase` context. For integration tests that go through the HTTP client, the settings must be seeded directly into the `QuestBoardContext` via `factory.Services`. Use:

```csharp
using var scope = factory.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
context.AdminSettings.AddRange(
    new AdminSettingEntity { Key = "IsEnabled", Value = "True", UpdatedAt = DateTime.UtcNow },
    new AdminSettingEntity { Key = "OmphalosUrl", Value = "https://omphalos.example.com", UpdatedAt = DateTime.UtcNow },
    new AdminSettingEntity { Key = "OmphalosSharedSecret", Value = "test-secret", UpdatedAt = DateTime.UtcNow }
);
await context.SaveChangesAsync();
```

---

## 5. Test Patterns

### 5.1 Unit Test Pattern — IntegrationTokenService

**File to create:** `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs`

No mock dependencies — pure function. Pattern from `EmailServiceTests.cs`:

```csharp
using EuphoriaInn.Domain.Services;

namespace EuphoriaInn.UnitTests.Services;

public class IntegrationTokenServiceTests
{
    private static readonly IntegrationTokenService _sut = new();
```

Tests needed (per D-18):

**Test 1: Canonical message construction**
Fix expiry via time manipulation is NOT possible in a pure `new()` call (the service calls `DateTimeOffset.UtcNow` internally). The test must verify URL structure rather than the exact HMAC value, OR extract the canonical message computation for direct testing. Recommended approach: extract a `BuildCanonicalMessage(long expiry, int questId, string questTitle, string username)` internal/private method AND a separate `ComputeHmac(string message, string secret)` method, then test them via reflection OR make `GenerateSignedUrl` accept an optional `expiry` parameter for testing (test-seam pattern).

Simpler alternative: Call `GenerateSignedUrl` with known inputs and verify the returned URL contains the expected parameters in the expected shape (expiry is in the future, questId matches, questTitle is percent-encoded, username is lowercase, sig is lowercase hex of correct length).

```csharp
[Fact]
public void GenerateSignedUrl_ReturnsUrlWithCorrectEndpointPath()
{
    var url = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 42, "Dragon's Lair", "DMDave", "secret");

    url.Should().Contain("/api/sso/open-quest");
    url.Should().StartWith("https://omphalos.example.com/api/sso/open-quest");
}

[Fact]
public void GenerateSignedUrl_LowercasesUsername()
{
    var url = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 42, "Quest", "UPPERCASE_DM", "secret");

    url.Should().Contain("username=uppercase_dm");
}

[Fact]
public void GenerateSignedUrl_PercentEncodesQuestTitle()
{
    var url = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 1, "Dragon's Lair", "dm", "secret");

    url.Should().Contain("questTitle=Dragon%27s%20Lair");
}

[Fact]
public void GenerateSignedUrl_SigIsLowercaseHex64Chars()
{
    var url = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 1, "Quest", "dm", "secret");

    var sigValue = ExtractQueryParam(url, "sig");
    sigValue.Should().MatchRegex("^[0-9a-f]{64}$"); // SHA-256 = 32 bytes = 64 hex chars
}

[Fact]
public void GenerateSignedUrl_ExpiryIsInFuture()
{
    var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var url = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 1, "Quest", "dm", "secret");
    var after = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds();

    var expiryStr = ExtractQueryParam(url, "expiry");
    var expiry = long.Parse(expiryStr);
    expiry.Should().BeInRange(before + 299, after + 1);
}

[Fact]
public void GenerateSignedUrl_TrimsTrailingSlashFromBaseUrl()
{
    var urlWithSlash = _sut.GenerateSignedUrl(
        "https://omphalos.example.com/", 1, "Quest", "dm", "secret");
    var urlWithout = _sut.GenerateSignedUrl(
        "https://omphalos.example.com", 1, "Quest", "dm", "secret");

    // Both should produce the same endpoint path (only sig will differ due to timing)
    urlWithSlash.Should().Contain("/api/sso/open-quest");
    urlWithout.Should().Contain("/api/sso/open-quest");
    urlWithSlash.Should().NotContain("//api/sso/open-quest");
}

[Fact]
public void GenerateSignedUrl_DifferentSecretsProduceDifferentSigs()
{
    var url1 = _sut.GenerateSignedUrl("https://x.com", 1, "Q", "dm", "secret1");
    var url2 = _sut.GenerateSignedUrl("https://x.com", 1, "Q", "dm", "secret2");

    ExtractQueryParam(url1, "sig").Should().NotBe(ExtractQueryParam(url2, "sig"));
}
```

### 5.2 Integration Test Pattern — LaunchOmphalos Endpoint

**File to create:** `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs`

Pattern follows `AdminControllerIntegrationTests.cs` + `QuestControllerIntegrationTests_Comprehensive.cs`. Uses `IClassFixture<WebApplicationFactoryBase>` and `AuthenticationHelper.CreateAuthenticatedDMClientAsync`.

```csharp
public class LaunchOmphalosIntegrationTests(WebApplicationFactoryBase factory)
    : IClassFixture<WebApplicationFactoryBase>
{
    private async Task SeedSettingsAsync(
        string? url = "https://omphalos.example.com",
        string? secret = "test-secret",
        bool isEnabled = true) { ... }

    [Fact]
    public async Task LaunchOmphalos_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        var client = factory.CreateNonRedirectingClient();
        var response = await client.GetAsync("/Quest/LaunchOmphalos/1");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LaunchOmphalos_WhenPlayerRole_ShouldReturnForbidden()
    { ... }

    [Fact]
    public async Task LaunchOmphalos_WhenIntegrationDisabled_ShouldReturn404()
    {
        // Seed settings with IsEnabled=false
        // Authenticate as DM
        // GET /Quest/LaunchOmphalos/{id}
        // Assert 404
    }

    [Fact]
    public async Task LaunchOmphalos_WhenNoUrlConfigured_ShouldReturn404()
    {
        // Seed settings with empty OmphalosUrl
        // Assert 404
    }

    [Fact]
    public async Task LaunchOmphalos_WhenIntegrationEnabled_ShouldRedirectToOmphalosUrl()
    {
        // Seed settings + quest + DM user
        // GET /Quest/LaunchOmphalos/{questId}
        // Assert 302 Redirect
        // Location header starts with "https://omphalos.example.com/api/sso/open-quest"
    }

    [Fact]
    public async Task LaunchOmphalos_RedirectUrl_ContainsExpectedQueryParameters()
    {
        // Parse Location header query string
        // Assert questId, username (lowercase), sig (lowercase hex), expiry (> now) all present
    }
}
```

Key pattern from `AdminControllerIntegrationTests`: the factory uses `WebApplicationFactoryBase` (not raw `WebApplicationFactory<Program>`), non-redirecting client via `factory.CreateNonRedirectingClient()`, and `AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory)` for DM access.

AdminSettingEntity seeding (for `LaunchOmphalos` tests that need `IsConfigured = true`):
```csharp
private async Task SeedSettingsAsync(...)
{
    using var scope = factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
    // Clear any existing settings first
    context.AdminSettings.RemoveRange(context.AdminSettings);
    await context.SaveChangesAsync();
    // Seed
    context.AdminSettings.AddRange(
        new AdminSettingEntity { Key = "IsEnabled", Value = isEnabled.ToString(), UpdatedAt = DateTime.UtcNow },
        new AdminSettingEntity { Key = "OmphalosUrl", Value = url, UpdatedAt = DateTime.UtcNow },
        new AdminSettingEntity { Key = "OmphalosSharedSecret", Value = secret, UpdatedAt = DateTime.UtcNow }
    );
    await context.SaveChangesAsync();
}
```

---

## 6. Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xunit.v3 3.2.2 + FluentAssertions 8.9.0 |
| Unit config | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| Integration config | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run (unit) | `dotnet test EuphoriaInn.UnitTests` |
| Quick run (integration) | `dotnet test EuphoriaInn.IntegrationTests` |
| Full suite | `dotnet test` (from solution root) |

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TOKEN-01 | `IIntegrationTokenService` generates signed URL | unit | `dotnet test EuphoriaInn.UnitTests --filter IntegrationTokenService` | No — Wave 0 |
| TOKEN-02 | Canonical MAC message format | unit | same | No — Wave 0 |
| TOKEN-03 | Token expiry 300 seconds | unit | same | No — Wave 0 |
| TOKEN-04 | Username lowercase | unit | same | No — Wave 0 |
| TOKEN-05 / NAV-03 / NAV-04 | `LaunchOmphalos` returns redirect or 404 | integration | `dotnet test EuphoriaInn.IntegrationTests --filter LaunchOmphalos` | No — Wave 0 |
| NAV-01 | ViewComponent renders in layout when configured | integration (smoke) | `dotnet test EuphoriaInn.IntegrationTests --filter OmphalosNavItem` | No — Wave 0 |
| NAV-05 | No Omphalos UI when disabled | integration | same filter | No — Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests` (< 5 seconds, pure unit)
- **Per wave merge:** `dotnet test` (full solution)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` — covers TOKEN-01 through TOKEN-04
- [ ] `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` — covers TOKEN-05, NAV-03, NAV-04, NAV-05
- [ ] `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` — new ViewComponent class
- [ ] `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` — new view

---

## Project Constraints (from CLAUDE.md)

| Directive | Impact on This Phase |
|-----------|---------------------|
| ASP.NET Core 10 MVC only | No framework changes — ViewComponents are native MVC feature |
| EF Core migrations required for schema changes | No schema changes in this phase — no migration needed |
| Deployable via `docker-compose up` | No infrastructure changes |
| No user-facing functionality may be removed or broken | Existing QuestController actions unchanged except constructor extension |
| EF packages in Repository project only | `IntegrationTokenService` uses BCL only — no EF |
| Modern card styling (`modern-card`, `modern-card-header`, `modern-card-body`) | "Open Session Notes" card on Manage page must use these classes |
| Buttons: filled colored, FontAwesome icon with `me-2`, semantic colors | Button uses `btn-warning` + `fas fa-book-open me-2` |
| `[JsonIgnore]` on `OmphalosSharedSecret` | Already applied in `IntegrationSettings` — do not remove |

---

## Sources

### Primary (HIGH confidence — verified against live codebase)

- `EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs` — interface pattern for `IIntegrationTokenService`
- `EuphoriaInn.Domain/Services/AdminSettingService.cs` — service implementation pattern
- `EuphoriaInn.Domain/Models/IntegrationSettings.cs` — `IsConfigured` property shape
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — DI registration location and pattern
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — constructor, ViewBag pattern, `[Authorize]` usage
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — exact DM dropdown structure (lines 59–84)
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` lines 572–586 — "DM Controls" card structure
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 475–487 — "View Public Page" card pattern
- `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs` — test factory pattern
- `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` — service-layer test pattern
- `EuphoriaInn.IntegrationTests/Helpers/AuthenticationHelper.cs` — DM/admin client creation helpers
- `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — controller integration test pattern
- `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` — unit test pattern for Domain services
- `EuphoriaInn.Repository/Entities/AdminSettingEntity.cs` — entity structure for test seeding

### Secondary (MEDIUM confidence — ASP.NET Core MVC conventions)

- ASP.NET Core ViewComponent auto-discovery: class name ends `ViewComponent`, placed in any folder, returns `IViewComponentResult`, view in `Views/Shared/Components/{Name}/Default.cshtml` [ASSUMED — standard .NET 10 convention, not verified against a running instance]

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `return Content(string.Empty)` from a ViewComponent renders no HTML to the layout | Section 4, Risk 1 | ViewComponent might require a view to exist; fallback: return `View("Empty")` with empty Default.cshtml |
| A2 | ASP.NET Core MVC auto-discovers ViewComponents in any folder within the assembly | Section 3.4 | If discovery requires specific folder, `Components/` is the conventional location and should still work |
| A3 | `Convert.ToHexString(bytes).ToLower()` is available in .NET 10 | Section 4, Risk 8 | It was introduced in .NET 5; .NET 10 definitely has it. Risk is negligible. |

**If this table is empty:** All claims in this research were verified or cited.

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH — BCL only, no new packages
- Architecture: HIGH — all patterns read directly from live code
- Pitfalls: HIGH — risks derived from reading actual code (null guards, cast safety, URL construction)
- Test patterns: HIGH — patterns read from existing test files

**Research date:** 2026-06-18
**Valid until:** Stable (no external dependencies; only changes if codebase changes)
