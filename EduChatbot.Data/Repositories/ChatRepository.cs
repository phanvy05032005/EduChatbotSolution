using EduChatbot.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace EduChatbot.Data.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatConversation>> GetConversationsByUserAsync(string userId)
    {
        return await _context.ChatConversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<ChatConversation?> GetConversationWithMessagesAsync(int conversationId, string userId)
    {
        return await _context.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
    }

    public async Task<ChatConversation> AddConversationAsync(ChatConversation conversation)
    {
        await _context.ChatConversations.AddAsync(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateConversationAsync(ChatConversation conversation)
    {
        _context.ChatConversations.Update(conversation);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentChunk>> SearchChunksAsync(float[] queryEmbedding, int topK = 5)
    {
        if (queryEmbedding.Length == 0)
        {
            return [];
        }

        var vector = new Vector(queryEmbedding);

        return await _context.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(vector))
            .Take(topK)
            .ToListAsync();
    }
}
