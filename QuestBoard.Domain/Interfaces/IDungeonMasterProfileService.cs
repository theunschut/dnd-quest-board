using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IDungeonMasterProfileService : IBaseService<DungeonMasterProfile>
{
    Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default);
    Task UpsertProfileAsync(int userId, string? bio, byte[]? imageBytes, bool removeImage = false, CancellationToken token = default);
    Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default);
}
