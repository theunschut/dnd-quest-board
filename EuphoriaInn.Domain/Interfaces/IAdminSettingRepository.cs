namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken token = default);
    Task StageUpsertAsync(string key, string? value, CancellationToken token = default);
    Task SaveAsync(CancellationToken token = default);
}
