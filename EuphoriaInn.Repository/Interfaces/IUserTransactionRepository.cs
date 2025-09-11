using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IUserTransactionRepository : IBaseRepository<UserTransactionEntity>
{
    Task<IList<UserTransactionEntity>> GetTransactionsByUserAsync(int userId, CancellationToken token = default);
    Task<IList<UserTransactionEntity>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default);
    Task<IList<UserTransactionEntity>> GetTransactionsByTypeAsync(int type, CancellationToken token = default);
    Task<UserTransactionEntity?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default);
}