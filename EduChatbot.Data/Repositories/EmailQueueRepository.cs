using EduChatbot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduChatbot.Data.Repositories;

public class EmailQueueRepository : IEmailQueueRepository
{
    private readonly ApplicationDbContext _context;

    public EmailQueueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnqueueAsync(EmailQueue email, CancellationToken cancellationToken = default)
    {
        await _context.EmailQueues.AddAsync(email, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<EmailQueue>> DequeuePendingBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        // Đọc + cập nhật trạng thái sang Processing trong 1 transaction để tránh nhiều worker lấy trùng.
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var items = await _context.EmailQueues
            .Where(x => x.Status == EmailQueueStatuses.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return items;
        }

        foreach (var item in items)
        {
            item.Status = EmailQueueStatuses.Processing;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return items;
    }

    public async Task MarkAsSentAsync(int id, DateTime sentAtUtc, CancellationToken cancellationToken = default)
    {
        var item = await _context.EmailQueues.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item == null) return;

        item.Status = EmailQueueStatuses.Sent;
        item.SentAt = sentAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsFailedAsync(int id, int retryCount, CancellationToken cancellationToken = default)
    {
        var item = await _context.EmailQueues.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item == null) return;

        item.Status = EmailQueueStatuses.Failed;
        item.RetryCount = retryCount;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsPendingAsync(int id, int retryCount, CancellationToken cancellationToken = default)
    {
        var item = await _context.EmailQueues.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item == null) return;

        item.Status = EmailQueueStatuses.Pending;
        item.RetryCount = retryCount;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
