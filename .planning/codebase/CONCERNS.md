# Codebase Concerns

**Analysis Date:** 2026-04-15

---

## Security Concerns

### 1. Hardcoded Database Password in Committed Config File

- **Risk:** Development database credentials are committed to git and visible to anyone with repo access.
- **Files:** `EuphoriaInn.Service/appsettings.json` line 15
- **Detail:** The connection string `Password=QuestBoardUser!` is checked into source control. While this is a dev credential, it normalises the practice and the same file is copied into Docker images.
- **Fix approach:** Replace with a placeholder (empty string or `<see-env>`), add `appsettings.json` to `.gitignore` for sensitive overrides, and provide `appsettings.Development.json.example`. Use environment variable overrides (`ConnectionStrings__DefaultConnection`) in production.

### 2. `.env` File Committed to Git

- **Risk:** The `.env` file is tracked by git (confirmed via `git ls-files`). If a developer ever puts real SMTP or DB passwords into it, they will be permanently in history.
- **Files:** `.env` (repo root)
- **Detail:** Currently contains placeholder values, but the pattern is dangerous. The `.gitignore` exists for this project but does not exclude `.env`.
- **Fix approach:** Add `.env` to `.gitignore`. Keep only `.env.example` tracked. Document that developers copy `.env.example` to `.env` locally.

### 3. Account Lockout Disabled for Login

- **Risk:** No brute-force protection on login. An attacker can try unlimited passwords against any account.
- **Files:** `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` line 26
- **Detail:** `lockoutOnFailure: false` is explicitly passed to `PasswordSignInAsync`. ASP.NET Core Identity supports lockout out of the box but it is disabled here.
- **Fix approach:** Change to `lockoutOnFailure: true` and configure `options.Lockout` in `Program.cs` (e.g., `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`).

### 4. Minimum Password Length Is Too Short

- **Risk:** Weak passwords are accepted (minimum 6 characters).
- **Files:** `EuphoriaInn.Service/Program.cs` lines 36–41
- **Detail:** `RequiredLength = 6` and `RequireNonAlphanumeric = false`. OWASP recommends at least 8–12 characters with complexity.
- **Fix approach:** Increase `RequiredLength` to at least 8 and consider setting `RequireNonAlphanumeric = true`.

### 5. Users Can Self-Grant the `HasKey` Privilege

- **Risk:** Any authenticated user can check the "I have a building key" checkbox on their own profile edit form, granting themselves a flag that displays on public quest pages.
- **Files:** `EuphoriaInn.Service/Views/Account/Edit.cshtml` lines 38–44; `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` line 128
- **Detail:** `HasKey` is exposed as an editable checkbox in the regular user profile edit view and is directly saved to `user.HasKey` without any role check. While the flag only affects a UI indicator (who can lock up a venue), it is a privilege that should be admin-controlled.
- **Fix approach:** Remove the `HasKey` field from `EditProfileViewModel` and `Account/Edit.cshtml`. Only allow it to be set via `Admin/EditUser`.

### 6. No Rate Limiting on Any Endpoint

- **Risk:** Registration, password change, and purchase endpoints are unprotected against abuse.
- **Files:** `EuphoriaInn.Service/Program.cs` (no `AddRateLimiter` call)
- **Fix approach:** Add ASP.NET Core rate limiting middleware (available in .NET 7+). Apply a fixed-window policy to `Account/Login`, `Account/Register`, and `Shop/Purchase`.

### 7. No Error Controller/View for Production Exception Handler

- **Risk:** `app.UseExceptionHandler("/Error")` is configured for non-development environments, but there is no `ErrorController` or `Views/Shared/Error.cshtml`. Any unhandled exception in production will result in a blank or framework-default response.
- **Files:** `EuphoriaInn.Service/Program.cs` line 84
- **Fix approach:** Create an `ErrorController` with a `[Route("/Error")]` action and corresponding `Views/Shared/Error.cshtml`.

### 8. `User` Domain Model Exposes a `Password` Property

- **Risk:** The domain `User` class has a public `Password` string property. If any AutoMapper mapping or serialization inadvertently maps this field, password data could leak.
- **Files:** `EuphoriaInn.Domain/Models/User.cs` lines 20, 34, 40
- **Detail:** The property is included in `Equals()` and `GetHashCode()`. The AutoMapper profile explicitly ignores it (`opt => opt.Ignore()`), but its presence is a latent risk. It has no apparent use since Identity manages passwords.
- **Fix approach:** Remove the `Password` property from the `User` domain model. Remove it from `Equals`/`GetHashCode`. The field has no runtime value.

### 9. Profile Image Served with Hardcoded `image/jpeg` Content-Type

- **Risk:** PNG and GIF files uploaded as profile pictures are served with `Content-Type: image/jpeg`. Some browsers may refuse to render them or handle them incorrectly.
- **Files:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` line 270
- **Detail:** `return File(profilePicture, "image/jpeg")` is always used regardless of what format was uploaded. The upload validates `.jpg`, `.jpeg`, `.png`, `.gif`.
- **Fix approach:** Store the MIME type alongside the image data (add a `ContentType` column to `CharacterImages`) or detect it from magic bytes at serve time.

---

## Performance Concerns

### 10. N+1 Query: Admin User Listing Calls `GetRolesAsync` Per User

- **Risk:** The `Admin/Users` page executes one `GetRolesAsync` database call per user. With many users, this generates N+1 queries.
- **Files:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` lines 14–28
- **Detail:** `foreach (var user in allUsers)` calls `await userService.GetRolesAsync(user)`, which hits the Identity `UserRoles` join table once per user.
- **Fix approach:** Use `UserManager.Users` with a join on `UserRoles` and `Roles` at the repository layer, or batch-load all role mappings in a single query and join in memory.

### 11. `GetCompletedQuestsAsync` Loads All Quests then Filters In Memory

- **Risk:** As the quest log grows, this method loads every quest (with all related entities) from the database and then discards the majority in-process.
- **Files:** `EuphoriaInn.Domain/Services/QuestService.cs` lines 331–342; `EuphoriaInn.Repository/QuestRepository.cs` (`GetQuestsWithDetailsAsync`)
- **Detail:** `repository.GetQuestsWithDetailsAsync` returns all quests, then the service filters by `IsFinalized && FinalizedDate <= yesterday`. The filter should be pushed to the database.
- **Fix approach:** Add a `GetCompletedQuestsAsync` method on `IQuestRepository` with a `.Where(q => q.IsFinalized && q.FinalizedDate <= cutoff)` clause applied before `ToListAsync`.

### 12. Character Profile Images Stored as Binary Blobs in SQL Server

- **Risk:** Every request for a character image fetches potentially large byte arrays from SQL Server. This bypasses HTTP caching and puts unnecessary load on the database.
- **Files:** `EuphoriaInn.Repository/Entities/CharacterImageEntity.cs`; `EuphoriaInn.Repository/CharacterRepository.cs` (`GetCharacterProfilePictureAsync`)
- **Detail:** `byte[] ImageData` with no size constraint. The entity is defined as a separate table (moved there in the `MoveCharacterImagesToSeparateTable` migration) which is good for query projection, but images are still DB-stored blobs.
- **Fix approach:** Move images to filesystem or blob storage (Azure Blob, S3). Store only a URL in the database. Add `Cache-Control` headers to the image endpoint.

### 13. Quest Detail Page Loads All Quests for Calendar Context

- **Risk:** Every quest details page fetches the full `GetQuestsForCalendarAsync` result (all quests with full eager loads) to build the small mini-calendar sidebar.
- **Files:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 267–282
- **Detail:** `GetQuestsForCalendarAsync` loads every quest with player signups, proposed dates, votes, and player data. This is done on every page load of every quest.
- **Fix approach:** Only load quests for the months that have proposed dates for this specific quest, or move the calendar to a separate lazy-loaded AJAX endpoint.

### 14. Deep Eager Loading on Most Quest Queries (`ProjectWithoutCharacterImages`)

- **Risk:** The default "project without character images" query loads a 5-level-deep object graph for every quest list: Quest → ProposedDates → PlayerVotes → PlayerSignup → Player, plus PlayerSignups → DateVotes and PlayerSignups → Character → Classes.
- **Files:** `EuphoriaInn.Repository/QuestRepository.cs` lines 96–113
- **Detail:** This is used for all list views (Home, MyQuests, Edit) regardless of whether every level of data is actually needed. `AsSplitQuery` mitigates the Cartesian product, but it still issues 6+ SQL queries per call.
- **Fix approach:** Introduce projection-specific repository methods that only include the navigations needed for each view. For example, the home page index does not need `DateVotes` inside `PlayerSignups`.

---

## Technical Debt

### 15. `SecurityConfiguration` Class Is Dead Code

- **Risk:** Misleads developers into thinking a custom hashing scheme is in use, when all password handling is delegated to ASP.NET Core Identity.
- **Files:** `EuphoriaInn.Domain/Configuration/SecurityConfiguration.cs`
- **Detail:** The class defines `PasswordIterations`, `SaltSize`, `HashSize`. It is not registered, injected, or used anywhere. The `appsettings.json` has a matching `Security` section whose values go nowhere.
- **Fix approach:** Delete the class and remove the `Security` section from `appsettings.json`.

### 16. `UpdateQuestPropertiesAsync` (Non-Notification Variant) Is Dead Code

- **Risk:** Maintains an unnecessary parallel implementation that diverges silently.
- **Files:** `EuphoriaInn.Domain/Services/QuestService.cs` lines 144–163; `EuphoriaInn.Domain/Interfaces/IQuestService.cs` line 24
- **Detail:** This method exists alongside `UpdateQuestPropertiesWithNotificationsAsync`. The controllers only call the notification variant. The non-notification variant is defined on the interface but never called from any controller.
- **Fix approach:** Remove `UpdateQuestPropertiesAsync` from both the interface and the service.

### 17. `IsSameDateTime` Uses Magic Number (30-Minute Window)

- **Risk:** Implicit business rule buried in a private method. If proposed dates can be changed to within 30 minutes of an existing date, the old date is silently preserved and the new one is dropped.
- **Files:** `EuphoriaInn.Domain/Services/QuestService.cs` line 191
- **Detail:** `Math.Abs((date1 - date2).TotalMinutes) <= 30` — undocumented and non-configurable.
- **Fix approach:** Extract as a named constant with a comment. Consider whether minute-level precision is appropriate for quest scheduling or whether date-only comparison is sufficient.

### 18. `PlayerSignupEntity.SignupRole` Uses Raw Int with Inline Magic Numbers

- **Risk:** `SignupRole == 1` is used in multiple places in service code without reference to the `SignupRole` enum, making refactoring error-prone.
- **Files:** `EuphoriaInn.Domain/Services/QuestService.cs` line 25 (`playerSignup.SignupRole == 1`); `EuphoriaInn.Domain/Services/PlayerSignupService.cs` line 21 (`playerSignupEntity.SignupRole == 1`)
- **Fix approach:** The entity should expose the enum directly (or the service should cast to `SignupRole` before comparing). At minimum, use a named constant: `const int SpectatorRole = 1`.

### 19. Email Service Reconstructs SMTP Client on Every Email Send

- **Risk:** Configuration is re-read from `IConfiguration` and a new `SmtpClient` is constructed and disposed on every call. `System.Net.Mail.SmtpClient` is also marked `[Obsolete]` in .NET for new development.
- **Files:** `EuphoriaInn.Domain/Services/EmailService.cs` lines 15–31 and 70–86
- **Detail:** Both `SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync` duplicate the same SMTP setup block. The `[Quest Board URL]` placeholder in the date-changed email body has never been replaced (line 102).
- **Fix approach:** Extract SMTP client creation to a private method or use a library like `MailKit` (which is not deprecated). Fix the `[Quest Board URL]` placeholder.

### 20. Duplicate ViewModel Directory: `CharacterViewModels` vs `GuildMembersViewModels`

- **Risk:** Naming inconsistency in the view model folder structure. Two separate folders serve overlapping concerns.
- **Files:** `EuphoriaInn.Service/ViewModels/CharacterViewModels/GuildMembersIndexViewModel.cs`; `EuphoriaInn.Service/ViewModels/GuildMembersViewModels/GuildMembersIndexViewModel.cs`
- **Detail:** `CharacterViewModels/GuildMembersIndexViewModel.cs` actually defines `CharactersIndexViewModel` (different type, different namespace). The similar filename across two folders is confusing.
- **Fix approach:** Rename the `CharacterViewModels` file to `CharactersIndexViewModel.cs` to match the class name and clarify intent.

---

## Missing / Incomplete Features

### 21. No Email Verification on Registration

- **Risk:** Anyone can register with an arbitrary email address. Notifications may be sent to unverified addresses.
- **Files:** `EuphoriaInn.Domain/Services/UserService.cs` (`CreateAsync`, lines 38–57)
- **Detail:** `userManager.CreateAsync` is called and the user is immediately signed in. There is no call to `GenerateEmailConfirmationTokenAsync` or similar.
- **Fix approach:** Implement email confirmation flow using Identity's built-in token provider, or at minimum add a note that email verification is out of scope.

### 22. `[Quest Board URL]` Placeholder in Email Body

- **Risk:** Players receiving the "quest dates changed" email see a literal `[Quest Board URL]` text instead of a real link.
- **Files:** `EuphoriaInn.Domain/Services/EmailService.cs` line 102
- **Fix approach:** Inject `IConfiguration` or a settings class to get the application's public base URL and replace the placeholder with the actual `/Quest/Details/{questId}` URL. The `questId` would need to be added as a parameter.

### 23. Shop `SellItemToShop` Does Not Validate User Owns the Item

- **Risk:** A user can sell any quantity of any published shop item without previously owning it, effectively creating gold from nothing.
- **Files:** `EuphoriaInn.Domain/Services/ShopService.cs` lines 187–221; `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` lines 153–180
- **Detail:** `SellItemToShopAsync(int itemId, int quantity, User user)` only checks that the item exists and is published. It does not verify the user has a purchase transaction for that item. Compare to `ReturnOrSellItemAsync` which does check `originalTransaction.UserId == user.Id`.
- **Fix approach:** Before processing a sell-to-shop transaction, verify the user has sufficient quantity in unretired purchase transactions for that item.

---

## Scalability Concerns

### 24. No Pagination on Any List View

- **Risk:** Home page, Quest Log, Shop, Guild Members, and Admin pages all load unbounded lists.
- **Files:** `EuphoriaInn.Service/Controllers/QuestBoard/HomeController.cs`; `EuphoriaInn.Service/Controllers/Admin/AdminController.cs`; `EuphoriaInn.Service/Controllers/Shop/ShopController.cs`
- **Detail:** Every list query uses `ToListAsync` with no `Skip`/`Take`. For a small D&D group this is fine, but if the quest log or shop grows large, page load times will degrade.
- **Fix approach:** Add pagination parameters (`page`, `pageSize`) with sensible defaults to list queries and corresponding pagination controls in views.

### 25. No Caching on Frequently-Read, Rarely-Changed Data

- **Risk:** Every calendar view, home page load, and quest details page re-queries the full quest dataset from SQL Server.
- **Files:** `EuphoriaInn.Repository/QuestRepository.cs` (`GetQuestsForCalendarAsync`, `GetQuestsWithSignupsForRoleAsync`)
- **Fix approach:** Apply `IMemoryCache` (already available in ASP.NET Core) with a short TTL (e.g. 30 seconds) on calendar and home-page quest list queries.

---

## Error Handling Gaps

### 26. Generic Exception Caught and Swallowed in Shop Controller

- **Risk:** Unexpected errors in shop operations are silently converted to a generic TempData message. The original exception is not logged.
- **Files:** `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` lines 97, 143, 175
- **Detail:**
  ```csharp
  catch (Exception)
  {
      TempData["Error"] = "An error occurred...";
      return RedirectToAction(...);
  }
  ```
  No `ILogger` is injected in `ShopController`. Errors will not appear in application logs.
- **Fix approach:** Inject `ILogger<ShopController>` and call `_logger.LogError(ex, ...)` inside the catch block.

### 27. Race Condition in Shop Item Stock Decrement

- **Risk:** Concurrent purchases of a limited-stock item can over-sell stock. Two requests can both read `quantity > 0`, both pass the check, and both decrement.
- **Files:** `EuphoriaInn.Domain/Services/ShopService.cs` lines 77–101
- **Detail:** The check-then-update sequence (`if (itemEntity.Quantity > 0)` → `itemEntity.Quantity -= quantity`) is not atomic. EF Core does not use database-level row locking here.
- **Fix approach:** Use an optimistic concurrency check on `ShopItemEntity` (add a `[Timestamp]` / `[ConcurrencyCheck]` column) or issue a direct SQL `UPDATE ... SET Quantity = Quantity - @qty WHERE Quantity >= @qty` and check rows affected.

### 28. Spectator Email Notifications on Finalize Use Stale Quest State

- **Risk:** The finalized email loop operates on the `quest` object fetched before `FinalizeQuestAsync` mutates the database. Spectator logic reads `ps.Role == SignupRole.Spectator` on the pre-save in-memory collection.
- **Files:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 643–656
- **Detail:** `quest` is fetched before `FinalizeQuestAsync`. The loop then tries to send emails using that same in-memory `quest.PlayerSignups`. If EF's `AsNoTracking` returns a separate object graph, `selectedPlayerIds` comparison may work, but the condition `|| ps.Role == SignupRole.Spectator` sends to all spectators regardless of whether they were actually included in `selectedPlayerIds`.
- **Fix approach:** Re-fetch the quest after `FinalizeQuestAsync` before building the email list, or restructure `FinalizeQuestAsync` to return the list of email-eligible signups.

---

## Configuration / Secrets Management

### 29. Email Settings Left Empty in Committed `appsettings.json`

- **Risk:** Email notifications silently fail in any environment where settings are not overridden. The application logs a `LogWarning` and returns silently.
- **Files:** `EuphoriaInn.Service/appsettings.json` lines 17–24
- **Detail:** `SmtpUsername`, `SmtpPassword`, `FromEmail` are all empty strings. Developers may not notice emails are not sending in development.
- **Note:** This is intentional per design, but there is no startup validation or health-check flag that surfaces the missing email configuration.
- **Fix approach:** Add a startup validation log (e.g., `LogInformation("Email notifications are DISABLED: EmailSettings not configured.")`) so developers know the state.

### 30. Email SMTP Config Re-read Inside Business Logic Instead of Options Pattern

- **Risk:** Configuration access is scattered and non-validatable at startup.
- **Files:** `EuphoriaInn.Domain/Services/EmailService.cs` lines 15–22 and 70–77
- **Detail:** `configuration.GetSection("EmailSettings")["SmtpServer"]` is called inside each send method. If settings change at runtime (rare but possible with `IConfiguration` reloading), the behaviour is unpredictable.
- **Fix approach:** Use `IOptions<EmailSettings>` with a typed `EmailSettings` record validated via `services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings")`.

---

*Concerns audit: 2026-04-15*
