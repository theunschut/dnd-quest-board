using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IPlayerSignupService : IBaseService<PlayerSignup>
{
    Task UpdatePlayerDateVotesAsync(int playerSignupId, List<PlayerDateVote> dateVotes, CancellationToken cancellationToken = default);
}