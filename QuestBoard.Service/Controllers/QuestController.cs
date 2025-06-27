using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.ViewModels.QuestViewModels;

namespace QuestBoard.Service.Controllers;

public class QuestController(
    IUserService dmService,
    IEmailService emailService,
    IMapper mapper,
    IPlayerSignupService playerSignupService,
    IQuestService questService
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken token = default)
    {
        var dms = await dmService.GetAllAsync(token);
        return View(new CreateQuestViewModel { DungeonMasters = dms });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestViewModel viewModel, CancellationToken token = default)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        if (!(await dmService.ExistsAsync(viewModel.Quest.DungeonMasterId, token)))
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

        // Check if current user is signed up
        var playerName = HttpContext.Session.GetString($"PlayerName_{id}");
        ViewBag.IsPlayerSignedUp = !string.IsNullOrEmpty(playerName) && quest.PlayerSignups.Any(ps => ps.PlayerName == playerName);

        // Get DM name for management access
        ViewBag.DmNameForManagement = HttpContext.Session.GetString($"DmName_{id}");

        var signup = new PlayerSignup
        {
            Quest = quest,
            DateVotes = [.. quest.ProposedDates.Select(x => new PlayerDateVote { ProposedDate = x, ProposedDateId = x.Id })],
        };

        return View(signup);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(int questId, PlayerSignup signup)
    {
        var quest = await questService.GetQuestWithDetailsAsync(questId);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        signup.PlayerName = signup.PlayerName.Trim();
        signup.PlayerEmail = signup.PlayerEmail?.Trim();

        if (string.IsNullOrEmpty(signup.PlayerName))
        {
            ModelState.AddModelError("", "Player name is required.");
            return await Details(questId);
        }

        // Check if player already signed up
        if (quest.PlayerSignups.Any(ps => ps.PlayerName.Equals(signup.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("", "A player with this name has already signed up.");
            return await Details(questId);
        }

        await playerSignupService.AddAsync(signup);

        // Store player name in session for future reference
        HttpContext.Session.SetString($"PlayerName_{questId}", signup.PlayerName);

        return RedirectToAction("Details", new { questId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        var quest = await questService.GetQuestWithDetailsAsync(id);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        // Verify DM authorization
        var sessionDmName = HttpContext.Session.GetString($"DmName_{id}");
        if (string.IsNullOrEmpty(sessionDmName) || !sessionDmName.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
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
        var selectedPlayers = quest.PlayerSignups.Where(ps => ps.IsSelected && !string.IsNullOrEmpty(ps.PlayerEmail));

        foreach (var player in selectedPlayers)
        {
            await emailService.SendQuestFinalizedEmailAsync(
                player.PlayerEmail!,
                player.PlayerName,
                quest.Title,
                quest.DungeonMaster.Name,
                quest.FinalizedDate.Value
            );
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int id)
    {
        var quest = await questService.GetQuestWithManageDetailsAsync(id);

        if (quest == null)
        {
            return NotFound();
        }

        // Check if DM is authorized
        var sessionDmName = HttpContext.Session.GetString($"DmName_{id}");
        ViewBag.IsAuthorized = !string.IsNullOrEmpty(sessionDmName) && sessionDmName.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase);

        return View(quest);
    }

    [HttpGet]
    public IActionResult MyQuests()
    {
        return View(new List<Quest>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyQuests(string dmName)
    {
        if (string.IsNullOrEmpty(dmName))
        {
            ModelState.AddModelError("", "Please enter your DM name.");
            return View(new List<Quest>());
        }

        var quests = await questService.GetQuestsByDmNameAsync(dmName);

        ViewBag.DmName = dmName;
        return View(quests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyDm(int id)
    {
        var quest = await questService.GetQuestWithManageDetailsAsync(id);
        if (quest == null)
        {
            return NotFound();
        }

        var dmName = Request.Form["DmName"].ToString().Trim();

        if (string.IsNullOrEmpty(dmName) || !dmName.Equals(quest.DungeonMaster?.Name, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "DM name does not match.");
            return await Manage(id);
        }

        // Store DM name in session
        HttpContext.Session.SetString($"DmName_{id}", dmName);

        return RedirectToAction("Manage", new { id });
    }
}