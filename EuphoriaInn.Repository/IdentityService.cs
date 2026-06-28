using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Repository.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EuphoriaInn.Repository;

internal class IdentityService(UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager) : IIdentityService
{
    public async Task<IdentityResult> AddToRoleAsync(int userId, string role)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return IdentityResult.Failed();
        return await userManager.AddToRoleAsync(entity, role);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword)
    {
        var entity = await userManager.GetUserAsync(user);
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        return await userManager.ChangePasswordAsync(entity, oldPassword, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        return await userManager.ChangePasswordAsync(entity, oldPassword, newPassword);
    }

    public async Task<IdentityResult> CreateUserAsync(string email, string name, string password)
    {
        var entity = new UserEntity
        {
            UserName = email,
            Email = email,
            Name = name
        };
        var result = await userManager.CreateAsync(entity, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(entity, "Player");
            // Do not sign in until email is confirmed — the admin must send a confirmation
            // link first (via AdminController.SendConfirmationEmail).
        }

        return result;
    }

    public async Task<IList<string>> GetRolesAsync(int userId)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return [];
        return await userManager.GetRolesAsync(entity);
    }

    public async Task<int?> GetUserIdAsync(ClaimsPrincipal user)
    {
        var entity = await userManager.GetUserAsync(user);
        return entity?.Id;
    }

    public async Task<bool> IsInRoleAsync(int userId, string role)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return false;
        return await userManager.IsInRoleAsync(entity, role);
    }

    public async Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role)
    {
        var entity = await userManager.GetUserAsync(user);
        if (entity == null) return false;
        return await userManager.IsInRoleAsync(entity, role);
    }

    public Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure)
        => signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure);

    public async Task<IdentityResult> RemoveFromRoleAsync(int userId, string role)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return IdentityResult.Failed();
        return await userManager.RemoveFromRoleAsync(entity, role);
    }

    public async Task<IdentityResult> ResetPasswordAsync(int userId, string token, string newPassword)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        return await userManager.ResetPasswordAsync(entity, token, newPassword);
    }

    public async Task<IdentityResult> AdminResetPasswordAsync(ClaimsPrincipal adminUser, int targetUserId, string newPassword)
    {
        var adminEntity = await userManager.GetUserAsync(adminUser);
        if (adminEntity == null || !await userManager.IsInRoleAsync(adminEntity, "Admin"))
            return IdentityResult.Failed(new IdentityError { Description = "Admin user not found or not authorized." });

        var entity = await userManager.FindByIdAsync(targetUserId.ToString());
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(entity);
        return await userManager.ResetPasswordAsync(entity, resetToken, newPassword);
    }

    public async Task<int?> GetIdByEmailAsync(string email)
    {
        var entity = await userManager.FindByEmailAsync(email);
        return entity?.Id;
    }

    public async Task<string?> GenerateEmailConfirmationAsync(int userId)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return null;
        return await userManager.GenerateEmailConfirmationTokenAsync(entity);
    }

    public async Task<IdentityResult> ConfirmEmailAsync(int userId, string token)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        return await userManager.ConfirmEmailAsync(entity, token);
    }

    public async Task<string?> GenerateChangeEmailTokenAsync(int userId, string newEmail)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null) return null;
        return await userManager.GenerateChangeEmailTokenAsync(entity, newEmail);
    }

    public async Task<IdentityResult> ChangeEmailAsync(int userId, string newEmail, string token)
    {
        var entity = await userManager.FindByIdAsync(userId.ToString());
        if (entity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var result = await userManager.ChangeEmailAsync(entity, newEmail, token);
        if (result.Succeeded)
            await userManager.SetUserNameAsync(entity, newEmail);

        return result;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();
}
