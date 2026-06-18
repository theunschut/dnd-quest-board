# Phase 2: Email & Service Consolidation - Research

**Researched:** 2026-04-17
**Domain:** ASP.NET Core 8 — Typed Options, Service Layer Email Dispatch, Controller Slimming
**Confidence:** HIGH

## Summary

Phase 2 moves email dispatch out of `QuestController` and into `QuestService`, moves remaining-quantity calculation out of `ShopController.Index` into `ShopService`, and refactors `EmailService` to use the typed `IOptions<EmailSettings>` pattern instead of raw `IConfiguration`. All changes are confined to existing files with one new type (`ServiceResult<T>`) and one new record (`EmailSettings`) to introduce.

The codebase is already well-structured for this work. `QuestService` and `EmailService` both live in `EuphoriaInn.Domain`, so `QuestService` can inject `IEmailService` without any layer-boundary changes. The Domain's `ServiceExtensions.cs` uses primary constructor injection throughout — the same pattern applies when adding `IEmailService emailService` to `QuestService`. The `IOptions<T>` typed-options pattern is standard ASP.NET Core 8 but has no prior use in this codebase — this phase introduces the first instance.

The stale-state bug (EMAIL-04) occurs because `QuestController.Finalize` builds the email recipient list from the pre-finalize `quest` object fetched before `FinalizeQuestAsync` runs. The fix (D-05) is to re-fetch post-save. The placeholder bug (EMAIL-03) is a literal string `[Quest Board URL]` in `SendQuestDateChangedEmailAsync` — replaced by an `AppUrl` field on `EmailSettings`.

**Primary recommendation:** Implement in dependency order — `EmailSettings` record + `IOptions` refactor first (no signature changes), then `ServiceResult<T>` type, then `QuestService` gains `IEmailService` + email dispatch, then controller slimming, then `ShopService` remaining-quantity extraction.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01:** Introduce a generic `ServiceResult<T>` type in `EuphoriaInn.Domain`. Signature: `(bool Success, T? Data, string? Error)`. This is the standard going forward — not a one-off type.

**D-02:** `UpdateQuestPropertiesWithNotificationsAsync` changes its return type from `IList<User>` to `ServiceResult<T>` (exact T parameter at planner's discretion) so the controller receives only the operation outcome, not a user list. Email dispatch moves inside the service.

**D-03:** Add an `AppUrl` field to the existing `EmailSettings` config section. The `EmailSettings` typed options record gains a `string AppUrl` property. `EmailService.SendQuestDateChangedEmailAsync` reads `AppUrl` from injected options and substitutes it for `[Quest Board URL]`.

**D-04:** Add `"AppUrl": ""` (empty default) to `appsettings.json` `EmailSettings` section. Docker deployments override via `EmailSettings__AppUrl`.

**D-05:** Keep `FinalizeQuestAsync` returning void (no interface signature change). After `FinalizeQuestAsync` completes, re-fetch the quest from the repository to get post-save state before building the email recipient list. One additional DB round-trip is acceptable.

**D-06:** Create an `EmailSettings` record (or class) in `EuphoriaInn.Domain` with all SMTP properties: `SmtpServer`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `FromEmail`, `FromName`, `AppUrl`.

**D-07:** Register via `AddOptions<EmailSettings>().BindConfiguration("EmailSettings")` in `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` (or Domain's ServiceExtensions — planner decides based on where `EmailService` is registered). Note: `EmailService` is registered in `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — that is the correct home for the options registration.

**D-08:** `EmailService` injects `IOptions<EmailSettings>` instead of `IConfiguration`. The duplicated SMTP setup block is extracted to a private helper method used by both send methods.

**D-09:** Claude's discretion on the `QuestService` constructor signature adding `IEmailService`.

**D-10:** Move the remaining-quantity calculation from `ShopController.Index` into `ShopService`. The controller only maps and renders.

### Claude's Discretion

- Constructor signature for `QuestService` adding `IEmailService` — planner decides.
- Whether `ServiceResult<T>` lives in its own file or is co-located with a related model.
- Exact `ServiceResult<T>` type parameter for the date-change update (could be `int` for affected player count, or `Unit`/`bool` if the count is not needed by the controller).
- Method name and return type details for the ShopService remaining-quantity extraction.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CTRL-01 | Quest finalization (email dispatch included) is fully handled inside `QuestService.FinalizeQuestAsync`; controller action is ≤ 20 lines | Move email loop from QuestController lines 642-656 into QuestService; re-fetch post-save per D-05 |
| CTRL-02 | `QuestController` does not inject `IEmailService` directly | Remove `IEmailService emailService` from QuestController primary constructor (line 15) |
| CTRL-03 | Date-change email dispatch is handled inside `QuestService.UpdateQuestPropertiesWithNotificationsAsync`; controller receives `ServiceResult` not a user list | Change return type on interface + service; move email loop from controller lines 179-193 into service |
| CTRL-04 | Shop remaining-quantity calculation is handled inside `ShopService`; `ShopController.Index` only maps and renders | Move ShopController lines 36-45 into a new `ShopService` method; controller calls that method |
| EMAIL-01 | `EmailSettings` typed options record exists and is registered with `AddOptions<EmailSettings>().BindConfiguration()` in `ServiceExtensions` | New record in Domain; `AddOptions` call in Domain ServiceExtensions |
| EMAIL-02 | `EmailService` injects `IOptions<EmailSettings>` instead of `IConfiguration`; SMTP setup is not duplicated across methods | Replace constructor param; extract private `CreateSmtpClient()` helper |
| EMAIL-03 | The `[Quest Board URL]` placeholder in the date-changed email body is replaced with the real application URL | `AppUrl` property on `EmailSettings`; string interpolation replaces literal placeholder |
| EMAIL-04 | Email finalize dispatch builds its recipient list from post-save entity state (not pre-finalize fetched `quest` object) | Re-fetch via `GetQuestWithDetailsAsync` after `FinalizeQuestAsync` call |
</phase_requirements>

---

## Standard Stack

### Core (all already present in project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.Extensions.Options` | 8.x (in-box with ASP.NET Core 8) | `IOptions<T>` typed configuration | Zero-allocation, strongly-typed config; standard ASP.NET Core 8 pattern |
| `Microsoft.Extensions.DependencyInjection` | 8.x (in-box) | `AddOptions<T>().BindConfiguration()` extension | Built-in DI; no additional packages needed |
| `System.Net.Mail.SmtpClient` | .NET 8 BCL | SMTP delivery (kept, not replaced this phase) | PERF-02 deferred; SmtpClient warning suppressed per REQUIREMENTS.md |

### No New Packages Required
All required APIs (`IOptions<T>`, `AddOptions<T>()`, `BindConfiguration()`) ship with ASP.NET Core 8. No NuGet additions are needed for this phase.

**Installation:** None required.

---

## Architecture Patterns

### Pattern 1: Typed Options with `IOptions<T>`

**What:** Replace raw `IConfiguration` injection with a strongly-typed POCO/record bound to a config section.

**When to use:** Whenever a service reads a fixed set of configuration keys from one section. Avoids magic strings, enables compile-time safety, and is the standard ASP.NET Core pattern.

**Registration (in ServiceExtensions.cs):**
```csharp
// EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings");

    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IEmailService, EmailService>();
    // ... rest unchanged
    return services;
}
```

**Record definition (new file in Domain):**
```csharp
// EuphoriaInn.Domain/Models/EmailSettings.cs  (or Configuration/EmailSettings.cs)
namespace EuphoriaInn.Domain.Models;

public record EmailSettings
{
    public string SmtpServer { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "D&D Quest Board";
    public string AppUrl { get; init; } = string.Empty;
}
```

**Usage in EmailService:**
```csharp
// Source: ASP.NET Core 8 official docs — Options pattern
public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    private SmtpClient? CreateSmtpClient()
    {
        if (string.IsNullOrEmpty(_settings.SmtpUsername) ||
            string.IsNullOrEmpty(_settings.SmtpPassword) ||
            string.IsNullOrEmpty(_settings.FromEmail))
        {
            logger.LogWarning("Email settings not configured. Skipping email notification.");
            return null;
        }

        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
        return client;
    }
}
```

### Pattern 2: `ServiceResult<T>` Generic Result Type

**What:** A generic discriminated union-lite type returned by service operations that need to communicate success/failure and an optional payload to the caller.

**Placement:** New file `EuphoriaInn.Domain/Models/ServiceResult.cs`.

**Definition:**
```csharp
namespace EuphoriaInn.Domain.Models;

public record ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static ServiceResult<T> Ok(T? data = default) =>
        new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}
```

**Usage in IQuestService interface:**
```csharp
// Return type changes from Task<IList<User>> to Task<ServiceResult<int>>
// The int represents affected player count (or use bool if count is not needed by controller)
Task<ServiceResult<int>> UpdateQuestPropertiesWithNotificationsAsync(
    int questId, string title, string description, int challengeRating,
    int totalPlayerCount, bool dungeonMasterSession,
    bool updateProposedDates = false, IList<DateTime>? proposedDates = null,
    CancellationToken token = default);
```

**Controller usage after change:**
```csharp
// QuestController.Edit — the controller no longer deals with user lists or email
var result = await questService.UpdateQuestPropertiesWithNotificationsAsync(
    id, viewModel.Quest.Title, viewModel.Quest.Description,
    viewModel.Quest.ChallengeRating, viewModel.Quest.TotalPlayerCount,
    viewModel.Quest.DungeonMasterSession, true,
    viewModel.Quest.ProposedDates, token);

return RedirectToAction("Manage", new { id });
```

### Pattern 3: Email Dispatch Inside QuestService

**What:** `QuestService` injects `IEmailService` via primary constructor and calls it after database operations complete.

**Primary constructor addition:**
```csharp
internal class QuestService(
    IQuestRepository repository,
    IPlayerSignupRepository playerSignupRepository,
    IEmailService emailService,
    IMapper mapper) : BaseService<Quest>(repository, mapper), IQuestService
```

**FinalizeQuestAsync with post-save re-fetch (EMAIL-04):**
```csharp
public async Task FinalizeQuestAsync(int questId, DateTime finalizedDate,
    IList<int> selectedPlayerSignupIds, CancellationToken token = default)
{
    await repository.FinalizeQuestAsync(questId, finalizedDate, selectedPlayerSignupIds, token);

    // Re-fetch from DB to get post-save state (EMAIL-04 fix)
    var quest = await repository.GetQuestWithDetailsAsync(questId, token);
    if (quest == null) return;

    var selectedSignups = quest.PlayerSignups
        .Where(ps => (selectedPlayerSignupIds.Contains(ps.Id) || ps.Role == SignupRole.Spectator)
                     && !string.IsNullOrEmpty(ps.Player.Email));

    foreach (var signup in selectedSignups)
    {
        await emailService.SendQuestFinalizedEmailAsync(
            signup.Player.Email!,
            signup.Player.Name,
            quest.Title,
            quest.DungeonMaster?.Name ?? "Unknown DM",
            finalizedDate);
    }
}
```

### Pattern 4: ShopService Remaining-Quantity Extraction (CTRL-04)

**What:** The nested loop in `ShopController.Index` (lines 36-45) that computes `RemainingQuantity` per transaction belongs in the service layer.

**Recommended approach:** Add a new method `GetUserTransactionsWithRemainingAsync` to `IShopService` and `ShopService` that returns `IList<UserTransaction>` already enriched with a computed remaining value, OR return a new DTO. However, `UserTransaction` is a domain model — adding a computed property directly is cleaner.

**Simplest correct approach:** Add a method to `IShopService`:
```csharp
Task<IList<UserTransaction>> GetUserTransactionsWithRemainingAsync(int userId, CancellationToken token = default);
```

`ShopService` implements it internally — it already has `transactionRepository` and knows how to compute the remaining quantity (same logic exists in `ReturnOrSellItemAsync`). The existing `UserTransactionViewModel.RemainingQuantity` property stays; AutoMapper maps it. The controller calls the enriched method and no longer does any calculation.

**Alternative (planner chooses):** Return `IList<(UserTransaction Transaction, int Remaining)>` tuples, or set `RemainingQuantity` as a property on `UserTransaction` domain model. A separate enriched method is the cleanest — no model pollution.

### Anti-Patterns to Avoid

- **Injecting `IEmailService` into any controller:** After this phase, all email calls go through domain services only.
- **Reading `IConfiguration` directly in services:** All config access goes through `IOptions<T>` after this phase.
- **Building email recipient list from pre-finalize state:** Always re-fetch after the DB write (EMAIL-04 root cause).
- **Registering `AddOptions<EmailSettings>()` in Repository ServiceExtensions:** `EmailService` is a Domain service — the registration must live in Domain's `ServiceExtensions.cs` to keep layer ownership correct.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Typed config binding | Manual `configuration.GetSection("X")["key"]` calls | `AddOptions<T>().BindConfiguration()` + `IOptions<T>` | Null-safe, refactoring-safe, testable; one source of truth |
| Result type | Custom exception hierarchy or out-params | `ServiceResult<T>` record (D-01) | Already decided; consistent going forward |

**Key insight:** The Options pattern handles null coalescing, validation hooks (`ValidateDataAnnotations()`), and reloading transparently. The manual `configuration.GetSection("EmailSettings")["SmtpPort"] ?? "587"` pattern in the current `EmailService` is error-prone and test-hostile.

---

## Common Pitfalls

### Pitfall 1: Wrong ServiceExtensions for AddOptions Registration
**What goes wrong:** `AddOptions<EmailSettings>()` is registered in Repository's `ServiceExtensions.cs` rather than Domain's. This creates an implicit dependency on Repository knowing about Domain configuration types, inverts ownership.
**Why it happens:** `AddRepositoryServices` is called from `Program.cs` and developers gravitate to it because it already handles DB registration.
**How to avoid:** `EmailService` is registered in `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs`. The options for a Domain service belong in the same extension method (`AddDomainServices`).
**Warning signs:** Repository project getting a reference to `EmailSettings` from Domain (reversed).

### Pitfall 2: Pre-Finalize Stale State (EMAIL-04)
**What goes wrong:** The finalize email is sent to signups fetched before `FinalizeQuestAsync` runs. `IsSelected` flags are not yet set on the in-memory `quest` object, so the recipient list is built from wrong state.
**Why it happens:** The `quest` object fetched at the top of `QuestController.Finalize` is the same reference used throughout. After `FinalizeQuestAsync` writes to DB, the in-memory object is stale.
**How to avoid:** After `await repository.FinalizeQuestAsync(...)`, immediately call `await repository.GetQuestWithDetailsAsync(questId, token)` and build the recipient list from the freshly-loaded entity.
**Warning signs:** All signups receiving email, or no signups receiving email, regardless of `IsSelected` state.

### Pitfall 3: `IOptions<T>` vs `IOptionsSnapshot<T>` vs `IOptionsMonitor<T>`
**What goes wrong:** Using `IOptionsSnapshot<T>` (scoped) or `IOptionsMonitor<T>` (singleton) when `IOptions<T>` (singleton-safe, no reload) is correct for SMTP settings.
**Why it happens:** The three interfaces look similar; snapshots/monitors add unnecessary complexity for static config.
**How to avoid:** Use `IOptions<EmailSettings>` — SMTP credentials do not change at runtime; no reload is needed.

### Pitfall 4: Disposing SmtpClient in a Helper That Returns It
**What goes wrong:** If `CreateSmtpClient()` returns an `SmtpClient` that the caller uses in a `using` block, callers must remember to dispose. If `CreateSmtpClient()` itself disposes, the caller gets an invalid client.
**How to avoid:** `CreateSmtpClient()` returns the client; each send method wraps it in `using var client = CreateSmtpClient();`. The helper only constructs — never disposes.

### Pitfall 5: Forgetting to Update `IQuestService` Interface Signature
**What goes wrong:** `QuestService` method signature changes but `IQuestService` is not updated, causing a compile error or interface mismatch that only surfaces at DI resolution.
**How to avoid:** Change `IQuestService` first (return type from `Task<IList<User>>` to `Task<ServiceResult<int>>`), then update `QuestService`, then update the controller call site. The compiler enforces the chain.

### Pitfall 6: ShopService Remaining-Quantity Duplicating Existing Logic
**What goes wrong:** The new `GetUserTransactionsWithRemainingAsync` method re-implements the remaining-quantity computation differently from `ReturnOrSellItemAsync`, creating two diverged implementations.
**How to avoid:** Extract the computation into a private static helper: `private static int CalculateRemainingQuantity(UserTransaction purchase, IList<UserTransaction> allTransactions)`. Both methods call it.

---

## Code Examples

### EmailSettings Record
```csharp
// EuphoriaInn.Domain/Models/EmailSettings.cs
namespace EuphoriaInn.Domain.Models;

public record EmailSettings
{
    public string SmtpServer { get; init; } = "smtp.gmail.com";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "D&D Quest Board";
    public string AppUrl { get; init; } = string.Empty;
}
```

### appsettings.json EmailSettings section (after change)
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "",
  "SmtpPassword": "",
  "FromEmail": "",
  "FromName": "D&D Quest Board",
  "AppUrl": ""
}
```

### AddOptions registration in Domain ServiceExtensions
```csharp
// EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings");

    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IEmailService, EmailService>();
    // ... rest unchanged
    return services;
}
```

### EmailService refactored (deduplicated SMTP setup)
```csharp
public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    private SmtpClient? CreateSmtpClient()
    {
        if (string.IsNullOrEmpty(_settings.SmtpUsername) ||
            string.IsNullOrEmpty(_settings.SmtpPassword) ||
            string.IsNullOrEmpty(_settings.FromEmail))
        {
            logger.LogWarning("Email settings not configured. Skipping email notification.");
            return null;
        }
        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
        };
        return client;
    }

    public async Task SendQuestDateChangedEmailAsync(
        string toEmail, string playerName, string questTitle, string dmName)
    {
        try
        {
            using var client = CreateSmtpClient();
            if (client == null) return;

            var appUrl = string.IsNullOrEmpty(_settings.AppUrl) ? "[Quest Board URL]" : _settings.AppUrl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = $"Quest Dates Updated: {questTitle}",
                Body = $"...You can view and update your signup at: {appUrl}...",
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send quest date changed email for quest {QuestTitle}", questTitle);
        }
    }
}
```

### ServiceResult<T> type
```csharp
// EuphoriaInn.Domain/Models/ServiceResult.cs
namespace EuphoriaInn.Domain.Models;

public record ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static ServiceResult<T> Ok(T? data = default) =>
        new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}
```

### QuestController.Finalize target state (CTRL-01 — ≤ 20 lines)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> Finalize(int id)
{
    var quest = await questService.GetQuestWithDetailsAsync(id);
    if (quest == null || quest.IsFinalized) return NotFound();

    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null) return Challenge();
    if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin")) return Forbid();

    if (!int.TryParse(Request.Form["SelectedDateId"], out var selectedDateId))
    { TempData["Error"] = "Please select a date."; return RedirectToAction("Manage", new { id }); }

    var selectedDate = quest.ProposedDates.FirstOrDefault(pd => pd.Id == selectedDateId);
    if (selectedDate == null)
    { TempData["Error"] = "Please select a date."; return RedirectToAction("Manage", new { id }); }

    var selectedPlayerIds = Request.Form["SelectedPlayerIds"]
        .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
        .Select(s => int.Parse(s!)).ToList();

    var playerRoleCount = quest.PlayerSignups
        .Where(ps => selectedPlayerIds.Contains(ps.Id) && ps.Role == SignupRole.Player).Count();
    if (playerRoleCount > quest.TotalPlayerCount)
    { TempData["Error"] = $"Cannot select more than {quest.TotalPlayerCount} players."; return RedirectToAction("Manage", new { id }); }

    await questService.FinalizeQuestAsync(id, selectedDate.Date, selectedPlayerIds);
    return RedirectToAction("Details", new { id });
}
```
> Note: The form-parsing and validation lines push near 20 — planner should verify exact count and may extract a private helper `ParseSelectedPlayerIds()` if needed to meet CTRL-01's ≤ 20 line requirement.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IConfiguration` string access | `IOptions<T>` typed binding | ASP.NET Core 2.0+ | Compile-time safety, no magic strings, DI-friendly |
| Email dispatch in controller | Email dispatch in service | This phase | Controller becomes transport-layer only |
| `IList<User>` return from update method | `ServiceResult<T>` | This phase | Standard result pattern going forward |

---

## Open Questions

1. **ServiceResult type parameter for date-change**
   - What we know: D-02 says `ServiceResult<int>` (or similar); controller only needs to know success/failure
   - What's unclear: Does the controller need the count of affected players for any UI message?
   - Recommendation: Use `ServiceResult<int>` where `Data` = affected player count; controller ignores `Data` for now but the count is available if needed later. This avoids `Unit`/`bool` which would require a separate type or lose information.

2. **ShopService method signature for remaining-quantity enrichment**
   - What we know: `GetUserTransactionsAsync` already returns raw transactions; `RemainingQuantity` is computed client-side
   - What's unclear: Whether to enrich `UserTransaction` domain model (add `RemainingQuantity` property) or introduce a new method
   - Recommendation: Add `Task<IList<UserTransaction>> GetUserTransactionsWithRemainingAsync(int userId, CancellationToken token)` to `IShopService`; keep `UserTransaction` clean. The new method computes and sets a computed property or returns a wrapper. The planner should decide whether `UserTransaction` gains a settable `RemainingQuantity` property or whether a new value type is introduced.

3. **`IQuestService.UpdateQuestPropertiesWithNotificationsAsync` — controller receives ServiceResult but has no error path**
   - What we know: The controller currently uses the result only to decide whether to send emails (if any); after this phase it doesn't use the result at all
   - What's unclear: Whether the planner should also remove `UpdateQuestPropertiesAsync` (non-notification variant) per QUAL-02 — but QUAL-02 is scoped to Phase 3
   - Recommendation: Do not remove `UpdateQuestPropertiesAsync` in this phase; QUAL-02 is Phase 3 scope.

---

## Environment Availability

Step 2.6: SKIPPED — This phase involves code/config-only changes with no new external dependencies. All required APIs are already available via the existing .NET 8 runtime.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | none (xUnit auto-discovers) |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --no-build` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CTRL-01 | Finalize action is ≤ 20 lines; email sent without controller touching IEmailService | Integration | `dotnet test EuphoriaInn.IntegrationTests --no-build --filter "Finalize"` | ✅ `QuestControllerIntegrationTests_Comprehensive.cs` |
| CTRL-02 | `QuestController` has no `IEmailService` in its constructor | Build verification | `dotnet build EuphoriaInn.Service` | — (compile enforces) |
| CTRL-03 | `UpdateQuestPropertiesWithNotificationsAsync` returns `ServiceResult<T>`; controller receives it | Build verification + integration | `dotnet test EuphoriaInn.IntegrationTests --no-build --filter "Edit"` | ✅ Existing Edit tests |
| CTRL-04 | `ShopController.Index` contains no quantity-calculation logic | Build verification + integration | `dotnet test EuphoriaInn.IntegrationTests --no-build --filter "Shop"` | ✅ `ShopControllerIntegrationTests.cs` |
| EMAIL-01 | `EmailSettings` record exists and `AddOptions` registration is present | Build verification | `dotnet build` | ❌ Wave 0 — new file |
| EMAIL-02 | `EmailService` uses `IOptions<EmailSettings>`; no duplicated SMTP block | Unit test | `dotnet test EuphoriaInn.UnitTests --no-build --filter "EmailService"` | ❌ Wave 0 — new test file |
| EMAIL-03 | `[Quest Board URL]` placeholder replaced by real URL from settings | Unit test | `dotnet test EuphoriaInn.UnitTests --no-build --filter "EmailService"` | ❌ Wave 0 — new test file |
| EMAIL-04 | Finalize email uses post-save quest state | Integration | `dotnet test EuphoriaInn.IntegrationTests --no-build --filter "Finalize"` | ✅ Existing (may need assertion update) |

### Sampling Rate
- **Per task commit:** `dotnet build` (compile verification)
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `EuphoriaInn.Domain/Models/EmailSettings.cs` — new record (EMAIL-01)
- [ ] `EuphoriaInn.Domain/Models/ServiceResult.cs` — new type (CTRL-03)
- [ ] `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` — covers EMAIL-02, EMAIL-03: verify `IOptions` injection, verify AppUrl substitution, verify no SMTP duplication
- [ ] No framework install needed — xUnit already present

---

## Project Constraints (from CLAUDE.md)

All directives from `CLAUDE.md` that the planner must verify compliance with:

| Directive | Impact on This Phase |
|-----------|---------------------|
| EF packages ONLY in Repository project | Not applicable — no EF changes this phase |
| Auto-applied migrations on startup | Not applicable — no schema changes this phase |
| Domain → Repository direction only | `EmailService` is Domain; it injects `IEmailService` (Domain interface). `QuestService` injects `IEmailService` (Domain interface). No Repository dependency introduced. COMPLIANT. |
| All new views use `modern-card` styling | Not applicable — no new views this phase |
| Buttons: filled, FontAwesome icons, semantic colors | Not applicable — no view changes |
| No user-facing functionality removed | `QuestController.Finalize` and `Edit` must produce identical outcomes after refactor; integration tests verify |
| Stay on ASP.NET Core 8 MVC + SQL Server + EF Core | COMPLIANT — no framework changes |
| Deployable via `docker-compose up` | `appsettings.json` `EmailSettings__AppUrl` override via env var is the correct Docker pattern (double-underscore) |
| `IEmailService` and implementations are internal | `EmailService` is currently `public class EmailService` — this is fine; only `QuestService` is `internal`. No requirement to make `EmailService` internal. |
| Primary constructor injection pattern throughout | `QuestService` gains `IEmailService emailService` as additional primary constructor param. `EmailService` replaces `IConfiguration configuration` with `IOptions<EmailSettings> options`. Both follow the project pattern. |

---

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection — all findings verified against actual source files at listed line numbers
- ASP.NET Core 8 IOptions pattern — standard framework feature, no external verification needed for a project already on ASP.NET Core 8

### Secondary (MEDIUM confidence)
- Line counts for Finalize action: manually counted from `QuestController.cs` lines 586-659 (74 lines → target ≤ 20 means significant reduction required; form-parsing boilerplate is the main challenge)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages already present; `IOptions<T>` is built-in ASP.NET Core 8
- Architecture: HIGH — verified against actual source files; all interfaces and classes read directly
- Pitfalls: HIGH — EMAIL-04 root cause verified from QuestController.cs lines 640-656 (stale `quest` object confirmed); EMAIL-03 placeholder confirmed at EmailService.cs line 103

**Research date:** 2026-04-17
**Valid until:** 2026-05-17 (stable stack, 30-day window)
