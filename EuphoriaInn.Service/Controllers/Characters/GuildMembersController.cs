using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Service.ViewModels.CharacterViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.Characters
{
    [Authorize]
    public class GuildMembersController(
        ICharacterService characterService,
        IUserService userService,
        IMapper mapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var allCharacters = await characterService.GetAllCharactersWithDetailsAsync(token);
            var characterViewModels = mapper.Map<List<CharacterViewModel>>(allCharacters);

            var viewModel = new CharactersIndexViewModel
            {
                CurrentUserId = currentUser.Id,
                MyCharacters = characterViewModels.Where(c => c.OwnerId == currentUser.Id)
                    .OrderByDescending(c => c.Role == CharacterRole.Main)
                    .ThenByDescending(c => c.Status == CharacterStatus.Active)
                    .ThenBy(c => c.Name)
                    .ToList(),
                OtherCharacters = characterViewModels.Where(c => c.OwnerId != currentUser.Id)
                    .OrderBy(c => c.OwnerName)
                    .ThenByDescending(c => c.Status == CharacterStatus.Active)
                    .ThenBy(c => c.Name)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken token = default)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (character == null)
            {
                return NotFound();
            }

            var currentUser = await userService.GetUserAsync(User);
            var viewModel = mapper.Map<CharacterViewModel>(character);
            viewModel.IsOwner = currentUser != null && character.OwnerId == currentUser.Id;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken token = default)
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var viewModel = new CharacterViewModel
            {
                OwnerId = currentUser.Id,
                OwnerName = currentUser.Name,
                Level = 1,
                Status = CharacterStatus.Active,
                Role = CharacterRole.Backup,
                Classes = [new CharacterClassViewModel { ClassLevel = 1 }]
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CharacterViewModel viewModel, CancellationToken token = default)
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            viewModel.OwnerId = currentUser.Id;

            // Validate class levels
            if (!await characterService.ValidateCharacterClassLevelsAsync(viewModel.Level, 
                mapper.Map<List<CharacterClass>>(viewModel.Classes)))
            {
                ModelState.AddModelError(string.Empty, 
                    "The sum of all class levels must equal the character's total level.");
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Handle profile picture upload
            if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
                viewModel.ProfilePicture = memoryStream.ToArray();
            }

            var character = mapper.Map<Character>(viewModel);
            await characterService.AddAsync(character, token);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken token = default)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (character == null)
            {
                return NotFound();
            }

            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null || character.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            var viewModel = mapper.Map<CharacterViewModel>(character);
            viewModel.IsOwner = true;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CharacterViewModel viewModel, CancellationToken token = default)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            var existingCharacter = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (existingCharacter == null)
            {
                return NotFound();
            }

            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null || existingCharacter.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            // Validate class levels
            if (!await characterService.ValidateCharacterClassLevelsAsync(viewModel.Level,
                mapper.Map<List<CharacterClass>>(viewModel.Classes)))
            {
                ModelState.AddModelError(string.Empty,
                    "The sum of all class levels must equal the character's total level.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.IsOwner = true;
                return View(viewModel);
            }

            // Update the existing character properties manually instead of mapping
            existingCharacter.Name = viewModel.Name;
            existingCharacter.Level = viewModel.Level;
            existingCharacter.Status = viewModel.Status;
            existingCharacter.Role = viewModel.Role;
            existingCharacter.SheetLink = viewModel.SheetLink;
            existingCharacter.Description = viewModel.Description;
            existingCharacter.Backstory = viewModel.Backstory;
            
            // Handle profile picture upload - clear old picture first if new one is being uploaded
            if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.Length > 0)
            {
                // Upload the new picture
                using var memoryStream = new MemoryStream();
                await viewModel.ProfilePictureFile.CopyToAsync(memoryStream, token);
                existingCharacter.ProfilePicture = memoryStream.ToArray();
            }
            // Otherwise, profile picture remains unchanged

            // Update classes
            existingCharacter.Classes = mapper.Map<List<CharacterClass>>(viewModel.Classes);
            
            // If setting as main, update all user's characters
            if (viewModel.Role == CharacterRole.Main && existingCharacter.Role != CharacterRole.Main)
            {
                await characterService.SetAsMainCharacterAsync(id, currentUser.Id, token);
            }
            else
            {
                await characterService.UpdateAsync(existingCharacter, token);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken token = default)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (character == null)
            {
                return NotFound();
            }

            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null || character.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            await characterService.RemoveAsync(character, token);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRetirement(int id, CancellationToken token = default)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (character == null)
            {
                return NotFound();
            }

            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null || character.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            character.Status = character.Status == CharacterStatus.Active 
                ? CharacterStatus.Retired 
                : CharacterStatus.Active;

            await characterService.UpdateAsync(character, token);

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetProfilePicture(int id, CancellationToken token = default)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(id, token);
            if (character == null || character.ProfilePicture == null)
            {
                return NotFound();
            }

            return File(character.ProfilePicture, "image/jpeg");
        }
    }
}
