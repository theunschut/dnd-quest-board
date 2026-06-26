using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class ReminderLogRepository(QuestBoardContext dbContext) : IReminderLogRepository
{
    public async Task<bool> ExistsAsync(int questId, int playerId, CancellationToken token = default)
    {
        return await dbContext.ReminderLogs
            .AnyAsync(r => r.QuestId == questId && r.PlayerId == playerId, token);
    }

    public async Task AddAsync(int questId, int playerId, CancellationToken token = default)
    {
        dbContext.ReminderLogs.Add(new ReminderLogEntity
        {
            QuestId = questId,
            PlayerId = playerId,
            SentAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(token);
    }
}
