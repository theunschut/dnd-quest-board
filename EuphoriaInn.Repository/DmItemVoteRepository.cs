using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class DmItemVoteRepository(QuestBoardContext dbContext) : BaseRepository<DmItemVoteEntity>(dbContext), IDmItemVoteRepository
{
    public override async Task<IList<DmItemVoteEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.DmItemVotes
            .Include(v => v.Dm)
            .Include(v => v.ShopItem)
            .OrderByDescending(v => v.VoteDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<DmItemVoteEntity>> GetVotesByItemAsync(int itemId, CancellationToken token = default)
    {
        return await DbContext.DmItemVotes
            .Include(v => v.Dm)
            .Include(v => v.ShopItem)
            .Where(v => v.ShopItemId == itemId)
            .OrderByDescending(v => v.VoteDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<DmItemVoteEntity>> GetVotesByDmAsync(int dmId, CancellationToken token = default)
    {
        return await DbContext.DmItemVotes
            .Include(v => v.Dm)
            .Include(v => v.ShopItem)
            .Where(v => v.DmId == dmId)
            .OrderByDescending(v => v.VoteDate)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<DmItemVoteEntity?> GetVoteAsync(int itemId, int dmId, CancellationToken token = default)
    {
        return await DbContext.DmItemVotes
            .Include(v => v.Dm)
            .Include(v => v.ShopItem)
            .FirstOrDefaultAsync(v => v.ShopItemId == itemId && v.DmId == dmId, cancellationToken: token);
    }
}