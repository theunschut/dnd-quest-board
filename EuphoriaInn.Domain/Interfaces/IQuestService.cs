using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Interfaces;

public interface IQuestService : IBaseService<Quest>
{
    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsForCalendarAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default);

    Task<ServiceResult<int>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);

    Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default);

    Task OpenQuestAsync(int questId, CancellationToken token = default);

    Task<IList<Quest>> GetCompletedQuestsAsync(CancellationToken token = default);

    Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default);

    /// <summary>
    /// Creates a follow-up quest from a finalized original quest.
    /// Copies Title+" - Part 2", Description, ChallengeRating, TotalPlayerCount, DungeonMasterId (D-01, D-02).
    /// Clears ProposedDates (D-03). Resets DungeonMasterSession to false (D-04).
    /// Bulk-imports IsSelected=true signups from original as SignupRole.Player (D-05, D-06, D-07).
    /// Returns the Id of the newly created follow-up quest.
    /// </summary>
    Task<int> CreateFollowUpQuestAsync(int originalQuestId, CancellationToken token = default);

    /// <summary>
    /// Returns all quests where DungeonMasterId == dmUserId, ordered by most recent first.
    /// Includes both finalized and active quests (D-08).
    /// </summary>
    Task<IList<Quest>> GetQuestsByDungeonMasterAsync(int dmUserId, CancellationToken token = default);
}