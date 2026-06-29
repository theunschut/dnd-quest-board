using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IDungeonMasterProfileRepository : IBaseRepository<DungeonMasterProfile>
{
    Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default);
    Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default);
    Task UpsertProfileImageAsync(int userId, byte[]? imageData, CancellationToken token = default);
}
