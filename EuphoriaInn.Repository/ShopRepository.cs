using Microsoft.EntityFrameworkCore;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Repository;

internal class ShopRepository(QuestBoardContext dbContext) : BaseRepository<ShopItemEntity>(dbContext), IShopRepository
{
    public override async Task<IList<ShopItemEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Include(si => si.Transactions)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetPublishedItemsAsync(CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Type)
            .ThenBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetItemsByStatusAsync(int status, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == status)
            .OrderByDescending(si => si.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetItemsByTypeAsync(int type, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Type == type && si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Rarity)
            .ThenBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<ShopItemEntity?> GetItemWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Include(si => si.Transactions)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(si => si.Id == id, cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetItemsByDmAsync(int dmId, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.CreatedByDmId == dmId)
            .OrderByDescending(si => si.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }
}