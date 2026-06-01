using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EduChatbot.Business.Services;

public class OpenRouterEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _openRouterSettings;
    private readonly EmbeddingSettings _embeddingSettings;

    public OpenRouterEmbeddingService(
        HttpClient httpClient,
        IOptions<OpenRouterSettings> openRouterSettings,
        IOptions<EmbeddingSettings> embeddingSettings)
    {
        _httpClient = httpClient;
        _openRouterSettings = openRouterSettings.Value;
        _embeddingSettings = embeddingSettings.Value;
    }

    public async Task<float[]> CreateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(_openRouterSettings.ApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key chưa được cấu hình.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Không thể tạo embedding cho nội dung rỗng.", nameof(text));
        }

        var requestBody = new
        {
            model = _embeddingSettings.Model,
            input = text.Trim(),
            encoding_format = "float",
            dimensions = _embeddingSettings.Dimensions
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _embeddingSettings.BaseUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openRouterSettings.ApiKey);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouter embedding API lỗi HTTP {(int)response.StatusCode}: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var embedding = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();

        if (_embeddingSettings.Dimensions > 0 && embedding.Length != _embeddingSettings.Dimensions)
        {
            throw new InvalidOperationException(
                $"Embedding trả về {embedding.Length} chiều, khác cấu hình {_embeddingSettings.Dimensions} chiều.");
        }

        return embedding;
    }
}
