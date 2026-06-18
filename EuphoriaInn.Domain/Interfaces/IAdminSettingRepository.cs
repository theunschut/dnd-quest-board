namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken token = default);
    Task UpsertAsync(string key, string? value, CancellationToken token = default);
}
