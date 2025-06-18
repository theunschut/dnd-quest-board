using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;
using QuestBoard.Services;

namespace QuestBoard.Pages;

public class ManageQuestModel : PageModel
{
    private readonly QuestBoardContext _context;
    private readonly IEmailService _emailService;

    public ManageQuestModel(QuestBoardContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public Quest Quest { get; set; } = default!;
    public bool IsAuthorized { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
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

        Quest = quest;

        // Check if DM is authorized
        var sessionDmName = HttpContext.Session.GetString($"DmName_{id}");
        IsAuthorized = !string.IsNullOrEmpty(sessionDmName) && 
                      sessionDmName.Equals(quest.DmName, StringComparison.OrdinalIgnoreCase);

        return Page();
    }

    public async Task<IActionResult> OnPostVerifyDmAsync(int id)
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
            return await OnGetAsync(id);
        }

        // Store DM name in session
        HttpContext.Session.SetString($"DmName_{id}", dmName);

        return RedirectToPage("/ManageQuest", new { id = id });
    }

    public async Task<IActionResult> OnPostFinalizeQuestAsync(int id)
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
            return await OnGetAsync(id);
        }
        
        var selectedDate = quest.ProposedDates.FirstOrDefault(pd => pd.Id == selectedDateId);
        
        if (selectedDate == null)
        {
            ModelState.AddModelError("", "Please select a date.");
            return await OnGetAsync(id);
        }

        // Get selected players
        var selectedPlayerIds = Request.Form["SelectedPlayerIds"]
            .Where(idStr => !string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out _))
            .Select(idStr => int.Parse(idStr))
            .ToList();

        if (selectedPlayerIds.Count > 6)
        {
            ModelState.AddModelError("", "Cannot select more than 6 players.");
            return await OnGetAsync(id);
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

        return RedirectToPage("/Quest", new { id = id });
    }
}