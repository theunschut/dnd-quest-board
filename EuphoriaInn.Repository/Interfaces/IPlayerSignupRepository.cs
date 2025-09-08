using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces
{
    public interface IPlayerSignupRepository : IBaseRepository<PlayerSignupEntity>
    {
        Task<PlayerSignupEntity?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default);
    }
}