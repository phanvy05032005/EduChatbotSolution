using System.ComponentModel.DataAnnotations;

namespace EduChatbot.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Role { get; set; } = "user";

    [Required]
    public string Content { get; set; } = string.Empty;

    // JSON string chứa thông tin source citation, ví dụ: [{"doc":"file.pdf","chunk":1}]
    public string? SourceChunks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatConversation? Conversation { get; set; }
}
