using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IQuestService : IBaseService<Quest>
{
    Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default);

    Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default);

    Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default);

    Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default);
    
    Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);
    Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default);
    
    Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default);
    
    Task OpenQuestAsync(int questId, CancellationToken token = default);
}