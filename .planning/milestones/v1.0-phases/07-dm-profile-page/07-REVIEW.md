---
phase: 07-dm-profile-page
reviewed: 2026-06-17T19:28:13Z
depth: standard
files_reviewed: 27
files_reviewed_list:
  - EuphoriaInn.Domain/Extensions/ServiceExtensions.cs
  - EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileRepository.cs
  - EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs
  - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
  - EuphoriaInn.Domain/Interfaces/IQuestService.cs
  - EuphoriaInn.Domain/Models/DungeonMasterProfile.cs
  - EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs
  - EuphoriaInn.Domain/Services/QuestService.cs
  - EuphoriaInn.IntegrationTests/Controllers/DungeonMasterControllerIntegrationTests.cs
  - EuphoriaInn.IntegrationTests/Controllers/PlayersControllerIntegrationTests.cs
  - EuphoriaInn.Repository/Automapper/EntityProfile.cs
  - EuphoriaInn.Repository/DungeonMasterProfileRepository.cs
  - EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs
  - EuphoriaInn.Repository/Entities/DungeonMasterProfileImageEntity.cs
  - EuphoriaInn.Repository/Entities/QuestBoardContext.cs
  - EuphoriaInn.Repository/Extensions/ServiceExtensions.cs
  - EuphoriaInn.Repository/Migrations/20260617191315_AddDMProfileSystem.cs
  - EuphoriaInn.Repository/QuestRepository.cs
  - EuphoriaInn.Service/Automapper/ViewModelProfile.cs
  - EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs
  - EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/DMProfileViewModel.cs
  - EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/EditDMProfileViewModel.cs
  - EuphoriaInn.Service/Views/DungeonMaster/EditProfile.cshtml
  - EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml
  - EuphoriaInn.Service/Views/Players/Index.cshtml
  - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
  - EuphoriaInn.Service/wwwroot/css/dm-profile.css
findings:
  critical: 0
  warning: 4
  info: 3
  total: 7
status: issues_found
---

# Phase 7: Code Review Report

**Reviewed:** 2026-06-17T19:28:13Z
**Depth:** standard
**Files Reviewed:** 27
**Status:** issues_found

## Summary

This phase delivers the DM Profile Page feature: a public profile view per DM, an edit page gated by the `DungeonMasterOnly` policy, an image upsert flow backed by a separate `DungeonMasterProfileImages` table, and a quest-history section mapped from `GetQuestsByDungeonMasterAsync`. The architecture is consistent with the rest of the codebase — clean separation across domain, repository, and service layers, correct use of AutoMapper profiles, and proper authorization checks at the controller level.

No critical security vulnerabilities were found. Four warnings cover correctness gaps that could surface in production: a silent image-clear bug during bio-only edits, a hardcoded MIME type mismatch, a missing size cap on uploaded image data reaching the database, and a race condition in the upsert flow. Three informational items are noted below.

---

## Warnings

### WR-01: Bio-only save silently clears the profile picture

**File:** `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs:27-31`

**Issue:** `UpsertProfileAsync` only calls `UpsertProfileImageAsync` when `imageBytes != null`. When a DM edits their bio without uploading a new photo, `imageBytes` is `null` and the image upsert is skipped — which is correct. However, the controller passes `imageBytes = null` whenever no file is uploaded regardless of whether the DM intends to keep their existing picture. The repository's `UpsertProfileImageAsync` has an explicit `if (imageData == null) entity.ProfileImage = null` branch (line 36). Because that branch is only reached when `imageBytes != null` in the service, the null-clear path is currently dead code — but the code is one refactor away from accidentally activating it. The deeper issue is that there is no way for the edit form to distinguish "no new file uploaded, keep existing" from "user explicitly removed the picture". If a future developer wires a "remove photo" button that sends `imageBytes = null` through `UpsertProfileAsync`, it will silently delete existing images even though the service skips calling the repository for null. The current behavior is safe but the intent is unclear and fragile.

**Fix:** Document the invariant explicitly, or make the service signature unambiguous:

```csharp
// DungeonMasterProfileService.cs
public async Task UpsertProfileAsync(
    int userId,
    string? bio,
    byte[]? imageBytes,
    bool removeImage = false,  // explicit intent signal
    CancellationToken token = default)
{
    var profile = await repository.GetProfileByUserIdAsync(userId, token);
    if (profile == null)
    {
        var newProfile = new DungeonMasterProfile { Id = userId, Bio = bio };
        await repository.AddAsync(newProfile, token);
        if (imageBytes != null)
            await repository.UpsertProfileImageAsync(userId, imageBytes, token);
    }
    else
    {
        profile.Bio = bio;
        await repository.UpdateAsync(profile, token);
        if (imageBytes != null || removeImage)
            await repository.UpsertProfileImageAsync(userId, imageBytes, token);
    }
}
```

---

### WR-02: `GetDMProfilePicture` always returns `Content-Type: image/jpeg` regardless of actual format

**File:** `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs:99`

**Issue:** The endpoint returns `File(bytes, "image/jpeg")` for all images. The `EditDMProfileViewModel` accepts `.jpg`, `.jpeg`, `.png`, and `.gif`. A PNG or GIF uploaded by the DM will be served to the browser with a `jpeg` content type, which causes some browsers to refuse to display it or render it incorrectly. This is the same issue present in the `GuildMembersController.GetProfilePicture` pattern this endpoint was modelled on — copying it propagates the defect.

**Fix:** Store the MIME type alongside the image bytes, or detect it from the magic bytes at serve time:

```csharp
// Minimal fix using magic bytes — no schema change required
public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
{
    var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
    if (bytes == null) return NotFound();

    var contentType = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50
        ? "image/png"
        : bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46
        ? "image/gif"
        : "image/jpeg";

    return File(bytes, contentType);
}
```

---

### WR-03: No server-side enforcement of the 5 MB image size limit

**File:** `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs:80-85`

**Issue:** The `[MaxFileSize(5 * 1024 * 1024)]` attribute on `EditDMProfileViewModel.ProfilePictureFile` performs validation, but only if `ModelState.IsValid` is checked before reading the file. The controller does check `ModelState.IsValid` at line 76 — that is correct. However, ASP.NET Core's `[MaxFileSize]` is a custom attribute (defined in `CharacterViewModels`) that runs as a `ValidationAttribute`. Custom `ValidationAttribute`s run during model binding, but `IFormFile.Length` is only available after the file has already been fully buffered into memory by the request body reader. This means a 50 MB upload will be held in memory and then rejected. More critically, the default Kestrel request body size limit (`30 MB`) applies globally, but if an operator raises it (e.g., in `Program.cs` via `options.Limits.MaxRequestBodySize`), there is nothing at the repository/domain layer preventing arbitrarily large blobs from being written to `varbinary(max)`. The client-side JS check in `EditProfile.cshtml` is a UX convenience only — it is trivially bypassed.

**Fix:** Add an explicit size check in the controller action before copying the stream:

```csharp
if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
{
    const long maxSize = 5 * 1024 * 1024;
    if (viewModel.ProfilePictureFile.Length > maxSize)
    {
        ModelState.AddModelError(nameof(viewModel.ProfilePictureFile),
            "Profile picture cannot exceed 5 MB.");
        return View(viewModel);
    }
    using var memoryStream = new MemoryStream();
    await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
    imageBytes = memoryStream.ToArray();
}
```

---

### WR-04: TOCTOU race in `UpsertProfileAsync` — double AddAsync possible under concurrent requests

**File:** `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs:17-33`

**Issue:** `UpsertProfileAsync` reads the profile (`GetProfileByUserIdAsync`), then conditionally calls `AddAsync` if null. Two concurrent POST requests for the same user (e.g., double-click or network retry) will both read null and both attempt to insert a row with the same primary key (`Id = userId`). The `DungeonMasterProfileEntity` has `DatabaseGeneratedOption.None`, so EF Core will issue two `INSERT` statements for the same PK, causing a primary key violation unhandled exception (`DbUpdateException`). The `QuestRepository.AddAsync` has a similar guard for follow-up quests (catching `DbUpdateException`) — the same pattern should be applied here.

**Fix:** Wrap `AddAsync` in the service, or add a DB-level upsert in the repository:

```csharp
// Repository approach — avoids the check-then-act pattern entirely
public async Task UpsertProfileAsync(int userId, string? bio, CancellationToken token = default)
{
    var entity = await DbContext.DungeonMasterProfiles
        .FirstOrDefaultAsync(p => p.Id == userId, token);

    if (entity == null)
    {
        entity = new DungeonMasterProfileEntity { Id = userId, Bio = bio };
        DbContext.DungeonMasterProfiles.Add(entity);
    }
    else
    {
        entity.Bio = bio;
    }
    await DbContext.SaveChangesAsync(token);
}
```

Alternatively, catch `DbUpdateException` in `DungeonMasterProfileService.UpsertProfileAsync` and retry with an update, consistent with how `QuestRepository.AddAsync` handles the duplicate follow-up case.

---

## Info

### IN-01: `QuestSummaryViewModel` defined in the same file as `DMProfileViewModel`

**File:** `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/DMProfileViewModel.cs:14-19`

**Issue:** `QuestSummaryViewModel` is a second public class declared in `DMProfileViewModel.cs`. The project convention is one class per file (enforced by naming patterns in CLAUDE.md). `QuestSummaryViewModel` is also imported in `ViewModelProfile.cs`, making discovery by file name impossible.

**Fix:** Move `QuestSummaryViewModel` to its own file `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/QuestSummaryViewModel.cs`.

---

### IN-02: `GetDMProfilePicture` has no cache headers — every page load re-fetches profile image bytes from SQL

**File:** `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs:95-100`

**Issue:** The image endpoint returns raw binary with no `Cache-Control` header. Every render of `Profile.cshtml` or `EditProfile.cshtml` hits SQL Server to read `varbinary(max)` image data. For a profile page that may be opened frequently, this is unnecessary load. The `GuildMembersController` has the same issue.

**Fix:** Add a short cache header:

```csharp
Response.Headers.CacheControl = "public, max-age=3600";
return File(bytes, contentType);
```

---

### IN-03: `Profile.cshtml` quest history table shows `Difficulty` column but maps `ChallengeRating` as integer

**File:** `EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml:94-101`

**Issue:** The table header is labeled "Difficulty" but the underlying `QuestSummaryViewModel.ChallengeRating` is an `int`. The view uses a `switch` expression to convert 1→Easy, 2→Medium, 3→Hard, 4→Deadly, with a fallback of `$"CR {quest.ChallengeRating}"`. This is correct for the current data range, but the fallback leaks the internal integer representation to users if a new difficulty level is added without updating this view. The existing `Difficulty` enum in the domain is not used here — the view duplicates the mapping logic already present elsewhere in the codebase.

**Fix:** Add `Difficulty` (the enum value) to `QuestSummaryViewModel` and map it from the domain `Quest.ChallengeRating` cast to `Difficulty`, then use a shared partial or display template instead of inline switch logic. At minimum, add a code comment noting that the switch must stay in sync with the `Difficulty` enum.

---

_Reviewed: 2026-06-17T19:28:13Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
