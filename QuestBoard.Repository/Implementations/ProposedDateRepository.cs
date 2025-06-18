using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Interfaces;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Repository.Implementations;

public class ProposedDateRepository : GenericRepository<ProposedDate>, IProposedDateRepository
{
    public ProposedDateRepository(QuestBoardContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProposedDate>> GetByQuestIdAsync(int questId)
    {
        return await _dbSet
            .Where(pd => pd.QuestId == questId)
            .ToListAsync();
    }
}