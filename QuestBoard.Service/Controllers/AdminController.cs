using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Service.ViewModels.AdminViewModels;

namespace QuestBoard.Service.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(IUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var allUsers = await userService.GetAllAsync();
        var userViewModels = new List<UserManagementViewModel>();

        foreach (var user in allUsers)
        {
            var roles = await userService.GetRolesAsync(user);
            userViewModels.Add(new UserManagementViewModel
            {
                User = user,
                Roles = roles,
                IsAdmin = roles.Contains("Admin"),
                IsDungeonMaster = roles.Contains("DungeonMaster"),
                IsPlayer = roles.Contains("Player")
            });
        }

        return View(userViewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Remove other roles and add Admin role
        await userService.RemoveFromRoleAsync(user, "Player");
        await userService.RemoveFromRoleAsync(user, "DungeonMaster");
        await userService.AddToRoleAsync(user, "Admin");

        TempData["SuccessMessage"] = $"{user.Name} has been promoted to Administrator.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteFromAdmin(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Remove Admin role and add DungeonMaster role (since admin implies DM privileges)
        await userService.RemoveFromRoleAsync(user, "Admin");
        await userService.AddToRoleAsync(user, "DungeonMaster");

        TempData["SuccessMessage"] = $"{user.Name} has been demoted from Administrator to Dungeon Master.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToDM(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        await userService.RemoveFromRoleAsync(user, "Player");
        await userService.AddToRoleAsync(user, "DungeonMaster");

        TempData["SuccessMessage"] = $"{user.Name} has been promoted to Dungeon Master.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteToPlayer(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        await userService.RemoveFromRoleAsync(user, "DungeonMaster");
        await userService.AddToRoleAsync(user, "Player");

        TempData["SuccessMessage"] = $"{user.Name} has been demoted to Player.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        var isDungeonMaster = await userService.IsInRoleAsync(user, "DungeonMaster");
        var isAdmin = await userService.IsInRoleAsync(user, "Admin");
        
        var model = new EditUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsDungeonMaster = isDungeonMaster || isAdmin,
            HasKey = user.HasKey
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userService.GetByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }
            
            user.Name = model.Name;
            user.Email = model.Email;
            user.HasKey = model.HasKey;

            // Handle role changes
            var currentIsDungeonMaster = await userService.IsInRoleAsync(user, "DungeonMaster");
            var currentIsAdmin = await userService.IsInRoleAsync(user, "Admin");
            var currentIsDMOrAdmin = currentIsDungeonMaster || currentIsAdmin;
            
            if (model.IsDungeonMaster != currentIsDMOrAdmin)
            {
                if (model.IsDungeonMaster)
                {
                    await userService.RemoveFromRoleAsync(user, "Player");
                    await userService.AddToRoleAsync(user, "DungeonMaster");
                }
                else
                {
                    await userService.RemoveFromRoleAsync(user, "DungeonMaster");
                    await userService.RemoveFromRoleAsync(user, "Admin");
                    await userService.AddToRoleAsync(user, "Player");
                }
            }

            await userService.UpdateAsync(user);

            TempData["SuccessMessage"] = $"{user.Name}'s profile has been updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        return View(model);
    }
}