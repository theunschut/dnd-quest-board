using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default);

    Task<IList<User>> GetAllPlayers(CancellationToken token = default);
}
