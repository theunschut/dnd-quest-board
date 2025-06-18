using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Pages;

public class QuestModel : PageModel
{
    private readonly QuestBoardContext _context;

    public QuestModel(QuestBoardContext context)
    {
        _context = context;
    }

    public Quest Quest { get; set; } = default!;
    public bool IsPlayerSignedUp { get; set; }
    public string? DmNameForManagement { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
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

        Quest = quest;

        // Check if current user is signed up (simple check by session/cookie)
        var playerName = HttpContext.Session.GetString($"PlayerName_{id}");
        IsPlayerSignedUp = !string.IsNullOrEmpty(playerName) && 
                          Quest.PlayerSignups.Any(ps => ps.PlayerName == playerName);

        // Get DM name for management access (simple check)
        DmNameForManagement = HttpContext.Session.GetString($"DmName_{id}");

        return Page();
    }

    public async Task<IActionResult> OnPostSignUpAsync(int id)
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
            return await OnGetAsync(id);
        }

        // Check if player already signed up
        if (quest.PlayerSignups.Any(ps => ps.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("", "A player with this name has already signed up.");
            return await OnGetAsync(id);
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

        return RedirectToPage("/Quest", new { id = id });
    }
}