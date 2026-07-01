using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface ITradeItemRepository : IBaseRepository<TradeItemEntity>
{
    /// <summary>
    /// Returns trade items with Available status, newest listed first, with the offering player loaded.
    /// </summary>
    Task<IList<TradeItemEntity>> GetAvailableTradeItemsAsync(CancellationToken token = default);

    /// <summary>
    /// Returns all trade items offered by the given player, newest listed first.
    /// </summary>
    Task<IList<TradeItemEntity>> GetTradeItemsByPlayerAsync(int playerId, CancellationToken token = default);

    /// <summary>
    /// Returns trade items matching the given status, newest listed first.
    /// </summary>
    Task<IList<TradeItemEntity>> GetTradeItemsByStatusAsync(int status, CancellationToken token = default);

    /// <summary>
    /// Returns a single trade item with the offering player loaded.
    /// </summary>
    Task<TradeItemEntity?> GetTradeItemWithDetailsAsync(int id, CancellationToken token = default);
}