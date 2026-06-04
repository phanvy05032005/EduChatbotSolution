using Microsoft.Extensions.Logging;

namespace EduChatbot.Business.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private const string EmailLogDirectory = @"C:\Users\truon\.gemini\antigravity\brain\068de350-8b17-4025-bbcb-fb2d649f000f\emails";

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("Sending email to {ToEmail} with Subject: {Subject}", toEmail, subject);

        try
        {
            if (!Directory.Exists(EmailLogDirectory))
            {
                Directory.CreateDirectory(EmailLogDirectory);
            }

            var safeEmail = toEmail.Replace("@", "_").Replace(".", "_");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = Path.Combine(EmailLogDirectory, $"{safeEmail}-{timestamp}.txt");

            var emailContent = $@"To: {toEmail}
Date: {DateTime.UtcNow}
Subject: {subject}
--------------------------------------------------
{body}
";
            await File.WriteAllTextAsync(fileName, emailContent);
            _logger.LogInformation("Email successfully logged to file: {FilePath}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write email log to file for user {ToEmail}.", toEmail);
        }
    }
}
