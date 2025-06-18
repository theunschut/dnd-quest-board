namespace QuestBoard.Services;

public interface IEmailService
{
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
}