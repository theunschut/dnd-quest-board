namespace QuestBoard.Domain.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an HTML email via the configured SMTP relay. Used by all Hangfire email jobs.
    /// Silently no-ops if SMTP settings are not configured.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
