using Microsoft.AspNetCore.Mvc;

namespace QuestBoard.Service.Controllers.QuestBoard;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
