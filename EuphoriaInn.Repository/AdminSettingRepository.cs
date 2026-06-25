using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class AdminSettingRepository(QuestBoardContext dbContext) : IAdminSettingRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken token = default)
    {
        var entity = await dbContext.AdminSettings.FindAsync([key], cancellationToken: token);
        return entity?.Value;
    }

    public async Task StageUpsertAsync(string key, string? value, CancellationToken token = default)
    {
        var existing = await dbContext.AdminSettings.FindAsync([key], cancellationToken: token);
        if (existing == null)
        {
            await dbContext.AdminSettings.AddAsync(new AdminSettingEntity
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            }, token);
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task SaveAsync(CancellationToken token = default)
    {
        await dbContext.SaveChangesAsync(token);
    }
}
