# Phase 24: Email Confirmation Flow - Pattern Map

**Mapped:** 2026-06-26
**Files analyzed:** 13 new/modified files
**Analogs found:** 13 / 13

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `EuphoriaInn.Domain/Models/User.cs` | model | transform | self (modify) | exact |
| `EuphoriaInn.Domain/Extensions/UserExtensions.cs` | utility | transform | `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | role-match |
| `EuphoriaInn.Domain/Interfaces/IIdentityService.cs` | service | request-response | self (modify) | exact |
| `EuphoriaInn.Repository/IdentityService.cs` | service | request-response | self (modify) — `AdminResetPasswordAsync` is the direct inner analog | exact |
| `EuphoriaInn.Repository/Automapper/EntityProfile.cs` | config | transform | self (modify) — `CreateMap<UserEntity, User>()` line 39 | exact |
| `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` | controller | request-response | self (modify) — `ResetPassword` POST action | exact |
| `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` | controller | request-response | self (modify) — existing `[HttpGet]` actions | exact |
| `EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs` | model | transform | self (modify) | exact |
| `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` | config | transform | self (modify) | exact |
| `EuphoriaInn.Service/Views/Admin/Users.cshtml` | component | request-response | self (modify) — inline form buttons pattern | exact |
| `EuphoriaInn.Service/Views/Account/Login.cshtml` | component | request-response | `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 32-55 | role-match |
| `EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs` | test | transform | `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` | role-match |
| `EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs` | test | request-response | `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` | role-match |

**Job guard application (not new files — inline edits):**

| Modified File | Role | Data Flow | Inner Analog |
|---------------|------|-----------|--------------|
| `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` | service | event-driven | self — `targetSignups` iteration, line 68 |
| `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` | service | event-driven | `QuestDateChangedEmailJob.cs` — string[] pattern |
| `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` | service | event-driven | self |

---

## Pattern Assignments

### `EuphoriaInn.Domain/Models/User.cs` — add `bool EmailConfirmed`

**Analog:** self (current file)

**Current property block** (lines 8-19 — where `EmailConfirmed` slots in after `HasKey`):
```csharp
public int Id { get; set; }

[Required]
[StringLength(100)]
public string Name { get; set; } = string.Empty;

[EmailAddress]
[StringLength(200)]
public string? Email { get; set; }

public bool HasKey { get; set; }
// ADD: public bool EmailConfirmed { get; set; }
```

**Equals/GetHashCode pattern** (lines 24-36 — both must include `EmailConfirmed`):
```csharp
public override bool Equals(object? obj)
{
    return obj is User user &&
           Id == user.Id &&
           Name == user.Name &&
           Email == user.Email &&
           HasKey == user.HasKey;
           // ADD: && EmailConfirmed == user.EmailConfirmed
}

public override int GetHashCode()
{
    return HashCode.Combine(Id, Name, Email, HasKey);
    // ADD EmailConfirmed: HashCode.Combine(Id, Name, Email, HasKey, EmailConfirmed)
}
```

**Key constraint:** `EmailConfirmed` must appear in both `Equals` and `GetHashCode` — omitting it causes deduplication bugs on collections where users differ only by confirmation state (RESEARCH.md Pitfall 5).

---

### `EuphoriaInn.Domain/Extensions/UserExtensions.cs` — NEW FILE

**Analog:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (same namespace, same static class pattern)

**Namespace and static class pattern** (ServiceExtensions.cs lines 1-8):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
// ...
namespace EuphoriaInn.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(...) { ... }
}
```

**New file structure — copy namespace, adapt class body:**
```csharp
namespace EuphoriaInn.Domain.Extensions;

public static class UserExtensions
{
    public static IEnumerable<User> WhereEmailConfirmed(this IEnumerable<User> users)
        => users.Where(u => u.EmailConfirmed);
}
```

Using directive `using EuphoriaInn.Domain.Models;` needed for `User`. No other imports required.

---

### `EuphoriaInn.Domain/Interfaces/IIdentityService.cs` — add two method signatures

**Analog:** self (current file, lines 12-24)

**Existing interface signature pattern** (lines 12-24):
```csharp
public interface IIdentityService
{
    Task<IdentityResult> AddToRoleAsync(int userId, string role);
    Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword);
    // ... (all methods return Task<T> with no default implementations)
    Task<IdentityResult> AdminResetPasswordAsync(ClaimsPrincipal adminUser, int targetUserId, string newPassword);
    Task SignOutAsync();
}
```

**Add these two signatures following the same convention:**
```csharp
Task<string?> GenerateEmailConfirmationAsync(int userId);
Task<IdentityResult> ConfirmEmailAsync(int userId, string token);
```

Existing imports (`using Microsoft.AspNetCore.Identity; using System.Security.Claims;`) cover `IdentityResult` — no new usings needed.

---

### `EuphoriaInn.Repository/IdentityService.cs` — add two method implementations

**Analog:** `AdminResetPasswordAsync` (lines 97-109) — find by userId, call UserManager operation, return IdentityResult

**Core pattern to copy** (lines 97-109):
```csharp
public async Task<IdentityResult> AdminResetPasswordAsync(ClaimsPrincipal adminUser, int targetUserId, string newPassword)
{
    var adminEntity = await userManager.GetUserAsync(adminUser);
    if (adminEntity == null || !await userManager.IsInRoleAsync(adminEntity, "Admin"))
        return IdentityResult.Failed(new IdentityError { Description = "Admin user not found or not authorized." });

    var entity = await userManager.FindByIdAsync(targetUserId.ToString());
    if (entity == null)
        return IdentityResult.Failed(new IdentityError { Description = "User not found." });

    var resetToken = await userManager.GeneratePasswordResetTokenAsync(entity);
    return await userManager.ResetPasswordAsync(entity, resetToken, newPassword);
}
```

**New implementations — directly mirror this pattern:**
```csharp
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

**Imports pattern** (lines 1-5 — already present, no changes needed):
```csharp
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Repository.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
```

---

### `EuphoriaInn.Repository/Automapper/EntityProfile.cs` — no code change needed

**Analog:** self, line 39

**Existing convention mapping** (line 39):
```csharp
CreateMap<UserEntity, User>();
```

AutoMapper convention picks up all matching public property names automatically. Since `UserEntity` inherits from `IdentityUser<int>` which already has `bool EmailConfirmed`, and `User` will have `bool EmailConfirmed` added, this mapping requires **zero changes**. The `HasKey` bool already maps by convention through this same line — `EmailConfirmed` follows identically.

---

### `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — add `SendConfirmationEmail` POST action

**Analog:** `ResetPassword` POST action (lines 171-197) — the closest match for "find user, perform identity operation, set TempData, redirect to Users"

**Constructor injection pattern** (line 9 — must be extended):
```csharp
// CURRENT:
public class AdminController(IUserService userService, IQuestService questService) : Controller

// ADD IIdentityService and IEmailService:
public class AdminController(
    IUserService userService,
    IQuestService questService,
    IIdentityService identityService,
    IEmailService emailService) : Controller
```

**TempData + redirect pattern** (lines 184-197 from `ResetPassword`):
```csharp
if (result.Succeeded)
{
    TempData["SuccessMessage"] = $"Password reset successfully for {user.Name}!";
    return RedirectToAction(nameof(Users));
}
foreach (var error in result.Errors)
{
    ModelState.AddModelError(string.Empty, error.Description);
}
```

**IMPORTANT KEY DIFFERENCE:** Phase 24 uses `TempData["Success"]` / `TempData["Error"]` (not `TempData["SuccessMessage"]` as used by `ResetPassword`). This matches the banner keys in `Users.cshtml` after modification — see RESEARCH.md anti-pattern note.

**New action to add — core pattern:**
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

**New usings needed at top of file:**
```csharp
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
```

**Auth attribute:** The existing `[Authorize(Policy = "AdminOnly")]` class-level attribute (line 8) already covers this new action — no per-action attribute needed.

---

### `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` — add `ConfirmEmail` GET action

**Analog:** `Login` GET action (lines 11-15) — simple `[HttpGet]` with redirect

**Constructor pattern** (line 9 — must be extended):
```csharp
// CURRENT:
public class AccountController(IUserService userService) : Controller

// ADD IIdentityService:
public class AccountController(IUserService userService, IIdentityService identityService) : Controller
```

**GET action pattern** (Login GET, lines 11-15):
```csharp
[HttpGet]
public IActionResult Login(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View();
}
```

**New action — follows GET + TempData + redirect to Login:**
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

**New usings needed:**
```csharp
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
```

No `[Authorize]` on `ConfirmEmail` — it must be accessible without being logged in.

---

### `EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs` — add `bool EmailConfirmed`

**Analog:** self (lines 1-12)

**Current class** (lines 1-12):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class UserManagementViewModel
{
    public User User { get; set; } = new();
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin { get; set; }
    public bool IsDungeonMaster { get; set; }
    public bool IsPlayer { get; set; }
}
```

**Add one property after `IsPlayer`:**
```csharp
public bool EmailConfirmed { get; set; }
```

Note: `EmailConfirmed` is populated in `AdminController.Users` GET action (not through AutoMapper) — the action currently builds `UserManagementViewModel` manually (lines 21-27). The `EmailConfirmed` value must be assigned there from `user.EmailConfirmed`.

---

### `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` — no change needed

AutoMapper convention maps `User.EmailConfirmed` to `UserManagementViewModel.EmailConfirmed` automatically if property names match — same as EntityProfile does for `UserEntity → User`. However, because `AdminController.Users` builds the ViewModel manually (not via AutoMapper), ViewModelProfile is not involved in this mapping for the Users page. No change to this file is required for Phase 24.

---

### `EuphoriaInn.Service/Views/Admin/Users.cshtml` — add TempData banners + conditional button

**Analog:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 32-55 (TempData banner block)

**TempData banner pattern to add** (Manage.cshtml lines 32-55):
```html
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

**Insert location:** Inside `<div class="card-body modern-card-body">` (line 17), before the `@if (Model.Any())` block (line 18).

**Existing inline form button pattern** (Users.cshtml lines 76-93 — PromoteToAdmin form):
```html
<form asp-action="PromoteToAdmin" method="post" class="d-inline me-2">
    <input type="hidden" name="userId" value="@userModel.User.Id" />
    <button type="submit" class="btn btn-sm btn-danger">
        <i class="fas fa-arrow-up me-1"></i>
        Promote to Admin
    </button>
</form>
```

**New conditional button to add in the Actions column** — insert before the Edit button (line 121), after the Demote buttons:
```html
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

### `EuphoriaInn.Service/Views/Account/Login.cshtml` — add TempData banners

**Analog:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 32-55 (same banner block)

**Current Login.cshtml structure** (lines 1-56) has no TempData banner. The banner block must be added inside `<div class="card-body modern-card-body">` (line 17), before the `<form>` tag (line 18).

**Banner block to add** (copy from Manage.cshtml pattern above):
```html
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

---

### `EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs` — NEW FILE

**Analog:** `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` — test file structure, NSubstitute style, FluentAssertions

**Test class structure pattern** (SessionReminderJobTests.cs lines 1-52):
```csharp
using EuphoriaInn.Domain.Models;
// ... other usings
using NSubstitute;

namespace EuphoriaInn.UnitTests.Services;

public class SessionReminderJobTests
{
    // Fields and constructor to set up substitutes...

    [Fact]
    public async Task ExecuteAsync_WhenReminderAlreadySent_AndForceResendFalse_SkipsEmailSend()
    {
        // Arrange / Act / Assert
        await _emailService.DidNotReceive().SendAsync(...);
    }
}
```

**Helper factory method pattern** (lines 55-76 — `MakeQuest`, `MakeSignup`):
```csharp
private static Quest MakeQuest(int id, bool isFinalized = true, ...) =>
    new() { Id = id, ... };

private static PlayerSignup MakeSignup(Quest quest, int playerId, ...) =>
    new() { Player = new User { Id = playerId, ... }, ... };
```

**New test file — no service scope needed (pure extension method, no DI):**
```csharp
using EuphoriaInn.Domain.Extensions;
using EuphoriaInn.Domain.Models;
using FluentAssertions;

namespace EuphoriaInn.UnitTests.Services;

public class UserExtensionsTests
{
    private static User MakeUser(int id, bool emailConfirmed) =>
        new() { Id = id, Name = $"User{id}", EmailConfirmed = emailConfirmed };

    [Fact]
    public void WhereEmailConfirmed_ReturnsOnlyConfirmedUsers()
    {
        var users = new[] { MakeUser(1, true), MakeUser(2, false), MakeUser(3, true) };
        var result = users.WhereEmailConfirmed().ToList();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(u => u.EmailConfirmed.Should().BeTrue());
    }

    [Fact]
    public void WhereEmailConfirmed_WhenAllUnconfirmed_ReturnsEmpty()
    {
        var users = new[] { MakeUser(1, false), MakeUser(2, false) };
        users.WhereEmailConfirmed().Should().BeEmpty();
    }

    [Fact]
    public void WhereEmailConfirmed_WhenEmptyInput_ReturnsEmpty()
    {
        Enumerable.Empty<User>().WhereEmailConfirmed().Should().BeEmpty();
    }
}
```

---

### `EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs` — NEW FILE

**Analog:** `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` — IServiceScopeFactory setup, NSubstitute, Fact pattern

**Key difference from SessionReminderJobTests:** `QuestFinalizedEmailJob` and `QuestDateChangedEmailJob` take `string[] recipientEmails` — the guard is at the call site that builds those arrays, not inside the job. Tests should verify that the call site filters by `EmailConfirmed` before constructing the arrays.

**IServiceScopeFactory + IServiceProvider chain** (SessionReminderJobTests.cs lines 32-47):
```csharp
var serviceProvider = Substitute.For<IServiceProvider>();
serviceProvider.GetService(typeof(IQuestRepository)).Returns(_questRepository);
// ... register other services

var scope = Substitute.For<IServiceScope>();
scope.ServiceProvider.Returns(serviceProvider);

_scopeFactory = Substitute.For<IServiceScopeFactory>();
_scopeFactory.CreateAsyncScope().Returns(new AsyncServiceScope(scope));

var logger = Substitute.For<ILogger<SessionReminderJob>>();
_sut = new SessionReminderJob(_scopeFactory, logger);
```

**FluentAssertions assertion style** (lines 97-98, 116-117):
```csharp
await _emailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
await _emailService.Received(1).SendAsync("player@example.com", Arg.Any<string>(), Arg.Any<string>());
```

---

## Job Guard Application (inline edits to existing jobs)

### `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` — guard in recipient loop

**Target location:** Lines 68-108 — the `foreach (var signup in targetSignups)` loop.

**Current recipient iteration** (lines 68-69):
```csharp
foreach (var signup in targetSignups)
{
    if (string.IsNullOrEmpty(signup.Player?.Email))
```

**Add `EmailConfirmed` guard inline before the email-null check:**
```csharp
foreach (var signup in targetSignups)
{
    if (signup.Player == null || !signup.Player.EmailConfirmed)
    {
        logger.LogInformation(
            "SessionReminderJob: player {PlayerId} email not confirmed, skipping.",
            signup.Player?.Id);
        continue;
    }
    if (string.IsNullOrEmpty(signup.Player.Email))
```

Alternative (using the `WhereEmailConfirmed` extension on `Player`): apply a `.Where(ps => ps.Player?.EmailConfirmed == true)` on `targetSignups` before the loop. Either approach is acceptable; the inline guard is simpler and avoids a `PlayerSignup`-level extension.

### `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs` — guard at call site

**Current signature** (lines 14-23 — string[] parameters, no User objects):
```csharp
public async Task ExecuteAsync(
    int questId,
    DateTime finalizedDate,
    string[] recipientEmails,
    string[] playerNames,
    string questTitle,
    string dmName,
    string questDescription,
    int challengeRating,
    CancellationToken cancellationToken = default)
```

**No change to the job itself.** The guard must be applied at the call site — wherever `recipientEmails` and `playerNames` arrays are constructed before `ExecuteAsync` is called. Use `.Where(u => u.EmailConfirmed)` on the `User` enumerable before `.Select(u => u.Email)` / `.Select(u => u.Name)`.

### `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs` — guard at call site

Same constraint as `QuestFinalizedEmailJob` — identical `string[]` signature (lines 14-22). Guard at the call site before building the arrays.

---

## Shared Patterns

### TempData Banner Block
**Source:** `EuphoriaInn.Service/Views/Quest/Manage.cshtml` lines 32-55
**Apply to:** `Users.cshtml` and `Login.cshtml`
```html
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
**Key:** Use `TempData["Success"]` / `TempData["Error"]` — NOT `TempData["SuccessMessage"]` (which is the legacy key used in `AdminController.ResetPassword` and `AccountController.Edit`).

### Identity Operation Pattern (find-by-id + UserManager call)
**Source:** `EuphoriaInn.Repository/IdentityService.cs` lines 89-95 (`ResetPasswordAsync`) and lines 97-109 (`AdminResetPasswordAsync`)
**Apply to:** New `GenerateEmailConfirmationAsync` and `ConfirmEmailAsync` methods in `IdentityService.cs`
```csharp
var entity = await userManager.FindByIdAsync(userId.ToString());
if (entity == null)
    return IdentityResult.Failed(new IdentityError { Description = "User not found." });
// ... call UserManager operation on entity
```

### Admin POST Action Pattern (find-user + service call + TempData + redirect)
**Source:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — `ResetPassword` POST (lines 171-197), but note TempData key difference
**Apply to:** New `SendConfirmationEmail` POST action
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ActionName(int userId)
{
    var user = await userService.GetByIdAsync(userId);
    if (user == null)
        return RedirectToAction(nameof(Users));

    // ... perform operation ...

    TempData["Success"] = "...";   // NOT "SuccessMessage"
    return RedirectToAction(nameof(Users));
}
```

### Token URL-Safety Pattern
**Source:** RESEARCH.md Pattern 3 / CONTEXT.md specifics
**Apply to:** `AdminController.SendConfirmationEmail` (encode) and `AccountController.ConfirmEmail` (decode)
```csharp
// Encoding side (AdminController):
var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
var callbackUrl = Url.Action("ConfirmEmail", "Account",
    new { userId, token = encodedToken }, Request.Scheme);  // protocol required for absolute URL

// Decoding side (AccountController):
var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
```

### Inline Form Button in Table Row
**Source:** `EuphoriaInn.Service/Views/Admin/Users.cshtml` lines 76-83 (PromoteToAdmin form)
**Apply to:** New "Send Confirmation Email" button in Users.cshtml Actions column
```html
<form asp-action="ActionName" method="post" class="d-inline me-2">
    <input type="hidden" name="userId" value="@userModel.User.Id" />
    <button type="submit" class="btn btn-sm btn-COLOR">
        <i class="fas fa-ICON me-1"></i>
        Label
    </button>
</form>
```

---

## No Analog Found

All files in Phase 24 have close analogs. No entries.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Domain`, `EuphoriaInn.Repository`, `EuphoriaInn.Service`, `EuphoriaInn.UnitTests`
**Files scanned:** 13 target files + 8 analog files read
**Key finding:** `EntityProfile.cs` line 39 `CreateMap<UserEntity, User>()` requires **zero changes** — convention mapping picks up `EmailConfirmed` automatically. `ViewModelProfile.cs` also requires zero changes because `AdminController.Users` builds `UserManagementViewModel` manually.
**Pattern extraction date:** 2026-06-26
