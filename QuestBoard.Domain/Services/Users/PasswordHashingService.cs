using QuestBoard.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace QuestBoard.Domain.Services.Users;

internal class PasswordHashingService : IPasswordHashingService
{
    private readonly int _saltSize;
    private readonly int _hashSize;
    private readonly int _iterations;

    /// <summary>
    /// Initializes the password hashing service with PBKDF2 parameters
    /// </summary>
    /// <param name="saltSize">Size of the salt in bytes (default: 32)</param>
    /// <param name="hashSize">Size of the hash in bytes (default: 32)</param>
    /// <param name="iterations">Number of PBKDF2 iterations (default: 100000)</param>
    public PasswordHashingService(int saltSize = 32, int hashSize = 32, int iterations = 100000)
    {
        if (saltSize < 16)
            throw new ArgumentException("Salt size must be at least 16 bytes", nameof(saltSize));
        if (hashSize < 16)
            throw new ArgumentException("Hash size must be at least 16 bytes", nameof(hashSize));
        if (iterations < 10000)
            throw new ArgumentException("Iterations must be at least 10,000", nameof(iterations));

        _saltSize = saltSize;
        _hashSize = hashSize;
        _iterations = iterations;
    }

    /// <summary>
    /// Hashes a plain text password using PBKDF2 with SHA256
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>Base64 encoded string containing iterations, salt, and hash</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        // Generate a random salt
        byte[] salt = new byte[_saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash the password with PBKDF2
        byte[] hash = HashPasswordWithSalt(password, salt, _iterations);

        // Combine iterations, salt, and hash into a single string
        // Format: iterations.salt.hash (all base64 encoded)
        string result = $"{_iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        return result;
    }

    /// <summary>
    /// Verifies a plain text password against a hashed password
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The hashed password to verify against</param>
    /// <returns>True if the password matches, false otherwise</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
        }

        try
        {
            // Parse the hashed password
            string[] parts = hashedPassword.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] storedHash = Convert.FromBase64String(parts[2]);

            // Hash the provided password with the same salt and iterations
            byte[] computedHash = HashPasswordWithSalt(password, salt, iterations);

            // Compare the hashes using a constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch (Exception)
        {
            // If parsing or verification fails, return false
            return false;
        }
    }

    /// <summary>
    /// Checks if a password needs to be rehashed (useful for upgrading iterations)
    /// </summary>
    /// <param name="hashedPassword">The hashed password to check</param>
    /// <returns>True if the password should be rehashed with current iteration count</returns>
    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            return true;
        }

        try
        {
            string[] parts = hashedPassword.Split('.');
            if (parts.Length != 3)
            {
                return true;
            }

            int currentIterations = int.Parse(parts[0]);
            return currentIterations < _iterations;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Internal method to hash password with salt using PBKDF2
    /// </summary>
    private byte[] HashPasswordWithSalt(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(_hashSize);
    }

    /// <summary>
    /// Gets information about the current hashing configuration
    /// </summary>
    /// <returns>Configuration details as a string</returns>
    public string GetHashingInfo()
    {
        return $"PBKDF2-SHA256: {_iterations} iterations, {_saltSize}-byte salt, {_hashSize}-byte hash";
    }
}