namespace QuestBoard.Repository.Interfaces;

public interface IBaseRepository<T>
{
    /// <summary>
    /// Inserts a new entity and persists it immediately, writing the DB-generated Id back onto the entity.
    /// </summary>
    Task AddAsync(T entity, CancellationToken token = default);

    /// <summary>
    /// Returns whether an entity with the given Id exists.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns every entity of this type, subject to any active group query filter.
    /// </summary>
    Task<IList<T>> GetAllAsync(CancellationToken token = default);

    /// <summary>
    /// Returns a single entity by Id, or null if not found.
    /// </summary>
    Task<T?> GetByIdAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Deletes the entity matching the given entity's Id and persists the change immediately.
    /// No-ops if the entity no longer exists.
    /// </summary>
    Task RemoveAsync(T entity, CancellationToken token = default);

    /// <summary>
    /// Persists any pending tracked changes on the underlying DbContext.
    /// </summary>
    Task SaveChangesAsync(CancellationToken token = default);

    /// <summary>
    /// Applies the given entity's values onto the persisted entity with the same Id and saves immediately.
    /// No-ops if the entity no longer exists.
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken token = default);
}