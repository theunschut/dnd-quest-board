using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<bool> ExistsAsync(string name);
}