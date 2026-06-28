namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailService
{
    // Generic method — used by all Hangfire jobs (Phase 21+)
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
