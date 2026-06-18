using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EuphoriaInn.Domain.Services;

public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    private SmtpClient? CreateSmtpClient()
    {
        if (string.IsNullOrEmpty(_settings.SmtpUsername) ||
            string.IsNullOrEmpty(_settings.SmtpPassword) ||
            string.IsNullOrEmpty(_settings.FromEmail))
        {
            logger.LogWarning("Email settings not configured. Skipping email notification.");
            return null;
        }

        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
        };
        return client;
    }

    public async Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate)
    {
        try
        {
            using var client = CreateSmtpClient();
            if (client == null) return;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = $"Quest Finalized: {questTitle}",
                Body = $@"
Hello {playerName},

Great news! The quest ""{questTitle}"" has been finalized.

Quest Details:
- Title: {questTitle}
- DM: {dmName}
- Date & Time: {questDate:dddd, MMMM dd, yyyy 'at' h:mm tt}

You have been selected to participate in this quest. Please make sure you're available at the scheduled time.

See you at the table!

- D&D Quest Board
",
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send quest finalized email for quest {QuestTitle}", questTitle);
        }
    }

    public async Task SendQuestDateChangedEmailAsync(string toEmail, string playerName, string questTitle, string dmName)
    {
        try
        {
            using var client = CreateSmtpClient();
            if (client == null) return;

            var appUrl = string.IsNullOrEmpty(_settings.AppUrl) ? "[Quest Board URL]" : _settings.AppUrl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = $"Quest Dates Updated: {questTitle}",
                Body = $@"
Hello {playerName},

The quest ""{questTitle}"" has had some proposed dates changed by the DM.

Quest Details:
- Title: {questTitle}
- DM: {dmName}

Some of your previously selected date preferences may have been removed. Please visit the quest page to review the new available dates and update your preferences if needed.

You can view and update your signup at: {appUrl}

Thanks for your understanding!

- D&D Quest Board
",
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send quest date changed email for quest {QuestTitle}", questTitle);
        }
    }
}
