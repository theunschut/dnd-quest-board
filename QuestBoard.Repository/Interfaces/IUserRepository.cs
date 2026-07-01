using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    /// <summary>
    /// Returns whether a user with the given name exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsAsync(string name);

    /// <summary>
    /// Returns all users holding the DungeonMaster or Admin group role in the active group.
    /// Returns an empty list when there is no active group.
    /// </summary>
    Task<IList<UserEntity>> GetAllDungeonMasters(CancellationToken token = default);

    /// <summary>
    /// Returns all users holding the Player group role in the active group.
    /// Returns an empty list when there is no active group.
    /// </summary>
    Task<IList<UserEntity>> GetAllPlayers(CancellationToken token = default);
}