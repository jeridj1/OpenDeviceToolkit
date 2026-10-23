namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// A single AI backend the router can delegate diagnostic analysis to.
/// Implementations must throw <see cref="AiProviderException"/> on failure so
/// the router can fall back to the next provider in priority order.
/// </summary>
public interface IAiProvider
{
    AiProviderKind Kind { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<AiRecommendation> AnalyzeAsync(AiRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raised when an AI provider cannot complete a request (missing key,
/// transport failure, or malformed response). Treated as non-fatal by the
/// router so it can try the next provider.
/// </summary>
public sealed class AiProviderException : Exception
{
    public AiProviderException(string message) : base(message) { }
    public AiProviderException(string message, Exception inner) : base(message, inner) { }
}
