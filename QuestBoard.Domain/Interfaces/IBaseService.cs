namespace QuestBoard.Domain.Interfaces;

public interface IBaseService<T>
{
    Task AddAsync(T model, CancellationToken token = default);

    Task<bool> ExistsAsync(int id, CancellationToken token = default);

    Task<IList<T>> GetAllAsync(CancellationToken token = default);

    Task<T?> GetByIdAsync(int id, CancellationToken token = default);

    Task RemoveAsync(T model, CancellationToken token = default);

    Task UpdateAsync(T model, CancellationToken token = default);
}