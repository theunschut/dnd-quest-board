using EuphoriaInn.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EuphoriaInn.Service.Jobs;

public class DailyReminderJob(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<DailyReminderJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // FinalizedDate is stored as server local time (no UTC annotation on QuestEntity).
        // DateTime.Today is server local time on the LXC container (CET/CEST).
        // Comparison is correct — no timezone conversion needed. (D-05, RESEARCH.md FinalizedDate section)
        var tomorrow = DateTime.Today.AddDays(1);

        await using var scope = scopeFactory.CreateAsyncScope();
        var questRepository = scope.ServiceProvider.GetRequiredService<IQuestRepository>();

        var quests = await questRepository.GetFinalizedQuestsForDateAsync(tomorrow, cancellationToken);

        if (quests.Count == 0)
        {
            logger.LogInformation(
                "DailyReminderJob: no finalized quests found for {Date}.",
                tomorrow.ToShortDateString());
            return;
        }

        foreach (var quest in quests)
        {
            backgroundJobClient.Enqueue<SessionReminderJob>(
                job => job.ExecuteAsync(quest.Id, false, false, CancellationToken.None));

            logger.LogInformation(
                "DailyReminderJob: queued SessionReminderJob for quest {QuestId} on {Date}.",
                quest.Id, tomorrow.ToShortDateString());
        }
    }
}
