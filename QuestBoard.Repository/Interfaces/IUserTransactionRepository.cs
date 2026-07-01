using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IUserTransactionRepository : IBaseRepository<UserTransactionEntity>
{
    /// <summary>
    /// Returns all transactions for the given user, newest first, with user and shop item loaded.
    /// </summary>
    Task<IList<UserTransactionEntity>> GetTransactionsByUserAsync(int userId, CancellationToken token = default);

    /// <summary>
    /// Returns all transactions for the given shop item, newest first, with user and shop item loaded.
    /// </summary>
    Task<IList<UserTransactionEntity>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default);

    /// <summary>
    /// Returns all transactions of the given type, newest first, with user and shop item loaded.
    /// </summary>
    Task<IList<UserTransactionEntity>> GetTransactionsByTypeAsync(int type, CancellationToken token = default);

    /// <summary>
    /// Returns a single transaction with user, shop item, and the item's creating DM loaded.
    /// </summary>
    Task<UserTransactionEntity?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default);
}