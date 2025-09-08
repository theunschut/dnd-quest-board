using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal class ShopService(IShopRepository repository, IDmItemVoteRepository voteRepository, IMapper mapper) : BaseService<ShopItem, ShopItemEntity>(repository, mapper), IShopService
{
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

    public async Task<IList<ShopItem>> GetItemsWithVotesAsync(CancellationToken token = default)
    {
        var itemEntities = await repository.GetItemsWithVotesAsync(token);
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

    public async Task<bool> HasDmVotedAsync(int itemId, int dmId, CancellationToken token = default)
    {
        return await repository.HasDmVotedAsync(itemId, dmId, token);
    }

    public async Task<int> GetYesVotesCountAsync(int itemId, CancellationToken token = default)
    {
        return await repository.GetYesVotesCountAsync(itemId, token);
    }

    public async Task<int> GetTotalDmCountAsync(CancellationToken token = default)
    {
        return await repository.GetTotalDmCountAsync(token);
    }

    // Business logic methods

    public async Task<decimal> CalculateItemPriceAsync(ItemRarity rarity, CancellationToken token = default)
    {
        // Implement Tasha's Cauldron pricing guidelines
        return rarity switch
        {
            ItemRarity.Common => 100m,
            ItemRarity.Uncommon => 500m,
            ItemRarity.Rare => 5000m,
            ItemRarity.VeryRare => 50000m,
            ItemRarity.Legendary => 200000m,
            _ => 100m
        };
    }

    public async Task SubmitForApprovalAsync(int itemId, CancellationToken token = default)
    {
        var itemEntity = await repository.GetByIdAsync(itemId, token);
        if (itemEntity != null && itemEntity.Status == (int)ItemStatus.Draft)
        {
            itemEntity.Status = (int)ItemStatus.UnderReview;
            await repository.UpdateAsync(itemEntity, token);
        }
    }

    public async Task VoteOnItemAsync(int itemId, int dmId, VoteType voteType, CancellationToken token = default)
    {
        // Check if DM has already voted
        var existingVote = await voteRepository.GetVoteAsync(itemId, dmId, token);
        
        if (existingVote != null)
        {
            // Update existing vote
            existingVote.VoteType = (int)voteType;
            existingVote.VoteDate = DateTime.UtcNow;
            await voteRepository.UpdateAsync(existingVote, token);
        }
        else
        {
            // Create new vote
            var newVote = new DmItemVoteEntity
            {
                ShopItemId = itemId,
                DmId = dmId,
                VoteType = (int)voteType,
                VoteDate = DateTime.UtcNow
            };
            await voteRepository.AddAsync(newVote, token);
        }
    }

    public async Task<bool> CheckApprovalStatusAsync(int itemId, CancellationToken token = default)
    {
        var yesVotes = await GetYesVotesCountAsync(itemId, token);
        var totalDms = await GetTotalDmCountAsync(token);
        
        // Require majority approval (more than 50% of DMs)
        return totalDms > 0 && yesVotes > (totalDms / 2);
    }

    public async Task PublishItemAsync(int itemId, CancellationToken token = default)
    {
        var itemEntity = await repository.GetByIdAsync(itemId, token);
        if (itemEntity != null && itemEntity.Status == (int)ItemStatus.UnderReview)
        {
            var isApproved = await CheckApprovalStatusAsync(itemId, token);
            if (isApproved)
            {
                itemEntity.Status = (int)ItemStatus.Published;
                await repository.UpdateAsync(itemEntity, token);
            }
        }
    }
}