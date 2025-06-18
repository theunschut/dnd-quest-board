using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Interfaces;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Repository.Implementations;

public class QuestRepository : GenericRepository<Quest>, IQuestRepository
{
    public QuestRepository(QuestBoardContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Quest>> GetQuestsWithSignupsAsync()
    {
        return await _dbSet
            .Include(q => q.PlayerSignups)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Quest?> GetQuestWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Quest?> GetQuestWithManageDetailsAsync(int id)
    {
        return await _dbSet
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<Quest>> GetQuestsByDmNameAsync(string dmName)
    {
        return await _dbSet
            .Include(q => q.PlayerSignups)
            .Where(q => q.DmName.Equals(dmName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
}