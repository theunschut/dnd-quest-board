using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Domain.Enums;

namespace EuphoriaInn.Domain.Interfaces;

public interface IShopService : IBaseService<ShopItem>
{
    Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByStatusAsync(ItemStatus status, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByTypeAsync(ItemType type, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsWithVotesAsync(CancellationToken token = default);
    Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default);
    Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default);
    Task<bool> HasDmVotedAsync(int itemId, int dmId, CancellationToken token = default);
    Task<int> GetYesVotesCountAsync(int itemId, CancellationToken token = default);
    Task<int> GetTotalDmCountAsync(CancellationToken token = default);
    
    // Business logic methods
    Task<decimal> CalculateItemPriceAsync(ItemRarity rarity, CancellationToken token = default);
    Task SubmitForApprovalAsync(int itemId, CancellationToken token = default);
    Task VoteOnItemAsync(int itemId, int dmId, VoteType voteType, CancellationToken token = default);
    Task<bool> CheckApprovalStatusAsync(int itemId, CancellationToken token = default);
    Task PublishItemAsync(int itemId, CancellationToken token = default);
}