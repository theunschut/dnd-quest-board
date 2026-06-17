using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class DungeonMasterProfileRepository(QuestBoardContext dbContext, IMapper mapper)
    : BaseRepository<DungeonMasterProfile, DungeonMasterProfileEntity>(dbContext, mapper), IDungeonMasterProfileRepository
{
    public async Task<DungeonMasterProfile?> GetProfileByUserIdAsync(int userId, CancellationToken token = default)
    {
        var entity = await DbContext.DungeonMasterProfiles
            .Include(p => p.ProfileImage)
            .FirstOrDefaultAsync(p => p.Id == userId, token);
        return entity == null ? null : Mapper.Map<DungeonMasterProfile>(entity);
    }

    public async Task<byte[]?> GetProfilePictureAsync(int userId, CancellationToken token = default)
    {
        return await DbContext.DungeonMasterProfileImages
            .Where(p => p.Id == userId)
            .Select(p => p.ImageData)
            .FirstOrDefaultAsync(token);
    }

    public async Task UpsertProfileImageAsync(int userId, byte[]? imageData, CancellationToken token = default)
    {
        var entity = await DbContext.DungeonMasterProfiles
            .Include(p => p.ProfileImage)
            .FirstOrDefaultAsync(p => p.Id == userId, token);
        if (entity == null) return;

        if (imageData == null)
        {
            entity.ProfileImage = null;
        }
        else if (entity.ProfileImage == null)
        {
            entity.ProfileImage = new DungeonMasterProfileImageEntity
            {
                Id = entity.Id,
                ImageData = imageData
            };
        }
        else
        {
            entity.ProfileImage.ImageData = imageData;
        }

        await DbContext.SaveChangesAsync(token);
    }
}
