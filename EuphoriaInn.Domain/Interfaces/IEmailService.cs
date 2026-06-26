namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    // Generic method — used by all Hangfire jobs (Phase 21+)
    Task SendAsync(string toEmail, string subject, string htmlBody);

    // Legacy typed methods — kept to avoid breaking existing tests; jobs no longer call these
    [Obsolete("Use SendAsync with pre-rendered HTML. Will be removed in a future phase.")]
    Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate);
    [Obsolete("Use SendAsync with pre-rendered HTML. Will be removed in a future phase.")]
    Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName);
}
