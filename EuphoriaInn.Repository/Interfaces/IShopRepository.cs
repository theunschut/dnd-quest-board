using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IShopRepository : IBaseRepository<ShopItemEntity>
{
    Task<IList<ShopItemEntity>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByStatusAsync(int status, CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByTypeAsync(int type, CancellationToken token = default);
    Task<ShopItemEntity?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItemEntity>> GetItemsByDmAsync(int dmId, CancellationToken token = default);

    Task<(IList<ShopItemEntity> Items, int TotalCount)> GetPagedPublishedItemsAsync(
        int? type,
        IList<int>? rarityInts,
        string? sort,
        string? search,
        int page,
        int pageSize,
        CancellationToken token = default);
}