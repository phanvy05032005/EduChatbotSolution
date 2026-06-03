using EduChatbot.Models;

namespace EduChatbot.Data.Repositories;

public interface IDocumentRepository
{
    Task<List<Document>> GetAllAsync(string? searchTerm = null, string? uploadedById = null);

    Task<DocumentDashboardSummary> GetDashboardSummaryAsync();

    Task<Document?> GetByIdAsync(int id, string? uploadedById = null);

    Task<bool> ExistsByUploadedByAndFileNameAsync(string uploadedById, string fileName);

    Task AddAsync(Document document);

    Task UpdateAsync(Document document);

    Task DeleteAsync(Document document);
}
