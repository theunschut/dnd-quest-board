# Phase 24: Email Confirmation Flow - Research

**Researched:** 2026-06-26
**Domain:** ASP.NET Core Identity email confirmation, domain model extension, job guard pattern
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01:** Add `bool EmailConfirmed` to the `User` domain model. Wire through AutoMapper `EntityProfile` (same pattern as `HasKey`). This makes the field available to jobs and ViewModels without layer violations.

**D-02:** `GET /Account/ConfirmEmail?userId=X&token=Y` lives in `AccountController` — it already owns all identity flows (Login, Register, Profile, ChangePassword).

**D-03:** On success: redirect to `AccountController.Login` with a TempData success banner ("Email confirmed — you can now log in"). On failure: same redirect with a TempData error message. No dedicated views needed.

**D-04:** "Send Confirmation Email" button appears inline in the existing Actions column for each row where `EmailConfirmed == false`. The button is absent for already-confirmed users. Consistent with existing Promote/Demote/Reset row actions.

**D-05:** After POSTing to `AdminController.SendConfirmationEmail`, page reloads and a TempData success or error banner appears at the top — same feedback pattern as promotions and password resets.

**D-06:** Introduce a shared extension method `WhereEmailConfirmed()` on `IEnumerable<User>` in the Domain layer. All four jobs (`QuestFinalizedEmailJob`, `QuestDateChangedEmailJob`, `SessionReminderJob`, `DailyReminderJob`) call this to filter their recipient list before sending.

**D-07:** The extension method lives in a new class in `EuphoriaInn.Domain` (e.g., `UserExtensions`), alongside the `User` model it operates on. Keeps logic testable in isolation.

**Discretion:** Email subject/body for the confirmation email in this phase: plain inline HTML string (Phase 25 upgrades it to a Razor component). Claude may choose a reasonable subject line and body. Token generation: use `UserManager.GenerateEmailConfirmationTokenAsync` + `WebEncoders.Base64UrlEncode` for URL-safe encoding.

### Deferred Ideas (OUT OF SCOPE)

- Styled Razor component for the confirmation email — Phase 25 (already planned)
- Auto-send confirmation email on user registration — out of scope for this phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| REQ-24-01 | Admin/Users page shows "Send Confirmation Email" button for every user where `EmailConfirmed == false`; absent for confirmed users | `UserManagementViewModel.EmailConfirmed` drives `@if (!userModel.EmailConfirmed)` in Users.cshtml; IdentityUser.EmailConfirmed already on UserEntity |
| REQ-24-02 | Clicking button POSTs to `AdminController.SendConfirmationEmail`, generates Identity token, builds callback URL, sends via `IEmailService.SendAsync`, shows TempData success | `IIdentityService` extension with two new methods; `Url.Action` for callback; `IEmailService.SendAsync` already available |
| REQ-24-03 | `GET /Account/ConfirmEmail?userId=X&token=Y` calls `UserManager.ConfirmEmailAsync`, sets EmailConfirmed=true, redirects to Login with TempData | Identity `ConfirmEmailAsync` takes entity + token; token must be Base64Url-decoded before use |
| REQ-24-04 | Every Hangfire job skips recipients whose `EmailConfirmed == false` — verified by unit tests | `WhereEmailConfirmed()` extension on `IEnumerable<User>`; unit tests follow NSubstitute pattern from SessionReminderJobTests |
</phase_requirements>

---

## Summary

Phase 24 adds three capabilities: an admin-triggered confirmation email flow, a confirmation callback endpoint in `AccountController`, and a `EmailConfirmed` guard across all four Hangfire email jobs.

The domain work is minimal and additive. `IdentityUser<int>` (the base of `UserEntity`) already carries a `bool EmailConfirmed` column — no EF migration is needed. The only schema impact is that AutoMapper must be told to surface this field from `UserEntity` to the `User` domain model, and from `User` to `UserManagementViewModel`. The existing `HasKey` bool property in `EntityProfile.cs` (line 39: `CreateMap<UserEntity, User>()`) shows the exact pattern: AutoMapper maps all simple properties by convention, so adding `EmailConfirmed` to `User.cs` is sufficient — no `ForMember` call is required.

The identity operations (`GenerateEmailConfirmationTokenAsync`, `ConfirmEmailAsync`) follow the established `IIdentityService` / `IdentityService` pattern precisely. `AdminResetPasswordAsync` in `IdentityService.cs` is the direct analog: find user by ID, call a `UserManager` operation, return `IdentityResult`. Two new interface methods (`GenerateEmailConfirmationAsync` returning a token string, `ConfirmEmailAsync` returning `IdentityResult`) slot cleanly into `IIdentityService`. The token must be URL-safe — `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))` on generation, reversed (`Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken))`) on receipt.

The job guard is a single 2-line extension method that all four jobs call. `QuestFinalizedEmailJob` and `QuestDateChangedEmailJob` receive `string[] recipientEmails` and `string[] playerNames` as parallel arrays — they do NOT receive `User` objects. The guard must be applied at the caller site (in `QuestController` / wherever the jobs are enqueued) before the arrays are built, OR the jobs must be refactored to receive `User` objects. Given D-06/D-07 specify `WhereEmailConfirmed()` on `IEnumerable<User>`, the most natural application point differs by job — see Architectural Responsibility Map and Pitfalls sections.

**Primary recommendation:** Implement in four sequential waves: (1) domain model + AutoMapper + IdentityService interface/impl, (2) AdminController action + Users.cshtml + ViewModel, (3) AccountController.ConfirmEmail callback + Login.cshtml TempData banner, (4) job guards + unit tests.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| `EmailConfirmed` field on domain model | Domain | — | Lives on `User`; flows up via AutoMapper like `HasKey` |
| Token generation + email dispatch | Repository (IdentityService) | Domain (IIdentityService interface) | `UserManager` only accessible at Repository layer |
| Admin "send" action | Service (AdminController) | — | Coordinates identity service + email service; stays in controller per project pattern |
| Confirmation callback | Service (AccountController) | — | Stateless GET; AccountController owns all identity flows |
| Email job guard | Service (jobs) | Domain (UserExtensions) | Extension method in Domain; called by jobs in Service layer |
| ViewModel exposure | Service (ViewModelProfile + UserManagementViewModel) | — | Presentation layer concern |

---

## Standard Stack

### Core

All required libraries are already installed. No new packages needed.

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| Microsoft.AspNetCore.Identity | (built-in, .NET 10) | `UserManager<T>` — token generation and confirmation | [VERIFIED: codebase; IdentityService.cs already uses it] |
| Microsoft.AspNetCore.WebUtilities | (built-in, .NET 10) | `WebEncoders.Base64UrlEncode` / `Base64UrlDecode` | [VERIFIED: CONTEXT.md canonical ref; in ASP.NET Core framework] |
| System.Text.Encoding | (BCL) | `Encoding.UTF8.GetBytes` / `GetString` for token conversion | [VERIFIED: .NET BCL] |

### No New Packages

`IEmailService.SendAsync` (Phase 21), `IIdentityService` / `IdentityService` (existing), `UserManager<UserEntity>` (existing DI registration) — all available. `WebEncoders` is in `Microsoft.AspNetCore.WebUtilities` which ships with the ASP.NET Core framework. No `<PackageReference>` additions required. [VERIFIED: codebase — EuphoriaInn.Repository already uses UserManager]

---

## Architecture Patterns

### System Architecture Diagram

```
Admin browser
    |
    | POST /Admin/SendConfirmationEmail?userId=X
    v
AdminController.SendConfirmationEmail
    |
    |-- IUserService.GetByIdAsync(userId)          → User domain model (with EmailConfirmed)
    |-- IIdentityService.GenerateEmailConfirmationAsync(userId)  → token string
    |-- Url.Action("ConfirmEmail","Account", {userId, token})    → callback URL
    |-- IEmailService.SendAsync(email, subject, html)            → SMTP
    |-- TempData["Success"] / TempData["Error"]
    |
    v  RedirectToAction(nameof(Users))

User's email client
    |
    | GET /Account/ConfirmEmail?userId=X&token=Y
    v
AccountController.ConfirmEmail
    |
    |-- IIdentityService.ConfirmEmailAsync(userId, decodedToken)  → IdentityResult
    |-- TempData["Success"] / TempData["Error"]
    |
    v  RedirectToAction(nameof(Login))

Hangfire job (all four)
    |
    | recipient list as IEnumerable<User>
    |
    |-- .WhereEmailConfirmed()   (UserExtensions, Domain layer)
    |
    v  send only to confirmed users
```

### Recommended Project Structure Changes

```
EuphoriaInn.Domain/
  Models/
    User.cs                          -- add: bool EmailConfirmed
  Extensions/
    UserExtensions.cs                -- NEW: WhereEmailConfirmed() extension method

EuphoriaInn.Repository/
  Automapper/
    EntityProfile.cs                 -- no change needed (convention mapping picks up EmailConfirmed)
  IdentityService.cs                 -- add: GenerateEmailConfirmationAsync, ConfirmEmailAsync

EuphoriaInn.Domain/
  Interfaces/
    IIdentityService.cs              -- add: GenerateEmailConfirmationAsync, ConfirmEmailAsync signatures

EuphoriaInn.Service/
  Controllers/Admin/
    AdminController.cs               -- add: SendConfirmationEmail POST action
    AccountController.cs             -- add: ConfirmEmail GET action
  ViewModels/AdminViewModels/
    UserManagementViewModel.cs       -- add: bool EmailConfirmed
  Views/Admin/
    Users.cshtml                     -- add: TempData banners + conditional button per row
  Views/Account/
    Login.cshtml                     -- add: TempData banners

EuphoriaInn.Service/Jobs/
  QuestFinalizedEmailJob.cs          -- guard: apply WhereEmailConfirmed at call site
  QuestDateChangedEmailJob.cs        -- guard: apply WhereEmailConfirmed at call site
  SessionReminderJob.cs              -- guard: .WhereEmailConfirmed() on targetSignups
  DailyReminderJob.cs                -- (no recipients directly; delegates to SessionReminderJob)

EuphoriaInn.Service/Automapper/
  ViewModelProfile.cs                -- add: map User.EmailConfirmed → UserManagementViewModel.EmailConfirmed

EuphoriaInn.UnitTests/
  Services/
    UserExtensionsTests.cs           -- NEW: WhereEmailConfirmed unit tests
    EmailConfirmationJobGuardTests.cs -- NEW: job guard tests per REQ-24-04
```

### Pattern 1: Adding a Boolean Property to the User Domain Model

`EmailConfirmed` is already stored on `UserEntity` (inherited from `IdentityUser<int>`). The `CreateMap<UserEntity, User>()` mapping in `EntityProfile.cs` uses AutoMapper convention — it maps all public properties with matching names automatically. Adding `bool EmailConfirmed` to `User.cs` is sufficient; no `ForMember` call is needed.

```csharp
// Source: EntityProfile.cs line 39 — existing pattern (HasKey maps by convention too)
CreateMap<UserEntity, User>();  // EmailConfirmed picks up automatically — no change required
```

```csharp
// EuphoriaInn.Domain/Models/User.cs — add one property
public bool HasKey { get; set; }
public bool EmailConfirmed { get; set; }   // ADD THIS
```

**Equals/GetHashCode:** `User.cs` has a hand-coded `Equals`/`GetHashCode` that includes `HasKey`. `EmailConfirmed` must be added to both methods to maintain consistency. [VERIFIED: codebase — User.cs lines 24-35]

### Pattern 2: IIdentityService Extension Methods

Follow `AdminResetPasswordAsync` exactly. Two new methods:

```csharp
// IIdentityService.cs additions
Task<string?> GenerateEmailConfirmationAsync(int userId);
Task<IdentityResult> ConfirmEmailAsync(int userId, string token);
```

```csharp
// IdentityService.cs implementations
public async Task<string?> GenerateEmailConfirmationAsync(int userId)
{
    var entity = await userManager.FindByIdAsync(userId.ToString());
    if (entity == null) return null;
    return await userManager.GenerateEmailConfirmationTokenAsync(entity);
}

public async Task<IdentityResult> ConfirmEmailAsync(int userId, string token)
{
    var entity = await userManager.FindByIdAsync(userId.ToString());
    if (entity == null)
        return IdentityResult.Failed(new IdentityError { Description = "User not found." });
    return await userManager.ConfirmEmailAsync(entity, token);
}
```

[VERIFIED: codebase — mirrors AdminResetPasswordAsync pattern at lines 97-109 of IdentityService.cs]

### Pattern 3: Token URL-Safety

ASP.NET Identity tokens contain characters that are invalid in URLs (`+`, `/`, `=`). They must be encoded before embedding in the callback URL and decoded before calling `ConfirmEmailAsync`.

```csharp
// In AdminController.SendConfirmationEmail — encoding
var rawToken = await identityService.GenerateEmailConfirmationAsync(userId);
var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
var callbackUrl = Url.Action(
    action: "ConfirmEmail",
    controller: "Account",
    values: new { userId, token = encodedToken },
    protocol: Request.Scheme);   // must include protocol for absolute URL in email

// In AccountController.ConfirmEmail — decoding
var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
var result = await identityService.ConfirmEmailAsync(userId, decodedToken);
```

[CITED: CONTEXT.md specifics section; ASSUMED: Url.Action with protocol parameter — standard ASP.NET Core pattern for generating absolute URLs in email links]

### Pattern 4: AdminController.SendConfirmationEmail POST Action

Mirrors `ResetPassword` POST action structure exactly.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SendConfirmationEmail(int userId)
{
    var user = await userService.GetByIdAsync(userId);
    if (user == null)
        return RedirectToAction(nameof(Users));

    var rawToken = await identityService.GenerateEmailConfirmationAsync(userId);
    if (rawToken == null)
    {
        TempData["Error"] = $"Failed to send confirmation email to {user.Name}. Please try again.";
        return RedirectToAction(nameof(Users));
    }

    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
    var callbackUrl = Url.Action("ConfirmEmail", "Account",
        new { userId, token = encodedToken }, Request.Scheme);

    var html = $"""
        <p>Hi {user.Name},</p>
        <p>Click <a href="{callbackUrl}">here</a> to confirm your email address and activate your Quest Board account.</p>
        """;

    await emailService.SendAsync(user.Email!, "Confirm your D&D Quest Board account", html);

    TempData["Success"] = $"Confirmation email sent to {user.Name}.";
    return RedirectToAction(nameof(Users));
}
```

**AdminController needs `IIdentityService` and `IEmailService` injected** — currently the controller only takes `IUserService` and `IQuestService`. Add `IIdentityService identityService, IEmailService emailService` to the primary constructor. [VERIFIED: codebase — AdminController.cs line 9]

### Pattern 5: WhereEmailConfirmed Extension Method

```csharp
// EuphoriaInn.Domain/Extensions/UserExtensions.cs (NEW FILE)
namespace EuphoriaInn.Domain.Extensions;

public static class UserExtensions
{
    public static IEnumerable<User> WhereEmailConfirmed(this IEnumerable<User> users)
        => users.Where(u => u.EmailConfirmed);
}
```

[VERIFIED: follows D-06/D-07; namespace matches existing ServiceExtensions.cs pattern]

### Pattern 6: Applying the Guard in Jobs

`SessionReminderJob` iterates `targetSignups` (type `IEnumerable<PlayerSignup>`), where each `PlayerSignup.Player` is a `User`. The guard filters on `User.EmailConfirmed`:

```csharp
// SessionReminderJob.cs — AFTER computing targetSignups, BEFORE the foreach:
var confirmedSignups = targetSignups.Where(ps => ps.Player != null && ps.Player.EmailConfirmed);
foreach (var signup in confirmedSignups)
{ ... }
```

Alternatively, add `WhereEmailConfirmed` to `PlayerSignup` — but D-06 says the extension is on `IEnumerable<User>`. The simplest conformant approach: filter with a lambda inline, or add a `WhereEmailConfirmedPlayers()` extension on `IEnumerable<PlayerSignup>` that delegates to the User's flag.

`QuestFinalizedEmailJob` and `QuestDateChangedEmailJob` receive `string[] recipientEmails` / `string[] playerNames` — they do NOT have `User` objects. The filter must happen at the call site in `QuestController` / `QuestService` before constructing those arrays. [VERIFIED: codebase — QuestFinalizedEmailJob.cs lines 20-23; QuestDateChangedEmailJob.cs lines 15-21]

`DailyReminderJob` enqueues `SessionReminderJob` per quest — no direct recipient iteration. The guard in `SessionReminderJob` is sufficient.

### Pattern 7: AccountController.ConfirmEmail GET Action

```csharp
[HttpGet]
public async Task<IActionResult> ConfirmEmail(int userId, string token)
{
    if (string.IsNullOrEmpty(token))
    {
        TempData["Error"] = "Email confirmation failed. The link may be expired or invalid.";
        return RedirectToAction(nameof(Login));
    }

    var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
    var result = await identityService.ConfirmEmailAsync(userId, decodedToken);

    if (result.Succeeded)
        TempData["Success"] = "Email confirmed — you can now log in.";
    else
        TempData["Error"] = "Email confirmation failed. The link may be expired or invalid. Contact an administrator.";

    return RedirectToAction(nameof(Login));
}
```

**AccountController needs `IIdentityService`** — currently it only takes `IUserService`. [VERIFIED: codebase — AccountController.cs line 9]

### Anti-Patterns to Avoid

- **Injecting `UserManager<UserEntity>` directly into `AdminController` or `AccountController`:** Violates layer boundary — UserManager belongs in the Repository layer. Use `IIdentityService` instead.
- **Not URL-encoding the token:** Raw Identity tokens contain `+`, `/`, `=` that break query-string parsing. Always use `WebEncoders.Base64UrlEncode`.
- **Applying the `WhereEmailConfirmed` guard only in some jobs:** All four jobs must guard. Missing `QuestFinalizedEmailJob` / `QuestDateChangedEmailJob` is the likely miss because their guard must be applied at the call site, not inside the job.
- **Using `TempData["SuccessMessage"]` in Users.cshtml:** The existing `AdminController.ResetPassword` uses `TempData["SuccessMessage"]` (inconsistency), but the UI spec (24-UI-SPEC.md) specifies `TempData["Success"]` / `TempData["Error"]` for Phase 24 to match the view's banner block keys.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Email confirmation token | Custom GUID or HMAC token | `UserManager.GenerateEmailConfirmationTokenAsync` | Identity's token includes timestamp, purpose, security stamp — cryptographically secure and self-expiring |
| Token URL encoding | Custom base64 variant | `WebEncoders.Base64UrlEncode` | Handles `+` / `/` / `=` substitution correctly; RFC 4648 compliant |
| Marking email confirmed | Direct EF `SaveChangesAsync` on `EmailConfirmed = true` | `UserManager.ConfirmEmailAsync` | Identity updates security stamp, validates token purpose, handles concurrency |

---

## Common Pitfalls

### Pitfall 1: Token Decoded Twice (or Not Decoded)

**What goes wrong:** `ConfirmEmailAsync` returns failure for every token.
**Why it happens:** Token is Base64Url-encoded before embedding in the URL. If `ConfirmEmail` action calls `identityService.ConfirmEmailAsync(userId, token)` with the raw query-string value (still encoded), Identity's token validation fails.
**How to avoid:** `ConfirmEmail` action must call `WebEncoders.Base64UrlDecode` then `Encoding.UTF8.GetString` before passing token to `ConfirmEmailAsync`.
**Warning signs:** `result.Succeeded == false` on every confirmation attempt, even for freshly generated tokens.

### Pitfall 2: Relative URL in Email Body

**What goes wrong:** User receives an email where the confirmation link is `/Account/ConfirmEmail?...` (relative path) instead of `https://questboard.example.com/Account/ConfirmEmail?...`. Clicking it opens their email client's domain, not the quest board.
**Why it happens:** `Url.Action("ConfirmEmail", "Account", values)` without the `protocol` parameter returns a relative URL.
**How to avoid:** Always pass `protocol: Request.Scheme` (and optionally `host: Request.Host.Value`) to `Url.Action` when generating links for email.
**Warning signs:** Email link navigates to wrong domain or shows 404.

### Pitfall 3: QuestFinalizedEmailJob Guard Applied Inside Job Instead of at Call Site

**What goes wrong:** The job receives `string[] recipientEmails` already pre-built from player emails. Filtering `IEnumerable<User>` inside the job would require passing User objects to the job — which is not the current signature.
**Why it happens:** D-06 says "jobs call `WhereEmailConfirmed()`" but `QuestFinalizedEmailJob` / `QuestDateChangedEmailJob` don't receive `User` objects.
**How to avoid:** Apply the `EmailConfirmed` filter in `QuestController` (or wherever `QuestFinalizedEmailJob.ExecuteAsync` is called) before constructing `recipientEmails[]` and `playerNames[]`. Alternatively, accept that `SessionReminderJob` / `DailyReminderJob` use the User-level extension while the other two jobs use a call-site filter.
**Warning signs:** Unconfirmed users still receive quest finalization emails after the phase is complete.

### Pitfall 4: Missing TempData Banners in Users.cshtml and Login.cshtml

**What goes wrong:** Emails are sent and confirmation succeeds, but no feedback appears to the user.
**Why it happens:** `Users.cshtml` currently has NO TempData banner block. `Login.cshtml` also has none. Both must have `TempData["Success"]` / `TempData["Error"]` blocks added.
**How to avoid:** Add the banner block from `Quest/Manage.cshtml` (lines 32-55) as the pattern. Check both views explicitly.
**Warning signs:** `TempData["Success"]` is set in controller but page shows nothing after redirect.

### Pitfall 5: Equals/GetHashCode Not Updated on User

**What goes wrong:** Collections of `User` objects deduplicate incorrectly after `EmailConfirmed` is added.
**Why it happens:** `User.cs` has a hand-coded `Equals`/`GetHashCode` (lines 24-35). Adding `EmailConfirmed` without updating these means two users with the same Id/Name/Email/HasKey but different `EmailConfirmed` values are considered equal.
**How to avoid:** Add `EmailConfirmed` to the `Equals` check and `HashCode.Combine` call when adding the property.
**Warning signs:** Unit tests that create duplicate users with different confirmation states behave unexpectedly.

---

## Code Examples

### EntityProfile — How EmailConfirmed Maps by Convention

```csharp
// Source: EuphoriaInn.Repository/Automapper/EntityProfile.cs line 39
// EXISTING — no change needed; EmailConfirmed on UserEntity maps automatically to User.EmailConfirmed
CreateMap<UserEntity, User>();
```

### ViewModelProfile — Existing AutoMapper Pattern for AdminViewModels

```csharp
// Source: CONVENTIONS.md — AutoMapper usage pattern
// Add to ViewModelProfile: map User.EmailConfirmed to UserManagementViewModel.EmailConfirmed
// (By convention, if property names match, AutoMapper does it automatically)
// Only explicit mapping needed if property names differ or projection required
```

### Users.cshtml — TempData Banner Block (from Quest/Manage.cshtml pattern)

```html
<!-- Source: EuphoriaInn.Service/Views/Quest/Manage.cshtml lines 32-55 -->
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="fas fa-exclamation-triangle me-2"></i>
        @TempData["Error"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show mb-2" role="alert">
        <i class="fas fa-check-circle me-2"></i>
        @TempData["Success"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```

### Users.cshtml — Send Confirmation Email Button (from UI-SPEC)

```html
<!-- Source: 24-UI-SPEC.md Component 1 — position: after Promote/Demote, before Edit -->
@if (!userModel.EmailConfirmed)
{
    <form asp-action="SendConfirmationEmail" method="post" class="d-inline me-2">
        <input type="hidden" name="userId" value="@userModel.User.Id" />
        <button type="submit" class="btn btn-sm btn-info">
            <i class="fas fa-envelope me-1"></i>
            Send Confirmation Email
        </button>
    </form>
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Dedicated ConfirmEmail view | Redirect to Login with TempData | D-03 (CONTEXT.md) | No new view files needed |
| `TempData["SuccessMessage"]` in AdminController | `TempData["Success"]` / `TempData["Error"]` for Phase 24 | 24-UI-SPEC.md note | Phase 24 uses different key than existing `ResetPassword` — intentional per UI spec |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Url.Action` with `protocol: Request.Scheme` parameter produces an absolute URL suitable for email | Code Examples — Pattern 3 | Low risk — this is the standard ASP.NET Core pattern; if wrong, easy to fix during implementation |
| A2 | `ViewModelProfile` will pick up `EmailConfirmed` by AutoMapper convention without explicit `ForMember` | Standard Stack / Pattern 1 | Low risk — EntityProfile already maps `HasKey` by convention; same will apply to ViewModel profile |

---

## Open Questions (RESOLVED)

1. **Where exactly is `QuestFinalizedEmailJob` enqueued?**
   - What we know: Job signature takes `string[] recipientEmails` not `User[]`; guard cannot be inside the job.
   - What's unclear: The specific call site in `QuestController` or `QuestService` where recipients are built.
   - Recommendation: Planner should check `QuestController.Finalize` action to locate recipient-array construction; apply `.Where(u => u.EmailConfirmed)` before building the `recipientEmails` array.
   - **RESOLVED**: Guard applied in `QuestService.FinalizeQuestAsync` (not QuestController) at the call site where `selectedSignups` are converted to recipient arrays. `QuestDateChangedEmailJob` guarded similarly in `QuestService.UpdateQuestPropertiesWithNotificationsAsync`. See Plan 24-05 Task 1.

2. **Does the existing `UserService.GetAllAsync()` path load `EmailConfirmed` from the database?**
   - What we know: `UserEntity.EmailConfirmed` is stored in `AspNetUsers`; `EntityProfile` maps by convention; `UserService` uses `BaseService<User, UserEntity>` which calls `mapper.Map`.
   - What's unclear: Whether `BaseService.GetAllAsync()` includes `EmailConfirmed` in its SELECT without EF explicit configuration.
   - Recommendation: Since `EmailConfirmed` is a column on `AspNetUsers` and EF loads all columns by default, this should work. Verify with a quick test after implementation.
   - **RESOLVED**: EF Core loads all columns by default (no explicit `.Select()` projection in BaseService). Convention mapping (`EntityProfile`) maps `UserEntity.EmailConfirmed` → `User.EmailConfirmed` automatically. No additional EF configuration required. Confirmed by `dotnet build` success in Plan 24-01.

---

## Environment Availability

Step 2.6: SKIPPED — Phase 24 is a code-only change. No external tools, services, CLIs, or runtimes beyond the existing project stack are required. All dependencies (ASP.NET Core Identity, EF Core, Hangfire) are already installed and running.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xunit.v3 3.2.2 + NSubstitute 5.3.0 + FluentAssertions 8.10.0 |
| Config file | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~UserExtensions\|FullyQualifiedName~EmailConfirmation" -x` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| REQ-24-01 | `UserManagementViewModel.EmailConfirmed` set from `User.EmailConfirmed` | unit | `dotnet test --filter "FullyQualifiedName~UserManagementViewModel" -x` | No — Wave 0 |
| REQ-24-04 | `WhereEmailConfirmed()` returns only confirmed users | unit | `dotnet test --filter "FullyQualifiedName~UserExtensionsTests" -x` | No — Wave 0 |
| REQ-24-04 | `SessionReminderJob` skips unconfirmed recipients | unit | `dotnet test --filter "FullyQualifiedName~SessionReminderJobTests" -x` | Yes — extend existing |
| REQ-24-04 | `QuestFinalizedEmailJob` guard at call site | unit | `dotnet test --filter "FullyQualifiedName~EmailConfirmationJobGuard" -x` | No — Wave 0 |
| REQ-24-02 | `AdminController.SendConfirmationEmail` calls identity service + email service | unit | (integration test or manual verification) | No |
| REQ-24-03 | `AccountController.ConfirmEmail` calls `ConfirmEmailAsync` and redirects | unit | (integration test or manual verification) | No |

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests -x`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- `EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs` — covers REQ-24-04 (`WhereEmailConfirmed` logic)
- `EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs` — covers job guard for `QuestFinalizedEmailJob` / `QuestDateChangedEmailJob`

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Identity `ConfirmEmailAsync` — token-based, purpose-bound |
| V3 Session Management | no | No session changes |
| V4 Access Control | yes | `[Authorize(Policy = "AdminOnly")]` on `SendConfirmationEmail` action |
| V5 Input Validation | yes | `userId` validated via `userService.GetByIdAsync`; null-checked before use |
| V6 Cryptography | yes | `UserManager.GenerateEmailConfirmationTokenAsync` — do not hand-roll |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unvalidated userId in confirmation callback | Tampering | `identityService.ConfirmEmailAsync` validates token matches the userId; no additional check needed |
| Token replay | Repudiation | ASP.NET Identity tokens are single-use and include security stamp — calling `ConfirmEmailAsync` invalidates the token |
| Open redirect after confirmation | Elevation of privilege | Always use `RedirectToAction(nameof(Login))` — never redirect to a URL parameter from the request |
| Confirmation email sent to unverified email | Spoofing | Admin-only action (`AdminOnly` policy) — admin has already verified the user exists |

---

## Sources

### Primary (HIGH confidence)

- `EuphoriaInn.Repository/IdentityService.cs` — `AdminResetPasswordAsync` as canonical analog for new identity methods [VERIFIED: codebase]
- `EuphoriaInn.Domain/Interfaces/IIdentityService.cs` — interface contract to extend [VERIFIED: codebase]
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` — `CreateMap<UserEntity, User>()` convention mapping [VERIFIED: codebase]
- `EuphoriaInn.Domain/Models/User.cs` — current model, missing `EmailConfirmed` [VERIFIED: codebase]
- `EuphoriaInn.Repository/Entities/UserEntity.cs` — `IdentityUser<int>` already has `EmailConfirmed` column [VERIFIED: codebase]
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — `ResetPassword` POST as analog for `SendConfirmationEmail` [VERIFIED: codebase]
- `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` — location for `ConfirmEmail` GET [VERIFIED: codebase]
- `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` — recipient iteration pattern for guard application [VERIFIED: codebase]
- `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` — string[] signature requiring call-site guard [VERIFIED: codebase]
- `24-CONTEXT.md` — locked decisions D-01 through D-07 [VERIFIED]
- `24-UI-SPEC.md` — ViewModel contract, button markup, TempData key convention [VERIFIED]

### Secondary (MEDIUM confidence)

- `Quest/Manage.cshtml` lines 32-55 — TempData banner markup pattern [VERIFIED: codebase]

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already installed, verified in codebase
- Architecture: HIGH — patterns verified directly from existing IdentityService.cs, EntityProfile.cs, and job files
- Pitfalls: HIGH — derived from direct code inspection of signatures and existing inconsistencies (TempData key naming)

**Research date:** 2026-06-26
**Valid until:** 2026-07-26 (stable — ASP.NET Identity API is mature; no breaking changes expected)
