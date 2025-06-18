using QuestBoard.Models;

namespace QuestBoard.Repository.Interfaces;

public interface IPlayerSignupRepository : IGenericRepository<PlayerSignup>
{
    Task<IEnumerable<PlayerSignup>> GetByQuestIdAsync(int questId);
    Task<PlayerSignup?> GetByQuestIdAndPlayerNameAsync(int questId, string playerName);
    Task<IEnumerable<PlayerSignup>> GetSelectedPlayersForQuestAsync(int questId);
}