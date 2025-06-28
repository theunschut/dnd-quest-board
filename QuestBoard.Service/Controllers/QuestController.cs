using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var dms = await userService.GetAllDungeonMastersAsync(token);
        return View(new CreateQuestViewModel { DungeonMasters = dms });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DungeonMasterOnly")]
    public async Task<IActionResult> Create(CreateQuestViewModel viewModel, CancellationToken token = default)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        if (!(await userService.ExistsAsync(viewModel.Quest.DungeonMasterId, token)))
        {
            return NotFound();
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
    public async Task<IActionResult> Details(int questId, PlayerSignup signup)
    {
        var quest = await questService.GetQuestWithDetailsAsync(questId);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        // Get current authenticated user
        var userEntity = await userService.GetUserAsync(User);
        if (userEntity == null)
        {
            return Challenge();
        }

        var currentUser = await userService.GetByIdAsync(userEntity.Id);
        if (currentUser == null)
        {
            return Challenge();
        }

        // Check if user already signed up
        if (quest.PlayerSignups.Any(ps => ps.Player.Id == currentUser.Id))
        {
            ModelState.AddModelError("", "You have already signed up for this quest.");
            return await Details(questId);
        }

        // Use the authenticated user instead of form input
        signup.Player = currentUser;
        signup.Quest = quest;

        await playerSignupService.AddAsync(signup);

        return RedirectToAction("Details", new { id = questId });
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

        if (selectedPlayerIds.Count > 6)
        {
            ModelState.AddModelError("", "Cannot select more than 6 players.");
            return await Manage(id);
        }

        // Update quest
        quest.IsFinalized = true;
        quest.FinalizedDate = selectedDate.Date;

        // Update player selections
        foreach (var playerSignup in quest.PlayerSignups)
        {
            playerSignup.IsSelected = selectedPlayerIds.Contains(playerSignup.Id);
        }

        await questService.UpdateAsync(quest);
        //await repository.SaveChangesAsync();

        // Send email notifications to selected players
        var selectedPlayers = quest.PlayerSignups.Where(ps => ps.IsSelected && !string.IsNullOrEmpty(ps.Player.Email));

        foreach (var player in selectedPlayers)
        {
            await emailService.SendQuestFinalizedEmailAsync(
                player.Player.Email!,
                player.Player.Name,
                quest.Title,
                quest.DungeonMaster.Name,
                quest.FinalizedDate.Value
            );
        }

        return RedirectToAction("Details", new { id });
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