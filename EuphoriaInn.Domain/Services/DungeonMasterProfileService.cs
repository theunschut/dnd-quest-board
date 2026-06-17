using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;

internal class DungeonMasterProfileService(IDungeonMasterProfileRepository repository, IMapper mapper)
    : BaseService<DungeonMasterProfile>(repository, mapper), IDungeonMasterProfileService
{
    public async Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default)
    {
        return await repository.GetProfileByUserIdAsync(userId, token);
    }

    public async Task UpsertProfileAsync(int userId, string? bio, byte[]? imageBytes, CancellationToken token = default)
    {
        var profile = await repository.GetProfileByUserIdAsync(userId, token);
        if (profile == null)
        {
            // Lazy create per D-03 — profile entity does not exist until DM first saves
            var newProfile = new DungeonMasterProfile { Id = userId, Bio = bio };
            await repository.AddAsync(newProfile, token);
            if (imageBytes != null)
                await repository.UpsertProfileImageAsync(userId, imageBytes, token);
        }
        else
        {
            profile.Bio = bio;
            await repository.UpdateAsync(profile, token);
            if (imageBytes != null)
                await repository.UpsertProfileImageAsync(userId, imageBytes, token);
        }
    }

    public async Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default)
    {
        return await repository.GetProfilePictureAsync(userId, token);
    }
}
