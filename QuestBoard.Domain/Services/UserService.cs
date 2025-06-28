using AutoMapper;
using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;
using System.Security.Claims;

namespace QuestBoard.Domain.Services;

internal class UserService(UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager, IUserRepository repository, IMapper mapper) : BaseService<User, UserEntity>(repository, mapper), IUserService
{
    public async Task<IdentityResult> CreateAsync(string email, string name, string password, bool isDungeonMaster)
    {
        var user = new UserEntity
        {
            UserName = email,
            Email = email,
            Name = name,
            IsDungeonMaster = isDungeonMaster
        };
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
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
}