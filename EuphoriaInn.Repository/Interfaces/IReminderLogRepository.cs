namespace EuphoriaInn.Repository.Interfaces;

public interface IReminderLogRepository
{
    Task<bool> ExistsAsync(int questId, int playerId, CancellationToken token = default);
    Task AddAsync(int questId, int playerId, CancellationToken token = default);
}
