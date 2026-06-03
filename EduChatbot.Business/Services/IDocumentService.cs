using EduChatbot.Models;

namespace EduChatbot.Business.Services;

public interface IDocumentService
{
    Task<DocumentListResult> GetDocumentsAsync(string? searchTerm = null, string? currentUserId = null, bool isAdmin = false);

    Task<DocumentDashboardSummary> GetDashboardSummaryAsync();

    Task<Document?> GetDocumentDetailsAsync(int id, string? currentUserId = null, bool isAdmin = false);

    Task<DocumentUploadResult> UpdateDocumentAsync(
        int id,
        string fileName,
        string? currentUserId = null,
        bool isAdmin = false);

    Task<bool> DeleteDocumentAsync(int id, string webRootPath, string? currentUserId = null, bool isAdmin = false);

    Task<DocumentUploadResult> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        string uploadedBy,
        string? uploadedById,
        string webRootPath,
        int courseId);

    Task<List<Course>> GetAvailableCoursesForUserAsync(string userId, bool isAdmin);
}
