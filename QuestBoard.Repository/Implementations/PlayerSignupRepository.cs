using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Interfaces;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Repository.Implementations;

public class PlayerSignupRepository : GenericRepository<PlayerSignup>, IPlayerSignupRepository
{
    public PlayerSignupRepository(QuestBoardContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PlayerSignup>> GetByQuestIdAsync(int questId)
    {
        return await _dbSet
            .Where(ps => ps.QuestId == questId)
            .ToListAsync();
    }

    public async Task<PlayerSignup?> GetByQuestIdAndPlayerNameAsync(int questId, string playerName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ps => ps.QuestId == questId && 
                                     ps.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<PlayerSignup>> GetSelectedPlayersForQuestAsync(int questId)
    {
        return await _dbSet
            .Where(ps => ps.QuestId == questId && ps.IsSelected)
            .ToListAsync();
    }
}