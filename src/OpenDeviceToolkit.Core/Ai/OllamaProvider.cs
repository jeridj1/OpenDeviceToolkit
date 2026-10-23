using System.Text;
using System.Text.Json;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Provider for a local Ollama server (default http://localhost:11434).
/// No API key: the model runs on the operator's machine, so it works fully
/// offline and keeps diagnostic data local. Requires Ollama to be installed
/// and the configured model to be pulled. If the local server is offline the
/// request fails fast and the router falls back to the next provider.
/// </summary>
public sealed class OllamaProvider : IAiProvider
{
    public const string DefaultUrl = "http://localhost:11434";

    private readonly HttpClient _http;

    public AiProviderKind Kind => AiProviderKind.Ollama;
    public string DisplayName => $"Ollama (local, {Model})";
    public string BaseUrl { get; }
    public string Model { get; }
    public bool IsConfigured => Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);

    public OllamaProvider(string model, string baseUrl = DefaultUrl, HttpClient? httpClient = null)
    {
        BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultUrl : baseUrl.TrimEnd('/');
        Model = string.IsNullOrWhiteSpace(model) ? "llama3.2" : model;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
    }

    public async Task<AiRecommendation> AnalyzeAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = Model,
            stream = false,
            messages = new object[]
            {
                new { role = "system", content = AiPrompts.BuildSystemPrompt() },
                new { role = "user", content = AiPrompts.BuildUserPrompt(request) }
            }
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/chat");
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new AiProviderException($"Ollama returned HTTP {(int)response.StatusCode}: {Truncate(body)}");

        string? content = null;
        using (var document = JsonDocument.Parse(body))
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.Object
                && message.TryGetProperty("content", out var messageContent)
                && messageContent.ValueKind == JsonValueKind.String)
            {
                content = messageContent.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new AiProviderException("Ollama returned an empty completion.");
        return AiPrompts.ParseRecommendation(content!, Kind);
    }

    private static string Truncate(string text, int max = 300)
        => text.Length <= max ? text : text[..max] + "...";
}
