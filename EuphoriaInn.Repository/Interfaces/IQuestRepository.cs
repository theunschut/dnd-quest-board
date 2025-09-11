using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Repository.Interfaces;

public interface IQuestRepository : IBaseRepository<QuestEntity>
{
    Task<IList<QuestEntity>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<IList<QuestEntity>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default);

    Task<QuestEntity?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<QuestEntity?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);
}