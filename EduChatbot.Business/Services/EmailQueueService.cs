using EduChatbot.Data.Repositories;
using EduChatbot.Models;

namespace EduChatbot.Business.Services;

public class EmailQueueService : IEmailQueueService
{
    private readonly IEmailQueueRepository _repository;

    public EmailQueueService(IEmailQueueRepository repository)
    {
        _repository = repository;
    }

    public Task EnqueueAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Validate đơn giản để tránh rác vào queue.
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.CompletedTask;
        }

        var email = new EmailQueue
        {
            ToEmail = toEmail.Trim(),
            Subject = subject?.Trim() ?? string.Empty,
            Body = body ?? string.Empty,
            Status = EmailQueueStatuses.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            SentAt = null
        };

        return _repository.EnqueueAsync(email, cancellationToken);
    }
}
