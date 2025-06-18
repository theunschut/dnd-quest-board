using Microsoft.AspNetCore.Mvc;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Service.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var quests = await _unitOfWork.Quests.GetQuestsWithSignupsAsync();
        return View(quests);
    }
}