using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class DungeonMasterRepository(QuestBoardContext context) : BaseRepository<DungeonMasterEntity>(context), IDungeonMasterRepositorry
{
}