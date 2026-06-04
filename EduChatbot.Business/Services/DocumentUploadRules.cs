using EduChatbot.Models;

namespace EduChatbot.Business.Services;

public class DocumentUploadRules : IDocumentUploadRules
{
    private static readonly string[] Extensions = [".pdf", ".docx"];
    private static readonly string[] Statuses =
    [
        DocumentStatuses.Approved,
        DocumentStatuses.PendingReview,
        DocumentStatuses.Rejected,
        DocumentStatuses.Failed
    ];

    public long MaxFileSizeBytes { get; } = 10 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedExtensions => Extensions;

    public IReadOnlyCollection<string> AllowedStatuses => Statuses;

    public bool IsAllowedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return Extensions.Contains(extension.Trim().ToLowerInvariant());
    }

    public bool IsAllowedStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return Statuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
