using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal abstract class BaseRepository<TModel, TEntity>(QuestBoardContext dbContext, IMapper mapper)
    : IBaseRepository<TModel>
    where TModel : class, IModel
    where TEntity : class, IEntity
{
    protected QuestBoardContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();

    protected IMapper Mapper { get; } = mapper;

    public virtual async Task AddAsync(TModel model, CancellationToken token = default)
    {
        var entity = Mapper.Map<TEntity>(model);
        await DbSet.AddAsync(entity, token);
        await DbContext.SaveChangesAsync(token);
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken token = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken: token);
    }

    public virtual async Task<IList<TModel>> GetAllAsync(CancellationToken token = default)
    {
        var entities = await DbSet.ToListAsync(cancellationToken: token);
        return Mapper.Map<IList<TModel>>(entities);
    }

    public virtual async Task<TModel?> GetByIdAsync(int id, CancellationToken token = default)
    {
        var entity = await DbSet.FindAsync([id], cancellationToken: token);
        return entity == null ? null : Mapper.Map<TModel>(entity);
    }

    public virtual async Task RemoveAsync(TModel model, CancellationToken token = default)
    {
        var entity = await DbSet.FindAsync([model.Id]);
        if (entity == null) return;
        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync(token);
    }

    public Task SaveChangesAsync(CancellationToken token = default) => DbContext.SaveChangesAsync(token);

    public virtual async Task UpdateAsync(TModel model, CancellationToken token = default)
    {
        var entity = await DbSet.FindAsync([model.Id]);
        if (entity == null) return;
        Mapper.Map(model, entity);
        await DbContext.SaveChangesAsync(token);
    }
}
