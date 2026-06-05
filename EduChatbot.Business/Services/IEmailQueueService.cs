namespace EduChatbot.Business.Services;

/// <summary>
/// Service push email vào queue (DB).
/// </summary>
public interface IEmailQueueService
{
    Task EnqueueAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
