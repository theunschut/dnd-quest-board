using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain;

internal class DungeonMasterService(IDungeonMasterRepositorry repository, IMapper mapper) : BaseService<DungeonMaster, DungeonMasterEntity>(repository, mapper), IDungeonMasterService
{
    public override Task UpdateAsync(DungeonMaster model, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}