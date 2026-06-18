using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IPlayerSignupRepository : IBaseRepository<PlayerSignup>
{
    Task<PlayerSignup?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default);
    Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default);
}
