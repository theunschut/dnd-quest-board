using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces
{
    public interface IPlayerSignupRepository : IBaseRepository<PlayerSignupEntity>
    {
        /// <summary>
        /// Returns a single player signup with its date votes loaded.
        /// </summary>
        Task<PlayerSignupEntity?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets or adds a Yes vote for the given proposed date on the signup, and marks the signup as selected.
        /// </summary>
        Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default);
    }
}