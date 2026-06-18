using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Services;
using EuphoriaInn.IntegrationTests.Helpers;
using EuphoriaInn.Repository;

namespace EuphoriaInn.IntegrationTests.Services;

public class AdminSettingServiceTests : IDisposable
{
    private readonly TestDatabase _db;
    private readonly IAdminSettingService _sut;

    public AdminSettingServiceTests()
    {
        _db = new TestDatabase($"AdminSettingTest_{Guid.NewGuid():N}");
        var context = _db.CreateContext();
        var repo = new AdminSettingRepository(context);
        _sut = new AdminSettingService(repo);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenDbEmpty_ReturnsDefault()
    {
        var result = await _sut.GetSettingsAsync();

        result.Should().NotBeNull();
        result.IsEnabled.Should().BeFalse();
        result.OmphalosUrl.Should().BeNull();
        result.OmphalosSharedSecret.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingsAsync_AfterSave_ReturnsStoredValues()
    {
        await _sut.SaveSettingsAsync("https://omphalos.example.com", "secret123", true);

        var result = await _sut.GetSettingsAsync();

        result.OmphalosUrl.Should().Be("https://omphalos.example.com");
        result.IsEnabled.Should().BeTrue();
        result.OmphalosSharedSecret.Should().Be("secret123");
    }

    [Fact]
    public async Task SaveSettingsAsync_WithBlankSecret_PreservesExistingSecret()
    {
        // Arrange: set an initial secret
        await _sut.SaveSettingsAsync("https://omphalos.example.com", "initial-secret", true);

        // Act: save again with blank secret
        await _sut.SaveSettingsAsync("https://omphalos.example.com", null, true);

        // Assert: existing secret is preserved
        var result = await _sut.GetSettingsAsync();
        result.OmphalosSharedSecret.Should().Be("initial-secret");
    }

    [Fact]
    public async Task SaveSettingsAsync_CalledTwice_SecondOverwritesFirst()
    {
        await _sut.SaveSettingsAsync("https://first.com", "secret", false);
        await _sut.SaveSettingsAsync("https://second.com", "secret", true);

        var result = await _sut.GetSettingsAsync();
        result.OmphalosUrl.Should().Be("https://second.com");
        result.IsEnabled.Should().BeTrue();
    }

    public void Dispose() => _db.Dispose();
}
