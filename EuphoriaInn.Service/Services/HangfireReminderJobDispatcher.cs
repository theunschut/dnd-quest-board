using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.Jobs;
using Hangfire;

namespace EuphoriaInn.Service.Services;

/// <summary>
/// Implements IReminderJobDispatcher by enqueueing a Hangfire fire-and-forget job.
/// Lives in Service to avoid a Domain → Service circular dependency.
/// </summary>
public class HangfireReminderJobDispatcher(IBackgroundJobClient jobClient) : IReminderJobDispatcher
{
    public void EnqueueSessionReminder(int questId, bool forceResend = false, bool useYesMaybeVoters = false)
    {
        jobClient.Enqueue<SessionReminderJob>(j => j.ExecuteAsync(questId, forceResend, useYesMaybeVoters, CancellationToken.None));
    }
}
