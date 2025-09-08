namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}