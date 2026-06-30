using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace QuestBoard.Repository;

internal class UserRepository(QuestBoardContext dbContext, IMapper mapper, IActiveGroupContext activeGroupContext)
    : BaseRepository<User, UserEntity>(dbContext, mapper), IUserRepository
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await DbSet.AnyAsync(u => u.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default)
    {
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return [];

        var entities = await DbSet
            .Where(u => DbContext.UserGroups
                .Any(ug => ug.UserId == u.Id
                        && ug.GroupId == groupId.Value
                        && (ug.GroupRole == (int)GroupRole.DungeonMaster
                            || ug.GroupRole == (int)GroupRole.Admin)))
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<User>>(entities);
    }

    public async Task<IList<User>> GetAllPlayers(CancellationToken token = default)
    {
        var groupId = activeGroupContext.ActiveGroupId;
        if (groupId == null) return [];

        var entities = await DbSet
            .Where(u => DbContext.UserGroups
                .Any(ug => ug.UserId == u.Id
                        && ug.GroupId == groupId.Value
                        && ug.GroupRole == (int)GroupRole.Player))
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<User>>(entities);
    }

    public async Task<GroupRole?> GetGroupRoleAsync(int userId, int groupId)
    {
        var ug = await DbContext.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        if (ug == null) return null;
        return (GroupRole)ug.GroupRole;
    }

    public async Task<int?> SetGroupRoleAsync(int userId, int groupId, GroupRole role)
    {
        var ug = await DbContext.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        if (ug == null) return null;
        ug.GroupRole = (int)role;
        await DbContext.SaveChangesAsync();
        return ug.Id;
    }
}
