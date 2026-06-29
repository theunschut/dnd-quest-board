using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.Components.Emails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QuestBoard.Service.Jobs;

public class ConfirmationEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ConfirmationEmailJob> logger)
{
    public async Task ExecuteAsync(string toEmail, string userName, string callbackUrl, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var renderService = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
        var emailService  = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

        var html = await renderService.RenderAsync<ConfirmEmail>(new Dictionary<string, object?>
        {
            { nameof(ConfirmEmail.UserName),    userName },
            { nameof(ConfirmEmail.CallbackUrl), callbackUrl },
            { nameof(ConfirmEmail.AppUrl),      emailSettings.AppUrl }
        });

        await emailService.SendAsync(toEmail, "Confirm your D&D Quest Board account", html);
    }
}
