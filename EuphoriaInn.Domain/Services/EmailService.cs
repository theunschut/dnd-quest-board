using EuphoriaInn.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace EuphoriaInn.Domain.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendQuestFinalizedEmailAsync(string toEmail, string playerName, string questTitle, string dmName, DateTime questDate)
    {
        try
        {
            var smtpSettings = configuration.GetSection("EmailSettings");
            var smtpServer = smtpSettings["SmtpServer"];
            var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
            var smtpUsername = smtpSettings["SmtpUsername"];
            var smtpPassword = smtpSettings["SmtpPassword"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                logger.LogWarning("Email settings not configured. Skipping email notification.");
                return;
            }

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
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
            var smtpSettings = configuration.GetSection("EmailSettings");
            var smtpServer = smtpSettings["SmtpServer"];
            var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
            var smtpUsername = smtpSettings["SmtpUsername"];
            var smtpPassword = smtpSettings["SmtpPassword"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                logger.LogWarning("Email settings not configured. Skipping email notification.");
                return;
            }

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = $"Quest Dates Updated: {questTitle}",
                Body = $@"
Hello {playerName},

The quest ""{questTitle}"" has had some proposed dates changed by the DM.

Quest Details:
- Title: {questTitle}
- DM: {dmName}

Some of your previously selected date preferences may have been removed. Please visit the quest page to review the new available dates and update your preferences if needed.

You can view and update your signup at: [Quest Board URL]

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