using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.ViewModels.QuestViewModels;

namespace QuestBoard.Service.Controllers;

public class QuestController(
    IUserService userService,
    IEmailService emailService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService
    ) : Controller
{
    [HttpGet]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Create(CancellationToken token = default)
    {
        // Get current user and verify they are a DM
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is a registered DM
        if (!currentUser.IsDungeonMaster)
        {
            return Forbid();
        }

        return View(new CreateQuestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Create(CreateQuestViewModel viewModel, CancellationToken token = default)
    {
        // Get current user and verify they are a DM
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if current user is a registered DM
        if (!currentUser.IsDungeonMaster)
        {
            return Forbid();
        }

        // Automatically set the current user as the DM
        viewModel.Quest.DungeonMasterId = currentUser.Id;

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        // Create Quest entity from ViewModel using AutoMapper
        var quest = mapper.Map<Quest>(viewModel.Quest);

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
        if (!currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
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
        
        // Check if there are any player signups - if so, don't allow editing proposed dates
        var canEditProposedDates = !quest.PlayerSignups.Any();
        
        return View(new EditQuestViewModel 
        { 
            Id = quest.Id,
            Quest = questViewModel, 
            DungeonMasters = dms,
            CanEditProposedDates = canEditProposedDates
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
        if (!currentUser.Name.Equals(existingQuest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // Don't allow editing of finalized quests
        if (existingQuest.IsFinalized)
        {
            return BadRequest("Cannot edit a finalized quest. Open the quest first to make changes.");
        }

        // Check if there are any player signups - if so, don't allow editing proposed dates
        var canEditProposedDates = !existingQuest.PlayerSignups.Any();
        viewModel.CanEditProposedDates = canEditProposedDates;

        if (!ModelState.IsValid)
        {
            var dms = await userService.GetAllDungeonMastersAsync(token);
            viewModel.DungeonMasters = dms;
            return View(viewModel);
        }

        if (!(await userService.ExistsAsync(viewModel.Quest.DungeonMasterId, token)))
        {
            return NotFound();
        }

        // Use the specialized service method to update quest properties
        await questService.UpdateQuestPropertiesAsync(
            id,
            viewModel.Quest.Title,
            viewModel.Quest.Description,
            viewModel.Quest.Difficulty,
            viewModel.Quest.DungeonMasterId,
            viewModel.Quest.TotalPlayerCount,
            canEditProposedDates,
            canEditProposedDates ? viewModel.Quest.ProposedDates : null,
            token
        );

        return RedirectToAction("Manage", new { id });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null)
        {
            return NotFound();
        }

        await questService.RemoveAsync(quest);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null)
        {
            return NotFound();
        }

        // Get current user if authenticated
        User? currentUser = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userEntity = await userService.GetUserAsync(User);
            if (userEntity != null)
            {
                currentUser = await userService.GetByIdAsync(userEntity.Id);
            }
        }

        // Check if current user is signed up
        ViewBag.IsPlayerSignedUp = currentUser != null && quest.PlayerSignups.Any(ps => ps.Player.Id == currentUser.Id);

        // Check if current user is the DM
        ViewBag.DmNameForManagement = currentUser?.Name == quest.DungeonMaster?.Name ? currentUser?.Name : null;

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
    public async Task<IActionResult> Details(PlayerSignup signup)
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

        await playerSignupService.AddAsync(signup);

        return RedirectToAction("Details", new { id = questId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> JoinFinalizedQuest(int questId)
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

        // Check if quest has space (max 6 selected players)
        var selectedPlayersCount = quest.PlayerSignups.Where(ps => ps.IsSelected).Count();
        if (selectedPlayersCount >= quest.TotalPlayerCount)
        {
            ModelState.AddModelError("", $"This quest is full ({selectedPlayersCount}/{quest.TotalPlayerCount} players).");
            return RedirectToAction("Details", new { id = questId });
        }

        // Find the finalized date's corresponding proposed date for vote creation
        var finalizedProposedDate = quest.ProposedDates
            .FirstOrDefault(pd => pd.Date.Date == quest.FinalizedDate.Value.Date);

        if (finalizedProposedDate == null)
        {
            ModelState.AddModelError("", "Could not find the finalized date information.");
            return RedirectToAction("Details", new { id = questId });
        }

        // Create signup with automatic "Yes" vote for the finalized date
        var signup = new PlayerSignup
        {
            Player = user,
            Quest = quest,
            IsSelected = true, // Automatically select since quest is finalized and has space
            DateVotes = [new PlayerDateVote 
            { 
                ProposedDateId = finalizedProposedDate.Id,
                Vote = VoteType.Yes
            }]
        };

        await playerSignupService.AddAsync(signup);

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
        if (!currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // Get selected date
        if (!int.TryParse(Request.Form["SelectedDateId"], out var selectedDateId))
        {
            ModelState.AddModelError("", "Please select a date.");
            return await Manage(id);
        }

        var selectedDate = quest.ProposedDates.FirstOrDefault(pd => pd.Id == selectedDateId);

        if (selectedDate == null)
        {
            ModelState.AddModelError("", "Please select a date.");
            return await Manage(id);
        }

        // Get selected players
        var selectedPlayerIds = Request.Form["SelectedPlayerIds"]
            .Where(idStr => !string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out _))
            .Select(int.Parse)
            .ToList();

        if (selectedPlayerIds.Count > quest.TotalPlayerCount)
        {
            ModelState.AddModelError("", $"Cannot select more than {quest.TotalPlayerCount} players.");
            return await Manage(id);
        }

        // Finalize the quest using the specialized service method
        await questService.FinalizeQuestAsync(id, selectedDate.Date, selectedPlayerIds);

        // Send email notifications to selected players
        var selectedPlayers = quest.PlayerSignups.Where(ps => selectedPlayerIds.Contains(ps.Id) && !string.IsNullOrEmpty(ps.Player.Email));

        foreach (var player in selectedPlayers)
        {
            await emailService.SendQuestFinalizedEmailAsync(
                player.Player.Email!,
                player.Player.Name,
                quest.Title,
                quest.DungeonMaster.Name,
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
        if (!currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
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

        // Check if current user is the quest's DM
        ViewBag.IsAuthorized = currentUser.Name.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase);

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
        return View(quests);
    }
}