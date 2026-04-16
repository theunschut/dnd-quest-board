using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class ShopRepository(QuestBoardContext dbContext, IMapper mapper) : BaseRepository<ShopItem, ShopItemEntity>(dbContext, mapper), IShopRepository
{
    public override async Task<IList<ShopItem>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Include(si => si.Transactions)
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<ShopItem>>(entities);
    }

    public async Task<IList<ShopItem>> GetPublishedItemsAsync(CancellationToken token = default)
    {
        var entities = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<ShopItem>>(entities);
    }

    public async Task<IList<ShopItem>> GetItemsByStatusAsync(int status, CancellationToken token = default)
    {
        var entities = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Status == status)
            .OrderByDescending(si => si.CreatedAt)
            .ThenBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<ShopItem>>(entities);
    }

    public async Task<IList<ShopItem>> GetItemsByTypeAsync(int type, CancellationToken token = default)
    {
        var entities = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.Type == type && si.Status == 1) // Published
            .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
            .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow)
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<ShopItem>>(entities);
    }

    public async Task<ShopItem?> GetItemWithDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Include(si => si.Transactions)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(si => si.Id == id, cancellationToken: token);
        return entity == null ? null : Mapper.Map<ShopItem>(entity);
    }

    public async Task<IList<ShopItem>> GetItemsByDmAsync(int dmId, CancellationToken token = default)
    {
        var entities = await DbContext.ShopItems
            .Include(si => si.CreatedByDm)
            .Where(si => si.CreatedByDmId == dmId)
            .OrderByDescending(si => si.CreatedAt)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<ShopItem>>(entities);
    }
}
