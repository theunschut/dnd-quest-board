using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IDungeonMasterService : IBaseService<DungeonMaster>
{
    Task<bool> ExistsAsync(string name);
}