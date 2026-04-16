using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Domain.Services;

internal class QuestService(IQuestRepository repository, IPlayerSignupRepository playerSignupRepository, IMapper mapper) : BaseService<Quest>(repository, mapper), IQuestService
{
    public async Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default)
    {
        await repository.FinalizeQuestAsync(questId, finalizedDate, selectedPlayerSignupIds, token);
    }

    public async Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default)
    {
        return await repository.GetQuestsByDmNameAsync(dmName, token);
    }

    public async Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default)
    {
        return await repository.GetQuestsWithDetailsAsync(token);
    }

    public async Task<IList<Quest>> GetQuestsForCalendarAsync(CancellationToken token = default)
    {
        return await repository.GetQuestsForCalendarAsync(token);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default)
    {
        return await repository.GetQuestsWithSignupsAsync(token);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default)
    {
        return await repository.GetQuestsWithSignupsForRoleAsync(isAdminOrDm, token);
    }

    public async Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await repository.GetQuestWithDetailsAsync(id, token);
    }

    public async Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default)
    {
        return await repository.GetQuestWithManageDetailsAsync(id, token);
    }

    public async Task<Quest?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default)
    {
        return await repository.GetQuestWithManageViewDetailsAsync(id, token);
    }

    public async Task OpenQuestAsync(int questId, CancellationToken token = default)
    {
        await repository.OpenQuestAsync(questId, token);
    }

    public override async Task RemoveAsync(Quest model, CancellationToken token = default)
    {
        var quest = await repository.GetQuestWithManageDetailsAsync(model.Id, token);
        if (quest == null) return;

        // Manual cleanup required since Quest->PlayerSignup is NoAction to avoid cascade cycles
        // Remove PlayerSignups first (DateVotes will cascade delete from PlayerSignups)
        var playerSignupsToRemove = quest.PlayerSignups?.ToList() ?? [];
        foreach (var playerSignup in playerSignupsToRemove)
        {
            await playerSignupRepository.RemoveAsync(playerSignup, token);
        }

        // ProposedDates will cascade delete automatically when Quest is removed
        await repository.RemoveAsync(quest, token);
    }

    public override async Task UpdateAsync(Quest model, CancellationToken token = default)
    {
        await repository.UpdateAsync(model, token);
    }

    public async Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        await repository.UpdateQuestPropertiesAsync(questId, title, description, challengeRating, totalPlayerCount, dungeonMasterSession, updateProposedDates, proposedDates, token);
    }

    public async Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        return await repository.UpdateQuestPropertiesWithNotificationsAsync(questId, title, description, challengeRating, totalPlayerCount, dungeonMasterSession, updateProposedDates, proposedDates, token);
    }

    public async Task<IList<Quest>> GetCompletedQuestsAsync(CancellationToken token = default)
    {
        var quests = await repository.GetQuestsWithDetailsAsync(token);

        return quests
            .Where(q => q.IsFinalized && q.FinalizedDate.HasValue && q.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date)
            .OrderByDescending(q => q.FinalizedDate)
            .ToList();
    }

    public async Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default)
    {
        await repository.UpdateQuestRecapAsync(questId, recap, token);
    }
}
