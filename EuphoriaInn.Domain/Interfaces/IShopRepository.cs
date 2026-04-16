using EuphoriaInn.Domain.Models.Shop;

namespace EuphoriaInn.Domain.Interfaces;

public interface IShopRepository : IBaseRepository<ShopItem>
{
    Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByStatusAsync(int status, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByTypeAsync(int type, CancellationToken token = default);
    Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default);
}
