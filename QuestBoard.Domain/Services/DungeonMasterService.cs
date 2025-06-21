using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services;

internal class DungeonMasterService(IDungeonMasterRepositorry repository, IPasswordHashingService hashingService, IMapper mapper) : BaseService<DungeonMaster, DungeonMasterEntity>(repository, mapper), IDungeonMasterService
{
    public override Task AddAsync(DungeonMaster model, CancellationToken token = default)
    {
        var hashedPassword = hashingService.HashPassword(model.Password);
        model.Password = hashedPassword;
        return base.AddAsync(model, token);
    }

    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await repository.ExistsAsync(name);
    }

    public override Task UpdateAsync(DungeonMaster model, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}