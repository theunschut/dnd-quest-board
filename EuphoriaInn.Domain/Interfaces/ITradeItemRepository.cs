using EuphoriaInn.Domain.Models.Shop;

namespace EuphoriaInn.Domain.Interfaces;

public interface ITradeItemRepository : IBaseRepository<TradeItem>
{
    Task<IList<TradeItem>> GetAvailableTradeItemsAsync(CancellationToken token = default);
    Task<IList<TradeItem>> GetTradeItemsByPlayerAsync(int playerId, CancellationToken token = default);
    Task<IList<TradeItem>> GetTradeItemsByStatusAsync(int status, CancellationToken token = default);
    Task<TradeItem?> GetTradeItemWithDetailsAsync(int id, CancellationToken token = default);
}
