using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services;

internal abstract class BaseService<TModel, TEntity>(IBaseRepository<TEntity> repository, IMapper mapper) : IBaseService<TModel>
    where TModel : class, IModel
{
    protected IMapper Mapper => mapper;

    public async Task AddAsync(TModel model, CancellationToken token = default)
    {
        var entity = mapper.Map<TEntity>(model);
        await repository.AddAsync(entity, token);
    }

    public async Task<IList<TModel>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await repository.GetAllAsync(token);
        return mapper.Map<IList<TModel>>(entities);
    }

    public async Task<TModel?> GetByIdAsync(int id, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(id, token);
        return mapper.Map<TModel>(entity);
    }

    public async Task RemoveAsync(TModel model, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(model.Id, token);
        if (entity == null) return;

        await repository.RemoveAsync(entity, token);
    }

    public abstract Task UpdateAsync(TModel model, CancellationToken token = default);
}