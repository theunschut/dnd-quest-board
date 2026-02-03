using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;

public interface ICharacterService : IBaseService<Character>
{
    Task<IList<Character>> GetAllCharactersWithDetailsAsync(CancellationToken token = default);
    Task<IList<Character>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default);
    Task<Character?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default);
    Task<Character?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default);
    Task SetAsMainCharacterAsync(int characterId, int userId, CancellationToken token = default);
    Task<bool> ValidateCharacterClassLevelsAsync(int totalLevel, IList<CharacterClass> classes);
}
