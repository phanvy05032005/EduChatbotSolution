using EduChatbot.Models;

namespace EduChatbot.Data.Repositories;

/// <summary>
/// Repository thao tác với bảng EmailQueue.
/// </summary>
public interface IEmailQueueRepository
{
    Task EnqueueAsync(EmailQueue email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy 1 batch email Pending và chuyển sang Processing (atomic) để worker xử lý.
    /// </summary>
    Task<List<EmailQueue>> DequeuePendingBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    Task MarkAsSentAsync(int id, DateTime sentAtUtc, CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(int id, int retryCount, CancellationToken cancellationToken = default);

    Task MarkAsPendingAsync(int id, int retryCount, CancellationToken cancellationToken = default);
}
