using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface ITradeItemRepository : IBaseRepository<TradeItemEntity>
{
    Task<IList<TradeItemEntity>> GetAvailableTradeItemsAsync(CancellationToken token = default);
    Task<IList<TradeItemEntity>> GetTradeItemsByPlayerAsync(int playerId, CancellationToken token = default);
    Task<IList<TradeItemEntity>> GetTradeItemsByStatusAsync(int status, CancellationToken token = default);
    Task<TradeItemEntity?> GetTradeItemWithDetailsAsync(int id, CancellationToken token = default);
}