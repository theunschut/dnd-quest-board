using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Controllers;

public class HomeController(IQuestService questService, IUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        // Get current user if authenticated to check if they're a DM and for signup status
        string? currentUserName = null;
        int? currentUserId = null;
        Role userRole = Role.Player; // Default to Player role

        if (User.Identity?.IsAuthenticated == true)
        {
            var userEntity = await userService.GetUserAsync(User);
            if (userEntity != null)
            {
                var user = await userService.GetByIdAsync(userEntity.Id, token);
                currentUserName = user?.Name;
                currentUserId = user?.Id;

                // Determine user role for quest filtering
                var isAdmin = await userService.IsInRoleAsync(User, "Admin");
                var isDungeonMaster = await userService.IsInRoleAsync(User, "DungeonMaster");
                
                if (isAdmin)
                    userRole = Role.Admin;
                else if (isDungeonMaster)
                    userRole = Role.DungeonMaster;
            }
        }

        // Get quests filtered by user role
        var isAdminOrDm = userRole == Role.Admin || userRole == Role.DungeonMaster;
        var quests = await questService.GetQuestsWithSignupsForRoleAsync(isAdminOrDm, token);

        ViewBag.CurrentUserName = currentUserName;
        ViewBag.CurrentUserId = currentUserId;
        return View(quests);
    }
}