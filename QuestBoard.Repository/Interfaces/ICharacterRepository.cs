using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface ICharacterRepository : IBaseRepository<CharacterEntity>
{
    /// <summary>
    /// Returns all characters with owner, profile image, and classes loaded, active characters first, ordered by owner then character name.
    /// </summary>
    Task<IList<CharacterEntity>> GetAllCharactersWithDetailsAsync(CancellationToken token = default);

    /// <summary>
    /// Returns all characters owned by the given user, with owner, profile image, and classes loaded.
    /// Main character first, then active characters, then alphabetically.
    /// </summary>
    Task<IList<CharacterEntity>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default);

    /// <summary>
    /// Returns a single character with owner, profile image, and classes loaded.
    /// </summary>
    Task<CharacterEntity?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns the given user's character flagged as their main character, with profile image and classes loaded.
    /// </summary>
    Task<CharacterEntity?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default);

    /// <summary>
    /// Returns the raw profile picture bytes for a character, or null if none is set.
    /// </summary>
    Task<byte[]?> GetCharacterProfilePictureAsync(int id, CancellationToken token = default);
}
