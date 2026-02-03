using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal class ShopService(IShopRepository repository, IUserTransactionRepository transactionRepository, IMapper mapper) : BaseService<ShopItem, ShopItemEntity>(repository, mapper), IShopService
{
    public async Task<IList<ShopItem>> GetAllItemsAsync(CancellationToken token = default)
    {
        var itemEntities = await repository.GetAllAsync(token);
        return Mapper.Map<IList<ShopItem>>(itemEntities);
    }

    public async Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default)
    {
        var itemEntities = await repository.GetPublishedItemsAsync(token);
        return Mapper.Map<IList<ShopItem>>(itemEntities);
    }

    public async Task<IList<ShopItem>> GetItemsByStatusAsync(ItemStatus status, CancellationToken token = default)
    {
        var itemEntities = await repository.GetItemsByStatusAsync((int)status, token);
        return Mapper.Map<IList<ShopItem>>(itemEntities);
    }

    public async Task<IList<ShopItem>> GetItemsByTypeAsync(ItemType type, CancellationToken token = default)
    {
        var itemEntities = await repository.GetItemsByTypeAsync((int)type, token);
        return Mapper.Map<IList<ShopItem>>(itemEntities);
    }

    public async Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default)
    {
        var itemEntity = await repository.GetItemWithDetailsAsync(id, token);
        return itemEntity != null ? Mapper.Map<ShopItem>(itemEntity) : null;
    }

    public async Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default)
    {
        var itemEntities = await repository.GetItemsByDmAsync(dmId, token);
        return Mapper.Map<IList<ShopItem>>(itemEntities);
    }

    // Business logic methods

    public Task<decimal> CalculateItemPriceAsync(ItemRarity rarity, CancellationToken token = default)
    {
        // Implement Tasha's Cauldron pricing guidelines
        return Task.FromResult(rarity switch
        {
            ItemRarity.Common => 100m,
            ItemRarity.Uncommon => 500m,
            ItemRarity.Rare => 5000m,
            ItemRarity.VeryRare => 50000m,
            ItemRarity.Legendary => 200000m,
            _ => 100m
        });
    }

    public async Task PublishItemAsync(int itemId, CancellationToken token = default)
    {
        var itemEntity = await repository.GetByIdAsync(itemId, token);
        if (itemEntity != null)
        {
            itemEntity.Status = (int)ItemStatus.Published;
            await repository.UpdateAsync(itemEntity, token);
        }
    }

    public async Task<UserTransaction> PurchaseItemAsync(int itemId, int quantity, User user, CancellationToken token = default)
    {
        var itemEntity = await repository.GetByIdAsync(itemId, token);
        if (itemEntity == null || itemEntity.Status != (int)ItemStatus.Published)
        {
            throw new InvalidOperationException("Item is not available for purchase.");
        }

        // Check stock availability (-1 = unlimited, 0 = sold out, >0 = limited stock)
        if (itemEntity.Quantity == 0)
        {
            throw new InvalidOperationException("This item is sold out.");
        }
        
        // Only check quantity limits if not unlimited stock
        if (itemEntity.Quantity > 0 && itemEntity.Quantity < quantity)
        {
            throw new InvalidOperationException($"Only {itemEntity.Quantity} items available in stock.");
        }

        // Update item quantity only if it's limited stock (quantity > 0)
        // -1 = unlimited stock, don't modify
        // 0 = sold out, can't purchase (already checked above)
        if (itemEntity.Quantity > 0)
        {
            itemEntity.Quantity -= quantity;
            await repository.UpdateAsync(itemEntity, token);
        }

        // Create transaction record
        var transactionEntity = new UserTransactionEntity
        {
            ShopItemId = itemId,
            UserId = user.Id,
            Quantity = quantity,
            Price = itemEntity.Price * quantity,
            TransactionType = (int)TransactionType.Purchase,
            TransactionDate = DateTime.UtcNow,
            Notes = $"Purchase of {quantity}x {itemEntity.Name}"
        };

        await transactionRepository.AddAsync(transactionEntity, token);
        await transactionRepository.SaveChangesAsync(token);
        
        // Map back to domain model and return
        return Mapper.Map<UserTransaction>(transactionEntity);
    }

    public async Task<UserTransaction> ReturnOrSellItemAsync(int transactionId, int quantity, User user, CancellationToken token = default)
    {
        var originalTransaction = await transactionRepository.GetByIdAsync(transactionId, token);
        if (originalTransaction == null || originalTransaction.UserId != user.Id || originalTransaction.TransactionType != (int)TransactionType.Purchase)
        {
            throw new InvalidOperationException("Original purchase transaction not found or does not belong to the user.");
        }

        // Check how much has already been returned/sold for this original purchase
        var allUserTransactions = await transactionRepository.GetTransactionsByUserAsync(user.Id, token);
        var existingReturns = allUserTransactions
            .Where(t => t.TransactionType == (int)TransactionType.Sell && 
                       t.OriginalTransactionId == transactionId)
            .Sum(t => t.Quantity);

        var remainingQuantity = originalTransaction.Quantity - existingReturns;
        
        if (remainingQuantity <= 0)
        {
            throw new InvalidOperationException("This item has already been fully returned/sold.");
        }

        if (quantity > remainingQuantity)
        {
            throw new InvalidOperationException($"Cannot return/sell more items than remaining. Only {remainingQuantity} items can still be returned/sold.");
        }

        var itemEntity = await repository.GetByIdAsync(originalTransaction.ShopItemId, token)??throw new InvalidOperationException("Original item no longer exists.");

        // Calculate time since purchase
        var timeSincePurchase = DateTime.UtcNow - originalTransaction.TransactionDate;
        var isReturn = timeSincePurchase.TotalHours <= 24;

        // Calculate refund amount
        var originalUnitPrice = originalTransaction.Price / originalTransaction.Quantity;
        var refundAmount = isReturn ? originalUnitPrice * quantity : (originalUnitPrice * quantity * 0.5m);

        // Update item quantity if it's not unlimited (quantity != -1)
        if (itemEntity.Quantity >= 0)
        {
            itemEntity.Quantity += quantity;
            await repository.UpdateAsync(itemEntity, token);
        }

        // Create refund transaction record
        var refundTransactionEntity = new UserTransactionEntity
        {
            ShopItemId = originalTransaction.ShopItemId,
            UserId = user.Id,
            Quantity = quantity,
            Price = refundAmount,
            TransactionType = (int)TransactionType.Sell,
            TransactionDate = DateTime.UtcNow,
            OriginalTransactionId = transactionId,
            Notes = $"{(isReturn ? "Return" : "Sell")} of {quantity}x {itemEntity.Name}"
        };

        await transactionRepository.AddAsync(refundTransactionEntity, token);
        await transactionRepository.SaveChangesAsync(token);
        
        // Map back to domain model and return
        return Mapper.Map<UserTransaction>(refundTransactionEntity);
    }

    public async Task ArchiveItemAsync(int itemId, CancellationToken token = default)
    {
        var itemEntity = await repository.GetByIdAsync(itemId, token);
        if (itemEntity != null)
        {
            itemEntity.Status = (int)ItemStatus.Archived;
            await repository.UpdateAsync(itemEntity, token);
        }
    }

    public async Task<IList<UserTransaction>> GetUserTransactionsAsync(int userId, CancellationToken token = default)
    {
        var transactionEntities = await transactionRepository.GetTransactionsByUserAsync(userId, token);
        return Mapper.Map<IList<UserTransaction>>(transactionEntities);
    }

    public async Task<IList<UserTransaction>> GetAllTransactionsAsync(CancellationToken token = default)
    {
        var transactionEntities = await transactionRepository.GetAllAsync(token);
        return Mapper.Map<IList<UserTransaction>>(transactionEntities);
    }
}