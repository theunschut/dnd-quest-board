using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default);

    Task<IList<User>> GetAllPlayers(CancellationToken token = default);

    Task<GroupRole?> GetGroupRoleAsync(int userId, int groupId);

    Task<int?> SetGroupRoleAsync(int userId, int groupId, GroupRole role);
}
