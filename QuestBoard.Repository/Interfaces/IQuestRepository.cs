using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Interfaces;

public interface IQuestRepository : IBaseRepository<QuestEntity>
{
    Task<IList<QuestEntity>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsForCalendarAsync(CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    Task<QuestEntity?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<QuestEntity?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);

    Task<QuestEntity?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default);

    Task<IList<QuestEntity>> GetFinalizedQuestsForDateAsync(DateTime date, CancellationToken token = default);
}