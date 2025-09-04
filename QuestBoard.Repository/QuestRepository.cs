using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class QuestRepository(QuestBoardContext dbContext) : BaseRepository<QuestEntity>(dbContext), IQuestRepository
{

    public override async Task<IList<QuestEntity>> GetAllAsync(CancellationToken token)
    {
        return await DbContext.Quests
            .Include(q => q.DungeonMaster)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<QuestEntity>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default)
    {
        return await DbContext.Quests
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .Where(q => q.DungeonMaster.Name == dmName)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<QuestEntity>> GetQuestsWithDetailsAsync(CancellationToken token = default)
    {
        return await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps.Player)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<QuestEntity>> GetQuestsWithSignupsAsync(CancellationToken token = default)
    {
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        
        return await DbContext.Quests
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .Where(q => !q.IsFinalized || (q.IsFinalized && q.FinalizedDate > oneDayAgo))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<IList<QuestEntity>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default)
    {
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        
        return await DbContext.Quests
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .Where(q => (!q.IsFinalized || (q.IsFinalized && q.FinalizedDate > oneDayAgo)) &&
                       (!q.DungeonMasterSession || isAdminOrDm))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<QuestEntity?> GetQuestWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken: token);
    }

    public async Task<QuestEntity?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default)
    {
        return await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps.Player)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken: token);
    }
}