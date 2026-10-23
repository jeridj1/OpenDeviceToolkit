using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Provider for any OpenAI-compatible chat-completions endpoint
/// (Mistral api.mistral.ai, OpenAI api.openai.com, OpenRouter openrouter.ai).
/// The API key is supplied via an accessor delegate so keys can live in the
/// encrypted key store and never in configuration files.
/// </summary>
public sealed class OpenAiCompatibleProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly Func<string?> _apiKeyAccessor;

    public AiProviderKind Kind { get; }
    public string DisplayName { get; }
    public string Endpoint { get; }
    public string Model { get; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKeyAccessor());

    public OpenAiCompatibleProvider(AiProviderKind kind, string displayName, string endpoint, string model, Func<string?> apiKeyAccessor, HttpClient? httpClient = null)
    {
        if (kind == AiProviderKind.Ollama)
            throw new ArgumentException("Ollama is not an OpenAI-compatible endpoint; use OllamaProvider.", nameof(kind));
        Kind = kind;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Model = string.IsNullOrWhiteSpace(model)
            ? throw new ArgumentException("A model name is required.", nameof(model))
            : model;
        _apiKeyAccessor = apiKeyAccessor ?? throw new ArgumentNullException(nameof(apiKeyAccessor));
        _http = httpClient ?? new HttpClient();
    }

    public async Task<AiRecommendation> AnalyzeAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _apiKeyAccessor();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new AiProviderException($"{DisplayName} has no API key configured. Add one in AI Assist settings.");

        var payload = JsonSerializer.Serialize(new
        {
            model = Model,
            temperature = 0.2,
            max_tokens = 2048,
            messages = new object[]
            {
                new { role = "system", content = AiPrompts.BuildSystemPrompt() },
                new { role = "user", content = AiPrompts.BuildUserPrompt(request) }
            }
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new AiProviderException($"{DisplayName} returned HTTP {(int)response.StatusCode}: {Truncate(body)}");

        string? content = null;
        using (var document = JsonDocument.Parse(body))
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0
                && choices[0].ValueKind == JsonValueKind.Object
                && choices[0].TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.Object
                && message.TryGetProperty("content", out var messageContent)
                && messageContent.ValueKind == JsonValueKind.String)
            {
                content = messageContent.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new AiProviderException($"{DisplayName} returned an empty completion.");
        return AiPrompts.ParseRecommendation(content!, Kind);
    }

    private static string Truncate(string text, int max = 300)
        => text.Length <= max ? text : text[..max] + "...";
}
