using Microsoft.EntityFrameworkCore;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Repository;

internal class PlayerTransactionRepository(QuestBoardContext dbContext) : BaseRepository<PlayerTransactionEntity>(dbContext), IPlayerTransactionRepository
{
    public override async Task<IList<PlayerTransactionEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.PlayerTransactions
            .Include(t => t.Player)
            .Include(t => t.ShopItem)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<PlayerTransactionEntity>> GetTransactionsByPlayerAsync(int playerId, CancellationToken token = default)
    {
        return await DbContext.PlayerTransactions
            .Include(t => t.Player)
            .Include(t => t.ShopItem)
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<PlayerTransactionEntity>> GetTransactionsByItemAsync(int itemId, CancellationToken token = default)
    {
        return await DbContext.PlayerTransactions
            .Include(t => t.Player)
            .Include(t => t.ShopItem)
            .Where(t => t.ShopItemId == itemId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<PlayerTransactionEntity>> GetTransactionsByTypeAsync(int type, CancellationToken token = default)
    {
        return await DbContext.PlayerTransactions
            .Include(t => t.Player)
            .Include(t => t.ShopItem)
            .Where(t => t.TransactionType == type)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<PlayerTransactionEntity?> GetTransactionWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.PlayerTransactions
            .Include(t => t.Player)
            .Include(t => t.ShopItem)
                .ThenInclude(si => si.CreatedByDm)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: token);
    }
}