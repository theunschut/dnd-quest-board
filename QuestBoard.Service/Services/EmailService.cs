using System.Net;
using System.Net.Mail;

namespace QuestBoard.Service.Services;

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
            logger.LogInformation("Quest finalized email sent to {Email} for quest {QuestTitle}", toEmail, questTitle);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send quest finalized email to {Email} for quest {QuestTitle}", toEmail, questTitle);
        }
    }
}