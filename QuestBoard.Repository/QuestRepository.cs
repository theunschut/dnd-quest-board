using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository;

internal class QuestRepository(QuestBoardContext dbContext, IMapper mapper) : IQuestRepository
{
    public async Task AddAsync(Quest quest)
    {
        await dbContext.Quests.AddAsync(mapper.Map<QuestEntity>(quest));

        await dbContext.SaveChangesAsync();
    }

    public async Task AddAsync(PlayerSignup signup)
    {
        var entity = mapper.Map<PlayerSignupEntity>(signup);
        await dbContext.PlayerSignups.AddAsync(entity);

        await dbContext.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IList<Quest> quests)
    {
        var entities = mapper.Map<List<QuestEntity>>(quests);
        await dbContext.Quests.AddRangeAsync(entities);

        await dbContext.SaveChangesAsync();
    }

    public async Task<IList<Quest>> GetAllAsync()
    {
        var quests = await dbContext.Quests.ToListAsync();
        return mapper.Map<List<Quest>>(quests);
    }

    public async Task<Quest?> GetByIdAsync(int id)
    {
        var quest = await dbContext.Quests.FindAsync(id);
        return mapper.Map<Quest>(quest);
    }

    public async Task<IList<Quest>> GetQuestsByDmNameAsync(string dmName)
    {
        var quests = await dbContext.Quests
            .Include(q => q.PlayerSignups)
            .Where(q => q.DmName == dmName)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return mapper.Map<List<Quest>>(quests);
    }

    public async Task<IList<Quest>> GetQuestsWithSignupsAsync()
    {
        var quests = await dbContext.Quests
            .Include(q => q.PlayerSignups)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return mapper.Map<List<Quest>>(quests);
    }

    public async Task<Quest?> GetQuestWithDetailsAsync(int id)
    {
        var quest = await dbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);
        return mapper.Map<Quest>(quest);
    }

    public async Task<Quest?> GetQuestWithManageDetailsAsync(int id)
    {
        var quest = await dbContext.Quests
            .Include(q => q.ProposedDates)
                .ThenInclude(pd => pd.PlayerVotes)
                    .ThenInclude(pv => pv.PlayerSignup)
            .Include(q => q.PlayerSignups)
            .FirstOrDefaultAsync(q => q.Id == id);
        return mapper.Map<Quest>(quest);
    }

    public async Task RemoveAsync(Quest quest)
    {
        var entity = await dbContext.Quests.FindAsync(quest.Id);
        if (entity == null) return;

        dbContext.Quests.Remove(entity);

        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveRangeAsync(IList<Quest> quests)
    {
        var entities = mapper.Map<List<QuestEntity>>(quests);
        dbContext.Quests.RemoveRange(entities);

        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Quest quest)
    {
        var entity = await dbContext.Quests.FindAsync(quest.Id);
        if (entity == null) return false;

        entity.Title = quest.Title;
        entity.Description = quest.Description;
        entity.Difficulty = (int)quest.Difficulty;
        entity.IsFinalized = quest.IsFinalized;
        entity.FinalizedDate = quest.FinalizedDate;
        
        dbContext.Quests.Update(entity);

        await dbContext.SaveChangesAsync();
        return true;
    }
}