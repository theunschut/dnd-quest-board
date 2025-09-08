using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal abstract class BaseService<TModel, TEntity>(IBaseRepository<TEntity> repository, IMapper mapper) : IBaseService<TModel>
    where TModel : class, IModel
{
    protected IMapper Mapper => mapper;

    public virtual async Task AddAsync(TModel model, CancellationToken token = default)
    {
        var entity = mapper.Map<TEntity>(model);
        await repository.AddAsync(entity, token);
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken token = default)
    {
        return await repository.ExistsAsync(id, token);
    }

    public virtual async Task<IList<TModel>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await repository.GetAllAsync(token);
        return mapper.Map<IList<TModel>>(entities);
    }

    public virtual async Task<TModel?> GetByIdAsync(int id, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(id, token);
        return mapper.Map<TModel>(entity);
    }

    public virtual async Task RemoveAsync(TModel model, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(model.Id, token);
        if (entity == null) return;

        await repository.RemoveAsync(entity, token);
    }

    public Task SaveChangesAsync(CancellationToken token = default) => repository.SaveChangesAsync(token);

    public virtual async Task UpdateAsync(TModel model, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(model.Id, token);
        if (entity == null) return;

        mapper.Map(model, entity);
        await repository.UpdateAsync(entity, token);
    }
}