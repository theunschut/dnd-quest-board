using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;

internal class AdminSettingService(IAdminSettingRepository repository) : IAdminSettingService
{
    public async Task<IntegrationSettings> GetSettingsAsync(CancellationToken token = default)
    {
        return new IntegrationSettings
        {
            OmphalosUrl = await repository.GetValueAsync("OmphalosUrl", token),
            OmphalosSharedSecret = await repository.GetValueAsync("OmphalosSharedSecret", token),
            IsEnabled = bool.TryParse(
                await repository.GetValueAsync("IsEnabled", token), out var enabled) && enabled
        };
    }

    public async Task SaveSettingsAsync(
        string? url,
        string? secret,
        bool isEnabled,
        CancellationToken token = default)
    {
        await repository.UpsertAsync("OmphalosUrl", url, token);
        await repository.UpsertAsync("IsEnabled", isEnabled.ToString(), token);

        // D-08: blank secret = preserve existing — skip upsert entirely
        if (!string.IsNullOrWhiteSpace(secret))
        {
            await repository.UpsertAsync("OmphalosSharedSecret", secret, token);
        }
    }
}
