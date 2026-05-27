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

    // Embedding thật là vector số lớn; ở Assignment 1 chỉ lưu chuỗi demo để chứng minh đã index.
    [MaxLength(500)]
    public string EmbeddingPreview { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Uploaded";

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Một Document có nhiều chunk để phục vụ RAG/chatbot ở các assignment sau.
    public List<DocumentChunk> Chunks { get; set; } = [];
}
