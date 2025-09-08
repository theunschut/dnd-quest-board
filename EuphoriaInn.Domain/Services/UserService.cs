using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EuphoriaInn.Domain.Services;

internal class UserService(UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager, IUserRepository repository, IMapper mapper) : BaseService<User, UserEntity>(repository, mapper), IUserService
{
    public async Task<IdentityResult> AddToRoleAsync(User user, string role)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return IdentityResult.Failed();
        return await userManager.AddToRoleAsync(userEntity, role);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword)
    {
        var userEntity = await userManager.GetUserAsync(user);
        if (userEntity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        return await userManager.ChangePasswordAsync(userEntity, oldPassword, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        return await userManager.ChangePasswordAsync(userEntity, oldPassword, newPassword);
    }

    public async Task<IdentityResult> CreateAsync(string email, string name, string password)
    {
        var user = new UserEntity
        {
            UserName = email,
            Email = email,
            Name = name
        };
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            // All new users start as Players by default
            await userManager.AddToRoleAsync(user, "Player");

            await signInManager.SignInAsync(user, isPersistent: false);
        }

        return result;
    }

    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await repository.ExistsAsync(name);
    }

    public async Task<IList<User>> GetAllDungeonMastersAsync(CancellationToken token = default)
    {
        var dms = await repository.GetAllDungeonMasters(token);
        return Mapper.Map<IList<User>>(dms);
    }

    public async Task<IList<User>> GetAllPlayersAsync(CancellationToken token = default)
    {
        var dms = await repository.GetAllPlayers(token);
        return Mapper.Map<IList<User>>(dms);
    }

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return [];
        return await userManager.GetRolesAsync(userEntity);
    }

    public async Task<User> GetUserAsync(ClaimsPrincipal user)
    {
        var userEntity = await userManager.GetUserAsync(user);
        return Mapper.Map<User>(userEntity);
    }

    public async Task<bool> IsInRoleAsync(User user, string role)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return false;
        return await userManager.IsInRoleAsync(userEntity, role);
    }

    public async Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role)
    {
        var userEntity = await userManager.GetUserAsync(user);
        if (userEntity == null) return false;
        return await userManager.IsInRoleAsync(userEntity, role);
    }

    public Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure) => signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure);

    public async Task<IdentityResult> RemoveFromRoleAsync(User user, string role)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return IdentityResult.Failed();
        return await userManager.RemoveFromRoleAsync(userEntity, role);
    }

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        return await userManager.ResetPasswordAsync(userEntity, token, newPassword);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ClaimsPrincipal adminUser, User user, string newPassword)
    {
        var adminEntity = await userManager.GetUserAsync(adminUser);
        if (adminEntity == null || !await userManager.IsInRoleAsync(adminEntity, "Admin"))
            return IdentityResult.Failed(new IdentityError { Description = "Admin user not found or not authorized." });

        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(userEntity);
        return await userManager.ResetPasswordAsync(userEntity, resetToken, newPassword);
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();
}