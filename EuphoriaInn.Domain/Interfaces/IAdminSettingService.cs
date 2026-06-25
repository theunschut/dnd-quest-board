using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Interfaces;

public interface IAdminSettingService
{
    Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default);
    Task SaveSettingsAsync(string? url, string? secret, bool isEnabled, CancellationToken token = default);
}
