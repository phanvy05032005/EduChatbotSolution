using System.ComponentModel.DataAnnotations;

namespace EduChatbot.Models;

public class DocumentChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    // Nội dung text nhỏ sau khi chunk từ document gốc.
    public string Content { get; set; } = string.Empty;

    // Assignment 1 dùng embedding mock, chưa gọi AI thật.
    [Required]
    [MaxLength(1000)]
    public string EmbeddingData { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document? Document { get; set; }
}
