using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;
using QuestBoard.Services;

namespace QuestBoard.Controllers;

public class QuestController : Controller
{
    private readonly QuestBoardContext _context;
    private readonly IEmailService _emailService;

    public QuestController(QuestBoardContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public IActionResult Create()
    {
        return View(new CreateQuestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Create Quest entity from ViewModel
            var quest = new Quest
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                Difficulty = viewModel.Difficulty,
                DmName = viewModel.DmName,
                DmEmail = viewModel.DmEmail,
                CreatedAt = DateTime.UtcNow
            };

            _context.Quests.Add(quest);
            await _context.SaveChangesAsync();

            // Add proposed dates from ViewModel
            foreach (var date in viewModel.ProposedDates)
            {
                var proposedDate = new ProposedDate
                {
                    QuestId = quest.Id,
                    Date = date
                };
                _context.ProposedDates.Add(proposedDate);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            return View(viewModel);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var quest = await _context.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quest == null)
        {
            return NotFound();
        }

        // Check if current user is signed up
        var playerName = HttpContext.Session.GetString($"PlayerName_{id}");
        ViewBag.IsPlayerSignedUp = !string.IsNullOrEmpty(playerName) && 
                          quest.PlayerSignups.Any(ps => ps.PlayerName == playerName);

        // Get DM name for management access
        ViewBag.DmNameForManagement = HttpContext.Session.GetString($"DmName_{id}");

        return View(quest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(int id)
    {
        var quest = await _context.Quests
            .Include(q => q.ProposedDates)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        var playerName = Request.Form["PlayerName"].ToString().Trim();
        var playerEmail = Request.Form["PlayerEmail"].ToString().Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            ModelState.AddModelError("", "Player name is required.");
            return await Details(id);
        }

        // Check if player already signed up
        if (quest.PlayerSignups.Any(ps => ps.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("", "A player with this name has already signed up.");
            return await Details(id);
        }

        // Create player signup
        var playerSignup = new PlayerSignup
        {
            QuestId = quest.Id,
            PlayerName = playerName,
            PlayerEmail = string.IsNullOrEmpty(playerEmail) ? null : playerEmail,
            SignupTime = DateTime.UtcNow
        };

        _context.PlayerSignups.Add(playerSignup);
        await _context.SaveChangesAsync();

        // Create date votes
        foreach (var proposedDate in quest.ProposedDates)
        {
            var voteValue = Request.Form[$"DateVote_{proposedDate.Id}"].ToString();
            if (int.TryParse(voteValue, out var vote))
            {
                var playerDateVote = new PlayerDateVote
                {
                    PlayerSignupId = playerSignup.Id,
                    ProposedDateId = proposedDate.Id,
                    Vote = (VoteType)vote
                };

                _context.PlayerDateVotes.Add(playerDateVote);
            }
        }

        await _context.SaveChangesAsync();

        // Store player name in session for future reference
        HttpContext.Session.SetString($"PlayerName_{id}", playerName);

        return RedirectToAction("Details", new { id = id });
    }

    public async Task<IActionResult> Manage(int id)
    {
        var quest = await _context.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quest == null)
        {
            return NotFound();
        }

        // Check if DM is authorized
        var sessionDmName = HttpContext.Session.GetString($"DmName_{id}");
        ViewBag.IsAuthorized = !string.IsNullOrEmpty(sessionDmName) && 
                      sessionDmName.Equals(quest.DmName, StringComparison.OrdinalIgnoreCase);

        return View(quest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyDm(int id)
    {
        var quest = await _context.Quests.FindAsync(id);
        if (quest == null)
        {
            return NotFound();
        }

        var dmName = Request.Form["DmName"].ToString().Trim();
        
        if (string.IsNullOrEmpty(dmName) || !dmName.Equals(quest.DmName, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "DM name does not match.");
            return await Manage(id);
        }

        // Store DM name in session
        HttpContext.Session.SetString($"DmName_{id}", dmName);

        return RedirectToAction("Manage", new { id = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        var quest = await _context.Quests
            .Include(q => q.ProposedDates)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quest == null || quest.IsFinalized)
        {
            return NotFound();
        }

        // Verify DM authorization
        var sessionDmName = HttpContext.Session.GetString($"DmName_{id}");
        if (string.IsNullOrEmpty(sessionDmName) || 
            !sessionDmName.Equals(quest.DmName, StringComparison.OrdinalIgnoreCase))
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
            .Select(idStr => int.Parse(idStr))
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

        await _context.SaveChangesAsync();

        // Send email notifications to selected players
        var selectedPlayers = quest.PlayerSignups.Where(ps => ps.IsSelected && !string.IsNullOrEmpty(ps.PlayerEmail));
        
        foreach (var player in selectedPlayers)
        {
            await _emailService.SendQuestFinalizedEmailAsync(
                player.PlayerEmail!,
                player.PlayerName,
                quest.Title,
                quest.DmName,
                quest.FinalizedDate.Value
            );
        }

        return RedirectToAction("Details", new { id = id });
    }

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

        var quests = await _context.Quests
            .Include(q => q.PlayerSignups)
            .Where(q => q.DmName.Equals(dmName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        ViewBag.DmName = dmName;
        return View(quests);
    }
}