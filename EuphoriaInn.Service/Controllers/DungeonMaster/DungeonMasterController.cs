using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.DungeonMasterViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.DungeonMaster;

[Authorize]
public class DungeonMasterController(
    IDungeonMasterProfileService dmProfileService,
    IUserService userService,
    IQuestService questService,
    IMapper mapper) : Controller
{
    // DMPRO-01: Public DM profile page
    [HttpGet]
    [AllowAnonymous]
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
            Name = user.Name ?? string.Empty,
            Bio = profile?.Bio,
            HasProfilePicture = profile?.ProfilePicture != null,
            CanEdit = currentUser != null && (currentUser.Id == id || User.IsInRole("Admin")),
            Quests = mapper.Map<List<QuestSummaryViewModel>>(quests)
        };

        return View(viewModel);
    }

    // DMPRO-02 + DMPRO-03: Edit profile (own: no id / admin: with id)
    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> EditProfile(int? id, CancellationToken token = default)
    {
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var targetUserId = id ?? currentUser.Id;
        if (targetUserId != currentUser.Id && !User.IsInRole("Admin"))
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
            const long maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (viewModel.ProfilePictureFile.Length > maxFileSizeBytes)
            {
                ModelState.AddModelError(nameof(viewModel.ProfilePictureFile),
                    "Profile picture cannot exceed 5 MB.");
                return View(viewModel);
            }
            using var memoryStream = new MemoryStream();
            await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
            imageBytes = memoryStream.ToArray();
        }

        await dmProfileService.UpsertProfileAsync(targetUserId, viewModel.Bio, imageBytes, token);

        return RedirectToAction(nameof(Profile), new { id = targetUserId });
    }

    // Image serving endpoint — mirrors GuildMembersController.GetProfilePicture
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDMProfilePicture(int id, CancellationToken token = default)
    {
        var bytes = await dmProfileService.GetProfilePictureAsync(id, token);
        if (bytes == null) return NotFound();

        // Detect MIME type from magic bytes so PNG and GIF are served correctly.
        // PNG: 89 50 4E 47  GIF: 47 49 46 38  everything else: treat as JPEG.
        var contentType = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50
            ? "image/png"
            : bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46
            ? "image/gif"
            : "image/jpeg";

        return File(bytes, contentType);
    }
}
