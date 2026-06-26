using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace EuphoriaInn.Service.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
public class AdminController(IUserService userService, IQuestService questService, IIdentityService identityService, IEmailService emailService) : Controller
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
                IsPlayer = roles.Contains("Player"),
                EmailConfirmed = user.EmailConfirmed
            });
        }

        // Sort by account type first (Admin, DM, Player), then alphabetically by name
        var sortedUsers = userViewModels
            .OrderBy(u => u.IsAdmin ? 0 : u.IsDungeonMaster ? 1 : 2)  // Admin=0, DM=1, Player=2
            .ThenBy(u => u.User.Name)
            .ToList();

        return View(sortedUsers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        // Remove other roles and add Admin role
        await userService.RemoveFromRoleAsync(user, "Player");
        await userService.RemoveFromRoleAsync(user, "DungeonMaster");
        await userService.AddToRoleAsync(user, "Admin");

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteFromAdmin(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        // Remove Admin role and add DungeonMaster role (since admin implies DM privileges)
        await userService.RemoveFromRoleAsync(user, "Admin");
        await userService.AddToRoleAsync(user, "DungeonMaster");

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToDM(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        await userService.RemoveFromRoleAsync(user, "Player");
        await userService.AddToRoleAsync(user, "DungeonMaster");

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteToPlayer(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        await userService.RemoveFromRoleAsync(user, "DungeonMaster");
        await userService.AddToRoleAsync(user, "Player");

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
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
                return RedirectToAction(nameof(Users));
            }

            user.Name = model.Name;
            user.Email = model.Email;
            user.HasKey = model.HasKey;

            // Role changes are handled through dedicated promotion/demotion buttons

            await userService.UpdateAsync(user);

            return RedirectToAction(nameof(Users));
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        var model = new ResetPasswordViewModel
        {
            UserId = user.Id,
            UserName = user.Name
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userService.GetByIdAsync(model.UserId);
            if (user == null)
            {
                return RedirectToAction(nameof(Users));
            }

            var result = await userService.ResetPasswordAsync(User, user, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password reset successfully for {user.Name}!";
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await userService.RemoveAsync(user);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendConfirmationEmail(int userId)
    {
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Users));
        }

        var rawToken = await identityService.GenerateEmailConfirmationAsync(userId);
        if (rawToken == null || string.IsNullOrEmpty(user.Email))
        {
            TempData["Error"] = $"Failed to send confirmation email to {user.Name}. Please try again.";
            return RedirectToAction(nameof(Users));
        }

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId, token = encodedToken }, Request.Scheme);

        var html = $"""
            <p>Hi {user.Name},</p>
            <p>Click the button below to confirm your email address and activate your Quest Board account.</p>
            <p><a href="{callbackUrl}" style="display:inline-block;padding:10px 20px;background:#0dcaf0;color:#000;text-decoration:none;border-radius:4px;">Confirm Email</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;

        await emailService.SendAsync(user.Email!, "Confirm your D&D Quest Board account", html);
        TempData["Success"] = $"Confirmation email sent to {user.Name}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Quests()
    {
        var allQuests = await questService.GetAllAsync();

        // Sort by creation date (newest first)
        var sortedQuests = allQuests
            .OrderByDescending(q => q.CreatedAt)
            .ToList();

        return View(sortedQuests);
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuest(int id)
    {
        var quest = await questService.GetByIdAsync(id);
        if (quest == null)
        {
            return NotFound();
        }

        await questService.RemoveAsync(quest);
        return Ok();
    }
}