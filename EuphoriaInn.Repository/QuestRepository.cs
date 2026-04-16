using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class QuestRepository(QuestBoardContext dbContext, IMapper mapper) : BaseRepository<Quest, QuestEntity>(dbContext, mapper), IQuestRepository
{
    public override async Task<IList<Quest>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await DbContext.Quests
            .Include(q => q.DungeonMaster)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName, CancellationToken token = default)
    {
        var entities = await ProjectWithoutCharacterImages(DbContext.Quests)
            .Where(q => q.DungeonMaster!.Name == dmName)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<IList<Quest>> GetQuestsWithDetailsAsync(CancellationToken token = default)
    {
        var entities = await ProjectWithoutCharacterImages(DbContext.Quests)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<IList<Quest>> GetQuestsForCalendarAsync(CancellationToken token = default)
    {
        var entities = await ProjectForCalendar(DbContext.Quests)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsAsync(CancellationToken token = default)
    {
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        var entities = await ProjectWithoutCharacterImages(DbContext.Quests)
            .Where(q => !q.IsFinalized || (q.IsFinalized && q.FinalizedDate > oneDayAgo))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsForRoleAsync(bool isAdminOrDm, CancellationToken token = default)
    {
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        var entities = await ProjectWithoutCharacterImages(DbContext.Quests)
            .Where(q => (!q.IsFinalized || (q.IsFinalized && q.FinalizedDate > oneDayAgo)) &&
                        (!q.DungeonMasterSession || isAdminOrDm))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<Quest>>(entities);
    }

    public async Task<Quest?> GetQuestWithDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await ProjectWithoutCharacterImages(DbContext.Quests)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken: token);
        return entity == null ? null : Mapper.Map<Quest>(entity);
    }

    public async Task<Quest?> GetQuestWithManageDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps!.Player)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.DungeonMaster)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken: token);
        return entity == null ? null : Mapper.Map<Quest>(entity);
    }

    public async Task<Quest?> GetQuestWithManageViewDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await ProjectWithoutCharacterImages(DbContext.Quests)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken: token);
        return entity == null ? null : Mapper.Map<Quest>(entity);
    }

    public async Task FinalizeQuestAsync(int questId, DateTime finalizedDate, IList<int> selectedPlayerSignupIds, CancellationToken token = default)
    {
        var entity = await DbContext.Quests
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken: token);
        if (entity == null) return;

        entity.IsFinalized = true;
        entity.FinalizedDate = finalizedDate;

        foreach (var playerSignup in entity.PlayerSignups)
        {
            // Auto-approve spectators (SignupRole = 1)
            if (playerSignup.SignupRole == 1)
            {
                playerSignup.IsSelected = true;
            }
            else
            {
                playerSignup.IsSelected = selectedPlayerSignupIds.Contains(playerSignup.Id);
            }
        }

        await DbContext.SaveChangesAsync(token);
    }

    public async Task OpenQuestAsync(int questId, CancellationToken token = default)
    {
        var entity = await DbContext.Quests
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken: token);
        if (entity == null) return;

        entity.IsFinalized = false;
        entity.FinalizedDate = null;

        foreach (var playerSignup in entity.PlayerSignups)
        {
            playerSignup.IsSelected = false;
        }

        await DbContext.SaveChangesAsync(token);
    }

    public async Task UpdateQuestPropertiesAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        var entity = await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken: token);
        if (entity == null) return;

        entity.Title = title;
        entity.Description = description;
        entity.ChallengeRating = challengeRating;
        entity.TotalPlayerCount = totalPlayerCount;
        entity.DungeonMasterSession = dungeonMasterSession;

        if (updateProposedDates && proposedDates != null)
        {
            UpdateProposedDatesIntelligently(entity, proposedDates);
        }

        await DbContext.SaveChangesAsync(token);
    }

    public async Task<IList<User>> UpdateQuestPropertiesWithNotificationsAsync(int questId, string title, string description, int challengeRating, int totalPlayerCount, bool dungeonMasterSession, bool updateProposedDates = false, IList<DateTime>? proposedDates = null, CancellationToken token = default)
    {
        var entity = await DbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps!.Player)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken: token);
        if (entity == null) return [];

        var affectedPlayerEntities = new List<UserEntity>();

        entity.Title = title;
        entity.Description = description;
        entity.ChallengeRating = challengeRating;
        entity.TotalPlayerCount = totalPlayerCount;
        entity.DungeonMasterSession = dungeonMasterSession;

        if (updateProposedDates && proposedDates != null)
        {
            affectedPlayerEntities = UpdateProposedDatesWithNotificationTracking(entity, proposedDates);
        }

        await DbContext.SaveChangesAsync(token);

        var affectedPlayers = Mapper.Map<IList<User>>(affectedPlayerEntities);
        return affectedPlayers.GroupBy(p => p.Id).Select(g => g.First()).ToList();
    }

    public async Task UpdateQuestRecapAsync(int questId, string recap, CancellationToken token = default)
    {
        var entity = await DbContext.Quests.FindAsync([questId], cancellationToken: token);
        if (entity == null) return;

        entity.Recap = recap;
        await DbContext.SaveChangesAsync(token);
    }

    private static bool IsSameDateTime(DateTime date1, DateTime date2)
    {
        return Math.Abs((date1 - date2).TotalMinutes) <= 30;
    }

    private static void UpdateProposedDatesIntelligently(QuestEntity entity, IList<DateTime> newProposedDates)
    {
        var existingDates = entity.ProposedDates.ToList();
        var datesToRemove = new List<ProposedDateEntity>();

        foreach (var existingDate in existingDates)
        {
            var matchingNewDate = newProposedDates.FirstOrDefault(nd => IsSameDateTime(existingDate.Date, nd));
            if (matchingNewDate == default)
            {
                datesToRemove.Add(existingDate);
            }
            else
            {
                existingDate.Date = matchingNewDate;
            }
        }

        foreach (var newDate in newProposedDates)
        {
            if (!existingDates.Any(ed => IsSameDateTime(ed.Date, newDate)))
            {
                entity.ProposedDates.Add(new ProposedDateEntity { Date = newDate, Quest = entity, QuestId = entity.Id });
            }
        }

        foreach (var dateToRemove in datesToRemove)
        {
            entity.ProposedDates.Remove(dateToRemove);
        }
    }

    private static List<UserEntity> UpdateProposedDatesWithNotificationTracking(QuestEntity entity, IList<DateTime> newProposedDates)
    {
        var existingDates = entity.ProposedDates.ToList();
        var datesToRemove = new List<ProposedDateEntity>();
        var affectedPlayerEntities = new List<UserEntity>();

        foreach (var existingDate in existingDates)
        {
            var matchingNewDate = newProposedDates.FirstOrDefault(nd => IsSameDateTime(existingDate.Date, nd));
            if (matchingNewDate == default)
            {
                datesToRemove.Add(existingDate);
                if (existingDate.PlayerVotes?.Count > 0)
                {
                    affectedPlayerEntities.AddRange(
                        existingDate.PlayerVotes
                            .Where(pv => pv.PlayerSignup?.Player != null)
                            .Select(pv => pv.PlayerSignup!.Player!));
                }
            }
            else
            {
                existingDate.Date = matchingNewDate;
            }
        }

        foreach (var newDate in newProposedDates)
        {
            if (!existingDates.Any(ed => IsSameDateTime(ed.Date, newDate)))
            {
                entity.ProposedDates.Add(new ProposedDateEntity { Date = newDate, Quest = entity, QuestId = entity.Id });
            }
        }

        foreach (var dateToRemove in datesToRemove)
        {
            entity.ProposedDates.Remove(dateToRemove);
        }

        return affectedPlayerEntities;
    }

    private static IQueryable<QuestEntity> ProjectForCalendar(IQueryable<QuestEntity> query)
    {
        return query
            .AsNoTracking()
            .AsSplitQuery()
            .Include(q => q.DungeonMaster)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps!.Player);
    }

    private static IQueryable<QuestEntity> ProjectWithoutCharacterImages(IQueryable<QuestEntity> query)
    {
        return query
            .AsNoTracking()
            .AsSplitQuery()
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
                        .ThenInclude(ps => ps!.Player)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Player)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.DateVotes)
            .Include(q => q.PlayerSignups)
                .ThenInclude(ps => ps.Character)
                    .ThenInclude(c => c!.Classes)
            .Include(q => q.DungeonMaster);
    }
}
