using System.ComponentModel.DataAnnotations;

namespace EduChatbot.Models;

public class ChatConversation
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "Cuộc trò chuyện mới";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = [];
}
