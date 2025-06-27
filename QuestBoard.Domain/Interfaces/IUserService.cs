using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IUserService : IBaseService<User>
{
    Task<bool> ExistsAsync(string name);
}