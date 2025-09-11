using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<bool> ExistsAsync(string name);

    Task<IList<UserEntity>> GetAllDungeonMasters(CancellationToken token = default);

    Task<IList<UserEntity>> GetAllPlayers(CancellationToken token = default);
}