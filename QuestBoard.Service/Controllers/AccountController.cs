using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Service.ViewModels.AccountViewModels;

namespace QuestBoard.Service.Controllers;

public class AccountController(IUserService userService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await userService.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await userService.CreateAsync(model.Email, model.Name, model.Password);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await userService.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await userService.GetUserAsync(User);
        var isDungeonMaster = await userService.IsInRoleAsync(user, "DungeonMaster");
        var isAdmin = await userService.IsInRoleAsync(user, "Admin");
        
        var model = new ProfileViewModel
        {
            User = user
        };

        ViewData["IsDungeonMaster"] = isDungeonMaster;
        ViewData["IsAdmin"] = isAdmin;

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit()
    {
        var user = await userService.GetUserAsync(User);
        var isDungeonMaster = await userService.IsInRoleAsync(user, "DungeonMaster");
        var isAdmin = await userService.IsInRoleAsync(user, "Admin");
        
        var model = new EditProfileViewModel
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
    [Authorize]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userService.GetUserAsync(User);
            
            user.Name = model.Name;
            user.Email = model.Email;
            user.HasKey = model.HasKey;

            // Role changes are now handled only through Admin User Management

            await userService.UpdateAsync(user);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}