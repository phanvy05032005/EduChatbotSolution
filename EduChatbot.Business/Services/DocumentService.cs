using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using EduChatbot.Data;
using EduChatbot.Data.Repositories;
using EduChatbot.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using System.Net.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace EduChatbot.Business.Services;

public class DocumentService : IDocumentService
{
    private const string StatusProcessing = "Processing";
    private const string StatusCompleted = "Completed";
    private const string StatusFailed = "Failed";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentUploadRules _documentUploadRules;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DocumentService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentUploadRules documentUploadRules,
        IEmbeddingService embeddingService,
        ILogger<DocumentService> logger,
        ApplicationDbContext context,
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings,
        UserManager<ApplicationUser> userManager)
    {
        _documentRepository = documentRepository;
        _documentUploadRules = documentUploadRules;
        _embeddingService = embeddingService;
        _logger = logger;
        _context = context;
        _httpClient = httpClient;
        _settings = settings.Value;
        _userManager = userManager;
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

    public async Task<DocumentUploadResult> UpdateDocumentAsync(
        int id,
        string fileName,
        string? currentUserId = null,
        bool isAdmin = false)
    {
        var validationMessage = ValidateDocumentMetadata(fileName);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = validationMessage
            };
        }

        var ownerFilter = isAdmin ? null : currentUserId;
        var document = await _documentRepository.GetByIdAsync(id, ownerFilter);
        if (document == null)
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = "Document not found.",
                Status = StatusFailed
            };
        }

        document.FileName = fileName.Trim();

        await _documentRepository.UpdateAsync(document);

        return new DocumentUploadResult
        {
            IsSuccess = true,
            Message = "Document updated successfully.",
            DocumentId = document.Id,
            ChunkCount = document.ChunkCount,
            Status = document.Status
        };
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        string uploadedBy,
        string? uploadedById,
        string webRootPath,
        int courseId)
    {
        var safeOriginalFileName = Path.GetFileName(originalFileName)?.Trim() ?? string.Empty;
        var validationMessage = ValidateFile(safeOriginalFileName, fileSize);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = validationMessage
            };
        }

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = "Môn học không tồn tại."
            };
        }

        if (!string.IsNullOrWhiteSpace(uploadedById))
        {
            var user = await _userManager.FindByIdAsync(uploadedById);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin);
                if (!isAdmin)
                {
                    var isAssigned = await _context.LecturerCourses.AnyAsync(lc => lc.LecturerId == uploadedById && lc.CourseId == courseId);
                    if (!isAssigned)
                    {
                        return new DocumentUploadResult
                        {
                            IsSuccess = false,
                            Message = "Bạn không được phân công giảng dạy môn học này."
                        };
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(uploadedById) &&
            await _documentRepository.ExistsByUploadedByAndFileNameAsync(uploadedById, safeOriginalFileName))
        {
            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = "Bạn đã upload tài liệu có cùng tên file. Vui lòng đổi tên file hoặc xóa tài liệu cũ trước khi upload lại.",
                Status = StatusFailed
            };
        }

        string? physicalFilePath = null;
        try
        {
            var uploadFolder = Path.Combine(webRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadFolder);

            var extension = Path.GetExtension(safeOriginalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            physicalFilePath = Path.Combine(uploadFolder, storedFileName);

            await using (var outputStream = File.Create(physicalFilePath))
            {
                await fileStream.CopyToAsync(outputStream);
            }

            var extractedText = ExtractText(physicalFilePath, extension);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                File.Delete(physicalFilePath);
                return new DocumentUploadResult
                {
                    IsSuccess = false,
                    Message = "Không extract được nội dung text từ file. Vui lòng kiểm tra lại PDF/DOCX.",
                    Status = StatusFailed
                };
            }

            var chunks = ChunkText(extractedText);
            var documentChunks = new List<DocumentChunk>();

            for (var index = 0; index < chunks.Count; index++)
            {
                var chunkContent = chunks[index];
                var embedding = await _embeddingService.CreateEmbeddingAsync(chunkContent);

                documentChunks.Add(new DocumentChunk
                {
                    ChunkIndex = index,
                    Content = chunkContent,
                    Embedding = new Vector(embedding),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var document = new Document
            {
                FileName = safeOriginalFileName,
                StoredFileName = storedFileName,
                FilePath = $"/uploads/documents/{storedFileName}",
                UploadedBy = NormalizeUploadedBy(uploadedBy),
                UploadedById = uploadedById,
                ContentType = contentType,
                FileSize = fileSize,
                ExtractedText = extractedText,
                ChunkCount = documentChunks.Count,
                EmbeddingPreview = FormatEmbeddingPreview(documentChunks.FirstOrDefault()?.Embedding),
                Status = "Analyzing",
                UploadedAt = DateTime.UtcNow,
                CourseId = courseId,
                Chunks = documentChunks
            };

            var existingDocs = await _context.Documents
                .Where(d => d.CourseId == courseId && d.Status == "Valid")
                .ToListAsync();

            var aiResult = await ValidateDocumentWithAiAsync(extractedText, course.Code, course.Name, existingDocs);
            if (aiResult.IsValid)
            {
                document.Status = "Valid";
            }
            else
            {
                document.Status = "Invalid";
            }
            document.ValidationResult = aiResult.Reason;

            await _documentRepository.AddAsync(document);

            return new DocumentUploadResult
            {
                IsSuccess = true,
                Message = document.Status == "Valid"
                    ? "Tài liệu hợp lệ và đã được phê duyệt thành công."
                    : $"Tài liệu không hợp lệ (Phát hiện bởi AI): {aiResult.Reason}",
                DocumentId = document.Id,
                ChunkCount = document.ChunkCount,
                Status = document.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload/indexing failed for {FileName}", safeOriginalFileName);

            if (!string.IsNullOrWhiteSpace(physicalFilePath) && File.Exists(physicalFilePath))
            {
                File.Delete(physicalFilePath);
            }

            var message = ex is InvalidOperationException or ArgumentException
                ? ex.Message
                : "Xử lý tài liệu thất bại. Vui lòng kiểm tra file hoặc database.";

            return new DocumentUploadResult
            {
                IsSuccess = false,
                Message = message,
                Status = StatusFailed
            };
        }
    }

    private async Task<LlmValidationResult> ValidateDocumentWithAiAsync(string documentText, string courseCode, string courseName, List<Document> existingDocs)
    {
        var existingDocsContext = new StringBuilder();
        if (existingDocs.Count > 0)
        {
            existingDocsContext.AppendLine("Dưới đây là các tài liệu học tập ĐÃ CÓ của môn học này:");
            foreach (var doc in existingDocs)
            {
                existingDocsContext.AppendLine($"--- Tên tài liệu: {doc.FileName} ---");
                var previewText = doc.ExtractedText.Length > 2000
                    ? doc.ExtractedText[..2000] + "..."
                    : doc.ExtractedText;
                existingDocsContext.AppendLine(previewText);
                existingDocsContext.AppendLine();
            }
        }
        else
        {
            existingDocsContext.AppendLine("Chưa có tài liệu nào khác được upload cho môn học này.");
        }

        var systemPrompt = @"Bạn là chuyên gia kiểm định chất lượng tài liệu học thuật của EduChatbot.
Nhiệm vụ của bạn là đánh giá xem tài liệu học tập mới tải lên có hợp lệ hay không.
Quy tắc đánh giá:
1. Tính liên quan: Tài liệu mới phải thực sự liên quan đến môn học có tên và mã được cung cấp. Nếu tài liệu lạc đề (ví dụ: công thức nấu ăn, truyện cười, hoặc tài liệu của môn học khác hoàn toàn), hãy đánh dấu là không hợp lệ.
2. Tính nhất quán: Đối chiếu tài liệu mới với danh sách tài liệu cũ (nếu có). Phát hiện xem có mâu thuẫn thông tin nào không (ví dụ: tài liệu cũ nói bài thi 60 phút, tài liệu mới nói bài thi 90 phút; hoặc tài liệu cũ viết định nghĩa A là B, tài liệu mới viết định nghĩa A là C). Nếu có mâu thuẫn, hãy đánh dấu là không hợp lệ và chỉ rõ điểm mâu thuẫn, tài liệu nào đúng tài liệu nào sai (giả định tài liệu cũ là đúng).

Bạn PHẢI trả về kết quả dưới định dạng JSON duy nhất, có cấu trúc như sau:
{
  ""isValid"": true hoặc false,
  ""reason"": ""Lý do chi tiết vì sao tài liệu hợp lệ hoặc không hợp lệ. Nếu không hợp lệ và có mâu thuẫn, hãy chỉ rõ điểm mâu thuẫn với tài liệu cũ nào (ghi rõ tên tài liệu)""
}";

        var userPrompt = $@"Môn học: {courseCode} - {courseName}

{existingDocsContext}

--- TÀI LIỆU MỚI TẢI LÊN ---
{documentText}
";

        try
        {
            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new LlmValidationResult { IsValid = true, Reason = $"Không thể kiểm định qua AI (HTTP {(int)response.StatusCode}). Tạm thời phê duyệt." };
            }

            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmValidationResult { IsValid = true, Reason = "AI trả về phản hồi rỗng. Tạm thời phê duyệt." };
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<LlmValidationResult>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new LlmValidationResult { IsValid = true, Reason = "Không thể phân tích phản hồi JSON của AI. Tạm thời phê duyệt." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi AI kiểm định tài liệu.");
            return new LlmValidationResult { IsValid = true, Reason = $"Lỗi kết nối AI: {ex.Message}. Tạm thời phê duyệt." };
        }
    }

    private class LlmValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; } = string.Empty;
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

    private string ValidateFile(string fileName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Vui lòng chọn file cần upload.";
        }

        if (fileSize <= 0)
        {
            return "File upload không hợp lệ.";
        }

        if (fileSize > _documentUploadRules.MaxFileSizeBytes)
        {
            return "File không được vượt quá 10 MB.";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_documentUploadRules.IsAllowedExtension(extension))
        {
            return "Chỉ hỗ trợ file PDF hoặc DOCX.";
        }

        return string.Empty;
    }

    private static string ValidateDocumentMetadata(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "File name is required.";
        }

        if (fileName.Trim().Length > 255)
        {
            return "File name cannot exceed 255 characters.";
        }

        return string.Empty;
    }

    private static string NormalizeUploadedBy(string uploadedBy)
    {
        return string.IsNullOrWhiteSpace(uploadedBy)
            ? "Lecturer"
            : uploadedBy.Trim();
    }

    private static string ExtractText(string filePath, string extension)
    {
        return extension switch
        {
            ".pdf" => ExtractPdfText(filePath),
            ".docx" => ExtractDocxText(filePath),
            _ => string.Empty
        };
    }

    private static string ExtractPdfText(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        return NormalizeExtractedText(sb.ToString());
    }

    private static string ExtractDocxText(string filePath)
    {
        var sb = new StringBuilder();

        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null)
        {
            return string.Empty;
        }

        foreach (var paragraph in body.Descendants<OpenXmlParagraph>())
        {
            var paragraphText = string.Concat(paragraph.Descendants<OpenXmlText>().Select(text => text.Text));
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                sb.AppendLine(paragraphText);
            }
        }

        return NormalizeExtractedText(sb.ToString());
    }

    private static string NormalizeExtractedText(string text)
    {
        var lines = text
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> ChunkText(string text)
    {
        const int wordsPerChunk = 220;

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

    private static string FormatEmbeddingPreview(Vector? embedding)
    {
        if (embedding == null)
        {
            return string.Empty;
        }

        var values = embedding
            .ToArray()
            .Take(8)
            .Select(value => value.ToString("0.0000", CultureInfo.InvariantCulture));

        return string.Join(",", values);
    }

    public async Task<List<Course>> GetAvailableCoursesForUserAsync(string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Courses
                .OrderBy(c => c.Code)
                .ToListAsync();
        }

        return await _context.LecturerCourses
            .Where(lc => lc.LecturerId == userId)
            .Select(lc => lc.Course!)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }
}
