using EuphoriaInn.Domain.Interfaces;

namespace EuphoriaInn.Service.Services;

/// <summary>
/// No-op implementation of IQuestEmailDispatcher used in test environments
/// where Hangfire is not registered (IBackgroundJobClient is unavailable).
/// </summary>
public class NullQuestEmailDispatcher : IQuestEmailDispatcher
{
    public void EnqueueFinalizedEmail(
        int questId,
        DateTime finalizedDate,
        string[] recipientEmails,
        string[] playerNames,
        string questTitle,
        string dmName,
        string questDescription,
        int challengeRating)
    {
        // No-op — Hangfire not available in Testing environment
    }

    public void EnqueueDateChangedEmail(
        int questId,
        string[] recipientEmails,
        string[] playerNames,
        string questTitle,
        string dmName,
        DateTime oldDate,
        DateTime newDate)
    {
        // No-op — Hangfire not available in Testing environment
    }
}
