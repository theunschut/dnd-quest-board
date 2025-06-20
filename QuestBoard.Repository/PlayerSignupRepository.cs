using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class PlayerSignupRepository(QuestBoardContext context) : BaseRepository<PlayerSignupEntity>(context), IPlayerSignupRepository
{
}