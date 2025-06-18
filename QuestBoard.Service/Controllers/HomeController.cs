using Microsoft.AspNetCore.Mvc;
using QuestBoard.Repository;

namespace QuestBoard.Service.Controllers;

public class HomeController(IQuestRepository repository) : Controller
{
    public async Task<IActionResult> Index()
    {
        var quests = await repository.GetQuestsWithSignupsAsync();

        return View(quests);
    }
}