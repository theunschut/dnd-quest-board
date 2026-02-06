using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Service.ViewModels.CalendarViewModels;
using EuphoriaInn.Service.ViewModels.QuestViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.QuestBoard;

public class QuestController(
    IUserService userService,
    IEmailService emailService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService,
    ICharacterService characterService
    ) : Controller
{
    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Create(CancellationToken token = default)
    {
        // Get current user
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        return View(new QuestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Create(QuestViewModel viewModel, CancellationToken token = default)
    {
        // Get current user
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Automatically set the current user as the DM
        viewModel.DungeonMasterId = currentUser.Id;

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        // Create Quest entity from ViewModel using AutoMapper
        var quest = mapper.Map<Quest>(viewModel);

        // Set Quest reference for all ProposedDates
        foreach (var proposedDate in quest.ProposedDates)
        {
            proposedDate.Quest = quest;
            proposedDate.QuestId = quest.Id;
        }

        await questService.AddAsync(quest, token);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Edit(int id, CancellationToken token = default)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id, token);

        if (quest == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is the quest's DM
        if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Don't allow editing of finalized quests
        if (quest.IsFinalized)
        {
            return BadRequest("Cannot edit a finalized quest. Open the quest first to make changes.");
        }

        var dms = await userService.GetAllDungeonMastersAsync(token);
        var questViewModel = mapper.Map<QuestViewModel>(quest);

        // Allow editing proposed dates even with signups (service will handle it intelligently)
        var canEditProposedDates = true;
        var hasExistingSignups = quest.PlayerSignups.Any();

        return View(new EditQuestViewModel
        {
            Id = quest.Id,
            Quest = questViewModel,
            DungeonMasters = dms,
            CanEditProposedDates = canEditProposedDates,
            HasExistingSignups = hasExistingSignups
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Edit(int id, EditQuestViewModel viewModel, CancellationToken token = default)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var existingQuest = await questService.GetQuestWithDetailsAsync(id, token);

        if (existingQuest == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is the quest's DM
        if (!currentUser.Equals(existingQuest.DungeonMaster) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Don't allow editing of finalized quests
        if (existingQuest.IsFinalized)
        {
            return BadRequest("Cannot edit a finalized quest. Open the quest first to make changes.");
        }

        // Allow editing of proposed dates even with signups (service will handle it intelligently)
        var canEditProposedDates = true;
        var hasExistingSignups = existingQuest.PlayerSignups.Any();
        viewModel.CanEditProposedDates = canEditProposedDates;
        viewModel.HasExistingSignups = hasExistingSignups;

        if (!ModelState.IsValid)
        {
            var dms = await userService.GetAllDungeonMastersAsync(token);
            viewModel.DungeonMasters = dms;
            return View(viewModel);
        }

        // Use the specialized service method to update quest properties and get affected players
        var affectedPlayers = await questService.UpdateQuestPropertiesWithNotificationsAsync(
            id,
            viewModel.Quest.Title,
            viewModel.Quest.Description,
            viewModel.Quest.ChallengeRating,
            viewModel.Quest.TotalPlayerCount,
            viewModel.Quest.DungeonMasterSession,
            true, // Always allow date updates - service will handle intelligently
            viewModel.Quest.ProposedDates,
            token
        );

        // Send email notifications to affected players
        if (affectedPlayers.Any())
        {
            var quest = await questService.GetQuestWithDetailsAsync(id, token);
            if (quest != null)
            {
                foreach (var player in affectedPlayers.Where(p => !string.IsNullOrEmpty(p.Email)))
                {
                    await emailService.SendQuestDateChangedEmailAsync(
                        player.Email!,
                        player.Name,
                        quest.Title,
                        quest.DungeonMaster?.Name ?? "Unknown DM"
                    );
                }
            }
        }

        return RedirectToAction("Manage", new { id });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is the quest's DM or an Admin
        if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        await questService.RemoveAsync(quest);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token = default)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id, token);

        if (quest == null)
        {
            return NotFound();
        }

        // Get current user if authenticated
        User? currentUser = null;
        IList<Character>? userCharacters = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userEntity = await userService.GetUserAsync(User);
            if (userEntity != null)
            {
                currentUser = await userService.GetByIdAsync(userEntity.Id, token);
                
                // Get user's active characters
                if (currentUser != null)
                {
                    var allCharacters = await characterService.GetCharactersByOwnerIdAsync(currentUser.Id, token);
                    userCharacters = allCharacters.Where(c => c.Status == CharacterStatus.Active).ToList();
                }
            }
        }

        // Check if current user is signed up
        ViewBag.IsPlayerSignedUp = currentUser != null && quest.PlayerSignups.Any(ps => ps.Player.Id == currentUser.Id);
        ViewBag.UserCharacters = userCharacters ?? new List<Character>();

        // Check if current user can manage this quest (DM or admin)
        var isQuestDm = currentUser?.Name == quest.DungeonMaster?.Name;
        var isAdmin = currentUser != null && await userService.IsInRoleAsync(User, "Admin");
        ViewBag.CanManage = isQuestDm || isAdmin;

        // Get all quests for calendar context
        var allQuests = await questService.GetQuestsWithDetailsAsync(token);

        // Get unique months that have proposed dates for this quest
        var monthsWithProposedDates = quest.ProposedDates
            .Select(pd => new { pd.Date.Year, pd.Date.Month })
            .Distinct()
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .Select(m => new CalendarViewModel
            {
                Year = m.Year,
                Month = m.Month,
                Quests = allQuests.ToList()
            })
            .ToList();

        ViewBag.CalendarMonths = monthsWithProposedDates;
        ViewBag.IsDetailsPage = true;
        ViewBag.CurrentQuestId = id;
        ViewBag.CurrentUserId = currentUser?.Id;

        var signup = new PlayerSignup
        {
            Quest = quest,
            Player = currentUser ?? new User { Name = "", Email = "" },
            DateVotes = [.. quest.ProposedDates.Select(x => new PlayerDateVote { ProposedDate = x, ProposedDateId = x.Id })],
        };

        return View(signup);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Details(PlayerSignup signup, int selectedRole = 0)
    {
        if (signup.Quest?.Id == null || signup.Quest.Id == 0) return NotFound();
        var questId = signup.Quest.Id;

        var quest = await questService.GetQuestWithDetailsAsync(questId);
        if (quest == null || quest.IsFinalized)
            return NotFound();

        // Get current authenticated user
        var user = await userService.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Check if user already signed up
        if (quest.PlayerSignups.Any(ps => ps.Player.Id == user.Id))
        {
            ModelState.AddModelError("", "You have already signed up for this quest.");
            return await Details(questId);
        }

        // Use the authenticated user instead of form input
        signup.Player = user;
        signup.Quest = quest;
        signup.Role = (SignupRole)selectedRole; // Set role from form

        
        // Validate character if selected
        if (signup.CharacterId.HasValue)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(signup.CharacterId.Value);
            if (character == null || character.OwnerId != user.Id || character.Status != CharacterStatus.Active)
            {
                ModelState.AddModelError("", "Invalid character selection.");
                return await Details(questId);
            }
        }

        await playerSignupService.AddAsync(signup);

        return RedirectToAction("Details", new { id = questId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> JoinFinalizedQuest(int questId, int? characterId = null, int selectedRole = 0)
    {
        var quest = await questService.GetQuestWithDetailsAsync(questId);
        if (quest == null || !quest.IsFinalized || quest.FinalizedDate == null)
            return NotFound();

        // Get current authenticated user
        var user = await userService.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Check if user already signed up
        if (quest.PlayerSignups.Any(ps => ps.Player.Id == user.Id))
        {
            ModelState.AddModelError("", "You have already signed up for this quest.");
            return RedirectToAction("Details", new { id = questId });
        }

        var role = (SignupRole)selectedRole;

        // Check if quest has space - only count Player roles
        if (role == SignupRole.Player)
        {
            var selectedPlayersCount = quest.PlayerSignups
                .Where(ps => ps.IsSelected && ps.Role == SignupRole.Player)
                .Count();

            if (selectedPlayersCount >= quest.TotalPlayerCount)
            {
                ModelState.AddModelError("", $"This quest is full ({selectedPlayersCount}/{quest.TotalPlayerCount} players).");
                return RedirectToAction("Details", new { id = questId });
            }
        }

        // Validate character if selected
        if (characterId.HasValue)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(characterId.Value);
            if (character == null || character.OwnerId != user.Id || character.Status != CharacterStatus.Active)
            {
                ModelState.AddModelError("", "Invalid character selection.");
                return RedirectToAction("Details", new { id = questId });
            }
        }

        // Find the finalized date's corresponding proposed date for vote creation
        var finalizedProposedDate = quest.ProposedDates
            .FirstOrDefault(pd => pd.Date.Date == quest.FinalizedDate.Value.Date);

        if (finalizedProposedDate == null)
        {
            ModelState.AddModelError("", "Could not find the finalized date information.");
            return RedirectToAction("Details", new { id = questId });
        }

        // Create signup
        var signup = new PlayerSignup
        {
            Player = user,
            Quest = quest,
            CharacterId = characterId,
            Role = role,
            IsSelected = true, // Auto-approve all roles when joining finalized quest
            DateVotes = role == SignupRole.Spectator ? [] : // Spectators don't vote
                [new PlayerDateVote
                {
                    ProposedDateId = finalizedProposedDate.Id,
                    Vote = VoteType.Yes
                }]
        };

        await playerSignupService.AddAsync(signup);

        return RedirectToAction("Details", new { id = questId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> UpdateSignup(int questId, List<PlayerDateVote> dateVotes)
    {
        var quest = await questService.GetQuestWithDetailsAsync(questId);
        if (quest == null || quest.IsFinalized)
            return NotFound();

        // Get current authenticated user
        var user = await userService.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Find the user's signup for this quest
        var playerSignup = quest.PlayerSignups.FirstOrDefault(ps => ps.Player.Id == user.Id);
        if (playerSignup == null)
        {
            return BadRequest("You are not signed up for this quest.");
        }

        // Update the player's date votes
        await playerSignupService.UpdatePlayerDateVotesAsync(playerSignup.Id, dateVotes);

        return RedirectToAction("Details", new { id = questId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> UpdateSignupCharacter(int questId, int? characterId)
    {
        var quest = await questService.GetQuestWithDetailsAsync(questId);
        if (quest == null)
            return NotFound();

        // Get current authenticated user
        var user = await userService.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Find the user's signup for this quest
        var playerSignup = quest.PlayerSignups.FirstOrDefault(ps => ps.Player.Id == user.Id);
        if (playerSignup == null)
        {
            return BadRequest("You are not signed up for this quest.");
        }

        // Validate character if provided
        if (characterId.HasValue)
        {
            var character = await characterService.GetCharacterWithDetailsAsync(characterId.Value);
            if (character == null || character.OwnerId != user.Id || character.Status != CharacterStatus.Active)
            {
                return BadRequest("Invalid character selection.");
            }
        }

        // Update the character
        await playerSignupService.UpdateSignupCharacterAsync(playerSignup.Id, characterId);

        return RedirectToAction("Details", new { id = questId });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> RevokeSignup(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);
        if (quest == null)
            return NotFound();

        // Get current authenticated user
        var user = await userService.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Find the user's signup for this quest
        var playerSignup = quest.PlayerSignups.FirstOrDefault(ps => ps.Player.Id == user.Id);
        if (playerSignup == null)
        {
            return BadRequest("You are not signed up for this quest.");
        }

        // Remove the player signup (allow revoking at any time)
        await playerSignupService.RemoveAsync(playerSignup);

        return Ok();
    }

    [HttpDelete("Quest/RemovePlayerSignup/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemovePlayerSignup(int id)
    {
        // Get the signup
        var signup = await playerSignupService.GetByIdAsync(id);
        if (signup == null)
        {
            return NotFound();
        }

        // Remove the player signup (this will cascade delete all associated votes)
        await playerSignupService.RemoveAsync(signup);

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Finalize(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Verify DM authorization
        if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Get selected date
        if (!int.TryParse(Request.Form["SelectedDateId"], out var selectedDateId))
        {
            TempData["Error"] = "Please select a date.";
            return RedirectToAction("Manage", new { id });
        }

        var selectedDate = quest.ProposedDates.FirstOrDefault(pd => pd.Id == selectedDateId);

        if (selectedDate == null)
        {
            TempData["Error"] = "Please select a date.";
            return RedirectToAction("Manage", new { id });
        }

        // Get selected players
        var selectedPlayerIds = Request.Form["SelectedPlayerIds"]
            .Where(idStr => !string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out _))
            .Select(idStr => int.Parse(idStr!))
            .ToList();

        // Validate: Only count Player roles against the limit
        var selectedPlayerRoleCount = quest.PlayerSignups
            .Where(ps => selectedPlayerIds.Contains(ps.Id) && ps.Role == SignupRole.Player)
            .Count();

        if (selectedPlayerRoleCount > quest.TotalPlayerCount)
        {
            TempData["Error"] = $"Cannot select more than {quest.TotalPlayerCount} players.";
            return RedirectToAction("Manage", new { id });
        }

        // Finalize the quest using the specialized service method
        await questService.FinalizeQuestAsync(id, selectedDate.Date, selectedPlayerIds);

        // Send email notifications to ALL selected roles (Players, AssistantDMs, AND Spectators)
        var selectedSignups = quest.PlayerSignups
            .Where(ps => (selectedPlayerIds.Contains(ps.Id) || ps.Role == SignupRole.Spectator)
                         && !string.IsNullOrEmpty(ps.Player.Email));

        foreach (var signup in selectedSignups)
        {
            await emailService.SendQuestFinalizedEmailAsync(
                signup.Player.Email!,
                signup.Player.Name,
                quest.Title,
                quest.DungeonMaster?.Name ?? "Unknown DM",
                selectedDate.Date
            );
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Open(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null || !quest.IsFinalized)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Verify DM authorization
        if (!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Open the quest using the specialized service method
        await questService.OpenQuestAsync(id);

        return RedirectToAction("Manage", new { id });
    }

    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Manage(int id)
    {
        var quest = await questService.GetQuestWithManageDetailsAsync(id);

        if (quest == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is the quest's DM or an admin
        var isQuestDm = currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase);
        var isAdmin = await userService.IsInRoleAsync(User, "Admin");
        ViewBag.IsAuthorized = isQuestDm || isAdmin;
        ViewBag.IsAdmin = isAdmin;

        return View(quest);
    }

    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> MyQuests()
    {
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var quests = await questService.GetQuestsByDmNameAsync(currentUser.Name);

        var view = new MyQuestsViewModel
        {
            Open = quests.Where(q => !q.IsFinalized),
            Finalized = quests.Where(q => q.IsFinalized && q.FinalizedDate.HasValue && q.FinalizedDate.Value.Date > DateTime.UtcNow.AddDays(-1).Date),
            Done = quests.Where(q => q.IsFinalized && q.FinalizedDate.HasValue && q.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date)
        };

        return View(view);
    }
}