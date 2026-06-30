namespace QuestBoard.Domain.Interfaces;

/// <summary>
/// Dispatches quest-related email jobs to the background job infrastructure.
/// Defined in Domain so QuestService can call it without taking a dependency on Service-layer types.
/// </summary>
public interface IQuestEmailDispatcher
{
    void EnqueueFinalizedEmail(
        int questId,
        int groupId,
        DateTime finalizedDate,
        string[] recipientEmails,
        string[] playerNames,
        string questTitle,
        string dmName,
        string questDescription,
        int challengeRating);

    void EnqueueDateChangedEmail(
        int questId,
        string[] recipientEmails,
        string[] playerNames,
        string questTitle,
        string dmName,
        DateTime oldDate,
        DateTime newDate);
}
