using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IQuestRepository : IBaseRepository<QuestEntity>
{
    /// <summary>
    /// Returns all quests with proposed dates, votes, signups, and DM/original/follow-up quest links loaded,
    /// excluding character profile images to keep the payload small.
    /// </summary>
    Task<IList<QuestEntity>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    /// <summary>
    /// Returns all quests projected for the monthly calendar view, including DM, signups, and proposed-date votes.
    /// </summary>
    Task<IList<QuestEntity>> GetQuestsForCalendarAsync(CancellationToken token = default);

    /// <summary>
    /// Returns open quests and quests finalized within the last day, newest first, excluding character profile images.
    /// </summary>
    Task<IList<QuestEntity>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    /// <summary>
    /// Returns open/recently-finalized quests, additionally filtering out DM-only sessions unless the caller is an Admin or DM.
    /// </summary>
    Task<IList<QuestEntity>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    /// <summary>
    /// Returns a single quest with proposed dates, votes, signups, and DM/original/follow-up quest links loaded.
    /// </summary>
    Task<QuestEntity?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns a single quest with the full management detail graph loaded, including nested player votes and signups.
    /// </summary>
    Task<QuestEntity?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns a single quest with the same detail graph as GetQuestWithDetailsAsync, for the manage-view page.
    /// </summary>
    Task<QuestEntity?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns finalized quests whose finalized date matches the given date, scoped to the active group.
    /// </summary>
    Task<IList<QuestEntity>> GetFinalizedQuestsForDateAsync(DateTime date, CancellationToken token = default);
}