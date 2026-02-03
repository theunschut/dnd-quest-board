using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class UserTransactionRepository(QuestBoardContext dbContext) : BaseRepository<UserTransactionEntity>(dbContext), IUserTransactionRepository
{
    public override async Task<IList<UserTransactionEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.UserTransactions
            .Include(t => t.User)
            .Include(t => t.ShopItem)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<UserTransactionEntity>> GetTransactionsByUserAsync(int userId, CancellationToken token = default)
    {
        return await DbContext.UserTransactions
            .Include(t => t.User)
            .Include(t => t.ShopItem)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<UserTransactionEntity>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default)
    {
        return await DbContext.UserTransactions
            .Include(t => t.User)
            .Include(t => t.ShopItem)
            .Where(t => t.ShopItemId == itemId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<UserTransactionEntity>> GetTransactionsByTypeAsync(int type, CancellationToken token = default)
    {
        return await DbContext.UserTransactions
            .Include(t => t.User)
            .Include(t => t.ShopItem)
            .Where(t => t.TransactionType == type)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<UserTransactionEntity?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.UserTransactions
            .Include(t => t.User)
            .Include(t => t.ShopItem)
                .ThenInclude(si => si.CreatedByDm)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: token);
    }
}