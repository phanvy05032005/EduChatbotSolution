using System.Security.Cryptography;
using System.Text;
using EduChatbot.Data.Repositories;
using EduChatbot.Models;

namespace EduChatbot.Business.Services;

public class DocumentService : IDocumentService
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const string StatusProcessing = "Processing";
    private const string StatusCompleted = "Completed";
    private const string StatusFailed = "Failed";
    private static readonly string[] AllowedExtensions = [".pdf", ".docx"];

    private readonly IDocumentRepository _documentRepository;

    public DocumentService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<DocumentListResult> GetDocumentsAsync(string? searchTerm = null, string? currentUserId = null, bool isAdmin = false)
    {
        var ownerFilter = isAdmin ? null : currentUserId;
        var documents = await _documentRepository.GetAllAsync(searchTerm, ownerFilter);

        return new DocumentListResult
        {
            Documents = documents,
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            TotalCount = documents.Count
        };
    }

    public async Task<DocumentDashboardSummary> GetDashboardSummaryAsync()
    {
        return await _documentRepository.GetDashboardSummaryAsync();
    }

    public async Task<Document?> GetDocumentDetailsAsync(int id, string? currentUserId = null, bool isAdmin = false)
    {
        var ownerFilter = isAdmin ? null : currentUserId;
        return await _documentRepository.GetByIdAsync(id, ownerFilter);
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        string uploadedBy,
        string? uploadedById,
        string webRootPath)
    {
        var validationMessage = ValidateFile(originalFileName, fileSize);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = validationMessage
            };
        }

        try
        {
            var uploadFolder = Path.Combine(webRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadFolder);

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalFilePath = Path.Combine(uploadFolder, storedFileName);

            await using (var outputStream = File.Create(physicalFilePath))
            {
                await fileStream.CopyToAsync(outputStream);
            }

            // Các bước dưới đây là mock theo yêu cầu Assignment 1, chưa dùng AI thật.
            var extractedText = MockExtractText(originalFileName, extension, fileSize);
            var chunks = ChunkText(extractedText);
            var documentChunks = chunks
                .Select((chunkContent, index) => new DocumentChunk
                {
                    ChunkIndex = index,
                    Content = chunkContent,
                    EmbeddingData = GenerateMockEmbedding(chunkContent),
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            var document = new Document
            {
                FileName = originalFileName,
                StoredFileName = storedFileName,
                FilePath = $"/uploads/documents/{storedFileName}",
                UploadedBy = NormalizeUploadedBy(uploadedBy),
                UploadedById = uploadedById,
                ContentType = contentType,
                FileSize = fileSize,
                ExtractedText = extractedText,
                ChunkCount = documentChunks.Count,
                EmbeddingPreview = documentChunks.FirstOrDefault()?.EmbeddingData ?? string.Empty,
                Status = StatusProcessing,
                UploadedAt = DateTime.UtcNow,
                Chunks = documentChunks
            };

            // Vì xử lý đang chạy đồng bộ trong Assignment 1, document được lưu khi đã index xong.
            document.Status = StatusCompleted;
            await _documentRepository.AddAsync(document);

            return new DocumentUploadResult
            {
                IsSuccess = true,
                Message = "Upload success",
                DocumentId = document.Id,
                ChunkCount = document.ChunkCount,
                Status = document.Status
            };
        }
        catch
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = "Xử lý tài liệu thất bại. Vui lòng kiểm tra file hoặc database.",
                Status = StatusFailed
            };
        }
    }

    public async Task<bool> DeleteDocumentAsync(int id, string webRootPath, string? currentUserId = null, bool isAdmin = false)
    {
        var ownerFilter = isAdmin ? null : currentUserId;
        var document = await _documentRepository.GetByIdAsync(id, ownerFilter);
        if (document == null)
        {
            return false;
        }

        await _documentRepository.DeleteAsync(document);

        var physicalFilePath = Path.Combine(webRootPath, document.FilePath.TrimStart('/'));
        if (File.Exists(physicalFilePath))
        {
            // Xóa file vật lý sau khi database đã xóa thành công.
            File.Delete(physicalFilePath);
        }

        return true;
    }

    public async Task<bool> UpdateDocumentNameAsync(int id, string newFileName, string? currentUserId = null, bool isAdmin = false)
    {
        var ownerFilter = isAdmin ? null : currentUserId;
        var document = await _documentRepository.GetByIdAsync(id, ownerFilter);
        if (document == null)
        {
            return false;
        }

        document.FileName = newFileName;
        await _documentRepository.UpdateAsync(document);
        return true;
    }

    private static string ValidateFile(string fileName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Vui lòng chọn file cần upload.";
        }

        if (fileSize <= 0)
        {
            return "File upload không hợp lệ.";
        }

        if (fileSize > MaxFileSize)
        {
            return "File không được vượt quá 10 MB.";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return "Chỉ hỗ trợ file PDF hoặc DOCX.";
        }

        return string.Empty;
    }

    private static string NormalizeUploadedBy(string uploadedBy)
    {
        return string.IsNullOrWhiteSpace(uploadedBy)
            ? "Lecturer"
            : uploadedBy.Trim();
    }

    private static string MockExtractText(string fileName, string extension, long fileSize)
    {
        // Đây là text demo thay cho thư viện đọc PDF/DOCX thật trong Assignment 1.
        return $"""
        Tài liệu học tập: {fileName}
        Loại file: {extension}
        Dung lượng: {fileSize} bytes
        Nội dung mô phỏng: Đây là phần text đã được extract từ tài liệu. Hệ thống sẽ dùng nội dung này để chia chunk và tạo embedding demo phục vụ tìm kiếm sau này.
        Chủ đề mô phỏng: ASP.NET Core MVC, Entity Framework Core, PostgreSQL, kiến trúc 3 lớp, repository, service, RAG.
        """;
    }

    private static List<string> ChunkText(string text)
    {
        const int wordsPerChunk = 30;

        var words = text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var chunks = new List<string>();
        for (var index = 0; index < words.Count; index += wordsPerChunk)
        {
            chunks.Add(string.Join(' ', words.Skip(index).Take(wordsPerChunk)));
        }

        return chunks;
    }

    private static string GenerateMockEmbedding(string text)
    {
        // Chuyển hash thành vector số demo, không phải embedding AI production.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var values = bytes
            .Take(8)
            .Select(value => (value / 255d).ToString("0.0000"));

        return string.Join(",", values);
    }
}
