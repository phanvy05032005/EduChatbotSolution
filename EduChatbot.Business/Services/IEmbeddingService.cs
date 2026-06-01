namespace EduChatbot.Business.Services;

public interface IEmbeddingService
{
    Task<float[]> CreateEmbeddingAsync(string text);
}
