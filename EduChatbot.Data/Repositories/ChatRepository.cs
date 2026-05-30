using EduChatbot.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<DocumentChunk>> SearchChunksAsync(string keyword, int topK = 5)
    {
        // Tìm kiếm keyword trong nội dung chunk, dùng ILIKE của PostgreSQL (case-insensitive).
        var words = keyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2)
            .Take(5)
            .ToList();

        if (words.Count == 0)
        {
            return [];
        }

        // Build SQL WHERE clause với OR cho từng từ khóa.
        // EF Core LINQ không translate được dynamic OR + ILike nên dùng raw SQL.
        var conditions = new List<string>();
        var parameters = new List<Npgsql.NpgsqlParameter>();

        for (int i = 0; i < words.Count; i++)
        {
            conditions.Add($"dc.content ILIKE @p{i}");
            parameters.Add(new Npgsql.NpgsqlParameter($"p{i}", $"%{words[i]}%"));
        }

        var whereClause = string.Join(" OR ", conditions);
        var sql = $@"
            SELECT dc.id, dc.document_id, dc.chunk_index, dc.content, dc.embedding_data, dc.created_at
            FROM document_chunks dc
            WHERE {whereClause}
            ORDER BY dc.id
            LIMIT {topK}";

        var chunks = await _context.DocumentChunks
            .FromSqlRaw(sql, parameters.ToArray())
            .Include(c => c.Document)
            .ToListAsync();

        return chunks;
    }
}
