using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.Components.Emails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QuestBoard.Service.Jobs;

public class QuestFinalizedEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestFinalizedEmailJob> logger)
{
    public async Task ExecuteAsync(
        int questId,
        DateTime finalizedDate,
        string[] recipientEmails,
        string[] playerNames,
        string questTitle,
        string dmName,
        string questDescription,
        int challengeRating,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
        var renderService   = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
        var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSettings   = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

        // Dedup guard (D-13): use .Date comparison — "same session date" intent, not same millisecond
        var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);
        if (quest?.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
        {
            logger.LogInformation(
                "Finalized email already sent for quest {QuestId} on {Date}. Skipping.",
                questId, finalizedDate);
            return;
        }

        var questUrl = $"{emailSettings.AppUrl}/Quest/Details/{questId}";

        for (var i = 0; i < recipientEmails.Length; i++)
        {
            var html = await renderService.RenderAsync<QuestFinalized>(new Dictionary<string, object?>
            {
                { nameof(QuestFinalized.QuestTitle),           questTitle },
                { nameof(QuestFinalized.DmName),               dmName },
                { nameof(QuestFinalized.QuestDate),            finalizedDate },
                { nameof(QuestFinalized.QuestDescription),     questDescription },
                { nameof(QuestFinalized.ConfirmedPlayerNames), playerNames.ToList() },
                { nameof(QuestFinalized.QuestUrl),             questUrl },
                { nameof(QuestFinalized.ChallengeRating),      challengeRating },
                { nameof(QuestFinalized.AppUrl),               emailSettings.AppUrl }
            });

            await emailService.SendAsync(
                recipientEmails[i],
                $"Your quest has been confirmed: {questTitle}",
                html);
        }

        await questRepository.SetFinalizedEmailSentForDateAsync(questId, finalizedDate, cancellationToken);
    }
}
