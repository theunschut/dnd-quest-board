using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Interfaces;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Repository.Implementations;

public class PlayerDateVoteRepository : GenericRepository<PlayerDateVote>, IPlayerDateVoteRepository
{
    public PlayerDateVoteRepository(QuestBoardContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PlayerDateVote>> GetVotesByPlayerSignupIdAsync(int playerSignupId)
    {
        return await _dbSet
            .Where(pv => pv.PlayerSignupId == playerSignupId)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlayerDateVote>> GetVotesByProposedDateIdAsync(int proposedDateId)
    {
        return await _dbSet
            .Where(pv => pv.ProposedDateId == proposedDateId)
            .ToListAsync();
    }
}