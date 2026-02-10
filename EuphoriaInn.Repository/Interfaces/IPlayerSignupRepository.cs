using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces
{
    public interface IPlayerSignupRepository : IBaseRepository<PlayerSignupEntity>
    {
        Task<PlayerSignupEntity?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default);
        Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default);
    }
}