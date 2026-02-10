using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IPlayerSignupService : IBaseService<PlayerSignup>
{
    Task UpdatePlayerDateVotesAsync(int playerSignupId, List<PlayerDateVote> dateVotes, CancellationToken cancellationToken = default);
    Task UpdateSignupCharacterAsync(int playerSignupId, int? characterId, CancellationToken cancellationToken = default);
    Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default);
}