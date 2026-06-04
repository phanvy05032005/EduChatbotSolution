using System.ComponentModel.DataAnnotations;

namespace EduChatbot.Models;

public class Document
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UploadedBy { get; set; } = "Lecturer";

    [MaxLength(450)]
    public string? UploadedById { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    // Assignment 1 chỉ mock extract text, nên lưu nội dung demo trực tiếp trong Document.
    public string ExtractedText { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    // Preview ngắn của embedding đầu tiên để hiển thị nhanh trên UI.
    [MaxLength(500)]
    public string EmbeddingPreview { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Uploaded";

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int? CourseId { get; set; }

    public Course? Course { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public double? MatchScore { get; set; }

    public string? ValidationResult { get; set; }

    [MaxLength(450)]
    public string? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    // Một Document có nhiều chunk để phục vụ RAG/chatbot.
    public List<DocumentChunk> Chunks { get; set; } = [];
}
