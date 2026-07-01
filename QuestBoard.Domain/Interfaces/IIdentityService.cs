using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace QuestBoard.Domain.Interfaces;

/// <summary>
/// Abstracts ASP.NET Core Identity operations over user entities.
/// Implemented in the Repository layer where the concrete user entity is known.
/// </summary>
public interface IIdentityService
{
    Task<IdentityResult> AddToRoleAsync(int userId, string role);
    Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal user, string oldPassword, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<IdentityResult> CreateUserAsync(string email, string name);
    Task<IList<string>> GetRolesAsync(int userId);
    Task<int?> GetUserIdAsync(ClaimsPrincipal user);
    Task<bool> IsInRoleAsync(int userId, string role);
    Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role);
    Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure);
    Task<IdentityResult> RemoveFromRoleAsync(int userId, string role);
    Task<IdentityResult> ResetPasswordAsync(int userId, string token, string newPassword);
    Task<IdentityResult> AdminResetPasswordAsync(ClaimsPrincipal adminUser, int targetUserId, string newPassword);
    Task<bool> HasPasswordAsync(int userId);
    Task<string?> GeneratePasswordResetTokenForUserAsync(int userId);
    Task<IdentityResult> ConfirmEmailDirectlyAsync(int userId);
    Task<string?> GenerateChangeEmailTokenAsync(int userId, string newEmail);
    Task<IdentityResult> ChangeEmailAsync(int userId, string newEmail, string token);
    Task<int?> GetIdByEmailAsync(string email);
    Task SignOutAsync();
}
