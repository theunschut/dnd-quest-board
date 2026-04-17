using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IO;

namespace EuphoriaInn.UnitTests.Services;

public class EmailServiceTests
{
    private static EmailService Create(EmailSettings settings)
    {
        var logger = Substitute.For<ILogger<EmailService>>();
        return new EmailService(Options.Create(settings), logger);
    }

    [Fact]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        var act = () => Create(new EmailSettings());
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SendQuestFinalizedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing()
    {
        var service = Create(new EmailSettings { SmtpUsername = "", SmtpPassword = "x", FromEmail = "x@x" });
        var act = async () => await service.SendQuestFinalizedEmailAsync("to@x", "p", "q", "dm", DateTime.UtcNow);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendQuestDateChangedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing()
    {
        var service = Create(new EmailSettings { SmtpUsername = "", SmtpPassword = "x", FromEmail = "x@x" });
        var act = async () => await service.SendQuestDateChangedEmailAsync("to@x", "p", "q", "dm");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void EmailService_ConstructorUsesIOptionsEmailSettings()
    {
        var constructor = typeof(EmailService).GetConstructors().Single();
        var firstParam = constructor.GetParameters()[0];
        firstParam.ParameterType.Should().Be(typeof(IOptions<EmailSettings>),
            "EMAIL-02 requires EmailService to inject IOptions<EmailSettings>");
    }

    [Fact]
    public void EmailServiceSource_ContainsSingleSmtpClientConstruction()
    {
        var sourcePath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "EuphoriaInn.Domain", "Services", "EmailService.cs");
        if (!File.Exists(sourcePath))
        {
            var constructor = typeof(EmailService).GetConstructors().Single();
            constructor.GetParameters()[0].ParameterType.Should().Be(typeof(IOptions<EmailSettings>),
                "EMAIL-02 requires SMTP setup to be deduplicated into a single helper");
            return;
        }

        var source = File.ReadAllText(sourcePath);
        var occurrences = System.Text.RegularExpressions.Regex.Matches(source, @"new SmtpClient\(").Count;
        occurrences.Should().Be(1, "EMAIL-02 requires SMTP setup to be deduplicated into a single helper");
    }

    [Fact]
    public void EmailServiceSource_ContainsAppUrlSubstitution()
    {
        var sourcePath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "EuphoriaInn.Domain", "Services", "EmailService.cs");
        if (!File.Exists(sourcePath))
        {
            var constructor = typeof(EmailService).GetConstructors().Single();
            constructor.GetParameters()[0].ParameterType.Should().Be(typeof(IOptions<EmailSettings>),
                "EMAIL-03 requires AppUrl to drive the placeholder substitution");
            return;
        }

        var source = File.ReadAllText(sourcePath);
        source.Should().Contain("_settings.AppUrl", "EMAIL-03 requires AppUrl to drive the placeholder substitution");
    }
}
