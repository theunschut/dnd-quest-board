using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IShopRepository : IBaseRepository<ShopItemEntity>
{
    /// <summary>
    /// Returns published items currently within their availability window, ordered by name.
    /// </summary>
    Task<IList<ShopItemEntity>> GetPublishedItemsAsync(CancellationToken token = default);

    /// <summary>
    /// Returns items matching the given status, newest first then by name.
    /// </summary>
    Task<IList<ShopItemEntity>> GetItemsByStatusAsync(int status, CancellationToken token = default);

    /// <summary>
    /// Returns published items of the given type currently within their availability window, ordered by name.
    /// </summary>
    Task<IList<ShopItemEntity>> GetItemsByTypeAsync(int type, CancellationToken token = default);

    /// <summary>
    /// Returns a single item with its creating DM and transaction/purchaser details loaded.
    /// </summary>
    Task<ShopItemEntity?> GetItemWithDetailsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns all items created by the given DM, newest first.
    /// </summary>
    Task<IList<ShopItemEntity>> GetItemsByDmAsync(int dmId, CancellationToken token = default);

    /// <summary>
    /// Returns a filtered, sorted, and paged slice of published items currently within their availability window,
    /// along with the total matching count for pagination.
    /// </summary>
    Task<(IList<ShopItemEntity> Items, int TotalCount)> GetPagedPublishedItemsAsync(
        int? type,
        IList<int>? rarityInts,
        string? sort,
        string? search,
        int page,
        int pageSize,
        CancellationToken token = default);
}