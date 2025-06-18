using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Pages;

public class IndexModel : PageModel
{
    private readonly QuestBoardContext _context;

    public IndexModel(QuestBoardContext context)
    {
        _context = context;
    }

    public IList<Quest> Quests { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Quests = await _context.Quests
            .Include(q => q.PlayerSignups)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
}