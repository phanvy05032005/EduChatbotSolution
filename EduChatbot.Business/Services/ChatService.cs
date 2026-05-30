using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EduChatbot.Data.Repositories;
using EduChatbot.Models;
using Microsoft.Extensions.Options;

namespace EduChatbot.Business.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;

    public ChatService(
        IChatRepository chatRepository,
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings)
    {
        _chatRepository = chatRepository;
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<List<ChatConversation>> GetConversationsAsync(string userId)
    {
        return await _chatRepository.GetConversationsByUserAsync(userId);
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(int? conversationId, string userId)
    {
        if (conversationId.HasValue)
        {
            var existing = await _chatRepository.GetConversationWithMessagesAsync(conversationId.Value, userId);
            if (existing != null)
            {
                return existing;
            }
        }

        var conversation = new ChatConversation
        {
            UserId = userId,
            Title = "Cuộc trò chuyện mới",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _chatRepository.AddConversationAsync(conversation);
    }

    public async Task<ChatMessage> SendMessageAsync(int conversationId, string userId, string question)
    {
        // Bước 1: Lưu message của user vào DB.
        var userMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = question,
            CreatedAt = DateTime.UtcNow
        };
        await _chatRepository.AddMessageAsync(userMessage);

        // Bước 2: Search chunks liên quan trong DB.
        var chunks = await _chatRepository.SearchChunksAsync(question);

        // Bước 3: Build prompt context từ chunks.
        var context = BuildPromptContext(chunks);

        // Bước 4: Gọi OpenRouter API.
        var aiResponseText = await CallLlmAsync(question, context);

        // Bước 5: Build source citation.
        var sourceCitations = chunks
            .Where(c => c.Document != null)
            .Select(c => new { doc = c.Document!.FileName, chunk = c.ChunkIndex })
            .Distinct()
            .ToList();

        // Bước 6: Lưu AI response vào DB.
        var aiMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "ai",
            Content = aiResponseText,
            SourceChunks = sourceCitations.Count > 0
                ? JsonSerializer.Serialize(sourceCitations)
                : null,
            CreatedAt = DateTime.UtcNow
        };
        await _chatRepository.AddMessageAsync(aiMessage);

        // Bước 7: Cập nhật title conversation nếu là message đầu tiên.
        var conversation = await _chatRepository.GetConversationWithMessagesAsync(conversationId, userId);
        if (conversation != null)
        {
            var userMessages = conversation.Messages.Where(m => m.Role == "user").ToList();
            if (userMessages.Count == 1)
            {
                conversation.Title = question.Length > 80
                    ? question[..80] + "..."
                    : question;
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await _chatRepository.UpdateConversationAsync(conversation);
        }

        return aiMessage;
    }

    private static string BuildPromptContext(List<DocumentChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return "Không tìm thấy tài liệu liên quan trong hệ thống.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Dưới đây là các đoạn tài liệu liên quan được tìm thấy trong hệ thống:");
        sb.AppendLine();

        foreach (var chunk in chunks)
        {
            var docName = chunk.Document?.FileName ?? "Unknown";
            sb.AppendLine($"--- Tài liệu: {docName} | Chunk #{chunk.ChunkIndex} ---");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> CallLlmAsync(string question, string context)
    {
        try
        {
            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = """
                        Bạn là trợ lý học tập AI của hệ thống EduChatbot. Nhiệm vụ của bạn là trả lời câu hỏi
                        của sinh viên dựa trên nội dung tài liệu được cung cấp bên dưới.
                        - Trả lời bằng tiếng Việt, rõ ràng và dễ hiểu.
                        - Nếu tài liệu không chứa thông tin liên quan, hãy nói rõ và cố gắng trả lời dựa trên kiến thức chung.
                        - Luôn trích dẫn nguồn tài liệu khi có thể.
                        """
                    },
                    new
                    {
                        role = "user",
                        content = $"Ngữ cảnh tài liệu:\n{context}\n\nCâu hỏi của sinh viên: {question}"
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Xin lỗi, hệ thống AI đang gặp sự cố (HTTP {(int)response.StatusCode}). Vui lòng thử lại sau.";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "Không nhận được phản hồi từ AI.";
        }
        catch (Exception ex)
        {
            return $"Xin lỗi, không thể kết nối đến AI: {ex.Message}";
        }
    }
}
