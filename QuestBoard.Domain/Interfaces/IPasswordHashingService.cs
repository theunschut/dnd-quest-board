namespace QuestBoard.Domain.Interfaces;

/// <summary>
/// Interface for secure password hashing and verification
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a plain text password using PBKDF2 with salt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>The hashed password string with salt and iterations encoded</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain text password against a hashed password
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The hashed password to verify against</param>
    /// <returns>True if the password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}