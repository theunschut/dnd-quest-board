using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class UserRepository(QuestBoardContext context) : BaseRepository<UserEntity>(context), IUserRepository
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await DbSet.AnyAsync(u => u.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task<IList<UserEntity>> GetAllDungeonMasters(CancellationToken token = default)
    {
        return await DbSet
            .Where(u => DbContext.UserRoles
                .Any(ur => ur.UserId == u.Id &&
                          DbContext.Roles.Any(r => r.Id == ur.RoleId &&
                                                (r.Name == "DungeonMaster" || r.Name == "Admin"))))
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<UserEntity>> GetAllPlayers(CancellationToken token = default)
    {
        return await DbSet
            .Where(u => DbContext.UserRoles
                .Any(ur => ur.UserId == u.Id &&
                          DbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Player")))
            .ToListAsync(cancellationToken: token);
    }
}