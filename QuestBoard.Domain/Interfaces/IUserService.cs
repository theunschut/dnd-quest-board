using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace QuestBoard.Domain.Interfaces;

public interface IUserService : IBaseService<User>
{
    Task<IdentityResult> AddToRoleAsync(User user, string role);

    Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword);

    Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword);

    Task<IdentityResult> CreateAsync(string email, string name);

    Task<bool> ExistsAsync(string name);

    Task<IList<User>> GetAllDungeonMastersAsync(CancellationToken token = default);

    Task<IList<User>> GetAllPlayersAsync(CancellationToken token = default);

    Task<GroupRole?> GetGroupRoleAsync(ClaimsPrincipal user, int groupId);

    Task<GroupRole?> GetGroupRoleByIdAsync(int userId, int groupId);

    Task<IList<string>> GetRolesAsync(User user);

    Task<User> GetUserAsync(ClaimsPrincipal user);

    Task<bool> IsInRoleAsync(User user, string role);

    Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role);

    Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure);

    Task<IdentityResult> RemoveFromRoleAsync(User user, string role);

    Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);

    Task<IdentityResult> ResetPasswordAsync(ClaimsPrincipal adminUser, User user, string newPassword);

    Task<int?> SetGroupRoleAsync(int userId, int groupId, GroupRole role);

    Task SignOutAsync();
}
