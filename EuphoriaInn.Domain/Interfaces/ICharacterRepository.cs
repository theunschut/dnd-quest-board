using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;

public interface ICharacterRepository : IBaseRepository<Character>
{
    Task<IList<Character>> GetAllCharactersWithDetailsAsync(CancellationToken token = default);
    Task<IList<Character>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default);
    Task<Character?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default);
    Task<Character?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default);
    Task<byte[]?> GetCharacterProfilePictureAsync(int id, CancellationToken token = default);
    Task UpdateProfileImageAsync(int characterId, byte[]? imageData, CancellationToken token = default);
}
