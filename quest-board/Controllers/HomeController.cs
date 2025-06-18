using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Controllers;

public class HomeController : Controller
{
    private readonly QuestBoardContext _context;

    public HomeController(QuestBoardContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var quests = await _context.Quests
            .Include(q => q.PlayerSignups)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return View(quests);
    }
}