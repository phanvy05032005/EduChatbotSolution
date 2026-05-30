using EduChatbot.Models;

namespace EduChatbot.Business.Services;

public interface IChatService
{
    Task<List<ChatConversation>> GetConversationsAsync(string userId);

    Task<ChatConversation> GetOrCreateConversationAsync(int? conversationId, string userId);

    Task<ChatMessage> SendMessageAsync(int conversationId, string userId, string question);
}
