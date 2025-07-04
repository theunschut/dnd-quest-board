using AutoMapper;
using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;
using System.Security.Claims;

namespace QuestBoard.Domain.Services;

internal class UserService(UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager, RoleManager<IdentityRole<int>> roleManager, IUserRepository repository, IMapper mapper) : BaseService<User, UserEntity>(repository, mapper), IUserService
{
    public async Task<IdentityResult> CreateAsync(string email, string name, string password, bool isDungeonMaster)
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
            // Assign role based on isDungeonMaster flag
            var role = isDungeonMaster ? "DungeonMaster" : "Player";
            await userManager.AddToRoleAsync(user, role);
            
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

    public async Task<User> GetUserAsync(ClaimsPrincipal user)
    {
        var userEntity = await userManager.GetUserAsync(user);
        return Mapper.Map<User>(userEntity);
    }

    public Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure) => signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure);

    public Task SignOutAsync() => signInManager.SignOutAsync();

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

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return new List<string>();
        return await userManager.GetRolesAsync(userEntity);
    }

    public async Task<IdentityResult> AddToRoleAsync(User user, string role)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return IdentityResult.Failed();
        return await userManager.AddToRoleAsync(userEntity, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(User user, string role)
    {
        var userEntity = await userManager.FindByIdAsync(user.Id.ToString());
        if (userEntity == null) return IdentityResult.Failed();
        return await userManager.RemoveFromRoleAsync(userEntity, role);
    }
}