using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Models;
using System.Security.Claims;

namespace QuestBoard.Domain.Interfaces;

public interface IUserService : IBaseService<User>
{
    Task<IdentityResult> CreateAsync(string email, string name, string password, bool isDungeonMaster);

    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMastersAsync(CancellationToken token = default);

    Task<IList<User>> GetAllPlayersAsync(CancellationToken token = default);

    Task<User> GetUserAsync(ClaimsPrincipal user);

    Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure);

    Task SignOutAsync();

    Task<bool> IsInRoleAsync(User user, string role);
    
    Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role);
    
    Task<IList<string>> GetRolesAsync(User user);
    
    Task<IdentityResult> AddToRoleAsync(User user, string role);
    
    Task<IdentityResult> RemoveFromRoleAsync(User user, string role);
}