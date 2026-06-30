using QuestBoard.Domain.Models;
using QuestBoard.Domain.Models.QuestBoard;

namespace QuestBoard.Domain.Interfaces;

public interface IQuestRepository : IBaseRepository<Quest>
{
    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsForCalendarAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default);

    Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default);

    Task OpenQuestAsync(int questId, CancellationToken token = default);

    Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);

    Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default);

    Task SetFinalizedEmailSentForDateAsync(int questId, DateTime date, CancellationToken token = default);

    Task<bool> HasFollowUpQuestAsync(int questId, CancellationToken token = default);

    Task<IList<Quest>> GetQuestsByDungeonMasterAsync(int dmUserId, CancellationToken token = default);

    Task<IList<Quest>> GetFinalizedQuestsForDateAsync(DateTime date, CancellationToken token = default);

    /// <summary>
    /// Returns all finalized quests for the given date across ALL groups.
    /// Bypasses the group query filter — use only for system-wide sweep operations (DailyReminderJob). (D-08)
    /// </summary>
    Task<IList<Quest>> GetQuestsForTomorrowAllGroupsAsync(DateTime date, CancellationToken token = default);
}
