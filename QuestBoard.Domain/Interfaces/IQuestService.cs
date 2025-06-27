using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IQuestService : IBaseService<Quest>
{
    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);
}