using QuestBoard.Models;

namespace QuestBoard.Repository.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
    Task<IEnumerable<Quest>> GetQuestsWithSignupsAsync();
    Task<Quest?> GetQuestWithDetailsAsync(int id);
    Task<Quest?> GetQuestWithManageDetailsAsync(int id);
    Task<IEnumerable<Quest>> GetQuestsByDmNameAsync(string dmName);
}