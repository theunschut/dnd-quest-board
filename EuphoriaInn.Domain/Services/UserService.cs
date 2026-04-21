using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EuphoriaInn.Domain.Services;

internal class UserService(IIdentityService identityService, IUserRepository repository, IMapper mapper) : BaseService<User>(repository, mapper), IUserService
{
    public async Task<IdentityResult> AddToRoleAsync(User user, string role)
    {
        return await identityService.AddToRoleAsync(user.Id, role);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword)
    {
        return await identityService.ChangePasswordAsync(user, oldPassword, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword)
    {
        return await identityService.ChangePasswordAsync(user.Id, oldPassword, newPassword);
    }

    public async Task<IdentityResult> CreateAsync(string email, string name, string password)
    {
        return await identityService.CreateUserAsync(email, name, password);
    }

    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await repository.ExistsAsync(name);
    }

    public async Task<IList<User>> GetAllDungeonMastersAsync(CancellationToken token = default)
    {
        return await repository.GetAllDungeonMasters(token);
    }

    public async Task<IList<User>> GetAllPlayersAsync(CancellationToken token = default)
    {
        return await repository.GetAllPlayers(token);
    }

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        return await identityService.GetRolesAsync(user.Id);
    }

    public async Task<User> GetUserAsync(ClaimsPrincipal user)
    {
        var userId = await identityService.GetUserIdAsync(user);
        if (userId == null) return new User();
        return await repository.GetByIdAsync(userId.Value) ?? new User();
    }

    public async Task<bool> IsInRoleAsync(User user, string role)
    {
        return await identityService.IsInRoleAsync(user.Id, role);
    }

    public async Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role)
    {
        return await identityService.IsInRoleAsync(user, role);
    }

    public Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure) => identityService.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure);

    public async Task<IdentityResult> RemoveFromRoleAsync(User user, string role)
    {
        return await identityService.RemoveFromRoleAsync(user.Id, role);
    }

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
    {
        return await identityService.ResetPasswordAsync(user.Id, token, newPassword);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ClaimsPrincipal adminUser, User user, string newPassword)
    {
        return await identityService.AdminResetPasswordAsync(adminUser, user.Id, newPassword);
    }

    public Task SignOutAsync() => identityService.SignOutAsync();
}
