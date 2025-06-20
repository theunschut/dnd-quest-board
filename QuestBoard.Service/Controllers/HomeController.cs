using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Service.ViewModels;

namespace QuestBoard.Service.Controllers;

public class HomeController(IQuestService questService, IDungeonMasterService dungeonMasterService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        var dms = await dungeonMasterService.GetAllAsync(token);
        var quests = await questService.GetQuestsWithSignupsAsync(token);

        var viewModel = new QuestBoardViewModel
        {
            DungeonMasters = dms,
            Quests = quests
        };

        return View(viewModel);
    }
}