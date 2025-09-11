using Microsoft.EntityFrameworkCore;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Repository;

internal class TradeItemRepository(QuestBoardContext dbContext) : BaseRepository<TradeItemEntity>(dbContext), ITradeItemRepository
{
    public override async Task<IList<TradeItemEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<TradeItemEntity>> GetAvailableTradeItemsAsync(CancellationToken token = default)
    {
        return await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.Status == 0) // Available
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<TradeItemEntity>> GetTradeItemsByPlayerAsync(int playerId, CancellationToken token = default)
    {
        return await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.OfferedByPlayerId == playerId)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<TradeItemEntity>> GetTradeItemsByStatusAsync(int status, CancellationToken token = default)
    {
        return await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.Status == status)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<TradeItemEntity?> GetTradeItemWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .FirstOrDefaultAsync(ti => ti.Id == id, cancellationToken: token);
    }
}