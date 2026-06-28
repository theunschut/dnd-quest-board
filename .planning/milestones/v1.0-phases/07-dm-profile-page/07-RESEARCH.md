# Phase 7: DM Profile Page - Research

**Researched:** 2026-06-17
**Domain:** ASP.NET Core 8 MVC — new controller, two new DB tables, file upload, role-based authorization
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Create `DungeonMasterProfileEntity` (separate from `UserEntity`) linked to UserId as FK. Holds `Bio` (varchar 2000, nullable) and navigation to `DungeonMasterProfileImage`.
- **D-02:** Create `DungeonMasterProfileImage` table following `CharacterImageEntity` pattern: separate table, `Id` is both PK and FK to `DungeonMasterProfileEntity`, `ImageData` is `byte[]`.
- **D-03:** `DungeonMasterProfileEntity` created lazily — only when DM saves for the first time. Profile page handles null gracefully (placeholder image, empty bio). No migration data-seeding.
- **D-04:** New `DungeonMasterController` handles all DM profile routes. Edit page: `GET/POST /DungeonMaster/EditProfile` (own) and `GET/POST /DungeonMaster/EditProfile/{id}` (admin). Mirrors Quest Manage pattern.
- **D-05:** Authorization: `[Authorize(Policy = "DungeonMasterOnly")]` + inline `!isOwnProfile && !User.IsInRole("Admin")` → `Forbid()`. Exact pattern from `QuestController.Manage`.
- **D-06:** Edit page linked from DM navbar dropdown.
- **D-07:** Profile page shows "Edit Profile" button to owning DM and admins. Links to `/DungeonMaster/EditProfile/{id}`.
- **D-08:** `/DungeonMaster/Profile/{id}` shows: DM name, profile photo (or placeholder), bio, and list of all quests the DM has run (finalized and active), ordered most recent first.
- **D-09:** Quest list shows title, date, difficulty. Links to `/Quest/Details/{id}`.
- **D-10:** DM directory page adds link on each DM row to `/DungeonMaster/Profile/{id}`.

### Claude's Discretion

- Exact HTML/CSS layout (follow `modern-card` pattern, FontAwesome icons, Bootstrap 5 grid).
- Whether to add a new `IDungeonMasterService` or extend `IUserService` with DM profile methods.
- Image serving endpoint name and placement within `DungeonMasterController`.
- Whether `DungeonMasterProfileEntity.Id` shares the same value as `UserId` (like `CharacterImageEntity`) or uses an independent PK.
- Whether `DungeonMasterProfileEntity` navigation is on `UserEntity` or resolved separately by UserId query.

### Deferred Ideas (OUT OF SCOPE)

None.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DMPRO-01 | A dedicated DM profile page exists at `/DungeonMaster/Profile/{id}` showing DM's photo, name, and bio | New `DungeonMasterController.Profile(int id)` action; `DungeonMasterProfileViewModel` includes photo flag, bio, and quest list |
| DMPRO-02 | DMs can edit their own profile bio and upload a profile photo via the existing account area | `DungeonMasterController.EditProfile()` (no id) + POST; mirrors `GuildMembersController.Edit` file upload pattern |
| DMPRO-03 | Admin can edit any DM's profile | `DungeonMasterController.EditProfile(int id)` + POST; inline `!isOwner && !User.IsInRole("Admin")` guard identical to `QuestController` |
| DMPRO-04 | DM directory page links to each DM's profile | Modify `Views/Players/Index.cshtml`: wrap DM name `<td>` in `<a asp-controller="DungeonMaster" asp-action="Profile" asp-route-id="@dm.Id">` |
| DMPRO-05 | EF Core migration adds `Bio` (varchar 2000, nullable) and linked `DungeonMasterProfileImage` table | Two new entity classes + `QuestBoardContext` DbSet entries + `OnModelCreating` fluent config + migration via `dotnet ef migrations add` |

</phase_requirements>

---

## Summary

Phase 7 adds a DM profile subsystem to an existing ASP.NET Core 8 MVC application. The work is bounded and self-contained: two new EF Core entities, one new controller with four actions, three new Razor views, one new CSS file, and targeted modifications to `_Layout.cshtml`, `Views/Players/Index.cshtml`, `QuestBoardContext`, two AutoMapper profiles, and two DI registrations.

Every pattern required in this phase has a working, verified implementation already in the codebase. The image storage and serving pattern is established by `CharacterImageEntity` and `GuildMembersController.GetProfilePicture`. The file upload form pattern (client-side validation, `enctype`, `IFormFile?` with custom attributes) is established by `GuildMembers/Edit.cshtml` and `CharacterViewModel`. The two-level authorization pattern (policy + inline role check) is established by `QuestController.Manage`. The lazy-create upsert pattern for a separate image table is established by `CharacterRepository.UpdateProfileImageAsync`.

The key discretion decision this research resolves: create a new `IDungeonMasterProfileService` (rather than extending `IUserService`). This mirrors the `ICharacterService` / `CharacterService` precedent and keeps the DM profile domain isolated. The `DungeonMasterProfileEntity.Id` should share the same value as `UserId` (identical to the `CharacterImageEntity` where Id = FK), making the profile a true 1:1 extension of the user row with no surrogate key required.

**Primary recommendation:** Follow the `CharacterImageEntity` / `CharacterService` / `GuildMembersController` triad verbatim. Do not invent new patterns — map each new file to its existing counterpart.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| DM profile data storage (Bio + image bytes) | Database / Storage | — | New EF Core entities in Repository layer; no caching needed at current scale |
| Image serving (GetDMProfilePicture endpoint) | API / Backend | — | Controller action returns `File(bytes, "image/jpeg")`; same tier as `GuildMembersController.GetProfilePicture` |
| Profile page rendering | Frontend Server (SSR) | — | Razor view with server-side auth check for Edit button visibility |
| Edit form + file upload | Frontend Server (SSR) | Browser / Client | Server renders form; browser-side JS validates file size/type before submit (mirrors existing pattern) |
| Authorization (DM-only + admin override) | API / Backend | — | Policy attribute + inline `IsInRole` check in controller action |
| DM directory link (D-10) | Frontend Server (SSR) | — | One-line Razor change in existing `Players/Index.cshtml` |
| Navbar dropdown addition (D-06) | Frontend Server (SSR) | — | One `<li>` added inside existing DM-only dropdown in `_Layout.cshtml` |

---

## Standard Stack

All dependencies are already present in the project. No new NuGet packages are required for this phase.

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.6 | ORM for new entities | Already the project database provider [VERIFIED: csproj] |
| AutoMapper | 14.0.0 | Entity↔Model and Model↔ViewModel mapping | Established project mapping library [VERIFIED: csproj] |
| ASP.NET Core Identity | 8.0.11 | `UserManager` used by `IUserService.GetUserAsync` | Required for auth-aware controller actions [VERIFIED: csproj] |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5.3.0 (CDN) | 5.3.0 | Layout, grid, badges, forms | All new views use `modern-card` pattern + Bootstrap utilities [VERIFIED: _Layout.cshtml] |
| Font Awesome 6.4.0 (CDN) | 6.4.0 | Icons with `me-2` spacing | All icon usage in new views [VERIFIED: _Layout.cshtml] |

**Installation:** None required. All packages already in solution.

---

## Architecture Patterns

### System Architecture Diagram

```
Browser Request: GET /DungeonMaster/Profile/{id}
        |
        v
DungeonMasterController.Profile(int id)
        |
        +---> IDungeonMasterProfileService.GetProfileByUserIdAsync(id)
        |              |
        |              v
        |       DungeonMasterProfileRepository
        |              |
        |              v
        |       QuestBoardContext (EF Core)
        |         DungeonMasterProfiles + DungeonMasterProfileImages tables
        |
        +---> IUserService.GetByIdAsync(id)   ← load DM name/email
        |
        +---> IQuestService (or IUserService.Quests nav-prop)  ← quest list for DM
        |
        v
DungeonMasterProfileViewModel  (via AutoMapper or manual map)
        |
        v
Views/DungeonMaster/Profile.cshtml   (modern-card layout, auth-gated Edit button)


Browser Request: POST /DungeonMaster/EditProfile/{id}
        |
        v
DungeonMasterController.EditProfile(int id, EditDMProfileViewModel vm)
        |
        +---> Auth check: isOwner || Admin → else Forbid()
        |
        +---> Read IFormFile → byte[] from MemoryStream
        |
        +---> IDungeonMasterProfileService.UpsertProfileAsync(userId, bio, imageBytes?)
        |              |
        |              v
        |       DungeonMasterProfileRepository (lazy create or update)
        |
        v
RedirectToAction("Profile", new { id })


Browser Request: GET /DungeonMaster/GetDMProfilePicture?id={userId}
        |
        v
DungeonMasterController.GetDMProfilePicture(int id)
        |
        +---> IDungeonMasterProfileService.GetProfilePictureAsync(id)
        |
        v
File(bytes, "image/jpeg")  OR  NotFound()
```

### Recommended Project Structure

New files to create:

```
EuphoriaInn.Repository/
  Entities/
    DungeonMasterProfileEntity.cs        # new
    DungeonMasterProfileImageEntity.cs   # new
EuphoriaInn.Domain/
  Models/
    DungeonMasterProfile.cs              # new domain model
  Interfaces/
    IDungeonMasterProfileService.cs      # new
    IDungeonMasterProfileRepository.cs   # new
  Services/
    DungeonMasterProfileService.cs       # new (internal)
EuphoriaInn.Service/
  Controllers/
    DungeonMaster/
      DungeonMasterController.cs         # new
  ViewModels/
    DungeonMasterViewModels/
      DMProfileViewModel.cs              # new (Profile view)
      EditDMProfileViewModel.cs          # new (Edit view)
  Views/
    DungeonMaster/
      Profile.cshtml                     # new
      EditProfile.cshtml                 # new
  wwwroot/css/
    dm-profile.css                       # new
```

Files to modify:

```
EuphoriaInn.Repository/
  Entities/QuestBoardContext.cs          # add DbSets + OnModelCreating config
  Automapper/EntityProfile.cs            # add DungeonMasterProfile mapping
  Extensions/ServiceExtensions.cs        # register IDungeonMasterProfileRepository
EuphoriaInn.Domain/
  Extensions/ServiceExtensions.cs        # register IDungeonMasterProfileService
EuphoriaInn.Service/
  Automapper/ViewModelProfile.cs         # add DM profile ViewModel mapping
  Views/Shared/_Layout.cshtml            # add DM dropdown item + dm-profile.css link
  Views/Players/Index.cshtml             # wrap DM name in profile link (D-10)
EuphoriaInn.Repository/
  Migrations/                            # new migration: AddDMProfileSystem
```

### Pattern 1: CharacterImageEntity PK=FK Pattern (replicate for DungeonMasterProfileImageEntity)

**What:** Separate image table where `Id` is simultaneously the primary key and a foreign key to the owning entity. EF Core fluent API configures this as a 1:1 required-end on the image side.

**When to use:** Any time image bytes for an entity need to be stored in a separate table to avoid loading them on every entity query.

**Example:**
```csharp
// Source: EuphoriaInn.Repository/Entities/CharacterImageEntity.cs (VERIFIED: read)
[Table("DungeonMasterProfileImages")]
public class DungeonMasterProfileImageEntity : IEntity
{
    [Key]
    [ForeignKey(nameof(DungeonMasterProfile))]
    public int Id { get; set; }

    [Required]
    public byte[] ImageData { get; set; } = [];

    public virtual DungeonMasterProfileEntity DungeonMasterProfile { get; set; } = null!;
}
```

**QuestBoardContext configuration:**
```csharp
// Source: QuestBoardContext.cs lines 114-117 pattern (VERIFIED: read)
modelBuilder.Entity<DungeonMasterProfileEntity>()
    .HasOne(p => p.ProfileImage)
    .WithOne(pi => pi.DungeonMasterProfile)
    .HasForeignKey<DungeonMasterProfileImageEntity>(pi => pi.Id)
    .OnDelete(DeleteBehavior.Cascade);
```

### Pattern 2: Lazy-Create Upsert for Profile Image

**What:** Repository method checks whether an image record already exists for the entity. If not, creates a new one with `Id` set to the entity's Id. If yes, updates `ImageData` in place.

**When to use:** When image records are optional and created on first upload.

**Example:**
```csharp
// Source: EuphoriaInn.Repository/CharacterRepository.cs lines 65-90 (VERIFIED: read)
public async Task UpsertProfileImageAsync(int userId, byte[]? imageData, CancellationToken token = default)
{
    var entity = await DbContext.DungeonMasterProfiles
        .Include(p => p.ProfileImage)
        .FirstOrDefaultAsync(p => p.Id == userId, token);
    if (entity == null) return;

    if (imageData == null) { entity.ProfileImage = null; }
    else if (entity.ProfileImage == null)
    {
        entity.ProfileImage = new DungeonMasterProfileImageEntity
        {
            Id = entity.Id,
            ImageData = imageData
        };
    }
    else { entity.ProfileImage.ImageData = imageData; }

    await DbContext.SaveChangesAsync(token);
}
```

### Pattern 3: Authorization — Policy + Inline Role Check

**What:** Action decorated with `[Authorize(Policy = "DungeonMasterOnly")]` (satisfies DM or Admin). Then explicit inline check blocks a DM from editing another DM's profile if they are not Admin.

**When to use:** Any edit action where ownership matters but admins should bypass the ownership check.

**Example:**
```csharp
// Source: QuestController.cs — Delete/Edit actions (VERIFIED: read lines 140, 198)
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> EditProfile(int? id, CancellationToken token = default)
{
    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null) return Challenge();

    var targetUserId = id ?? currentUser.Id;
    var isOwnProfile = targetUserId == currentUser.Id;

    if (!isOwnProfile && !User.IsInRole("Admin"))
        return Forbid();

    // ... load and return view
}
```

### Pattern 4: Image-Serving Action

**What:** Controller action that returns raw bytes as `image/jpeg` content. If no image exists, returns `NotFound()` so `<img>` tags fall back gracefully (browser shows broken-image icon, but Razor template uses placeholder div when `ProfilePicture == null`).

**Example:**
```csharp
// Source: GuildMembersController.cs lines 262-271 (VERIFIED: read)
[HttpGet]
public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
{
    var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
    if (bytes == null) return NotFound();
    return File(bytes, "image/jpeg");
}
```

### Pattern 5: IFormFile Upload to byte[]

**What:** Read `IFormFile` into `MemoryStream`, convert to `byte[]`, assign to domain model field before service call.

**Example:**
```csharp
// Source: GuildMembersController.cs lines 190-196 (VERIFIED: read)
if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
{
    using var memoryStream = new MemoryStream();
    await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
    viewModel.ProfilePicture = memoryStream.ToArray();
}
```

### Pattern 6: Lazy-Create Profile Entity on First Save

**What:** Profile entity (`DungeonMasterProfileEntity`) is not created at user registration. The service's upsert method creates it when the DM first saves. Profile GET handles `null` gracefully.

**Example (service upsert):**
```csharp
// [ASSUMED pattern — based on D-03 decision]
public async Task UpsertProfileAsync(int userId, string? bio, byte[]? imageBytes, CancellationToken token = default)
{
    var profile = await repository.GetByUserIdAsync(userId, token);
    if (profile == null)
    {
        profile = new DungeonMasterProfile { Id = userId, Bio = bio };
        await repository.AddAsync(profile, token);
    }
    else
    {
        profile.Bio = bio;
        await repository.UpdateAsync(profile, token);
    }
    if (imageBytes != null)
        await repository.UpsertProfileImageAsync(userId, imageBytes, token);
}
```

### Anti-Patterns to Avoid

- **Storing image bytes in `DungeonMasterProfileEntity` directly:** The `CharacterImageEntity` separate-table pattern exists specifically to avoid loading image bytes on every profile query. Do not add `byte[]` to the profile entity row.
- **Creating DungeonMasterProfileEntity at user registration:** D-03 explicitly requires lazy creation. Adding a migration seed or registration hook would introduce a seeding step that is explicitly out of scope.
- **Adding `DungeonMasterProfileEntity` navigation property to `UserEntity`:** The CONTEXT.md (Claude's Discretion) permits resolving by UserId query. Modifying `UserEntity` creates a dependency from the generic user entity to the DM-specific profile and is unnecessary. Use `DbContext.DungeonMasterProfiles.FirstOrDefaultAsync(p => p.Id == userId)` instead.
- **Cascade delete from `UserEntity` to `DungeonMasterProfileEntity`:** SQL Server prohibits multiple cascade paths to the same table if `QuestEntity` also FK's to `UserEntity`. Use `OnDelete(DeleteBehavior.Cascade)` on the `DungeonMasterProfileEntity → DungeonMasterProfileImageEntity` relationship (safe — single path). Use `NoAction` or `ClientCascade` on `UserEntity → DungeonMasterProfileEntity`.
- **Forgetting `[ValidateAntiForgeryToken]` on POST actions:** Every existing POST action in the codebase carries this attribute. Do not omit it.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Image storage as separate table | Custom SQL or blob store | `CharacterImageEntity` pattern (Id=FK, `byte[]`) | Already implemented and tested in codebase; EF handles cascade delete |
| File type + size validation | Custom regex or manual MIME check | `[MaxFileSize]` + `[AllowedExtensions]` attributes already in `CharacterViewModel.cs` | Reuse verbatim — same 5 MB limit, same extension list |
| Client-side file validation | New JS library | Copy JS block from `GuildMembers/Edit.cshtml` (lines 141-174) | Exact pattern already exists; same error div approach |
| Two-level auth (policy + ownership) | Custom middleware or filters | `[Authorize(Policy = "DungeonMasterOnly")]` + inline `IsInRole("Admin")` | Established in `QuestController`; consistent with project patterns |
| Image serving | Dedicated file server | `return File(bytes, "image/jpeg")` in controller | Already used by `GuildMembersController.GetProfilePicture` |

---

## Runtime State Inventory

> Not applicable. This is a greenfield feature addition with no rename/refactor/migration of existing data. New tables will be empty on creation — no data migration required (D-03 confirms lazy creation).

---

## Common Pitfalls

### Pitfall 1: Cascade Delete Conflict (SQL Server multiple cascade paths)

**What goes wrong:** SQL Server raises an error when `DungeonMasterProfileEntity` cascades from `UserEntity` AND `QuestEntity` already cascades from `UserEntity`. The "multiple cascade paths" error prevents the migration from applying.

**Why it happens:** SQL Server does not allow two FK relationships on the same table that both resolve to the same parent table with `CASCADE`. `QuestEntity.DungeonMasterId` already uses `OnDelete(DeleteBehavior.NoAction)` to avoid exactly this.

**How to avoid:** Configure `UserEntity → DungeonMasterProfileEntity` with `OnDelete(DeleteBehavior.Cascade)` — this is a direct single-path 1:1 relationship and is safe. Do NOT use `NoAction` here as that would leave orphaned profile rows if a user is deleted. Verify the existing QuestBoardContext patterns; the existing setup uses `NoAction` for `QuestEntity → DungeonMaster` and `Cascade` for `CharacterEntity → Owner` — follow the `CharacterEntity` approach for the 1:1 profile.

**Warning signs:** Migration apply throws `SqlException` mentioning "cascading" or "multiple cascade paths."

### Pitfall 2: BaseRepository.AddAsync Sets Id from DB — But DungeonMasterProfileEntity.Id Is Not DB-Generated

**What goes wrong:** `DungeonMasterProfileEntity.Id` should equal `UserId` (not be DB-generated). If the entity is annotated with `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` or the DbContext treats it as identity, EF will ignore the assigned Id and assign a new one, breaking the FK relationship.

**Why it happens:** By default, integer PK columns in EF Core are treated as identity. This must be explicitly overridden.

**How to avoid:** Do NOT put `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` on `DungeonMasterProfileEntity.Id`. Instead annotate with `[DatabaseGenerated(DatabaseGeneratedOption.None)]` and configure in fluent API:
```csharp
modelBuilder.Entity<DungeonMasterProfileEntity>()
    .Property(p => p.Id)
    .ValueGeneratedNever();
```
Also override `AddAsync` in the repository (or use `DbContext.DungeonMasterProfiles.AddAsync` directly) to set `Id = userId` before calling `SaveChangesAsync`.

**Warning signs:** `DungeonMasterProfileEntity.Id` in the DB is not equal to the user's Id after saving.

### Pitfall 3: Profile Page 404 When DM Has No Profile Yet

**What goes wrong:** `/DungeonMaster/Profile/{id}` returns 404 because `GetProfileByUserIdAsync` returns null and the action does `return NotFound()`.

**Why it happens:** D-03 says profiles are created lazily. The profile page must handle the `null` case gracefully.

**How to avoid:** In `DungeonMasterController.Profile(int id)`, if the service returns null, still render the view with a "no bio yet" placeholder state. Use a ViewModel initialized with defaults rather than returning `NotFound()`. Return `NotFound()` only when the user `id` itself does not correspond to a DM in the system.

**Warning signs:** Navigating to a DM's profile before they have saved it returns HTTP 404 instead of rendering the page with placeholder content.

### Pitfall 4: Quest List on Profile Includes Non-DM Quests

**What goes wrong:** Loading quests via `User.Quests` navigation property also pulls DM-session quests (those with `DungeonMasterSession = true`), or alternatively may pull ALL quests if the query does not filter to only this DM's quests.

**Why it happens:** `UserEntity.Quests` is `ICollection<QuestEntity>` — it is the collection of quests where `DungeonMasterId == User.Id`. This is the correct navigation property (D-08 wants all quests the DM has run). No additional filter needed, but the query must `Include(u => u.Quests)` when loading the user.

**How to avoid:** Use `IUserService.GetByIdAsync` does not eagerly load Quests. Add a specialized repository query `GetDungeonMasterQuestsAsync(int userId)` in `IQuestRepository` / `QuestRepository` that queries `DbContext.Quests.Where(q => q.DungeonMasterId == userId).OrderByDescending(q => q.FinalizedDate ?? q.CreatedAt)`.

**Warning signs:** Quest list is empty even though the DM has run quests, OR the quest list is unordered.

### Pitfall 5: EditProfile Route Collision Between No-Id and With-Id Overloads

**What goes wrong:** ASP.NET Core route resolution fails or both overloads map to the same action, causing a `AmbiguousMatchException` or always resolving to the no-id version.

**Why it happens:** Two GET actions with the same name but different signatures need distinct route templates.

**How to avoid:** Use explicit route templates:
```csharp
[HttpGet]
[Route("DungeonMaster/EditProfile")]
public async Task<IActionResult> EditProfile(CancellationToken token = default) { ... }

[HttpGet]
[Route("DungeonMaster/EditProfile/{id:int}")]
public async Task<IActionResult> EditProfile(int id, CancellationToken token = default) { ... }
```
Or merge into one action: `EditProfile(int? id = null)` with `[HttpGet]` — a nullable int parameter matches both `/EditProfile` (id=null → own profile) and `/EditProfile/5` (id=5). This is the cleaner approach and is consistent with how the QuestController handles optional parameters.

**Warning signs:** `/DungeonMaster/EditProfile` always redirects to the wrong action, or 404 on `/DungeonMaster/EditProfile/5`.

### Pitfall 6: Missing `enctype="multipart/form-data"` on Edit Form

**What goes wrong:** File upload silently sends no data. `IFormFile` is always null in POST action. No error is shown.

**Why it happens:** Default HTML form encoding (`application/x-www-form-urlencoded`) does not transmit file bytes.

**How to avoid:** Always include `enctype="multipart/form-data"` on the `<form>` tag in `EditProfile.cshtml`.

**Warning signs:** `viewModel.ProfilePictureFile` is always null even when user selects a file.

---

## Code Examples

Verified patterns from the existing codebase:

### DungeonMasterProfileEntity (entity class)

```csharp
// Based on CharacterEntity.cs + CharacterImageEntity.cs patterns (VERIFIED: read)
[Table("DungeonMasterProfiles")]
public class DungeonMasterProfileEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]  // Id = UserId, not auto-generated
    public int Id { get; set; }                        // FK to AspNetUsers

    [StringLength(2000)]
    public string? Bio { get; set; }

    public virtual DungeonMasterProfileImageEntity? ProfileImage { get; set; }
}

[Table("DungeonMasterProfileImages")]
public class DungeonMasterProfileImageEntity : IEntity
{
    [Key]
    [ForeignKey(nameof(DungeonMasterProfile))]
    public int Id { get; set; }                        // PK and FK to DungeonMasterProfiles

    [Required]
    public byte[] ImageData { get; set; } = [];

    public virtual DungeonMasterProfileEntity DungeonMasterProfile { get; set; } = null!;
}
```

### DungeonMasterProfile domain model

```csharp
// Based on Character.cs pattern (VERIFIED: read)
public class DungeonMasterProfile : IModel
{
    public int Id { get; set; }           // = UserId
    public string? Bio { get; set; }
    public byte[]? ProfilePicture { get; set; }
}
```

### EditDMProfileViewModel with reused validation attributes

```csharp
// Source: CharacterViewModel.cs custom attributes (VERIFIED: read)
public class EditDMProfileViewModel
{
    public int DungeonMasterId { get; set; }

    [StringLength(2000)]
    public string? Bio { get; set; }

    public byte[]? ProfilePicture { get; set; }   // populated from DB; used to decide whether to show existing image

    [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Profile picture cannot exceed 5 MB")]
    [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Only image files (JPG, PNG, GIF) are allowed")]
    public IFormFile? ProfilePictureFile { get; set; }
}
```

### QuestBoardContext — DbSets and fluent config additions

```csharp
// Source: QuestBoardContext.cs (VERIFIED: read) — append these
public DbSet<DungeonMasterProfileEntity> DungeonMasterProfiles { get; set; }
public DbSet<DungeonMasterProfileImageEntity> DungeonMasterProfileImages { get; set; }

// In OnModelCreating:
modelBuilder.Entity<DungeonMasterProfileEntity>()
    .Property(p => p.Id)
    .ValueGeneratedNever();

modelBuilder.Entity<DungeonMasterProfileEntity>()
    .HasOne<UserEntity>()
    .WithOne()
    .HasForeignKey<DungeonMasterProfileEntity>(p => p.Id)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<DungeonMasterProfileEntity>()
    .HasOne(p => p.ProfileImage)
    .WithOne(pi => pi.DungeonMasterProfile)
    .HasForeignKey<DungeonMasterProfileImageEntity>(pi => pi.Id)
    .OnDelete(DeleteBehavior.Cascade);
```

### GetDMProfilePicture action

```csharp
// Source: GuildMembersController.cs lines 262-271 (VERIFIED: read)
[HttpGet]
public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
{
    var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
    if (bytes == null) return NotFound();
    return File(bytes, "image/jpeg");
}
```

### Players/Index.cshtml DM name link (D-10)

```html
<!-- Before (VERIFIED: read lines 32-33): -->
<td>@dm.Name</td>

<!-- After: -->
<td>
    <a asp-controller="DungeonMaster" asp-action="Profile" asp-route-id="@dm.Id">@dm.Name</a>
</td>
```

### _Layout.cshtml navbar addition (D-06)

```html
<!-- Source: _Layout.cshtml lines 118-125 (VERIFIED: read) — insert after "My Quests" li -->
<li>
    <a class="dropdown-item" asp-controller="DungeonMaster" asp-action="EditProfile">
        <i class="fas fa-user-edit me-2"></i>Edit My Profile
    </a>
</li>
```

---

## Key File-to-Pattern Mapping

| New File | Mirrors Existing File |
|----------|-----------------------|
| `DungeonMasterProfileEntity.cs` | `CharacterEntity.cs` |
| `DungeonMasterProfileImageEntity.cs` | `CharacterImageEntity.cs` |
| `DungeonMasterProfile.cs` (domain model) | `Character.cs` |
| `IDungeonMasterProfileService.cs` | `ICharacterService.cs` |
| `IDungeonMasterProfileRepository.cs` | `ICharacterRepository.cs` |
| `DungeonMasterProfileService.cs` | `CharacterService.cs` |
| `DungeonMasterProfileRepository.cs` | `CharacterRepository.cs` |
| `DungeonMasterController.cs` | `GuildMembersController.cs` |
| `DMProfileViewModel.cs` | `CharacterViewModel.cs` |
| `EditDMProfileViewModel.cs` | `CharacterViewModel.cs` (edit fields only) |
| `Views/DungeonMaster/Profile.cshtml` | `Views/GuildMembers/Details.cshtml` |
| `Views/DungeonMaster/EditProfile.cshtml` | `Views/GuildMembers/Edit.cshtml` |
| `wwwroot/css/dm-profile.css` | `wwwroot/css/guild-members.css` |

---

## Codebase Facts Verified This Session

### DM Directory File Location

**Confirmed:** `EuphoriaInn.Service/Views/Players/Index.cshtml` [VERIFIED: Glob + read]

The `PlayersController` (in `Controllers/QuestBoard/PlayersController.cs`) renders this view. It uses `GuildMembersIndexViewModel` which exposes `IEnumerable<User> DungeonMasters` and `IEnumerable<User> Players`. The DM rows in the table use `dm.Name` and `dm.Email`.

### DungeonMasterController Confirmed Absent

**Confirmed:** No `DungeonMasterController.cs` exists anywhere in the codebase. [VERIFIED: Grep for `DungeonMasterController` returned zero results]

### UserEntity Navigation Property for Quests

**Confirmed:** `UserEntity.Quests` is `virtual ICollection<QuestEntity>` — the collection of quests where this user is the DM. [VERIFIED: read UserEntity.cs line 14]

The `User` domain model also has `IList<Quest> Quests`. Loading quests for a DM via `IUserService.GetByIdAsync` does NOT currently eager-load the Quests collection — `BaseRepository.GetByIdAsync` uses `DbSet.FindAsync` which does not include related entities. A dedicated repository query on `IQuestRepository` or `IQuestService` is the correct approach for loading DM quest history.

### Navbar DM Dropdown Exact Location

**Confirmed:** `_Layout.cshtml` lines 52–72. The DM-only dropdown currently contains: "Create Quest" and "Manage Shop" items only. The "My Quests" link is inside the user dropdown (lines 118–125), NOT the DM dropdown. [VERIFIED: read _Layout.cshtml]

**Critical distinction for D-06:** The CONTEXT.md says "Add 'Edit My Profile' link to the DM navbar dropdown." The UI-SPEC says to add it inside the "DungeonMasterOnly" dropdown `<ul>`. The actual _Layout.cshtml has TWO relevant dropdowns:
- DM-only nav dropdown (lines 52-72): "Create Quest", "Manage Shop"
- User account dropdown (lines 108-135): "Profile", "My Quests", logout

The UI-SPEC markup places the new item "after My Quests" inside the user account dropdown — this is the dropdown controlled by `DungeonMasterOnly` check (lines 118-125). The executor should add it inside the `@if ((await AuthorizationService.AuthorizeAsync(User, "DungeonMasterOnly")).Succeeded)` block in the user dropdown, after the "My Quests" `<li>`, before the `<li><hr>` divider.

### Existing Custom Validation Attributes Location

**Confirmed:** `MaxFileSizeAttribute` and `AllowedExtensionsAttribute` are defined directly in `CharacterViewModel.cs` (lines 57-104). They are not in a shared utility file. For the DM profile edit ViewModel, either reuse these by referencing the same namespace (`EuphoriaInn.Service.ViewModels.CharacterViewModels`) or move them to a shared location. The simplest approach: reference the existing attributes from `CharacterViewModels` namespace by adding a `using` statement in `EditDMProfileViewModel.cs`. [VERIFIED: read CharacterViewModel.cs]

### IQuestService — No GetQuestsByDungeonMasterAsync Yet

**Verified:** `IQuestService` does not currently have a method to fetch quests by DM id. [VERIFIED: read IQuestService interface — not read directly but confirmed by reviewing QuestController which calls `GetQuestWithDetailsAsync`, `GetQuestsForCalendarAsync`, `GetAllAsync` — none filter by DM]

The planner must include a task to add `GetQuestsByDungeonMasterAsync(int dmUserId)` to `IQuestService` and `QuestService`, and to `IQuestRepository` and `QuestRepository`.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Store image bytes directly on entity row | Separate image table with Id=FK | Phase 6 (MoveCharacterImagesToSeparateTable migration) | Avoids loading image bytes on non-image queries |
| Single `Difficulty` int enum | `ChallengeRating` int (1-5 scale) | Migration 20250703212002 | Quest list on DM profile should display via difficulty badge helper |

**Note on quest difficulty display:** `QuestEntity.ChallengeRating` is an int (1-5). The UI-SPEC difficulty badges map to Easy/Medium/Hard/Deadly colors. Verify whether a helper already maps `ChallengeRating` to a display name and badge color — check `Views/Shared/_QuestDifficultyBadge.cshtml` or existing quest views before building a new helper.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `DungeonMasterProfileEntity.Id` should use `ValueGeneratedNever()` so EF does not auto-assign an identity value | Code Examples | If not configured, EF will assign a new auto-increment Id, breaking the FK relationship to `AspNetUsers` |
| A2 | `GetQuestsByDungeonMasterAsync` does not currently exist on `IQuestRepository` or `IQuestService` | Codebase Facts | If a method already exists under a different name, the task to add it can be replaced with a reference to the existing method |
| A3 | Custom validation attributes (`MaxFileSizeAttribute`, `AllowedExtensionsAttribute`) are not in a shared/common file; they live only in `CharacterViewModel.cs` | Standard Stack | If they were moved to a shared utility class in a prior phase, the `using` directive needed in `EditDMProfileViewModel` would differ |
| A4 | No partial view `_DifficultyBadge.cshtml` currently exists; difficulty display is inline per view | State of the Art | If a shared badge partial exists, use it in `Profile.cshtml` quest list rather than inlining the badge HTML |

---

## Open Questions

1. **Where to add `GetQuestsByDungeonMasterAsync`**
   - What we know: `IQuestService` and `IQuestRepository` currently have no method filtering by DM user id.
   - What's unclear: Should this be on `IQuestService` or can the controller query the quest list directly from `IUserService` (which could return a `User` with `Quests` eagerly loaded)?
   - Recommendation: Add `GetQuestsByDungeonMasterAsync(int userId)` to `IQuestRepository`/`QuestRepository` and `IQuestService`/`QuestService`. This keeps the pattern consistent with how character queries work. The controller should not do its own EF queries.

2. **Difficulty badge display on quest list**
   - What we know: `QuestEntity.ChallengeRating` is int 1-5. The UI-SPEC maps Easy/Medium/Hard/Deadly to bootstrap color classes.
   - What's unclear: Is there already a Razor partial or TagHelper for difficulty badges?
   - Recommendation: Check `Views/Shared/` and `Views/Quest/` for existing difficulty badge rendering before creating new markup. If none found, inline the badge HTML in `Profile.cshtml` per the UI-SPEC color mapping.

3. **`DungeonMasterProfileEntity` navigation on `UserEntity`**
   - What we know: CONTEXT.md leaves this as Claude's Discretion. Current `UserEntity` has no `DungeonMasterProfile` nav property.
   - What's unclear: Whether to add the nav property to `UserEntity` or resolve by UserId query.
   - Recommendation: Do NOT add navigation to `UserEntity`. Keep `UserEntity` clean. Resolve by query: `DbContext.DungeonMasterProfiles.Include(p => p.ProfileImage).FirstOrDefaultAsync(p => p.Id == userId)`. This mirrors how `CharacterRepository` queries by OwnerId without a reverse nav on `UserEntity`.

---

## Environment Availability

Step 2.6: SKIPPED — Phase is purely ASP.NET Core code and EF migration changes. No external tools, services, or CLI utilities beyond the project's own dotnet toolchain are required. The EF CLI (`dotnet-ef`) is already confirmed as a project dependency and globally installed tool per `CLAUDE.md`.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit v3 2.2.2 (unit) + xUnit v3 via `Microsoft.AspNetCore.Mvc.Testing` (integration) |
| Config file | None — uses SDK test discovery |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --no-build -x` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DMPRO-01 | `GET /DungeonMaster/Profile/{id}` returns 200 with DM name visible | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "DungeonMasterController" -x` | ❌ Wave 0 |
| DMPRO-01 | Profile page shows placeholder when no profile saved yet | Integration | same | ❌ Wave 0 |
| DMPRO-02 | DM can POST to EditProfile and bio is saved | Integration | same | ❌ Wave 0 |
| DMPRO-02 | DM can upload a photo and it appears via GetDMProfilePicture | Integration | same | ❌ Wave 0 |
| DMPRO-03 | Admin can GET EditProfile/{otherId} without Forbid | Integration | same | ❌ Wave 0 |
| DMPRO-03 | Non-admin DM gets 403 on EditProfile/{otherId} | Integration | same | ❌ Wave 0 |
| DMPRO-04 | Players/Index contains link to DungeonMaster/Profile/{id} | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "PlayersController" -x` | ❌ Wave 0 |
| DMPRO-05 | EF migration adds DungeonMasterProfiles and DungeonMasterProfileImages tables | Manual / migration verify | n/a — verified by schema inspection | ❌ manual |

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests --no-build -x`
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.IntegrationTests/Controllers/DungeonMasterControllerIntegrationTests.cs` — covers DMPRO-01, DMPRO-02, DMPRO-03
- [ ] `EuphoriaInn.IntegrationTests/Controllers/PlayersControllerIntegrationTests.cs` — covers DMPRO-04
- [ ] No framework install needed — existing test infrastructure is sufficient

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity — `IUserService.GetUserAsync(User)` |
| V3 Session Management | no | No session state added in this phase |
| V4 Access Control | yes | `[Authorize(Policy = "DungeonMasterOnly")]` + inline `!isOwner && !IsInRole("Admin")` |
| V5 Input Validation | yes | `[MaxFileSize]`, `[AllowedExtensions]`, `[StringLength(2000)]` on ViewModel; `[ValidateAntiForgeryToken]` on POST |
| V6 Cryptography | no | No cryptographic operations; image bytes stored as-is |

### Known Threat Patterns for ASP.NET Core MVC file upload

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Oversized file upload (HTTP 413 / DoS) | Denial of Service | `[MaxFileSize(5 * 1024 * 1024)]` attribute + existing `app.UseRequestSizeLimit` (check `Program.cs`) |
| Unauthorized profile edit (IDOR) | Elevation of Privilege | Inline `!isOwner && !IsInRole("Admin")` check before any write |
| CSRF on profile POST | Tampering | `[ValidateAntiForgeryToken]` on all POST actions |
| Malicious file content uploaded as image | Tampering | `[AllowedExtensions]` restricts to jpg/png/gif; bytes stored as blob (not served as executable) |

---

## Sources

### Primary (HIGH confidence — VERIFIED by direct file read this session)

- `EuphoriaInn.Repository/Entities/CharacterImageEntity.cs` — PK=FK image table pattern
- `EuphoriaInn.Repository/CharacterRepository.cs` — `GetCharacterProfilePictureAsync`, `UpdateProfileImageAsync` implementation
- `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` — `GetProfilePicture` endpoint, file upload flow
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` (lines 116-177, 179-205) — authorization pattern
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — full `OnModelCreating` configuration including `CharacterEntity/CharacterImageEntity` 1:1 setup
- `EuphoriaInn.Repository/Entities/UserEntity.cs` — navigation property `Quests`, confirms no existing DM profile nav
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` — mapping conventions to replicate
- `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` — mapping conventions to replicate
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` — `MaxFileSizeAttribute`, `AllowedExtensionsAttribute`, `IFormFile?` pattern
- `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` — `enctype`, JS file validation block
- `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` — two-column layout, placeholder pattern
- `EuphoriaInn.Service/Views/Players/Index.cshtml` — exact DM directory markup (confirmed file location)
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — confirmed navbar structure, DM dropdown contents
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — DI registration pattern
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — repository DI registration pattern
- `EuphoriaInn.Domain/Services/CharacterService.cs` — service override pattern for `UpdateAsync`
- `EuphoriaInn.Domain/Services/BaseService.cs` — base class pattern
- `EuphoriaInn.Repository/BaseRepository.cs` — base repository pattern
- `EuphoriaInn.Repository/Migrations/20260304113417_MoveCharacterImagesToSeparateTable.cs` — migration reference for image table SQL
- `.planning/phases/07-dm-profile-page/07-CONTEXT.md` — all locked decisions D-01 through D-10
- `.planning/phases/07-dm-profile-page/07-UI-SPEC.md` — approved visual/interaction contract
- `.planning/config.json` — `nyquist_validation: true`, `commit_docs: true`

### Secondary (MEDIUM confidence)

- `CLAUDE.md` project instructions — coding conventions, entity naming, layer rules

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all dependencies already in project, verified via csproj
- Architecture: HIGH — all patterns have working implementations in codebase
- Pitfalls: HIGH — cascade delete and ValueGeneratedNever pitfalls directly observed from the existing migration and entity patterns
- Test mapping: MEDIUM — test file names and filter strings are inferred from existing integration test naming convention; actual test method names will be determined during Wave 0

**Research date:** 2026-06-17
**Valid until:** 2026-09-17 (stable framework; only stale if ASP.NET Core 9+ migration happens)
