using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Controllers;

public class HomeController(IQuestService questService, IUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        var quests = await questService.GetQuestsWithSignupsAsync(token);

        // Get current user if authenticated to check if they're a DM and for signup status
        string? currentUserName = null;
        int? currentUserId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userEntity = await userService.GetUserAsync(User);
            if (userEntity != null)
            {
                var user = await userService.GetByIdAsync(userEntity.Id, token);
                currentUserName = user?.Name;
                currentUserId = user?.Id;
            }
        }

        ViewBag.CurrentUserName = currentUserName;
        ViewBag.CurrentUserId = currentUserId;
        return View(quests);
    }
}