namespace EduChatbot.Models;

/// <summary>
/// Bảng queue email đơn giản lưu trong DB để Background Worker xử lý gửi bất đồng bộ.
/// </summary>
public class EmailQueue
{
    public int Id { get; set; }

    public string ToEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Pending | Processing | Sent | Failed
    /// </summary>
    public string Status { get; set; } = EmailQueueStatuses.Pending;

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }
}
