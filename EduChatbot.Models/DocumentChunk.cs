using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace EduChatbot.Models;

public class DocumentChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    // Nội dung text nhỏ sau khi chunk từ document gốc.
    public string Content { get; set; } = string.Empty;

    // Vector embedding thật, tạo từ nội dung chunk bằng embedding model.
    [Column(TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document? Document { get; set; }
}
