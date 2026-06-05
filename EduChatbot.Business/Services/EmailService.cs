using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EduChatbot.Business.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IWebHostEnvironment env, IConfiguration config)
    {
        _logger = logger;
        _env = env;
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("Sending email to {ToEmail} with Subject: {Subject}", toEmail, subject);

        bool emailSentSuccessfully = false;

        // Try sending via SMTP if configured
        var smtpSection = _config.GetSection("Smtp");
        if (smtpSection.Exists())
        {
            try
            {
                var host = smtpSection["Host"] ?? "smtp.gmail.com";
                var port = int.Parse(smtpSection["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");
                var username = smtpSection["Username"];
                var password = smtpSection["Password"];
                var senderEmail = smtpSection["SenderEmail"] ?? username;
                var senderName = smtpSection["SenderName"] ?? "EduChatbot System";

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(senderEmail))
                {
                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = enableSsl;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(username, password);

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(senderEmail, senderName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = false
                        };
                        mailMessage.To.Add(toEmail);

                        await client.SendMailAsync(mailMessage);
                        _logger.LogInformation("Email successfully sent via SMTP to {ToEmail}.", toEmail);
                        emailSentSuccessfully = true;
                    }
                }
                else
                {
                    _logger.LogWarning("SMTP is configured but missing Username, Password, or SenderEmail.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP to {ToEmail}. Falling back to file log.", toEmail);
            }
        }
        else
        {
            _logger.LogInformation("SMTP section not found in configuration. Falling back to file log.");
        }

        // Always log to file as a backup or if SMTP failed/was not configured
        try
        {
            var emailLogDirectory = Path.Combine(_env.ContentRootPath, "email-logs");
            if (!Directory.Exists(emailLogDirectory))
            {
                Directory.CreateDirectory(emailLogDirectory);
            }

            var safeEmail = toEmail.Replace("@", "_").Replace(".", "_");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = Path.Combine(emailLogDirectory, $"{safeEmail}-{timestamp}.txt");

            var emailContent = $@"To: {toEmail}
Date: {DateTime.UtcNow}
Subject: {subject}
SentViaSmtp: {emailSentSuccessfully}
--------------------------------------------------
{body}
";
            await File.WriteAllTextAsync(fileName, emailContent);
            _logger.LogInformation("Email content logged to file: {FilePath}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write email backup log to file for user {ToEmail}.", toEmail);
        }
    }
}

