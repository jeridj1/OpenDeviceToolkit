using OpenDeviceToolkit.Core.Research;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Routes diagnostic analysis through the first available AI provider,
/// falling back down the configured priority list on failure. The router is
/// deliberately the only component that talks to providers, so AI policy
/// (provider choice, risk envelopes, fallback) is enforced in one place.
/// Model output is always treated as untrusted and validated by
/// <see cref="AiPrompts.ParseRecommendation"/>.
/// </summary>
public sealed class AiRouter
{
    private readonly IReadOnlyList<IAiProvider> _providers;

    public AiRouter(IEnumerable<IAiProvider> providersInPriorityOrder)
    {
        _providers = providersInPriorityOrder.ToList();
    }

    public IReadOnlyList<IAiProvider> Providers => _providers;

    public bool AnyConfigured => _providers.Any(p => p.IsConfigured);

    /// <summary>
    /// Builds a router from application configuration. Cloud provider keys
    /// are pulled lazily from the key store so saving a new key takes effect
    /// without rebuilding the router.
    /// </summary>
    public static AiRouter FromConfig(AppConfig config, AiKeyStore? keyStore = null, HttpClient? httpClient = null)
    {
        var store = keyStore ?? new AiKeyStore();
        var http = httpClient;
        var providers = new List<IAiProvider>();

        foreach (var name in config.Ai.ProviderPriority)
        {
            switch (Normalize(name))
            {
                case "ollama":
                    providers.Add(new OllamaProvider(config.Ai.OllamaModel, config.Ai.OllamaUrl, http));
                    break;
                case "mistral":
                    providers.Add(new OpenAiCompatibleProvider(
                        AiProviderKind.Mistral, "Mistral (cloud)",
                        "https://api.mistral.ai/v1/chat/completions",
                        config.Ai.MistralModel,
                        () => store.GetKey(AiProviderKind.Mistral),
                        http));
                    break;
                case "openai":
                    providers.Add(new OpenAiCompatibleProvider(
                        AiProviderKind.OpenAi, "OpenAI (cloud)",
                        "https://api.openai.com/v1/chat/completions",
                        config.Ai.OpenAiModel,
                        () => store.GetKey(AiProviderKind.OpenAi),
                        http));
                    break;
                case "openrouter":
                    providers.Add(new OpenAiCompatibleProvider(
                        AiProviderKind.OpenRouter, "OpenRouter (cloud)",
                        "https://openrouter.ai/api/v1/chat/completions",
                        config.Ai.OpenRouterModel,
                        () => store.GetKey(AiProviderKind.OpenRouter),
                        http));
                    break;
            }
        }
        return new AiRouter(providers);
    }

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();

    public async Task<AiRouterResult> AnalyzeAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Objective) && string.IsNullOrWhiteSpace(request.DiagnosticContext))
        {
            return new AiRouterResult
            {
                Status = AiRouterStatus.InvalidRequest,
                Message = "Provide an objective or diagnostic evidence before asking the AI."
            };
        }

        var available = _providers.Where(p => p.IsConfigured).ToList();
        if (available.Count == 0)
        {
            return new AiRouterResult
            {
                Status = AiRouterStatus.NoProviderConfigured,
                Message = "No AI provider is configured. Add an API key or install Ollama in AI Assist settings."
            };
        }

        var errors = new List<string>();
        foreach (var provider in available)
        {
            try
            {
                var recommendation = await provider.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
                ApplyEnvelope(recommendation, request.AuthorizedEnvelope);
                return new AiRouterResult
                {
                    Status = AiRouterStatus.Ok,
                    Recommendation = recommendation,
                    Message = $"Analysis completed via {provider.DisplayName}."
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"{provider.DisplayName}: {ex.Message}");
            }
        }

        return new AiRouterResult
        {
            Status = AiRouterStatus.AllProvidersFailed,
            Message = "All configured AI providers failed. Provider details:",
            ProviderErrors = errors
        };
    }

    /// <summary>
    /// Marks every proposed action above the operator's authorized risk
    /// envelope as blocked. Blocked actions stay visible (transparency) but
    /// can never be queued for execution.
    /// </summary>
    internal static void ApplyEnvelope(AiRecommendation recommendation, RiskLevel envelope)
    {
        foreach (var action in recommendation.Actions)
        {
            if (action.Status == AiActionStatus.Malformed)
                continue;
            if (action.Risk > envelope)
            {
                action.Status = AiActionStatus.BlockedByEnvelope;
                action.StatusReason = $"Above the authorized risk envelope ({envelope}). Raise the envelope in AI Assist to enable this action.";
            }
        }
    }
}
