namespace QuestBoard.Repository.Interfaces;

public interface IBaseRepository<T>
{
    Task AddAsync(T entity, CancellationToken token = default);

    Task<IList<T>> GetAllAsync(CancellationToken token = default);

    Task<T?> GetByIdAsync(int id, CancellationToken token = default);

    Task RemoveAsync(T entity, CancellationToken token = default);

    Task UpdateAsync(T entity, CancellationToken token = default);
}