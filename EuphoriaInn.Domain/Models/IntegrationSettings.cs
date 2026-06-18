namespace EuphoriaInn.Domain.Models;

public record IntegrationSettings
{
    public string? OmphalosUrl { get; init; }
    public string? OmphalosSharedSecret { get; init; }
    public bool IsEnabled { get; init; }

    // Phase 11 checks this before showing any Omphalos UI element
    public bool IsConfigured => IsEnabled
        && !string.IsNullOrWhiteSpace(OmphalosUrl)
        && !string.IsNullOrWhiteSpace(OmphalosSharedSecret);
}
