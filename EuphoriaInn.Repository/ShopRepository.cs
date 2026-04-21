using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class ShopRepository(QuestBoardContext dbContext) : BaseRepository<ShopItemEntity>(dbContext), IShopRepository
{
    public override async Task<IList<ShopItemEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Include(si => si.Transactions)
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetPublishedItemsAsync(CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetItemsByStatusAsync(int status, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == status)
            .OrderByDescending(si => si.CreatedAt)
            .ThenBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<ShopItemEntity>> GetItemsByTypeAsync(int type, CancellationToken token = default)
    {
        return await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Type == type && si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Name)
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

    public async Task<(IList<ShopItemEntity> Items, int TotalCount)> GetPagedPublishedItemsAsync(
        int? type,
        IList<int>? rarityInts,
        string? sort,
        string? search,
        int page,
        int pageSize,
        CancellationToken token = default)
    {
        var query = DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == 1)
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow);

        if (type.HasValue)
            query = query.Where(si => si.Type == type.Value);

        if (rarityInts is { Count: > 0 })
            query = query.Where(si => rarityInts.Contains(si.Rarity));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(si => si.Name.Contains(search) || si.Description.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken: token);

        query = sort switch
        {
            "price_asc" => query.OrderBy(si => si.Price),
            "price_desc" => query.OrderByDescending(si => si.Price),
            _ => query.OrderBy(si => si.Name)
        };

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken: token);

        return (entities, totalCount);
    }
}