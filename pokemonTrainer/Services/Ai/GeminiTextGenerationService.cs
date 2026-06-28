using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using pokemonTrainer.Options;

namespace pokemonTrainer.Services.Ai;

public class GeminiTextGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiTextGenerationService> _logger;

    public GeminiTextGenerationService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiTextGenerationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GenerateJsonAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("Gemini is disabled or API key is missing.");
            return null;
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"models/{_options.Model}:generateContent");

        request.Headers.Add("x-goog-api-key", _options.ApiKey);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            _logger.LogWarning(
                "Gemini request failed. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                errorBody);

            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        return ExtractTextFromGeminiResponse(responseBody);
    }

    private static string? ExtractTextFromGeminiResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var firstCandidate = candidates[0];

        if (!firstCandidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            return null;
        }

        var firstPart = parts[0];

        if (!firstPart.TryGetProperty("text", out var text))
        {
            return null;
        }

        return text.GetString();
    }
}