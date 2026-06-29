using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.Components.Emails;
using QuestBoard.Service.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace QuestBoard.UnitTests.Services;

public class ConfirmationEmailJobTests
{
    private readonly IEmailRenderService _renderService;
    private readonly IEmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConfirmationEmailJob _sut;

    public ConfirmationEmailJobTests()
    {
        _renderService = Substitute.For<IEmailRenderService>();
        _emailService  = Substitute.For<IEmailService>();

        var emailOptions = Substitute.For<IOptions<EmailSettings>>();
        emailOptions.Value.Returns(new EmailSettings { AppUrl = "https://example.com" });

        // Build the IServiceScopeFactory → IServiceScope → IServiceProvider chain
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEmailRenderService)).Returns(_renderService);
        serviceProvider.GetService(typeof(IEmailService)).Returns(_emailService);
        serviceProvider.GetService(typeof(IOptions<EmailSettings>)).Returns(emailOptions);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateAsyncScope().Returns(new AsyncServiceScope(scope));

        var logger = Substitute.For<ILogger<ConfirmationEmailJob>>();
        _sut = new ConfirmationEmailJob(_scopeFactory, logger);
    }

    // D-02: render-parameter dictionary contains the correct keys and values
    [Fact]
    public async Task ExecuteAsync_CallsRenderAsync_WithCorrectParameters()
    {
        // Arrange
        const string toEmail     = "player@example.com";
        const string userName    = "TestUser";
        const string callbackUrl = "https://example.com/confirm?token=abc";

        _renderService
            .RenderAsync<ConfirmEmail>(Arg.Any<Dictionary<string, object?>>())
            .Returns(Task.FromResult("<html>confirm-body</html>"));

        // Act
        await _sut.ExecuteAsync(toEmail, userName, callbackUrl);

        // Assert: RenderAsync called once with the expected render-parameter dictionary
        await _renderService.Received(1).RenderAsync<ConfirmEmail>(
            Arg.Is<Dictionary<string, object?>>(d =>
                object.Equals(d[nameof(ConfirmEmail.UserName)],    "TestUser") &&
                object.Equals(d[nameof(ConfirmEmail.CallbackUrl)], callbackUrl) &&
                object.Equals(d[nameof(ConfirmEmail.AppUrl)],      "https://example.com")));
    }

    // D-03: rendered HTML and exact subject flow through to SendAsync
    [Fact]
    public async Task ExecuteAsync_CallsSendAsync_WithRenderedHtml()
    {
        // Arrange
        const string toEmail     = "player@example.com";
        const string sentinelHtml = "<html>confirm-body</html>";

        _renderService
            .RenderAsync<ConfirmEmail>(Arg.Any<Dictionary<string, object?>>())
            .Returns(Task.FromResult(sentinelHtml));

        // Act
        await _sut.ExecuteAsync(toEmail, "TestUser", "https://example.com/confirm?token=abc");

        // Assert: SendAsync called with exact recipient, subject, and HTML sentinel
        await _emailService.Received(1).SendAsync(
            "player@example.com",
            "Confirm your D&D Quest Board account",
            sentinelHtml);
    }
}
