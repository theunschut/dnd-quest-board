using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default);

    Task<IList<User>> GetAllPlayers(CancellationToken token = default);
}
