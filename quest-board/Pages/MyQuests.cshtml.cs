using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Pages;

public class MyQuestsModel : PageModel
{
    private readonly QuestBoardContext _context;

    public MyQuestsModel(QuestBoardContext context)
    {
        _context = context;
    }

    public IList<Quest> Quests { get; set; } = new List<Quest>();
    public string? DmName { get; set; }

    public void OnGet()
    {
        // Empty list initially
    }

    public async Task<IActionResult> OnPostAsync()
    {
        DmName = Request.Form["DmName"].ToString().Trim();

        if (string.IsNullOrEmpty(DmName))
        {
            ModelState.AddModelError("", "Please enter your DM name.");
            return Page();
        }

        Quests = await _context.Quests
            .Include(q => q.PlayerSignups)
            .Where(q => q.DmName.Equals(DmName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return Page();
    }
}