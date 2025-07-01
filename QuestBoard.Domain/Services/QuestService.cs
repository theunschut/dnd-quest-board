using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services;

internal class QuestService(IQuestRepository repository, IMapper mapper) : BaseService<Quest, QuestEntity>(repository, mapper), IQuestService
{
    public async Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default)
    {
        var questEntities = await repository.GetQuestsByDmNameAsync(dmName, token);
        return Mapper.Map<IList<Quest>>(questEntities);
    }

    public async Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default)
    {
        var questEntities = await repository.GetQuestsWithDetailsAsync(token);
        return Mapper.Map<IList<Quest>>(questEntities);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default)
    {
        var questEntities = await repository.GetQuestsWithSignupsAsync(token);
        return Mapper.Map<IList<Quest>>(questEntities);
    }

    public async Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default)
    {
        if (await repository.GetQuestWithDetailsAsync(id, token) is not QuestEntity entity)
        {
            return null;
        }

        return Mapper.Map<Quest>(entity);
    }

    public async Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default)
    {
        if (await repository.GetQuestWithManageDetailsAsync(id, token) is not QuestEntity entity)
        {
            return null;
        }

        return Mapper.Map<Quest>(entity);
    }

    public override async Task UpdateAsync(Quest model, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(model.Id, token);
        if (entity == null) return;

        Mapper.Map(model, entity);
        await repository.SaveChangesAsync(token);
    }

    public override async Task RemoveAsync(Quest model, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(model.Id, token);
        if (entity == null) return;

        // Manual cleanup for PlayerSignups and their DateVotes (since Quest->PlayerSignup is NoAction)
        // Clear DateVotes from PlayerSignups first
        foreach (var playerSignup in entity.PlayerSignups)
        {
            playerSignup.DateVotes.Clear();
        }
        
        // Clear PlayerSignups (ProposedDates will be cascade deleted automatically)
        entity.PlayerSignups.Clear();

        await repository.RemoveAsync(entity, token);
    }

    public async Task UpdateQuestPropertiesAsync(int questId, string title, string description, Difficulty difficulty, int dungeonMasterId, int totalPlayerCount, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
        if (entity == null) return;

        // Update basic quest properties
        entity.Title = title;
        entity.Description = description;
        entity.Difficulty = (int)difficulty;
        entity.DungeonMasterId = dungeonMasterId;
        entity.TotalPlayerCount = totalPlayerCount;

        // Only update proposed dates if explicitly requested
        if (updateProposedDates && proposedDates != null)
        {
            // Remove existing proposed dates and add new ones
            // With cascade delete configured, PlayerDateVotes will be automatically deleted
            entity.ProposedDates.Clear();
            foreach (var proposedDate in proposedDates)
            {
                entity.ProposedDates.Add(new ProposedDateEntity
                {
                    Date = proposedDate,
                    Quest = entity,
                    QuestId = entity.Id
                });
            }
        }

        await repository.SaveChangesAsync(token);
    }

    public async Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
        if (entity == null) return;

        // Update quest finalization properties
        entity.IsFinalized = true;
        entity.FinalizedDate = finalizedDate;

        // Update player selections
        foreach (var playerSignup in entity.PlayerSignups)
        {
            playerSignup.IsSelected = selectedPlayerSignupIds.Contains(playerSignup.Id);
        }

        await repository.SaveChangesAsync(token);
    }

    public async Task OpenQuestAsync(int questId, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
        if (entity == null) return;

        // Update quest to open it back up
        entity.IsFinalized = false;
        entity.FinalizedDate = null;

        // Reset all player selections
        foreach (var playerSignup in entity.PlayerSignups)
        {
            playerSignup.IsSelected = false;
        }

        await repository.SaveChangesAsync(token);
    }
}