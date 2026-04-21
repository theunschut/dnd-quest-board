using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class TradeItemRepository(QuestBoardContext dbContext, IMapper mapper) : BaseRepository<TradeItem, TradeItemEntity>(dbContext, mapper), ITradeItemRepository
{
    public override async Task<IList<TradeItem>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<TradeItem>>(entities);
    }

    public async Task<IList<TradeItem>> GetAvailableTradeItemsAsync(CancellationToken token = default)
    {
        var entities = await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.Status == 0) // Available
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<TradeItem>>(entities);
    }

    public async Task<IList<TradeItem>> GetTradeItemsByPlayerAsync(int playerId, CancellationToken token = default)
    {
        var entities = await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.OfferedByPlayerId == playerId)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<TradeItem>>(entities);
    }

    public async Task<IList<TradeItem>> GetTradeItemsByStatusAsync(int status, CancellationToken token = default)
    {
        var entities = await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .Where(ti => ti.Status == status)
            .OrderByDescending(ti => ti.ListedDate)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<TradeItem>>(entities);
    }

    public async Task<TradeItem?> GetTradeItemWithDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await DbContext.TradeItems
            .Include(ti => ti.OfferedByPlayer)
            .FirstOrDefaultAsync(ti => ti.Id == id, cancellationToken: token);
        return entity == null ? null : Mapper.Map<TradeItem>(entity);
    }
}
