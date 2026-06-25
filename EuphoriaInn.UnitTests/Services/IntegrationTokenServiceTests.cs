using EuphoriaInn.Domain.Services;

namespace EuphoriaInn.UnitTests.Services;

public class IntegrationTokenServiceTests
{
    private static readonly IntegrationTokenService _sut = new();

    private static string? ExtractQueryParam(string url, string key)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query[key];
    }

    [Fact]
    public void GenerateSignedUrl_ReturnsUrlWithCorrectEndpointPath()
    {
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com", 42, "Dragon's Lair", "DMDave", "secret");

        url.Should().StartWith("https://omphalos.example.com/api/sso/open-quest");
    }

    [Fact]
    public void GenerateSignedUrl_TrimsTrailingSlashFromBaseUrl()
    {
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com/", 1, "Quest", "dm", "secret");

        url.Should().NotContain("//api/sso/open-quest");
        url.Should().Contain("/api/sso/open-quest");
    }

    [Fact]
    public void GenerateSignedUrl_LowercasesUsername()
    {
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com", 42, "Quest", "UPPERCASE_DM", "secret");

        url.Should().Contain("username=uppercase_dm");
    }

    [Fact]
    public void GenerateSignedUrl_PercentEncodesQuestTitle()
    {
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com", 1, "Dragon's Lair", "dm", "secret");

        // Uri.EscapeDataString: space → %20, apostrophe → %27
        url.Should().Contain("questTitle=Dragon%27s%20Lair");
    }

    [Fact]
    public void GenerateSignedUrl_SigIsLowercaseHex64Chars()
    {
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com", 1, "Quest", "dm", "secret");

        var sigValue = ExtractQueryParam(url, "sig");
        sigValue.Should().MatchRegex("^[0-9a-f]{64}$");  // SHA-256 = 32 bytes = 64 hex chars
    }

    [Fact]
    public void GenerateSignedUrl_ExpiryIsInFuture()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var url = _sut.GenerateSignedUrl("https://omphalos.example.com", 1, "Quest", "dm", "secret");
        var after = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds();

        var expiryStr = ExtractQueryParam(url, "expiry");
        var expiry = long.Parse(expiryStr!);
        expiry.Should().BeInRange(before + 299, after + 1);
    }

    [Fact]
    public void GenerateSignedUrl_DifferentSecretsProduceDifferentSigs()
    {
        var url1 = _sut.GenerateSignedUrl("https://x.com", 1, "Q", "dm", "secret1");
        var url2 = _sut.GenerateSignedUrl("https://x.com", 1, "Q", "dm", "secret2");

        ExtractQueryParam(url1, "sig").Should().NotBe(ExtractQueryParam(url2, "sig"));
    }
}
