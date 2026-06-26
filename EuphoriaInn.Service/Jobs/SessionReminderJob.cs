using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Service.Components.Emails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuphoriaInn.Service.Jobs;

public class SessionReminderJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionReminderJob> logger)
{
    public async Task ExecuteAsync(int questId, bool forceResend = false, bool useYesMaybeVoters = false, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();
        var reminderLog     = scope.ServiceProvider.GetRequiredService<IReminderLogRepository>();
        var renderService   = scope.ServiceProvider.GetRequiredService<IEmailRenderService>();
        var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSettings   = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

        var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);
        if (quest == null)
        {
            logger.LogWarning("SessionReminderJob: quest {QuestId} not found, skipping.", questId);
            return;
        }

        if (!quest.IsFinalized || !quest.FinalizedDate.HasValue)
        {
            logger.LogWarning("SessionReminderJob: quest {QuestId} is not finalized, skipping.", questId);
            return;
        }

        var questUrl = $"{emailSettings.AppUrl}/Quest/Details/{questId}";

        // Confirmed player names are always drawn from IsSelected players — this represents
        // who will actually attend the quest (shown in the email body to all recipients).
        var confirmedNames = quest.PlayerSignups
            .Where(ps => ps.IsSelected)
            .Select(ps => ps.Character?.Name ?? ps.Player.Name)
            .ToList();

        // Determine target recipient set:
        //   - Automated daily job (useYesMaybeVoters=false, D-06): IsSelected players only.
        //   - DM manual trigger (useYesMaybeVoters=true, D-08): Yes + Maybe voters on the
        //     finalized proposed date — not restricted to the finalized confirmed list.
        IEnumerable<Domain.Models.QuestBoard.PlayerSignup> targetSignups;
        if (useYesMaybeVoters)
        {
            // Filter by votes on the finalized proposed date to avoid including voters
            // who said Yes on a *different* proposed date (RESEARCH.md Pitfall 1).
            var finalizedProposedDate = quest.ProposedDates
                .FirstOrDefault(pd => pd.Date.Date == quest.FinalizedDate.Value.Date);

            targetSignups = quest.PlayerSignups.Where(ps => ps.DateVotes.Any(dv =>
                dv.ProposedDate?.Id == finalizedProposedDate?.Id &&
                (dv.Vote == VoteType.Yes || dv.Vote == VoteType.Maybe)));
        }
        else
        {
            targetSignups = quest.PlayerSignups.Where(ps => ps.IsSelected);
        }

        targetSignups = targetSignups.Where(ps => ps.Player != null && ps.Player.EmailConfirmed);

        foreach (var signup in targetSignups)
        {
            if (string.IsNullOrEmpty(signup.Player?.Email))
            {
                logger.LogWarning(
                    "SessionReminderJob: player {PlayerId} has no email, skipping.",
                    signup.Player?.Id);
                continue;
            }

            if (!forceResend && await reminderLog.ExistsAsync(questId, signup.Player.Id, cancellationToken))
            {
                logger.LogInformation(
                    "SessionReminderJob: reminder already sent for quest {QuestId} player {PlayerId}, skipping.",
                    questId, signup.Player.Id);
                continue;
            }

            var html = await renderService.RenderAsync<SessionReminder>(new Dictionary<string, object?>
            {
                { nameof(SessionReminder.QuestTitle),           quest.Title },
                { nameof(SessionReminder.DmName),               quest.DungeonMaster?.Name ?? string.Empty },
                { nameof(SessionReminder.QuestDate),            quest.FinalizedDate.Value },
                { nameof(SessionReminder.QuestDescription),     quest.Description },
                { nameof(SessionReminder.ConfirmedPlayerNames), confirmedNames },
                { nameof(SessionReminder.QuestUrl),             questUrl },
                { nameof(SessionReminder.ChallengeRating),      quest.ChallengeRating },
                { nameof(SessionReminder.AppUrl),               emailSettings.AppUrl }
            });

            await emailService.SendAsync(
                signup.Player.Email,
                $"Reminder: {quest.Title} is tomorrow",
                html);

            await reminderLog.AddAsync(questId, signup.Player.Id, cancellationToken);

            logger.LogInformation(
                "SessionReminderJob: sent reminder for quest {QuestId} to player {PlayerId}.",
                questId, signup.Player.Id);
        }
    }
}
