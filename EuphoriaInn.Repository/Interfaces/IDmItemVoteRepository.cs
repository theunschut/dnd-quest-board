using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IDmItemVoteRepository : IBaseRepository<DmItemVoteEntity>
{
    Task<IList<DmItemVoteEntity>> GetVotesByItemAsync(int itemId, CancellationToken token = default);
    Task<IList<DmItemVoteEntity>> GetVotesByDmAsync(int dmId, CancellationToken token = default);
    Task<DmItemVoteEntity?> GetVoteAsync(int itemId, int dmId, CancellationToken token = default);
}