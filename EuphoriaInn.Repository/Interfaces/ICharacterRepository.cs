using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface ICharacterRepository : IBaseRepository<CharacterEntity>
{
    Task<IList<CharacterEntity>> GetAllCharactersWithDetailsAsync(CancellationToken token = default);
    Task<IList<CharacterEntity>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default);
    Task<CharacterEntity?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default);
    Task<CharacterEntity?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default);
}
