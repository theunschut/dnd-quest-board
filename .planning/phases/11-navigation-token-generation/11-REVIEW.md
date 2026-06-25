---
phase: 11-navigation-token-generation
reviewed: 2026-06-18T00:00:00Z
depth: standard
files_reviewed: 11
files_reviewed_list:
  - EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs
  - EuphoriaInn.Domain/Services/IntegrationTokenService.cs
  - EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
  - EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs
  - EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs
  - EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs
  - EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs
  - EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml
  - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
  - EuphoriaInn.Service/Views/Quest/Details.cshtml
  - EuphoriaInn.Service/Views/Quest/Manage.cshtml
findings:
  critical: 0
  warning: 3
  info: 3
  total: 6
status: issues_found
---

# Phase 11: Code Review Report

**Reviewed:** 2026-06-18
**Depth:** standard
**Files Reviewed:** 11
**Status:** issues_found

## Summary

Phase 11 adds Omphalos integration: HMAC-SHA256 signed URL generation, a `LaunchOmphalos` controller action, and two UI surfaces (the `OmphalosNavItem` view component in the global nav, and inline buttons on the Details/Manage pages). The core cryptographic logic is correct — HMAC-SHA256 via BCL `HMACSHA256.HashData`, alphabetical canonical message order per the cross-repo contract, `Uri.EscapeDataString` for encoding, and lowercase hex output. The `IsConfigured` guard in `IntegrationSettings` is solid.

Three warnings and three info items were found. No critical (security-breaking or data-loss) issues exist.

Key concerns:
- The `OmphalosNavItem` view links directly to `OmphalosUrl` without a token, bypassing SSO entirely — this may or may not be intentional but is a behaviour gap worth confirming.
- The `LaunchOmphalos` action double-lowercases the username (caller lowercases before passing; service also lowercases internally), which is harmless but redundant and worth cleaning up to preserve contract clarity.
- A null-dereference risk exists in `_Layout.cshtml` if `GetUserAsync` returns null for an authenticated user.

## Warnings

### WR-01: OmphalosNavItem links raw base URL, not a signed token — bypasses SSO

**File:** `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml:4`

**Issue:** The nav item renders `href="@Model.OmphalosUrl"` — a direct link to the Omphalos base URL with no HMAC token attached. This takes the DM to Omphalos unauthenticated. The `LaunchOmphalos` action exists precisely to generate a signed URL and redirect; the nav item skips it entirely. If Omphalos requires SSO to access anything useful, users will land on a login wall and the integration achieves nothing from the nav bar.

The Details and Manage pages correctly link to `LaunchOmphalos`, making the nav item the only surface with this gap.

**Fix:** Route through the `LaunchOmphalos` action instead of linking the raw URL directly. Because the nav item runs per-request without a quest context, this link should either be removed from the global nav (and left only on the quest-specific pages where `id` is available), or it should navigate to a general Omphalos landing page if that is supported without a quest context.

If quest-scoped navigation is the only supported flow, remove the nav item from the layout dropdown and rely solely on the per-quest buttons:
```html
@* Remove this nav item — Omphalos access is quest-scoped via LaunchOmphalos *@
```

If a non-quest-scoped Omphalos landing URL exists, link there explicitly as an authenticated-but-unsigned entry point (only if Omphalos supports unauthenticated or session-cookie access to its root).

---

### WR-02: Double username lowercasing — caller pre-lowercases before passing to service that also lowercases

**File:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs:827`

**Issue:** `LaunchOmphalos` calls `integrationTokenService.GenerateSignedUrl(... currentUser.Name.ToLower() ...)`. `IntegrationTokenService.GenerateSignedUrl` then applies `.ToLower()` again at line 15 of the service (`var lowerUser = username.ToLower()`). The double-lowercasing is harmless in the current locale (ASCII names), but it muddies the contract: the service interface documents that it lowercases internally, so callers should not need to do so. If a caller ever passes a correctly-cased name expecting the service to normalise it, but the service contract is later changed to trust the input, callers that pre-lowercase will silently produce wrong output.

More concretely: the cross-repo MAC contract (D-03) is defined at the service level. Pre-lowercasing in the controller means the controller is partially re-implementing the MAC input normalisation, which is the service's responsibility.

**Fix:** Remove the `.ToLower()` call at the call site — let the service own the normalisation:
```csharp
// QuestController.cs, LaunchOmphalos action
var signedUrl = integrationTokenService.GenerateSignedUrl(
    settings.OmphalosUrl!,
    quest.Id,
    quest.Title,
    currentUser.Name,          // no ToLower() — service normalises
    settings.OmphalosSharedSecret!);
```

---

### WR-03: `_Layout.cshtml` dereferences `currentUser.Name` without null guard

**File:** `EuphoriaInn.Service/Views/Shared/_Layout.cshtml:118-122`

**Issue:**
```csharp
var currentUser = await UserService.GetUserAsync(User);
// ...
<i class="fas fa-user me-1"></i>@currentUser.Name
```

`GetUserAsync` can return `null` if the user record is not found in the database despite having a valid authentication cookie (e.g., after an account deletion, database seed, or identity migration). Accessing `.Name` on a null reference will throw a `NullReferenceException` on every page render for that user session, effectively locking them out with an unhandled 500.

The surrounding `if (User.Identity?.IsAuthenticated == true)` block only confirms the cookie is valid — it does not guarantee `GetUserAsync` returns a non-null user.

**Fix:** Add a null check before rendering the username:
```csharp
var currentUser = await UserService.GetUserAsync(User);
if (currentUser == null)
{
    // User authenticated but not found — render fallback or logout link
    <li class="nav-item">
        <a class="nav-link" asp-controller="Account" asp-action="Logout">Sign out</a>
    </li>
}
else
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" ...>
            <i class="fas fa-user me-1"></i>@currentUser.Name
        </a>
        ...
    </li>
}
```

Note: this null path exists in the pre-phase code and is not introduced by Phase 11. It is flagged here because Phase 11 added `@await Component.InvokeAsync("OmphalosNavItem")` inside the same authenticated block, increasing the likelihood this block is exercised and making the existing risk worth recording.

---

## Info

### IN-01: `IntegrationTokenService` registered as Transient — inconsistent with all other domain services

**File:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs:24`

**Issue:** All other domain services are registered as `Scoped`. `IntegrationTokenService` is registered as `Transient`. The service holds no state and creates no resources, so both lifetimes are functionally correct. However, the inconsistency may confuse future maintainers who expect uniform service lifetimes in this layer.

**Fix:** Align with the project convention unless there is a specific reason for `Transient` (there is none visible in the implementation):
```csharp
services.AddScoped<IIntegrationTokenService, IntegrationTokenService>();
```

---

### IN-02: `IntegrationTokenServiceTests` instantiates `internal` implementation directly

**File:** `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs:7`

**Issue:**
```csharp
private static readonly IntegrationTokenService _sut = new();
```

`IntegrationTokenService` is `internal`. The test project referencing it directly requires an `InternalsVisibleTo` attribute or the test project must be in the same assembly. This is a build-time concern but worth noting: if the `InternalsVisibleTo` attribute is removed (common during refactors), this line silently breaks compilation. The more resilient pattern is to instantiate via the interface:

```csharp
private static readonly IIntegrationTokenService _sut = new IntegrationTokenService();
```

This also ensures the test exercises only the public contract.

---

### IN-03: `GenerateSignedUrl_ExpiryIsInFuture` test has a flawed upper bound check

**File:** `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs:62-68`

**Issue:**
```csharp
var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var url = _sut.GenerateSignedUrl(...);
var after = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds();

expiry.Should().BeInRange(before + 299, after + 1);
```

The lower bound `before + 299` is fragile: if the test runs at a second boundary, `before` may be captured just before the second ticks, and `before + 299` could equal or exceed the actual `expiry` (which is `now + 300` at the moment of the call). If the OS schedules a context switch between capturing `before` and the `GenerateSignedUrl` call, the test could flake.

A safer bound is `before + 298` (one second of margin) or simply checking `expiry >= before + 299` separately from the upper bound. The upper bound `after + 1` is correct.

**Fix:**
```csharp
expiry.Should().BeGreaterThanOrEqualTo(before + 298);
expiry.Should().BeLessThanOrEqualTo(after + 1);
```

---

_Reviewed: 2026-06-18_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
