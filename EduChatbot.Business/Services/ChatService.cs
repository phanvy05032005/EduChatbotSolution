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
    private readonly IEmbeddingService _embeddingService;
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;

    public ChatService(
        IChatRepository chatRepository,
        IEmbeddingService embeddingService,
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings)
    {
        _chatRepository = chatRepository;
        _embeddingService = embeddingService;
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<List<ChatConversation>> GetConversationsAsync(string userId)
    {
        return await _chatRepository.GetConversationsByUserAsync(userId);
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(int? conversationId, string userId, int? courseId = null)
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
            Title = "New conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CourseId = courseId
        };

        return await _chatRepository.AddConversationAsync(conversation);
    }

    public async Task<ChatMessage> SendMessageAsync(int conversationId, string userId, string question)
    {
        // Step 1: Save user message to DB.
        var userMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = question,
            CreatedAt = DateTime.UtcNow
        };
        await _chatRepository.AddMessageAsync(userMessage);

        var conversation = await _chatRepository.GetConversationWithMessagesAsync(conversationId, userId);
        int? courseId = conversation?.CourseId;

        // Step 2: Create embedding for question and search related chunks by cosine similarity with CourseId.
        var questionEmbedding = await _embeddingService.CreateEmbeddingAsync(question);
        var chunks = await _chatRepository.SearchChunksAsync(questionEmbedding, courseId);

        // Step 3: Build prompt context from chunks.
        var context = BuildPromptContext(chunks);

        // Step 4: Call OpenRouter API.
        var aiResponseText = await CallLlmAsync(question, context);

        // Step 5: Build source citation.
        var sourceCitations = chunks
            .Where(c => c.Document != null)
            .Select(c => new { doc = c.Document!.FileName, chunk = c.ChunkIndex })
            .Distinct()
            .ToList();

        // Step 6: Save AI response to DB.
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

        // Step 7: Update conversation title if this is the first message.
        conversation = await _chatRepository.GetConversationWithMessagesAsync(conversationId, userId);
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
            return "No related documents found in the system.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Below are the relevant document segments found in the system:");
        sb.AppendLine();

        foreach (var chunk in chunks)
        {
            var docName = chunk.Document?.FileName ?? "Unknown";
            sb.AppendLine($"--- Document: {docName} | Chunk #{chunk.ChunkIndex} ---");
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
                        You are the AI learning assistant for the EduChatbot system. Your task is to answer the student's question based on the content of the documents provided below.
                        - Answer in the same language as the student's question.
                        - If the documents do not contain the relevant information, clearly state that and try to answer based on general knowledge.
                        - Always cite source documents when possible.
                        """
                    },
                    new
                    {
                        role = "user",
                        content = $"Document context:\n{context}\n\nStudent question: {question}"
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
                return $"Sorry, the AI system is experiencing issues (HTTP {(int)response.StatusCode}). Please try again later.";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "No response received from AI.";
        }
        catch (Exception ex)
        {
            return $"Sorry, could not connect to AI: {ex.Message}";
        }
    }

    public async Task<List<Course>> GetCoursesAsync()
    {
        return await _chatRepository.GetCoursesAsync();
    }
}
