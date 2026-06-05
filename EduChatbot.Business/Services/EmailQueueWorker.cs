using EduChatbot.Data.Repositories;
using EduChatbot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduChatbot.Business.Services;

/// <summary>
/// BackgroundService chạy nền: định kỳ lấy email Pending từ DB queue và gửi.
/// Thiết kế đơn giản theo yêu cầu: không dùng RabbitMQ/Kafka/ServiceBus.
/// </summary>
public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueWorker> _logger;

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;
    private const int MaxRetry = 5;

    public EmailQueueWorker(IServiceProvider serviceProvider, ILogger<EmailQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker started. Interval={IntervalSeconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailQueueWorker unexpected error.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }

        _logger.LogInformation("EmailQueueWorker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Lấy batch Pending và chuyển sang Processing để tránh trùng.
        var items = await repo.DequeuePendingBatchAsync(BatchSize, cancellationToken);
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                await emailService.SendEmailAsync(item.ToEmail, item.Subject, item.Body);
                await repo.MarkAsSentAsync(item.Id, DateTime.UtcNow, cancellationToken);
            }
            catch (Exception ex)
            {
                // Retry đơn giản: tăng retry, nếu quá ngưỡng thì Failed.
                var nextRetry = item.RetryCount + 1;

                _logger.LogWarning(ex, "Send email failed (Id={Id}, To={ToEmail}, Retry={Retry}).", item.Id, item.ToEmail, nextRetry);

                if (nextRetry >= MaxRetry)
                {
                    await repo.MarkAsFailedAsync(item.Id, nextRetry, cancellationToken);
                }
                else
                {
                    await repo.MarkAsPendingAsync(item.Id, nextRetry, cancellationToken);
                }
            }
        }
    }
}
