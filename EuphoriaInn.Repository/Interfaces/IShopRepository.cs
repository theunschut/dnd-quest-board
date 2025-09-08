using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IShopRepository : IBaseRepository<ShopItemEntity>
{
    Task<IList<ShopItemEntity>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByStatusAsync(int status, CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByTypeAsync(int type, CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsWithVotesAsync(CancellationToken token = default);
    Task<ShopItemEntity?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByDmAsync(int dmId, CancellationToken token = default);
    Task<bool> HasDmVotedAsync(int itemId, int dmId, CancellationToken token = default);
    Task<int> GetYesVotesCountAsync(int itemId, CancellationToken token = default);
    Task<int> GetTotalDmCountAsync(CancellationToken token = default);
}