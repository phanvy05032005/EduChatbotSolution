using EduChatbot.Models;

namespace EduChatbot.Data.Repositories;

public interface IChatRepository
{
    Task<List<ChatConversation>> GetConversationsByUserAsync(string userId);

    Task<ChatConversation?> GetConversationWithMessagesAsync(int conversationId, string userId);

    Task<ChatConversation> AddConversationAsync(ChatConversation conversation);

    Task AddMessageAsync(ChatMessage message);

    Task UpdateConversationAsync(ChatConversation conversation);

    Task<List<DocumentChunk>> SearchChunksAsync(string keyword, int topK = 5);
}
