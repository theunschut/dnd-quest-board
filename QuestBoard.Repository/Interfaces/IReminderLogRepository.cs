namespace QuestBoard.Repository.Interfaces;

public interface IReminderLogRepository
{
    /// <summary>
    /// Returns whether a reminder has already been logged as sent for the given quest/player pair.
    /// </summary>
    Task<bool> ExistsAsync(int questId, int playerId, CancellationToken token = default);

    /// <summary>
    /// Records that a reminder was sent for the given quest/player pair.
    /// Silently no-ops on a concurrent duplicate insert (unique index conflict).
    /// </summary>
    Task AddAsync(int questId, int playerId, CancellationToken token = default);
}
