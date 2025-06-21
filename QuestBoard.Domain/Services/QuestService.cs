using AutoMapper;
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

    public override Task UpdateAsync(Quest model, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}