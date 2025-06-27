using QuestBoard.Domain.Models;
using System.Security.Claims;

namespace QuestBoard.Domain.Interfaces;

public interface IUserService : IBaseService<User>
{
    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMasters(CancellationToken token = default);

    Task<IList<User>> GetAllPlayers(CancellationToken token = default);

    Task<User> GetUserAsync(ClaimsPrincipal user);
}