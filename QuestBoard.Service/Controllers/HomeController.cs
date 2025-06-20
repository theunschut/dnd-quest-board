using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Controllers;

public class HomeController(IQuestService questService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        var quests = await questService.GetQuestsWithSignupsAsync(token);

        return View(quests);
    }
}