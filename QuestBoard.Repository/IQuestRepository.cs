using QuestBoard.Domain.Models;

namespace QuestBoard.Repository;

public interface IQuestRepository
{
    Task AddAsync(Quest quest);

    Task AddAsync(PlayerSignup signup);

    Task AddRangeAsync(IList<Quest> quests);

    Task<IList<Quest>> GetAllAsync();

    Task<Quest?> GetByIdAsync(int id);

    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName);

    Task<IList<Quest>> GetQuestsWithSignupsAsync();

    Task<Quest?> GetQuestWithDetailsAsync(int id);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id);

    Task RemoveAsync(Quest quest);

    Task RemoveRangeAsync(IList<Quest> quests);

    Task<bool> UpdateAsync(Quest quest);
}