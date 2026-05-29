namespace EduChatbot.Business.Services;

public class OpenRouterSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "google/gemma-3-4b-it:free";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
}
