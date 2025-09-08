using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Domain.Models.QuestBoard;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services;

internal class QuestService(IQuestRepository repository, IPlayerSignupRepository playerSignupRepository, IMapper mapper) : BaseService<Quest, QuestEntity>(repository, mapper), IQuestService
{
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

    public async Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default)
    {
        var questEntities = await repository.GetQuestsWithSignupsForRoleAsync(isAdminOrDm, token);
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

    public override async Task RemoveAsync(Quest model, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(model.Id, token);
        if (entity == null) return;

        // Manual cleanup required since Quest->PlayerSignup is NoAction to avoid cascade cycles
        // Remove PlayerSignups first (DateVotes will cascade delete from PlayerSignups)
        var playerSignupsToRemove = entity.PlayerSignups.ToList();
        foreach (var playerSignup in playerSignupsToRemove)
        {
            // DateVotes will cascade delete when PlayerSignup is removed
            await playerSignupRepository.RemoveAsync(playerSignup, token);
        }

        // ProposedDates will cascade delete automatically when Quest is removed
        await repository.RemoveAsync(entity, token);
    }

    public override async Task UpdateAsync(Quest model, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(model.Id, token);
        if (entity == null) return;

        Mapper.Map(model, entity);
        await repository.SaveChangesAsync(token);
    }

    public async Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
        if (entity == null) return;

        // Update basic quest properties
        entity.Title = title;
        entity.Description = description;
        entity.ChallengeRating = challengeRating;
        entity.TotalPlayerCount = totalPlayerCount;
        entity.DungeonMasterSession = dungeonMasterSession;

        // Only update proposed dates if explicitly requested
        if (updateProposedDates && proposedDates != null)
        {
            await UpdateProposedDatesIntelligentlyAsync(entity, proposedDates);
        }

        await repository.SaveChangesAsync(token);
    }

    public async Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        var entity = await repository.GetQuestWithManageDetailsAsync(questId, token);
        if (entity == null) return [];

        var affectedPlayers = new List<User>();

        // Update basic quest properties
        entity.Title = title;
        entity.Description = description;
        entity.ChallengeRating = challengeRating;
        entity.TotalPlayerCount = totalPlayerCount;
        entity.DungeonMasterSession = dungeonMasterSession;

        // Only update proposed dates if explicitly requested
        if (updateProposedDates && proposedDates != null)
        {
            affectedPlayers = await UpdateProposedDatesWithNotificationTrackingAsync(entity, proposedDates);
        }

        await repository.SaveChangesAsync(token);
        return affectedPlayers;
    }

    private static bool IsSameDateTime(DateTime date1, DateTime date2)
    {
        // Consider dates the same if they're within 30 minutes of each other
        return Math.Abs((date1 - date2).TotalMinutes) <= 30;
    }

    private static Task UpdateProposedDatesIntelligentlyAsync(QuestEntity entity, IList<DateTime> newProposedDates)
    {
        var existingDates = entity.ProposedDates.ToList();
        var datesToRemove = new List<ProposedDateEntity>();
        var datesToAdd = new List<DateTime>();
        var affectedPlayerIds = new List<int>();

        // Find dates that need to be removed (no longer in new list or significantly changed)
        foreach (var existingDate in existingDates)
        {
            var matchingNewDate = newProposedDates.FirstOrDefault(nd =>
                IsSameDateTime(existingDate.Date, nd));

            if (matchingNewDate == default)
            {
                // This date was removed or significantly changed
                datesToRemove.Add(existingDate);

                // Track players affected by this removal
                if (existingDate.PlayerVotes?.Count > 0)
                {
                    affectedPlayerIds.AddRange(existingDate.PlayerVotes.Select(pv => pv.PlayerSignup?.PlayerId ?? 0).Where(id => id != 0));
                }
            }
            else
            {
                // Date remains unchanged, update the date value but preserve votes
                existingDate.Date = matchingNewDate;
            }
        }

        // Find dates that need to be added (new dates not in existing list)
        foreach (var newDate in newProposedDates)
        {
            var matchingExistingDate = existingDates.FirstOrDefault(ed =>
                IsSameDateTime(ed.Date, newDate));

            if (matchingExistingDate == null)
            {
                // This is a completely new date
                datesToAdd.Add(newDate);
            }
        }

        // Remove obsolete dates (this will cascade delete PlayerDateVotes)
        foreach (var dateToRemove in datesToRemove)
        {
            entity.ProposedDates.Remove(dateToRemove);
        }

        // Add new dates
        foreach (var dateToAdd in datesToAdd)
        {
            entity.ProposedDates.Add(new ProposedDateEntity
            {
                Date = dateToAdd,
                Quest = entity,
                QuestId = entity.Id
            });
        }

        // Email notifications will be handled in the controller layer
        // Return information about affected players if needed
        return Task.CompletedTask;
    }

    private Task<List<User>> UpdateProposedDatesWithNotificationTrackingAsync(QuestEntity entity, IList<DateTime> newProposedDates)
    {
        var existingDates = entity.ProposedDates.ToList();
        var datesToRemove = new List<ProposedDateEntity>();
        var datesToAdd = new List<DateTime>();
        var affectedPlayers = new List<User>();

        // Find dates that need to be removed (no longer in new list or significantly changed)
        foreach (var existingDate in existingDates)
        {
            var matchingNewDate = newProposedDates.FirstOrDefault(nd =>
                IsSameDateTime(existingDate.Date, nd));

            if (matchingNewDate == default)
            {
                // This date was removed or significantly changed
                datesToRemove.Add(existingDate);

                // Track players affected by this removal
                if (existingDate.PlayerVotes?.Count > 0)
                {
                    var playersFromVotes = existingDate.PlayerVotes
                        .Where(pv => pv.PlayerSignup?.Player != null)
                        .Select(pv => Mapper.Map<User>(pv.PlayerSignup!.Player))
                        .ToList();

                    affectedPlayers.AddRange(playersFromVotes);
                }
            }
            else
            {
                // Date remains unchanged, update the date value but preserve votes
                existingDate.Date = matchingNewDate;
            }
        }

        // Find dates that need to be added (new dates not in existing list)
        foreach (var newDate in newProposedDates)
        {
            var matchingExistingDate = existingDates.FirstOrDefault(ed =>
                IsSameDateTime(ed.Date, newDate));

            if (matchingExistingDate == null)
            {
                // This is a completely new date
                datesToAdd.Add(newDate);
            }
        }

        // Remove obsolete dates (this will cascade delete PlayerDateVotes)
        foreach (var dateToRemove in datesToRemove)
        {
            entity.ProposedDates.Remove(dateToRemove);
        }

        // Add new dates
        foreach (var dateToAdd in datesToAdd)
        {
            entity.ProposedDates.Add(new ProposedDateEntity
            {
                Date = dateToAdd,
                Quest = entity,
                QuestId = entity.Id
            });
        }

        // Return unique affected players
        return Task.FromResult(affectedPlayers.GroupBy(p => p.Id).Select(g => g.First()).ToList());
    }
}