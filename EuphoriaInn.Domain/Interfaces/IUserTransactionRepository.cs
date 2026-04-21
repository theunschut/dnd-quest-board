using EuphoriaInn.Domain.Models.Shop;

namespace EuphoriaInn.Domain.Interfaces;

public interface IUserTransactionRepository : IBaseRepository<UserTransaction>
{
    Task<IList<UserTransaction>> GetTransactionsByUserAsync(int userId, CancellationToken token = default);
    Task<IList<UserTransaction>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default);
    Task<IList<UserTransaction>> GetTransactionsByTypeAsync(int type, CancellationToken token = default);
    Task<UserTransaction?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default);
}
