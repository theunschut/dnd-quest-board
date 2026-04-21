using EuphoriaInn.Domain.Models.Shop;

namespace EuphoriaInn.Domain.Interfaces;

public interface IShopRepository : IBaseRepository<ShopItem>
{
    Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByStatusAsync(int status, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByTypeAsync(int type, CancellationToken token = default);
    Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default);

    Task<(IList<ShopItem> Items, int TotalCount)> GetPagedPublishedItemsAsync(
        int? type,
        IList<int>? rarityInts,
        string? sort,
        string? search,
        int page,
        int pageSize,
        CancellationToken token = default);
}
