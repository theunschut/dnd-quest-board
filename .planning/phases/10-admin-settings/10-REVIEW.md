---
phase: 10-admin-settings
reviewed_at: 2026-06-18
status: issues_found
finding_count: 9
critical: 0
high: 3
medium: 3
low: 3
---

# Code Review — Phase 10: Admin Settings

## Summary

9 findings across the Phase 10 diff. 3 high-severity (non-atomic writes, URL validation UX block, `IsConfigured` missing secret check), 3 medium (secret leakage surface, undisposed test context, migration version mismatch), 3 low/cleanup (test assertion gap, form tag fragility, `AddAsync` vs `Add`).

---

## Findings

### HIGH

#### 1. Non-atomic SaveSettingsAsync — partial write on transient DB failure
**File:** `EuphoriaInn.Domain/Services/AdminSettingService.cs:25`

Each `UpsertAsync` call commits independently via its own `SaveChangesAsync`. If the second call (IsEnabled) throws after the first (OmphalosUrl) already committed, the database is left with a new URL but the old enabled flag. A third-call secret update has the same exposure.

**Fix:** Move `SaveChangesAsync` out of `UpsertAsync` (rename to `PrepareUpsert` / stage-only), and call `SaveChangesAsync` once at the end of `SaveSettingsAsync`, or wrap the whole operation in `await dbContext.Database.BeginTransactionAsync()`.

---

#### 2. `[Url]` on `string?` rejects empty string — admin cannot submit form with blank URL
**File:** `EuphoriaInn.Service/ViewModels/AdminViewModels/SettingsViewModel.cs:7`

ASP.NET Core 8's model binder delivers `""` (not `null`) for a blank text input into a `string?` property. `UrlAttribute.IsValid("")` calls `Uri.IsWellFormedUriString("", UriKind.Absolute)` which returns `false`. `ModelState.IsValid` is `false` and the POST never saves. No `DisplayFormat(ConvertEmptyStringToNull = true)` or MVC option is configured to convert empty to null.

An admin who opens Settings for the first time and wants to toggle IsEnabled without supplying a URL is silently blocked.

**Fix:** Add `[DisplayFormat(ConvertEmptyStringToNull = true)]` above `[Url]` on `OmphalosUrl`, or coerce in the service (`string.IsNullOrWhiteSpace(url) ? null : url`).

---

#### 3. `IsConfigured` missing `OmphalosSharedSecret` check — Phase 11 HMAC throws on null secret
**File:** `EuphoriaInn.Domain/Models/IntegrationSettings.cs:10`

```csharp
public bool IsConfigured => IsEnabled && !string.IsNullOrWhiteSpace(OmphalosUrl);
```

An admin can set `IsEnabled = true` and a URL without ever entering a secret. `IsConfigured` returns `true`. Phase 11 will render the Omphalos navbar link and generate a token. `new HMACSHA256(Encoding.UTF8.GetBytes(null))` throws `ArgumentNullException`.

**Fix:**
```csharp
public bool IsConfigured => IsEnabled
    && !string.IsNullOrWhiteSpace(OmphalosUrl)
    && !string.IsNullOrWhiteSpace(OmphalosSharedSecret);
```

---

### MEDIUM

#### 4. `GetSettingsAsync` populates `OmphalosSharedSecret` — no `[JsonIgnore]`; future callers risk leaking the signing key
**File:** `EuphoriaInn.Domain/Services/AdminSettingService.cs:13`

`IntegrationSettings` is a fully public record with `OmphalosSharedSecret` as an `init` property. The only protection is `AdminController.cs:247` deliberately not mapping the field. A Phase 11 ViewComponent or diagnostic endpoint that serializes `IntegrationSettings` to JSON would emit the secret. No `[JsonIgnore]` or `[Sensitive]` guard exists at the model level.

**Fix:** Add `[JsonIgnore]` to `OmphalosSharedSecret` in `IntegrationSettings`, or document the exclusion contract on `IAdminSettingService`.

---

#### 5. `QuestBoardContext` from `CreateContext()` is never disposed in `AdminSettingServiceTests`
**File:** `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs:16`

The context is passed to `AdminSettingRepository` and held for the test lifetime, but no field stores it and `Dispose()` only calls `_db.Dispose()`. The EF change tracker and the SqliteConnection it wraps are never explicitly released.

**Fix:**
```csharp
private readonly QuestBoardContext _context;

public AdminSettingServiceTests()
{
    _db = new TestDatabase($"AdminSettingTest_{Guid.NewGuid():N}");
    _context = _db.CreateContext();
    var repo = new AdminSettingRepository(_context);
    _sut = new AdminSettingService(repo);
}

public void Dispose()
{
    _context.Dispose();
    _db.Dispose();
}
```

---

#### 6. Migration snapshot `ProductVersion` is `10.0.9`; csproj targets EF Core `9.0.6`
**File:** `EuphoriaInn.Repository/Migrations/20260618124958_AddAdminSettings.Designer.cs:23`

The designer file was generated with a .NET 10 EF Core toolchain (`ProductVersion: "10.0.9"`), but `EuphoriaInn.Repository.csproj` declares `Microsoft.EntityFrameworkCore` 9.0.6. When any developer runs `dotnet ef migrations add` with the 9.x SDK, the toolchain regenerates the snapshot targeting `ProductVersion 9.x`, diffing against the 10.x file and producing a spurious pending-migration warning on startup.

**Fix:** Regenerate the migration with the same EF Core toolchain version the project declares (9.0.6), or pin the `dotnet-ef` global tool version to match.

---

### LOW

#### 7. `AdminActions_WhenNotAuthenticated` Theory accepts `NotFound` for `/Admin/Settings` — masks broken route or missing auth
**File:** `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs:64`

`NotFound` is in the acceptable set to accommodate `/Admin/DeleteUser/1` (requires `DELETE`, returns 405/404 on GET). `/Admin/Settings` is a `[HttpGet]` on an `[Authorize]` controller. If the route is misconfigured or `[Authorize]` is accidentally removed, the unauthenticated request returns 404 and the test still passes.

**Fix:** Exclude `/Admin/Settings` from this Theory and rely on the separate `Settings_WhenNotAdmin_ShouldReturnForbiddenOrRedirect` Fact, or split the Theory so Settings has its own stricter assertion.

---

#### 8. Settings form lacks `asp-controller="Admin"` — fragile if view is reused
**File:** `EuphoriaInn.Service/Views/Admin/Settings.cshtml:39`

```html
<form asp-action="Settings" method="post">
```

Works via ambient route context, but breaks silently if the partial is ever included from a different controller context. Every other Admin form specifies `asp-controller` explicitly (consistent with `_Layout.cshtml` and `EditUser.cshtml`).

**Fix:** `<form asp-controller="Admin" asp-action="Settings" method="post">`

---

#### 9. `AddAsync` used for a non-identity string PK entity
**File:** `EuphoriaInn.Repository/AdminSettingRepository.cs:20`

`AddAsync` exists for value generators (HiLo sequences) that require async DB initialization. With `DatabaseGeneratedOption.None` the PK is application-provided; EF Core docs recommend synchronous `Add` in this case. No runtime bug, but adds an unnecessary async state machine and misleads readers.

**Fix:** `dbContext.AdminSettings.Add(new AdminSettingEntity { ... });`
