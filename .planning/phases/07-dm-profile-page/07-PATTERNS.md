# Phase 7: DM Profile Page - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 20 (13 new + 7 modified)
**Analogs found:** 20 / 20

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs` | model/entity | CRUD | `EuphoriaInn.Repository/Entities/CharacterEntity.cs` | exact |
| `EuphoriaInn.Repository/Entities/DungeonMasterProfileImageEntity.cs` | model/entity | CRUD | `EuphoriaInn.Repository/Entities/CharacterImageEntity.cs` | exact |
| `EuphoriaInn.Domain/Models/DungeonMasterProfile.cs` | model | CRUD | `EuphoriaInn.Domain/Models/Character.cs` | exact |
| `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs` | service interface | CRUD | `EuphoriaInn.Domain/Interfaces/ICharacterService.cs` | exact |
| `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileRepository.cs` | repository interface | CRUD | `EuphoriaInn.Domain/Interfaces/ICharacterRepository.cs` | exact |
| `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs` | service | CRUD + file-I/O | `EuphoriaInn.Domain/Services/CharacterService.cs` | exact |
| `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs` | repository | CRUD + file-I/O | `EuphoriaInn.Repository/CharacterRepository.cs` | exact |
| `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs` | controller | request-response + file-I/O | `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` | exact |
| `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/DMProfileViewModel.cs` | view model | request-response | `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` | role-match |
| `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/EditDMProfileViewModel.cs` | view model | request-response + file-I/O | `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` | exact |
| `EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` | exact |
| `EuphoriaInn.Service/Views/DungeonMaster/EditProfile.cshtml` | view | request-response + file-I/O | `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` | exact |
| `EuphoriaInn.Service/wwwroot/css/dm-profile.css` | config/style | — | `EuphoriaInn.Service/wwwroot/css/guild-members.css` | role-match |
| `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` | config | CRUD | `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` (existing) | self |
| `EuphoriaInn.Repository/Automapper/EntityProfile.cs` | config | transform | `EuphoriaInn.Repository/Automapper/EntityProfile.cs` (existing Character block) | self |
| `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` | config | — | `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` (existing) | self |
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | config | — | `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` (existing) | self |
| `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` | config | transform | `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` (existing Character block) | self |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (existing DM block) | self |
| `EuphoriaInn.Service/Views/Players/Index.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Players/Index.cshtml` (existing DM row) | self |

---

## Pattern Assignments

### `EuphoriaInn.Repository/Entities/DungeonMasterProfileEntity.cs` (entity, CRUD)

**Analog:** `EuphoriaInn.Repository/Entities/CharacterEntity.cs`

**Critical difference from analog:** `CharacterEntity` uses `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` on its `Id` because it is an independent entity with a surrogate PK. `DungeonMasterProfileEntity.Id` must be `[DatabaseGenerated(DatabaseGeneratedOption.None)]` because `Id = UserId` — EF must not auto-generate it.

**Imports pattern** (CharacterEntity.cs lines 1-4):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;
```

**Core entity pattern** — adapted from CharacterEntity.cs + CharacterImageEntity.cs:
```csharp
[Table("DungeonMasterProfiles")]
public class DungeonMasterProfileEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]   // Id = UserId, not auto-generated
    public int Id { get; set; }                         // FK to AspNetUsers

    [StringLength(2000)]
    public string? Bio { get; set; }

    public virtual DungeonMasterProfileImageEntity? ProfileImage { get; set; }
}
```

---

### `EuphoriaInn.Repository/Entities/DungeonMasterProfileImageEntity.cs` (entity, CRUD)

**Analog:** `EuphoriaInn.Repository/Entities/CharacterImageEntity.cs` (lines 1-17)

**Imports pattern** (lines 1-4):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;
```

**Core entity pattern** — copy verbatim from CharacterImageEntity.cs lines 6-17, renaming types:
```csharp
[Table("DungeonMasterProfileImages")]
public class DungeonMasterProfileImageEntity : IEntity
{
    [Key]
    [ForeignKey(nameof(DungeonMasterProfile))]
    public int Id { get; set; }             // PK and FK to DungeonMasterProfiles

    [Required]
    public byte[] ImageData { get; set; } = [];

    public virtual DungeonMasterProfileEntity DungeonMasterProfile { get; set; } = null!;
}
```

---

### `EuphoriaInn.Domain/Models/DungeonMasterProfile.cs` (domain model, CRUD)

**Analog:** `EuphoriaInn.Domain/Models/Character.cs` (lines 1-14)

**Imports pattern** (lines 1-3):
```csharp
namespace EuphoriaInn.Domain.Models;
```

**Core model pattern** — stripped-down Character.cs shape, no enums or collections needed:
```csharp
public class DungeonMasterProfile : IModel
{
    public int Id { get; set; }             // = UserId
    public string? Bio { get; set; }
    public byte[]? ProfilePicture { get; set; }
}
```

---

### `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs` (service interface, CRUD)

**Analog:** `EuphoriaInn.Domain/Interfaces/ICharacterService.cs` (lines 1-14)

**Imports pattern** (lines 1-3):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;
```

**Core interface pattern** — trimmed to only DM profile operations:
```csharp
public interface IDungeonMasterProfileService : IBaseService<DungeonMasterProfile>
{
    Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default);
    Task UpsertProfileAsync(int userId, string? bio, byte[]? imageBytes, CancellationToken token = default);
    Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default);
}
```

---

### `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileRepository.cs` (repository interface, CRUD)

**Analog:** `EuphoriaInn.Domain/Interfaces/ICharacterRepository.cs` (lines 1-13)

**Imports pattern** (lines 1-3):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;
```

**Core interface pattern** — mirrors ICharacterRepository, scoped to DM profile operations:
```csharp
public interface IDungeonMasterProfileRepository : IBaseRepository<DungeonMasterProfile>
{
    Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default);
    Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default);
    Task UpsertProfileImageAsync(int userId, byte[]? imageData, CancellationToken token = default);
}
```

---

### `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs` (service, CRUD + file-I/O)

**Analog:** `EuphoriaInn.Domain/Services/CharacterService.cs` (lines 1-70)

**Imports pattern** (lines 1-4):
```csharp
using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;
```

**Core service pattern** — class declaration mirrors CharacterService.cs line 8:
```csharp
internal class DungeonMasterProfileService(IDungeonMasterProfileRepository repository, IMapper mapper)
    : BaseService<DungeonMasterProfile>(repository, mapper), IDungeonMasterProfileService
```

**`UpdateAsync` override pattern** (CharacterService.cs lines 60-64) — override to handle image upsert before property update:
```csharp
public override async Task UpdateAsync(DungeonMasterProfile model, CancellationToken token = default)
{
    await repository.UpsertProfileImageAsync(model.Id, model.ProfilePicture, token);
    await repository.UpdateAsync(model, token);
}
```

**Upsert pattern** for lazy-create (D-03):
```csharp
public async Task UpsertProfileAsync(int userId, string? bio, byte[]? imageBytes, CancellationToken token = default)
{
    var profile = await repository.GetProfileByUserIdAsync(userId, token);
    if (profile == null)
    {
        profile = new DungeonMasterProfile { Id = userId, Bio = bio, ProfilePicture = imageBytes };
        await repository.AddAsync(profile, token);
    }
    else
    {
        profile.Bio = bio;
        if (imageBytes != null) profile.ProfilePicture = imageBytes;
        await repository.UpdateAsync(profile, token);
    }
}
```

---

### `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs` (repository, CRUD + file-I/O)

**Analog:** `EuphoriaInn.Repository/CharacterRepository.cs` (lines 1-91)

**Imports pattern** (lines 1-6):
```csharp
using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;
```

**Class declaration pattern** (line 9):
```csharp
internal class DungeonMasterProfileRepository(QuestBoardContext dbContext, IMapper mapper)
    : BaseRepository<DungeonMasterProfile, DungeonMasterProfileEntity>(dbContext, mapper), IDungeonMasterProfileRepository
```

**`GetProfileByUserIdAsync` pattern** — mirrors CharacterRepository's single-entity eager-load query (lines 38-46):
```csharp
public async Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default)
{
    var entity = await DbContext.DungeonMasterProfiles
        .Include(p => p.ProfileImage)
        .FirstOrDefaultAsync(p => p.Id == userId, token);
    return entity == null ? null : Mapper.Map<DungeonMasterProfile>(entity);
}
```

**`GetProfilePictureAsync` pattern** — mirrors CharacterRepository.GetCharacterProfilePictureAsync (lines 57-63):
```csharp
public async Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default)
{
    return await DbContext.DungeonMasterProfileImages
        .Where(p => p.Id == userId)
        .Select(p => p.ImageData)
        .FirstOrDefaultAsync(token);
}
```

**`UpsertProfileImageAsync` pattern** — copy verbatim from CharacterRepository.UpdateProfileImageAsync (lines 65-90), renaming entity types:
```csharp
public async Task UpsertProfileImageAsync(int userId, byte[]? imageData, CancellationToken token = default)
{
    var entity = await DbContext.DungeonMasterProfiles
        .Include(p => p.ProfileImage)
        .FirstOrDefaultAsync(p => p.Id == userId, token);
    if (entity == null) return;

    if (imageData == null)
    {
        entity.ProfileImage = null;
    }
    else if (entity.ProfileImage == null)
    {
        entity.ProfileImage = new DungeonMasterProfileImageEntity
        {
            Id = entity.Id,
            ImageData = imageData
        };
    }
    else
    {
        entity.ProfileImage.ImageData = imageData;
    }

    await DbContext.SaveChangesAsync(token);
}
```

---

### `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs` (controller, request-response + file-I/O)

**Analog:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` (lines 1-273)

**Imports pattern** (GuildMembersController.cs lines 1-8):
```csharp
using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Service.ViewModels.DungeonMasterViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.DungeonMaster;
```

**Class declaration with primary constructor** (GuildMembersController.cs lines 11-16):
```csharp
[Authorize]
public class DungeonMasterController(
    IDungeonMasterProfileService dmProfileService,
    IUserService userService,
    IQuestService questService,
    IMapper mapper) : Controller
```

**`Profile` GET action pattern** — mirrors GuildMembersController.Details (lines 47-61), with null-safe profile handling per D-03:
```csharp
[HttpGet]
public async Task<IActionResult> Profile(int id, CancellationToken token = default)
{
    var user = await userService.GetByIdAsync(id, token);
    if (user == null) return NotFound();

    var profile = await dmProfileService.GetProfileByUserIdAsync(id, token);
    var quests = await questService.GetQuestsByDungeonMasterAsync(id, token);

    var currentUser = await userService.GetUserAsync(User);
    var viewModel = new DMProfileViewModel
    {
        UserId = id,
        Name = user.Name,
        Bio = profile?.Bio,
        HasProfilePicture = profile?.ProfilePicture != null,
        Quests = mapper.Map<List<QuestSummaryViewModel>>(quests),
        CanEdit = currentUser != null && (currentUser.Id == id || User.IsInRole("Admin"))
    };

    return View(viewModel);
}
```

**`EditProfile` GET action pattern** — combines GuildMembersController.Edit auth check (lines 124-143) with QuestController.Edit ownership check (lines 81-92, DungeonMasterOnly policy):
```csharp
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

    var profile = await dmProfileService.GetProfileByUserIdAsync(targetUserId, token);
    var viewModel = new EditDMProfileViewModel
    {
        DungeonMasterId = targetUserId,
        Bio = profile?.Bio,
        ProfilePicture = profile?.ProfilePicture
    };

    return View(viewModel);
}
```

**`EditProfile` POST action pattern** — IFormFile → byte[] upload mirrors GuildMembersController.Edit POST (lines 145-212):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> EditProfile(int? id, EditDMProfileViewModel viewModel, CancellationToken token = default)
{
    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null) return Challenge();

    var targetUserId = id ?? currentUser.Id;
    if (targetUserId != currentUser.Id && !User.IsInRole("Admin"))
        return Forbid();

    if (!ModelState.IsValid)
        return View(viewModel);

    byte[]? imageBytes = null;
    if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
    {
        using var memoryStream = new MemoryStream();
        await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
        imageBytes = memoryStream.ToArray();
    }

    await dmProfileService.UpsertProfileAsync(targetUserId, viewModel.Bio, imageBytes, token);

    return RedirectToAction(nameof(Profile), new { id = targetUserId });
}
```

**`GetDMProfilePicture` action pattern** — copy verbatim from GuildMembersController.GetProfilePicture (lines 261-271):
```csharp
[HttpGet]
public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
{
    var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
    if (bytes == null) return NotFound();
    return File(bytes, "image/jpeg");
}
```

---

### `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/DMProfileViewModel.cs` (view model, request-response)

**Analog:** `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` (lines 1-44)

**Imports pattern**:
```csharp
using EuphoriaInn.Service.ViewModels.QuestViewModels;

namespace EuphoriaInn.Service.ViewModels.DungeonMasterViewModels;
```

**Core view model** — read-only display model for Profile.cshtml:
```csharp
public class DMProfileViewModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public bool HasProfilePicture { get; set; }
    public bool CanEdit { get; set; }
    public List<QuestSummaryViewModel> Quests { get; set; } = [];
}
```

**`QuestSummaryViewModel`** for the quest list on the profile page (D-08, D-09):
```csharp
public class QuestSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int ChallengeRating { get; set; }
}
```

---

### `EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/EditDMProfileViewModel.cs` (view model, request-response + file-I/O)

**Analog:** `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` (lines 1-44)

**Critical note:** `MaxFileSizeAttribute` and `AllowedExtensionsAttribute` are defined in `CharacterViewModel.cs` (lines 57-104) in the `EuphoriaInn.Service.ViewModels.CharacterViewModels` namespace. Reference them with a `using` statement — do NOT duplicate them.

**Imports pattern**:
```csharp
using EuphoriaInn.Service.ViewModels.CharacterViewModels;   // for MaxFileSize + AllowedExtensions
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.DungeonMasterViewModels;
```

**Core view model** — mirrors CharacterViewModel file upload fields (lines 35-43):
```csharp
public class EditDMProfileViewModel
{
    public int DungeonMasterId { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    public byte[]? ProfilePicture { get; set; }   // existing image; drives current-image display

    [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Profile picture cannot exceed 5 MB")]
    [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Only image files (JPG, PNG, GIF) are allowed")]
    public IFormFile? ProfilePictureFile { get; set; }
}
```

---

### `EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` (lines 1-163)

**Model directive and layout pattern** (Details.cshtml lines 1-8):
```html
@using EuphoriaInn.Service.ViewModels.DungeonMasterViewModels
@model DMProfileViewModel
@{
    ViewData["Title"] = Model.Name;
}
```

**Two-column layout pattern** (Details.cshtml lines 10-12, 88-90) — left col for photo+actions, right col for details:
```html
<div class="dm-profile-page">
    <div class="row">
        <div class="col-lg-4">
            <!-- Photo + Edit button (when CanEdit) -->
        </div>
        <div class="col-lg-8">
            <!-- Bio + Quest list -->
        </div>
    </div>
</div>
```

**Photo with placeholder pattern** (Details.cshtml lines 16-26):
```html
@if (Model.HasProfilePicture)
{
    <img src="@Url.Action("GetDMProfilePicture", new { id = Model.UserId })"
         alt="@Model.Name" class="dm-portrait img-fluid mb-3" />
}
else
{
    <div class="dm-portrait-placeholder mb-3">
        <i class="fas fa-crown fa-5x text-muted"></i>
    </div>
}
```

**Conditional Edit button pattern** (Details.cshtml lines 53-87) — show only to owner and admin:
```html
@if (Model.CanEdit)
{
    <div class="card modern-card">
        <div class="card-header modern-card-header">
            <h5 class="mb-0">Actions</h5>
        </div>
        <div class="card-body modern-card-body">
            <a asp-action="EditProfile" asp-route-id="@Model.UserId" class="btn btn-warning w-100">
                <i class="fas fa-user-edit me-2"></i>Edit Profile
            </a>
        </div>
    </div>
}
```

**Quest list section** (right column, per D-08/D-09):
```html
<div class="card modern-card mb-3">
    <div class="card-header modern-card-header">
        <h4 class="mb-0">
            <i class="fas fa-scroll text-warning me-2"></i>Quests Run
        </h4>
    </div>
    <div class="card-body modern-card-body">
        @foreach (var quest in Model.Quests)
        {
            <a asp-controller="Quest" asp-action="Details" asp-route-id="@quest.Id"
               class="d-flex justify-content-between align-items-center py-2 border-bottom text-decoration-none">
                <span>@quest.Title</span>
                <span class="text-muted small">@quest.Date?.ToString("MMM d, yyyy")</span>
            </a>
        }
    </div>
</div>
```

---

### `EuphoriaInn.Service/Views/DungeonMaster/EditProfile.cshtml` (view, request-response + file-I/O)

**Analog:** `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` (lines 1-214)

**Form declaration with multipart pattern** (Edit.cshtml lines 18-19):
```html
<form asp-action="EditProfile" asp-route-id="@Model.DungeonMasterId" method="post" enctype="multipart/form-data">
    <input type="hidden" asp-for="DungeonMasterId" />
```

**Card header pattern** (Edit.cshtml lines 10-16):
```html
<div class="card modern-card">
    <div class="card-header modern-card-header">
        <h2 class="mb-0">
            <i class="fas fa-user-edit me-2"></i>
            Edit DM Profile
        </h2>
    </div>
    <div class="card-body modern-card-body">
```

**File upload with current-image preview pattern** (Edit.cshtml lines 26-36):
```html
<label asp-for="ProfilePictureFile" class="form-label">Profile Picture</label>
@if (Model.ProfilePicture != null)
{
    <div class="mb-2">
        <img src="@Url.Action("GetDMProfilePicture", new { id = Model.DungeonMasterId })"
             alt="Current" class="img-thumbnail" style="max-width: 200px;" />
    </div>
}
<input type="file" asp-for="ProfilePictureFile" class="form-control"
       accept="image/*" id="profilePictureInput" />
<small class="form-text text-muted">Upload a profile photo (JPG, PNG, GIF - Max 5 MB)</small>
<span asp-validation-for="ProfilePictureFile" class="text-danger"></span>
<div id="fileSizeError" class="text-danger" style="display: none;"></div>
```

**Client-side JS validation block** (Edit.cshtml lines 141-174) — copy verbatim, only the `profilePictureInput` listener section (no class management JS needed):
```html
@section Scripts {
    <script>
        const MAX_FILE_SIZE = 5 * 1024 * 1024;
        const ALLOWED_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

        document.getElementById('profilePictureInput')?.addEventListener('change', function(e) {
            const file = e.target.files[0];
            const errorDiv = document.getElementById('fileSizeError');
            if (file) {
                if (file.size > MAX_FILE_SIZE) {
                    errorDiv.textContent = `File size exceeds 5 MB. Please choose a smaller image.`;
                    errorDiv.style.display = 'block';
                    e.target.value = '';
                    return;
                }
                if (!ALLOWED_TYPES.includes(file.type)) {
                    errorDiv.textContent = 'Only image files (JPG, PNG, GIF) are allowed.';
                    errorDiv.style.display = 'block';
                    e.target.value = '';
                    return;
                }
                errorDiv.style.display = 'none';
                errorDiv.textContent = '';
            }
        });
    </script>
}
```

**Button row pattern** (Edit.cshtml lines 127-134, per CLAUDE.md UI guidelines):
```html
<hr />
<div class="d-flex justify-content-between">
    <a asp-action="Profile" asp-route-id="@Model.DungeonMasterId" class="btn btn-secondary">
        <i class="fas fa-arrow-left me-2"></i>Cancel
    </a>
    <button type="submit" class="btn btn-warning">
        <i class="fas fa-save me-2"></i>Save Changes
    </button>
</div>
```

---

### `EuphoriaInn.Service/wwwroot/css/dm-profile.css` (style, —)

**Analog:** `EuphoriaInn.Service/wwwroot/css/guild-members.css` (lines 1-281)

**Structure pattern** — scope all rules under a `.dm-profile-page` parent class (mirrors `.guild-members-page` scope in guild-members.css). Copy the portrait, placeholder, and card theming blocks, adapting class names:
- `.dm-portrait` — mirrors `.guild-members-page .character-image` image dimensions + border-radius
- `.dm-portrait-placeholder` — mirrors `.guild-members-page .character-placeholder` flex centering + D&D gold color
- Cinzel font for DM name heading; `#F4E4BC` text color per project palette

**CSS variable reference from guild-members.css lines 19-26**:
```css
/* Base card theming — copy from guild-members.css and adapt class names */
.dm-profile-page .dm-portrait {
    max-width: 250px;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
}

.dm-profile-page .dm-portrait-placeholder {
    width: 250px;
    height: 250px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, rgba(139, 69, 19, 0.3), rgba(160, 82, 45, 0.3));
    color: rgba(255, 193, 7, 0.4);
    border-radius: 8px;
}
```

---

## Modified Files — Pattern Assignments

### `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — Add DbSets + fluent config

**Analog:** `QuestBoardContext.cs` existing Character block (lines 21-22 for DbSets; lines 100-117 for fluent config)

**DbSet additions** (after line 23, following CharacterImages pattern):
```csharp
public DbSet<DungeonMasterProfileEntity> DungeonMasterProfiles { get; set; }
public DbSet<DungeonMasterProfileImageEntity> DungeonMasterProfileImages { get; set; }
```

**`OnModelCreating` additions** — insert after the CharacterEntity block (after line 123). Two relationships required:

1. UserEntity → DungeonMasterProfileEntity (1:1, `Cascade` delete — safe because single-path, mirrors `CharacterEntity → Owner` at lines 101-105):
```csharp
modelBuilder.Entity<DungeonMasterProfileEntity>()
    .Property(p => p.Id)
    .ValueGeneratedNever();

modelBuilder.Entity<DungeonMasterProfileEntity>()
    .HasOne<UserEntity>()
    .WithOne()
    .HasForeignKey<DungeonMasterProfileEntity>(p => p.Id)
    .OnDelete(DeleteBehavior.Cascade);
```

2. DungeonMasterProfileEntity → DungeonMasterProfileImageEntity (1:1, `Cascade` delete — copy CharacterEntity image relationship at lines 113-117):
```csharp
modelBuilder.Entity<DungeonMasterProfileEntity>()
    .HasOne(p => p.ProfileImage)
    .WithOne(pi => pi.DungeonMasterProfile)
    .HasForeignKey<DungeonMasterProfileImageEntity>(pi => pi.Id)
    .OnDelete(DeleteBehavior.Cascade);
```

---

### `EuphoriaInn.Repository/Automapper/EntityProfile.cs` — Add DM profile mappings

**Analog:** EntityProfile.cs Character mapping block (lines 84-107)

**Addition** — insert after the CharacterClass mapping block (after line 107):
```csharp
// DungeonMasterProfile mapping
CreateMap<DungeonMasterProfile, DungeonMasterProfileEntity>()
    .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfilePicture == null
        ? null
        : new DungeonMasterProfileImageEntity
        {
            ImageData = src.ProfilePicture
        }));

CreateMap<DungeonMasterProfileEntity, DungeonMasterProfile>()
    .ForMember(dest => dest.ProfilePicture, opt => opt.MapFrom(src => src.ProfileImage != null
        ? src.ProfileImage.ImageData
        : null));
```

---

### `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — Register IDungeonMasterProfileRepository

**Analog:** ServiceExtensions.cs line 24 (`ICharacterRepository` registration):
```csharp
services.AddScoped<ICharacterRepository, CharacterRepository>();
```

**Addition** — insert after line 24, following identical pattern:
```csharp
services.AddScoped<IDungeonMasterProfileRepository, DungeonMasterProfileRepository>();
```

---

### `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — Register IDungeonMasterProfileService

**Analog:** ServiceExtensions.cs line 21 (`ICharacterService` registration):
```csharp
services.AddScoped<ICharacterService, CharacterService>();
```

**Addition** — insert after line 21:
```csharp
services.AddScoped<IDungeonMasterProfileService, DungeonMasterProfileService>();
```

---

### `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` — Add DM profile ViewModel mappings

**Analog:** ViewModelProfile.cs Character mapping block (lines 61-73)

**Addition** — insert after the CharacterClass mapping block (after line 73):
```csharp
// DungeonMasterProfile mappings
CreateMap<DungeonMasterProfile, DMProfileViewModel>()
    .ForMember(dest => dest.HasProfilePicture, opt => opt.MapFrom(src => src.ProfilePicture != null))
    .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
    .ForMember(dest => dest.Quests, opt => opt.Ignore());

CreateMap<EditDMProfileViewModel, DungeonMasterProfile>()
    .ForMember(dest => dest.ProfilePicture, opt => opt.Ignore());   // set from IFormFile in controller

// Quest summary for DM profile page quest list
CreateMap<Quest, QuestSummaryViewModel>()
    .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.FinalizedDate ?? (src.ProposedDates.Any() ? src.ProposedDates.Min(pd => pd.Date) : (DateTime?)null)));
```

---

### `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — Add navbar link + CSS link

**CSS link addition** (after line 15, following guild-members.css pattern):
```html
<link rel="stylesheet" href="~/css/dm-profile.css" asp-append-version="true" />
```

**Navbar DM dropdown addition** (insert after line 67, inside the DungeonMasterOnly `<ul>`, before the closing `</ul>` at line 70). This places "Edit My Profile" at the bottom of the DM-only dropdown (Create Quest, Manage Shop, then Edit My Profile):
```html
<li>
    <a class="dropdown-item" asp-controller="DungeonMaster" asp-action="EditProfile">
        <i class="fas fa-user-edit me-2"></i>Edit My Profile
    </a>
</li>
```

**User account dropdown addition** (D-06 per UI-SPEC — insert after line 124, after "My Quests" `<li>`, before `<li><hr>`):
```html
<li>
    <a class="dropdown-item" asp-controller="DungeonMaster" asp-action="EditProfile">
        <i class="fas fa-user-edit me-2"></i>Edit My Profile
    </a>
</li>
```

**Note:** RESEARCH.md §Codebase Facts clarifies the correct placement is inside the `@if ((await AuthorizationService.AuthorizeAsync(User, "DungeonMasterOnly")).Succeeded)` block in the user account dropdown (lines 118-125), after the "My Quests" `<li>`, before the `<li><hr>` divider. Use this placement, not the DM-only nav dropdown.

---

### `EuphoriaInn.Service/Views/Players/Index.cshtml` — Wrap DM name in profile link (D-10)

**Analog:** Players/Index.cshtml line 32:
```html
<td>@dm.Name</td>
```

**Replacement** — wrap in anchor using Tag Helpers:
```html
<td>
    <a asp-controller="DungeonMaster" asp-action="Profile" asp-route-id="@dm.Id">@dm.Name</a>
</td>
```

**Note:** `dm.Id` on the `User` model is the user's `Id` (int), which is the FK value that `DungeonMasterProfileEntity.Id` mirrors. Confirm the model type for `dm` in `GuildMembersIndexViewModel.DungeonMasters` — it is `IEnumerable<User>` per RESEARCH.md, so `dm.Id` is correct.

---

## New Service Method Required

### `IQuestService.GetQuestsByDungeonMasterAsync` and `IQuestRepository`

**Confirmed absent:** `IQuestService` (verified lines 1-42) has no method filtering quests by DM user id. `GetQuestsByDmNameAsync` filters by name string, not by user id.

**Interface addition to `IQuestService.cs`**:
```csharp
Task<IList<Quest>> GetQuestsByDungeonMasterAsync(int dmUserId, CancellationToken token = default);
```

**Interface addition to `IQuestRepository.cs`** — follow same shape as other repository-level query methods.

**Repository implementation pattern** — mirrors CharacterRepository.GetCharactersByOwnerIdAsync (lines 24-36):
```csharp
public async Task<IList<Quest>> GetQuestsByDungeonMasterAsync(int dmUserId, CancellationToken token = default)
{
    var entities = await DbContext.Quests
        .Include(q => q.DungeonMaster)
        .Where(q => q.DungeonMasterId == dmUserId)
        .OrderByDescending(q => q.FinalizedDate ?? q.CreatedAt)
        .ToListAsync(token);
    return Mapper.Map<IList<Quest>>(entities);
}
```

**Service delegation pattern** — mirrors CharacterService.GetCharactersByOwnerIdAsync (lines 15-18):
```csharp
public async Task<IList<Quest>> GetQuestsByDungeonMasterAsync(int dmUserId, CancellationToken token = default)
{
    return await repository.GetQuestsByDungeonMasterAsync(dmUserId, token);
}
```

---

## Shared Patterns

### Authorization: DungeonMasterOnly Policy + Inline Ownership Check
**Source:** `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` lines 71-92 (Edit action)
**Apply to:** `DungeonMasterController.EditProfile` GET and POST actions

```csharp
[HttpGet]
[Authorize(Policy = "DungeonMasterOnly")]
public async Task<IActionResult> EditProfile(int? id, ...)
{
    var currentUser = await userService.GetUserAsync(User);
    if (currentUser == null) return Challenge();

    var targetUserId = id ?? currentUser.Id;
    if (targetUserId != currentUser.Id && !User.IsInRole("Admin"))
        return Forbid();
    // ... proceed
}
```

### IFormFile → byte[] Upload
**Source:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` lines 111-116 (Create POST)
**Apply to:** `DungeonMasterController.EditProfile` POST action

```csharp
if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
{
    using var memoryStream = new MemoryStream();
    await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
    imageBytes = memoryStream.ToArray();
}
```

### Image Serving Action
**Source:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` lines 261-271
**Apply to:** `DungeonMasterController.GetDMProfilePicture`

```csharp
[HttpGet]
public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
{
    var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
    if (bytes == null) return NotFound();
    return File(bytes, "image/jpeg");
}
```

### Image Upsert (Lazy-Create)
**Source:** `EuphoriaInn.Repository/CharacterRepository.cs` lines 65-90 (`UpdateProfileImageAsync`)
**Apply to:** `DungeonMasterProfileRepository.UpsertProfileImageAsync`

Pattern: check if `entity.ProfileImage == null` → create new with `Id = entity.Id`; else update `entity.ProfileImage.ImageData`. Never set image bytes directly on the owning entity.

### modern-card View Layout
**Source:** `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` lines 14-17, 57-62
**Apply to:** `Views/DungeonMaster/Profile.cshtml` and `Views/DungeonMaster/EditProfile.cshtml`

```html
<div class="card modern-card">
    <div class="card-header modern-card-header">
        <h4 class="mb-0"><i class="fas fa-icon me-2"></i>Section Title</h4>
    </div>
    <div class="card-body modern-card-body">
```

### ValidateAntiForgeryToken on all POST actions
**Source:** Every POST action in the codebase — QuestController, GuildMembersController, AccountController
**Apply to:** All POST actions in `DungeonMasterController`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
```

### DI Registration — Scoped service + repository pair
**Source:** `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` line 21; `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` line 24
**Apply to:** Both ServiceExtensions files

```csharp
// Domain
services.AddScoped<IDungeonMasterProfileService, DungeonMasterProfileService>();

// Repository
services.AddScoped<IDungeonMasterProfileRepository, DungeonMasterProfileRepository>();
```

---

## No Analog Found

None — every file in this phase has a direct analog in the codebase. The research phase confirmed all patterns are established and verified.

---

## Metadata

**Analog search scope:** All five projects (`EuphoriaInn.Domain`, `EuphoriaInn.Repository`, `EuphoriaInn.Service`, `EuphoriaInn.UnitTests`, `EuphoriaInn.IntegrationTests`)
**Files scanned:** 18 source files read directly
**Pattern extraction date:** 2026-06-17
