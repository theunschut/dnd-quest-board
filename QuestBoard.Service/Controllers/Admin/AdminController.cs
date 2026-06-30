using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.Jobs;
using QuestBoard.Service.Services;
using QuestBoard.Service.ViewModels.AdminViewModels;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuestBoard.Service.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
public class AdminController(IUserService userService, IQuestService questService, IIdentityService identityService, IBackgroundJobClient jobClient, IHttpClientFactory httpClientFactory, IOptions<EmailSettings> emailOptions, IMemoryCache cache, IActiveGroupContext activeGroupContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var allUsers = await userService.GetAllAsync();
        var userViewModels = new List<UserManagementViewModel>();
        var groupId = activeGroupContext.ActiveGroupId;

        foreach (var user in allUsers)
        {
            GroupRole? groupRole = groupId.HasValue
                ? await userService.GetGroupRoleByIdAsync(user.Id, groupId.Value)
                : null;

            userViewModels.Add(new UserManagementViewModel
            {
                User = user,
                Roles = new List<string>(),
                IsAdmin = groupRole == GroupRole.Admin,
                IsDungeonMaster = groupRole == GroupRole.DungeonMaster,
                IsPlayer = groupRole == GroupRole.Player,
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
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return RedirectToAction(nameof(Users));
        await userService.SetGroupRoleAsync(userId, groupId.Value, GroupRole.Admin);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteFromAdmin(int userId)
    {
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return RedirectToAction(nameof(Users));
        await userService.SetGroupRoleAsync(userId, groupId.Value, GroupRole.DungeonMaster);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToDM(int userId)
    {
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return RedirectToAction(nameof(Users));
        await userService.SetGroupRoleAsync(userId, groupId.Value, GroupRole.DungeonMaster);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteToPlayer(int userId)
    {
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return RedirectToAction(nameof(Users));
        await userService.SetGroupRoleAsync(userId, groupId.Value, GroupRole.Player);
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

            var emailChanged = !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase);

            user.Name = model.Name;
            user.HasKey = model.HasKey;
            if (!emailChanged)
                user.Email = model.Email;

            // Role changes are handled through dedicated promotion/demotion buttons

            await userService.UpdateAsync(user);

            if (emailChanged && !string.IsNullOrEmpty(model.Email))
            {
                var rawToken = await identityService.GenerateChangeEmailTokenAsync(user.Id, model.Email);
                if (rawToken != null)
                {
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                    var callbackUrl = Url.Action("ConfirmEmailChange", "Account",
                        new { userId = user.Id, newEmail = model.Email, token = encodedToken }, Request.Scheme);
                    jobClient.Enqueue<ChangeEmailConfirmationJob>(j => j.ExecuteAsync(model.Email, user.Name, callbackUrl!, CancellationToken.None));
                    TempData["Success"] = $"A confirmation email has been sent to {model.Email} for {user.Name}. The address will update once confirmed.";
                    return RedirectToAction(nameof(Users));
                }
            }

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

        jobClient.Enqueue<ConfirmationEmailJob>(j => j.ExecuteAsync(user.Email!, user.Name, callbackUrl!, CancellationToken.None));
        TempData["Success"] = $"Confirmation email queued for {user.Name}.";
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

    [HttpGet]
    public async Task<IActionResult> EmailStats(bool force = false, CancellationToken token = default)
    {
        var apiKey = emailOptions.Value.ResendApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            return View(EmailStatsViewModel.MissingKey());

        const string cacheKey = "resend-email-stats";

        if (!force && cache.TryGetValue(cacheKey, out EmailStatsViewModel? cached) && cached is not null)
            return View(cached);

        cache.Remove(cacheKey);

        var (viewModel, error) = await GetResendStatsAsync(apiKey, token);

        if (error)
            return View(EmailStatsViewModel.ApiError());

        cache.Set(cacheKey, viewModel,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return View(viewModel);
    }

    private async Task<(EmailStatsViewModel stats, bool error)> GetResendStatsAsync(
        string apiKey, CancellationToken token)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Resend");
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var collected = new List<ResendEmailRecord>();
            string? afterId = null;
            bool hasMore = true;

            while (hasMore)
            {
                var url = afterId == null ? "emails?limit=100" : $"emails?limit=100&after={afterId}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await client.SendAsync(request, token);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(token);
                    Console.Error.WriteLine($"[Resend] {(int)response.StatusCode} {response.StatusCode} — {body}");
                    return (new EmailStatsViewModel(), true);
                }

                var json = await response.Content.ReadAsStringAsync(token);
                Console.Error.WriteLine($"[Resend] raw: {json[..Math.Min(500, json.Length)]}");
                var result = JsonSerializer.Deserialize<ResendEmailListResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data == null || result.Data.Count == 0) break;

                bool reachedCutoff = false;
                foreach (var email in result.Data)
                {
                    if (email.CreatedAt.UtcDateTime < cutoff) { reachedCutoff = true; break; }
                    collected.Add(email);
                }

                if (reachedCutoff || result.Data.Count < 100) hasMore = false;
                else afterId = result.Data[^1].Id;
            }

            var counts = ResendStatsAggregator.Aggregate(collected, cutoff);

            return (new EmailStatsViewModel
            {
                Sent = counts.Sent,
                Delivered = counts.Delivered,
                Bounced = counts.Bounced,
                Failed = counts.Failed,
                AsOf = DateTime.UtcNow
            }, false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Resend] Exception: {ex.GetType().Name}: {ex.Message}");
            return (new EmailStatsViewModel(), true);
        }
    }
}
