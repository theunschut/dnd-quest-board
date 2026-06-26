namespace EuphoriaInn.Domain.Interfaces;

/// <summary>
/// Dispatches session reminder jobs to the background job infrastructure.
/// Defined in Domain so QuestController can call it without taking a dependency on Service-layer types.
/// </summary>
public interface IReminderJobDispatcher
{
    void EnqueueSessionReminder(int questId, bool forceResend = false, bool useYesMaybeVoters = false);
}
