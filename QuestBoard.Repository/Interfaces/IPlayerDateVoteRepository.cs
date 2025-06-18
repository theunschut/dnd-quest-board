using QuestBoard.Models;

namespace QuestBoard.Repository.Interfaces;

public interface IPlayerDateVoteRepository : IGenericRepository<PlayerDateVote>
{
    Task<IEnumerable<PlayerDateVote>> GetVotesByPlayerSignupIdAsync(int playerSignupId);
    Task<IEnumerable<PlayerDateVote>> GetVotesByProposedDateIdAsync(int proposedDateId);
}