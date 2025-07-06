using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class PlayerSignupRepository(QuestBoardContext context) : BaseRepository<PlayerSignupEntity>(context), IPlayerSignupRepository
{
    public async Task<PlayerSignupEntity?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }
}