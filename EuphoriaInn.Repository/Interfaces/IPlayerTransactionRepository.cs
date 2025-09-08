using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IPlayerTransactionRepository : IBaseRepository<PlayerTransactionEntity>
{
    Task<IList<PlayerTransactionEntity>> GetTransactionsByPlayerAsync(int playerId, CancellationToken token = default);
    Task<IList<PlayerTransactionEntity>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default);
    Task<IList<PlayerTransactionEntity>> GetTransactionsByTypeAsync(int type, CancellationToken token = default);
    Task<PlayerTransactionEntity?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default);
}