using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Repository.Entities;
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
        try
        {
            dbContext.ReminderLogs.Add(new ReminderLogEntity
            {
                QuestId = questId,
                PlayerId = playerId,
                SentAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("IX_ReminderLogs_QuestId_PlayerId") == true
               || ex.InnerException?.Message.Contains("unique") == true)
        {
            // Concurrent insertion — another job already logged this send. Safe to ignore.
        }
    }
}
