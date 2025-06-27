using AutoMapper;
using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;
using System.Security.Claims;

namespace QuestBoard.Domain.Services;

internal class UserService(UserManager<UserEntity> userManager, IUserRepository repository, IMapper mapper) : BaseService<User, UserEntity>(repository, mapper), IUserService
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await repository.ExistsAsync(name);
    }

    public async Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default)
    {
        var dms = await repository.GetAllDungeonMasters(token);
        return Mapper.Map<IList<User>>(dms);
    }

    public async Task<IList<User>> GetAllPlayers(CancellationToken token = default)
    {
        var dms = await repository.GetAllPlayers(token);
        return Mapper.Map<IList<User>>(dms);
    }

    public async Task<User> GetUserAsync(ClaimsPrincipal user)
    {
        var userEntity = await userManager.GetUserAsync(user);
        return Mapper.Map<User>(userEntity);
    }

    public override Task UpdateAsync(User model, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}