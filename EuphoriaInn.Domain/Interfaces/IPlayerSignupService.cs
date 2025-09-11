using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IPlayerSignupService : IBaseService<PlayerSignup>
{
    Task UpdatePlayerDateVotesAsync(int playerSignupId, List<PlayerDateVote> dateVotes, CancellationToken cancellationToken = default);
}