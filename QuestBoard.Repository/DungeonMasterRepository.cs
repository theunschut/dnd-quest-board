using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class DungeonMasterRepository(QuestBoardContext context) : BaseRepository<DungeonMasterEntity>(context), IDungeonMasterRepositorry
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await DbSet.AnyAsync(dm => dm.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }
}