using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IDungeonMasterRepositorry : IBaseRepository<DungeonMasterEntity>
{
    Task<bool> ExistsAsync(string name);
}