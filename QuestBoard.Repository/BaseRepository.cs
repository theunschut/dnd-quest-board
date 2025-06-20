using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal abstract class BaseRepository<T>(QuestBoardContext dbContext) : IBaseRepository<T> where T : class, IEntity
{
    protected QuestBoardContext DbContext { get; } = dbContext;

    protected DbSet<T> DbSet { get; } = dbContext.Set<T>();

    public virtual async Task AddAsync(T entity, CancellationToken token = default)
    {
        await DbSet.AddAsync(entity, token);
        await DbContext.SaveChangesAsync(token);
    }

    public virtual async Task<IList<T>> GetAllAsync(CancellationToken token)
    {
        return await DbSet.ToListAsync(cancellationToken: token);
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken token)
    {
        return await DbSet.FindAsync([id], cancellationToken: token);
    }

    public virtual async Task RemoveAsync(T entity, CancellationToken token = default)
    {
        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync(token);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken token = default)
    {
        DbSet.Update(entity);
        await DbContext.SaveChangesAsync(token);
    }
}