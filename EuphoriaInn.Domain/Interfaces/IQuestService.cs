using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IQuestService : IBaseService<Quest>
{
    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);

    Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);
    Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);

    Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default);

    Task OpenQuestAsync(int questId, CancellationToken token = default);

    Task<IList<Quest>> GetCompletedQuestsAsync(CancellationToken token = default);

    Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default);
}