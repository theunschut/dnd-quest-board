using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class DungeonMasterProfileRepository(QuestBoardContext dbContext, IMapper mapper)
    : BaseRepository<DungeonMasterProfile, DungeonMasterProfileEntity>(dbContext, mapper), IDungeonMasterProfileRepository
{
    // DungeonMasterProfileEntity uses DatabaseGeneratedOption.None (Id = UserId).
    // Two concurrent first-saves for the same user would both attempt an INSERT
    // for the same PK, causing a DbUpdateException.  Catch and retry with Update
    // to make the upsert safe under concurrent requests — mirrors QuestRepository.AddAsync.
    public override async Task AddAsync(DungeonMasterProfile model, CancellationToken token = default)
    {
        try
        {
            await base.AddAsync(model, token);
        }
        catch (DbUpdateException)
        {
            // A concurrent request already inserted the row — fall back to an update.
            await UpdateAsync(model, token);
        }
    }

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
