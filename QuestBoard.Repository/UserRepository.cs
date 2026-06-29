using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace QuestBoard.Repository;

internal class UserRepository(QuestBoardContext dbContext, IMapper mapper) : BaseRepository<User, UserEntity>(dbContext, mapper), IUserRepository
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await DbSet.AnyAsync(u => u.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default)
    {
        var entities = await DbSet
            .Where(u => DbContext.UserRoles
                .Any(ur => ur.UserId == u.Id &&
                          DbContext.Roles.Any(r => r.Id == ur.RoleId &&
                                                (r.Name == "DungeonMaster" || r.Name == "Admin"))))
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<User>>(entities);
    }

    public async Task<IList<User>> GetAllPlayers(CancellationToken token = default)
    {
        var entities = await DbSet
            .Where(u => DbContext.UserRoles
                .Any(ur => ur.UserId == u.Id &&
                          DbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Player")))
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<User>>(entities);
    }
}
