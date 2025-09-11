using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Domain.Enums;

namespace EuphoriaInn.Domain.Interfaces;

public interface IShopService : IBaseService<ShopItem>
{
    Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByStatusAsync(ItemStatus status, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByTypeAsync(ItemType type, CancellationToken token = default);
    Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default);
    
    // Business logic methods
    Task<decimal> CalculateItemPriceAsync(ItemRarity rarity, CancellationToken token = default);
    Task PublishItemAsync(int itemId, CancellationToken token = default);
    Task<UserTransaction> PurchaseItemAsync(int itemId, int quantity, User user, CancellationToken token = default);
    Task<UserTransaction> ReturnOrSellItemAsync(int transactionId, int quantity, User user, CancellationToken token = default);
    Task ArchiveItemAsync(int itemId, CancellationToken token = default);
    
    // Transaction methods
    Task<IList<UserTransaction>> GetUserTransactionsAsync(int userId, CancellationToken token = default);
    Task<IList<UserTransaction>> GetAllTransactionsAsync(CancellationToken token = default);
}