using System.Security.Cryptography;
using System.Text;
using EuphoriaInn.Domain.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal class IntegrationTokenService : IIntegrationTokenService
{
    public string GenerateSignedUrl(
        string omphalosBaseUrl, int questId, string questTitle,
        string username, string sharedSecret)
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds();
        var encodedTitle = Uri.EscapeDataString(questTitle);  // percent-encoding: spaces → %20 (NOT UrlEncode which uses +)
        var lowerUser = username.ToLower();

        // Canonical message: keys in alphabetical order — locked by cross-repo contract (D-03)
        var message = $"expiry={expiry}&questId={questId}&questTitle={encodedTitle}&username={lowerUser}";

        var keyBytes = Encoding.UTF8.GetBytes(sharedSecret);
        var msgBytes = Encoding.UTF8.GetBytes(message);
        var hashBytes = HMACSHA256.HashData(keyBytes, msgBytes);
        var sig = Convert.ToHexString(hashBytes).ToLower();  // lowercase hex — .NET 5+ BCL

        // TrimEnd('/') prevents double-slash when OmphalosUrl has trailing slash
        return $"{omphalosBaseUrl.TrimEnd('/')}/api/sso/open-quest" +
               $"?expiry={expiry}&questId={questId}&questTitle={encodedTitle}&username={lowerUser}&sig={sig}";
    }
}
