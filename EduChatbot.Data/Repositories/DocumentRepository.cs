using EduChatbot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduChatbot.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Document>> GetAllAsync(string? searchTerm = null, string? uploadedById = null)
    {
        // Repository là nơi đọc dữ liệu từ database, Controller không gọi DbContext trực tiếp.
        var query = _context.Documents.Include(document => document.Course).AsQueryable();

        if (!string.IsNullOrWhiteSpace(uploadedById))
        {
            query = query.Where(document => document.UploadedById == uploadedById);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim().ToLower();
            query = query.Where(document =>
                document.FileName.ToLower().Contains(keyword) ||
                document.UploadedBy.ToLower().Contains(keyword) ||
                document.Status.ToLower().Contains(keyword));
        }

        return await query
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync();
    }

    public async Task<DocumentDashboardSummary> GetDashboardSummaryAsync()
    {
        return new DocumentDashboardSummary
        {
            TotalDocuments = await _context.Documents.CountAsync(),
            ReadyDocuments = await _context.Documents.CountAsync(document => document.Status == "Completed"),
            ProcessingDocuments = await _context.Documents.CountAsync(document => document.Status == "Processing"),
            FailedDocuments = await _context.Documents.CountAsync(document => document.Status == "Failed"),
            TotalChunks = await _context.DocumentChunks.CountAsync()
        };
    }

    public async Task<Document?> GetByIdAsync(int id, string? uploadedById = null)
    {
        var query = _context.Documents
            .Include(document => document.Course)
            .Include(document => document.Chunks.OrderBy(chunk => chunk.ChunkIndex))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(uploadedById))
        {
            query = query.Where(document => document.UploadedById == uploadedById);
        }

        return await query.FirstOrDefaultAsync(document => document.Id == id);
    }

    public async Task<bool> ExistsByUploadedByAndFileNameAsync(string uploadedById, string fileName)
    {
        var normalizedUploadedById = uploadedById.Trim();
        var normalizedFileName = fileName.Trim().ToLower();

        return await _context.Documents.AnyAsync(document =>
            document.UploadedById == normalizedUploadedById &&
            document.FileName.Trim().ToLower() == normalizedFileName);
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _context.Entry(document).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Document document)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
    }
}
