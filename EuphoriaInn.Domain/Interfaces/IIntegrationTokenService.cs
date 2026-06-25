namespace EuphoriaInn.Domain.Interfaces;

public interface IIntegrationTokenService
{
    string GenerateSignedUrl(string omphalosBaseUrl, int questId, string questTitle, string username, string sharedSecret);
}
